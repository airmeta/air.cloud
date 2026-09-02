using System.Text.Json;
using System.Text.Json.Serialization;

using Air.Cloud.DynamicApp.Contracts.Identifiers;
using Air.Cloud.DynamicApp.Contracts.Lifecycle;
using Air.Cloud.DynamicApp.Contracts.Runtime;

namespace Air.Cloud.DynamicApp.Manager;

/// <summary>
/// <para>zh-cn:文件型 DynamicApp 状态存储，适合单机 Compact Host 或开发环境的可恢复运行状态。</para>
/// <para>en-us:File-based DynamicApp state store for recoverable runtime state in single-node Compact Host or development environments.</para>
/// </summary>
public sealed class FileDynamicAppStateStore : IDynamicAppStateStore
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
        Converters =
        {
            new JsonStringEnumConverter()
        }
    };

    private readonly Dictionary<string, DynamicModVersionKey> _activeVersions = new(StringComparer.OrdinalIgnoreCase);
    private readonly string _filePath;
    private readonly object _syncRoot = new();
    private readonly Dictionary<string, DynamicAppRouteTarget> _targets = new(StringComparer.OrdinalIgnoreCase);
    private readonly FileDynamicAppStateStoreOptions _options;

    /// <summary>
    /// <para>zh-cn:创建文件型状态存储，并从状态文件恢复已有目标。</para>
    /// <para>en-us:Creates the file-based state store and restores existing targets from the state file.</para>
    /// </summary>
    /// <param name="options">
    /// <para>zh-cn:文件型状态存储选项。</para>
    /// <para>en-us:The file-based state store options.</para>
    /// </param>
    public FileDynamicAppStateStore(FileDynamicAppStateStoreOptions options)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _filePath = ResolveFilePath(options.FilePath, "dynamicapp-state.json");
        Load();
    }

    /// <summary>
    /// <para>zh-cn:新增或更新运行目标，并按配置保存到状态文件。</para>
    /// <para>en-us:Adds or updates a runtime target and saves it to the state file according to options.</para>
    /// </summary>
    /// <param name="target">
    /// <para>zh-cn:运行目标。</para>
    /// <para>en-us:The runtime target.</para>
    /// </param>
    public void UpsertTarget(DynamicAppRouteTarget target)
    {
        ArgumentNullException.ThrowIfNull(target);
        lock (_syncRoot)
        {
            using var fileLock = _options.SaveOnChange ? DynamicAppFileSnapshotLock.Acquire(_filePath) : null;
            if (_options.SaveOnChange) Load();
            var now = DateTimeOffset.UtcNow;
            var targetKey = BuildTargetKey(target);
            if (_targets.TryGetValue(targetKey, out var existing))
            {
                var updated = Clone(target);
                updated.CreatedAtUtc = existing.CreatedAtUtc == default ? now : existing.CreatedAtUtc;
                updated.UpdatedAtUtc = now;
                updated.DrainingSinceUtc = updated.State == DynamicModLifecycleState.Draining
                    ? updated.DrainingSinceUtc ?? existing.DrainingSinceUtc ?? now
                    : null;
                _targets[targetKey] = updated;
            }
            else
            {
                var created = Clone(target);
                created.CreatedAtUtc = created.CreatedAtUtc == default ? now : created.CreatedAtUtc;
                created.UpdatedAtUtc = now;
                _targets[targetKey] = created;
            }

            RebuildActiveVersions();
            SaveIfNeeded();
        }
    }

    /// <summary>
    /// <para>zh-cn:激活指定版本，将同一 DynamicApp/Mod 下仍可运行的其他版本标记为 Draining；已停止或失败目标保持原状态。</para>
    /// <para>en-us:Activates the specified version and drains other runnable versions for the same DynamicApp/Mod; already stopped or failed targets retain their state.</para>
    /// </summary>
    /// <param name="key">
    /// <para>zh-cn:目标版本键。</para>
    /// <para>en-us:Target version key.</para>
    /// </param>
    /// <returns>
    /// <para>zh-cn:激活结果。</para>
    /// <para>en-us:The activation result.</para>
    /// </returns>
    public DynamicAppOperationResult Activate(DynamicModVersionKey key)
        => Activate(DynamicAppRuntimeKey.Default(key));

    /// <summary>
    /// <para>zh-cn:激活指定包版本的全部部署实例，并排空其他版本；同一版本的实例保持并行运行。</para>
    /// <para>en-us:Activates all deployments of the specified package version and drains other versions while keeping same-version deployments running in parallel.</para>
    /// </summary>
    /// <param name="key">
    /// <para>zh-cn:运行实例键；激活按其中的包版本部分执行。</para>
    /// <para>en-us:The runtime key; activation uses its package-version portion.</para>
    /// </param>
    /// <returns>
    /// <para>zh-cn:激活结果。</para>
    /// <para>en-us:The activation result.</para>
    /// </returns>
    public DynamicAppOperationResult Activate(DynamicAppRuntimeKey key)
        => ActivatePackageVersion(key.PackageKey);

    /// <summary>
    /// <para>zh-cn:只激活指定部署实例，并排空其他包版本；同版本其他实例保持各自状态。</para>
    /// <para>en-us:Activates only the specified deployment and drains other package versions; other deployments of the same version retain their own state.</para>
    /// </summary>
    public DynamicAppOperationResult ActivateInstance(DynamicAppRuntimeKey key)
    {
        lock (_syncRoot)
        {
            using var fileLock = _options.SaveOnChange ? DynamicAppFileSnapshotLock.Acquire(_filePath) : null;
            if (_options.SaveOnChange) Load();
            var packageKey = key.PackageKey;
            var runtimeKey = key.ToString();
            if (!_targets.ContainsKey(runtimeKey))
            {
                return DynamicAppOperationResult.Failure($"Target `{key}` is not registered.");
            }

            foreach (var item in _targets.Values.Where(item => item.Key.DynamicAppId == packageKey.DynamicAppId && item.Key.ModId == packageKey.ModId))
            {
                var isSelected = BuildTargetKey(item) == runtimeKey;
                var nextState = isSelected ? DynamicModLifecycleState.Active : item.State is DynamicModLifecycleState.Pending or DynamicModLifecycleState.Stopped or DynamicModLifecycleState.Failed
                    ? item.State
                    : DynamicModLifecycleState.Draining;
                var nextHealth = isSelected ? DynamicModHealthStatus.Healthy : nextState == DynamicModLifecycleState.Draining
                    ? DynamicModHealthStatus.Draining
                    : item.HealthStatus;
                if (!DynamicAppLifecycleStateMachine.TryValidate(item.State, nextState, nextHealth, out var validationError))
                {
                    return DynamicAppOperationResult.Failure($"Target `{new DynamicAppRuntimeKey(item.Key, item.DeploymentId)}` activation was rejected. {validationError}");
                }
            }

            var now = DateTimeOffset.UtcNow;
            foreach (var item in _targets.Values.Where(item => item.Key.DynamicAppId == packageKey.DynamicAppId && item.Key.ModId == packageKey.ModId).ToArray())
            {
                var next = Clone(item);
                if (BuildTargetKey(next) == runtimeKey)
                {
                    next.State = DynamicModLifecycleState.Active;
                    next.HealthStatus = DynamicModHealthStatus.Healthy;
                    next.DrainingSinceUtc = null;
                    next.LastStatusMessage = $"Target `{packageKey}` deployment `{next.DeploymentId}` is active.";
                }
                else if (next.Key == packageKey || next.State is DynamicModLifecycleState.Pending or DynamicModLifecycleState.Stopped or DynamicModLifecycleState.Failed)
                {
                    continue;
                }
                else
                {
                    next.State = DynamicModLifecycleState.Draining;
                    next.HealthStatus = DynamicModHealthStatus.Draining;
                    next.DrainingSinceUtc ??= now;
                    next.LastStatusMessage = $"Target `{next.Key}` deployment `{next.DeploymentId}` is draining because `{key.DeploymentId}` is active.";
                }

                next.UpdatedAtUtc = now;
                _targets[BuildTargetKey(next)] = next;
            }

            RebuildActiveVersions();
            SaveIfNeeded();
            return DynamicAppOperationResult.Success($"Target `{key}` is active.");
        }
    }

    private DynamicAppOperationResult ActivatePackageVersion(DynamicModVersionKey packageKey)
    {
        lock (_syncRoot)
        {
            using var fileLock = _options.SaveOnChange ? DynamicAppFileSnapshotLock.Acquire(_filePath) : null;
            if (_options.SaveOnChange) Load();
            var matching = _targets.Values.Where(item => item.Key == packageKey).ToArray();
            if (matching.Length == 0)
            {
                return DynamicAppOperationResult.Failure($"Target `{packageKey}` is not registered.");
            }

            foreach (var item in _targets.Values.Where(item => item.Key.DynamicAppId == packageKey.DynamicAppId && item.Key.ModId == packageKey.ModId))
            {
                var nextState = item.Key == packageKey ? DynamicModLifecycleState.Active : item.State is DynamicModLifecycleState.Pending or DynamicModLifecycleState.Stopped or DynamicModLifecycleState.Failed
                    ? item.State
                    : DynamicModLifecycleState.Draining;
                var nextHealth = item.Key == packageKey ? DynamicModHealthStatus.Healthy : nextState == DynamicModLifecycleState.Draining
                    ? DynamicModHealthStatus.Draining
                    : item.HealthStatus;
                if (!DynamicAppLifecycleStateMachine.TryValidate(item.State, nextState, nextHealth, out var validationError))
                {
                    return DynamicAppOperationResult.Failure($"Target `{new DynamicAppRuntimeKey(item.Key, item.DeploymentId)}` activation was rejected. {validationError}");
                }
            }

            var now = DateTimeOffset.UtcNow;
            foreach (var item in _targets.Values.Where(item => item.Key.DynamicAppId == packageKey.DynamicAppId && item.Key.ModId == packageKey.ModId).ToArray())
            {
                var next = Clone(item);
                if (next.Key == packageKey)
                {
                    next.State = DynamicModLifecycleState.Active;
                    next.HealthStatus = DynamicModHealthStatus.Healthy;
                    next.DrainingSinceUtc = null;
                    next.LastStatusMessage = $"Target `{packageKey}` deployment `{next.DeploymentId}` is active.";
                }
                else if (next.State is DynamicModLifecycleState.Pending or DynamicModLifecycleState.Stopped or DynamicModLifecycleState.Failed)
                {
                    continue;
                }
                else
                {
                    next.State = DynamicModLifecycleState.Draining;
                    next.HealthStatus = DynamicModHealthStatus.Draining;
                    next.DrainingSinceUtc ??= now;
                    next.LastStatusMessage = $"Target `{next.Key}` deployment `{next.DeploymentId}` is draining because `{packageKey}` is active.";
                }

                next.UpdatedAtUtc = now;
                _targets[BuildTargetKey(next)] = next;
            }

            RebuildActiveVersions();
            SaveIfNeeded();
            return DynamicAppOperationResult.Success($"Target `{packageKey}` is active with {matching.Length} deployment(s).");
        }
    }

    /// <summary>
    /// <para>zh-cn:更新目标生命周期和健康状态，并按配置持久化。</para>
    /// <para>en-us:Updates target lifecycle and health state and persists it according to options.</para>
    /// </summary>
    /// <param name="key">
    /// <para>zh-cn:目标版本键。</para>
    /// <para>en-us:Target version key.</para>
    /// </param>
    /// <param name="state">
    /// <para>zh-cn:生命周期状态。</para>
    /// <para>en-us:Lifecycle state.</para>
    /// </param>
    /// <param name="healthStatus">
    /// <para>zh-cn:健康状态。</para>
    /// <para>en-us:Health status.</para>
    /// </param>
    /// <param name="message">
    /// <para>zh-cn:可选状态消息。</para>
    /// <para>en-us:Optional status message.</para>
    /// </param>
    /// <returns>
    /// <para>zh-cn:更新结果。</para>
    /// <para>en-us:The update result.</para>
    /// </returns>
    public DynamicAppOperationResult UpdateState(DynamicModVersionKey key, DynamicModLifecycleState state, DynamicModHealthStatus healthStatus, string? message = null)
    {
        lock (_syncRoot)
        {
            using var fileLock = _options.SaveOnChange ? DynamicAppFileSnapshotLock.Acquire(_filePath) : null;
            if (_options.SaveOnChange) Load();
            var matching = _targets.Values.Where(item => item.Key == key).ToArray();
            if (matching.Length == 0)
            {
                return DynamicAppOperationResult.Failure($"Target `{key}` is not registered.");
            }

            foreach (var target in matching)
            {
                if (!DynamicAppLifecycleStateMachine.TryValidate(target.State, state, healthStatus, out var validationError))
                {
                    return DynamicAppOperationResult.Failure($"Target `{key}` state update was rejected. {validationError}");
                }
            }

            foreach (var target in matching)
            {
                var runtimeKey = new DynamicAppRuntimeKey(key, target.DeploymentId);
                var next = Clone(target);
                next.State = state;
                next.HealthStatus = healthStatus;
                next.UpdatedAtUtc = DateTimeOffset.UtcNow;
                next.DrainingSinceUtc = state == DynamicModLifecycleState.Draining ? next.DrainingSinceUtc ?? next.UpdatedAtUtc : null;
                next.LastStatusMessage = string.IsNullOrWhiteSpace(message) ? $"Target `{runtimeKey}` state updated to `{state}`." : message;
                _targets[BuildTargetKey(next)] = next;
            }

            RebuildActiveVersions();
            SaveIfNeeded();
            return DynamicAppOperationResult.Success($"Target `{key}` state updated.");
        }
    }

    /// <summary>
    /// <para>zh-cn:只更新指定部署实例的生命周期和健康状态，避免集群中一个实例异常时污染同包其他实例。</para>
    /// <para>en-us:Updates lifecycle and health state for only the specified deployment so one failed cluster instance does not contaminate other instances of the same package.</para>
    /// </summary>
    /// <param name="key">
    /// <para>zh-cn:运行实例键。</para>
    /// <para>en-us:The runtime-instance key.</para>
    /// </param>
    /// <param name="state">
    /// <para>zh-cn:生命周期状态。</para>
    /// <para>en-us:The lifecycle state.</para>
    /// </param>
    /// <param name="healthStatus">
    /// <para>zh-cn:健康状态。</para>
    /// <para>en-us:The health status.</para>
    /// </param>
    /// <param name="message">
    /// <para>zh-cn:可选状态消息。</para>
    /// <para>en-us:Optional status message.</para>
    /// </param>
    /// <returns>
    /// <para>zh-cn:更新结果。</para>
    /// <para>en-us:The update result.</para>
    /// </returns>
    public DynamicAppOperationResult UpdateState(DynamicAppRuntimeKey key, DynamicModLifecycleState state, DynamicModHealthStatus healthStatus, string? message = null)
    {
        lock (_syncRoot)
        {
            using var fileLock = _options.SaveOnChange ? DynamicAppFileSnapshotLock.Acquire(_filePath) : null;
            if (_options.SaveOnChange) Load();
            if (!_targets.TryGetValue(key.ToString(), out var target))
            {
                return DynamicAppOperationResult.Failure($"Target `{key}` is not registered.");
            }

            if (!DynamicAppLifecycleStateMachine.TryValidate(target.State, state, healthStatus, out var validationError))
            {
                return DynamicAppOperationResult.Failure($"Target `{key}` state update was rejected. {validationError}");
            }

            var now = DateTimeOffset.UtcNow;
            var next = Clone(target);
            next.State = state;
            next.HealthStatus = healthStatus;
            next.UpdatedAtUtc = now;
            next.DrainingSinceUtc = state == DynamicModLifecycleState.Draining ? next.DrainingSinceUtc ?? now : null;
            next.LastStatusMessage = string.IsNullOrWhiteSpace(message) ? $"Target `{key}` state updated to `{state}`." : message;
            _targets[key.ToString()] = next;
            RebuildActiveVersions();
            SaveIfNeeded();
            return DynamicAppOperationResult.Success(next.LastStatusMessage);
        }
    }

    /// <summary>
    /// <para>zh-cn:删除一个运行目标，并将变更持久化到状态文件。</para>
    /// <para>en-us:Removes one runtime target and persists the change to the state file.</para>
    /// </summary>
    /// <param name="key">
    /// <para>zh-cn:目标版本键。</para>
    /// <para>en-us:Target version key.</para>
    /// </param>
    /// <returns>
    /// <para>zh-cn:删除结果。</para>
    /// <para>en-us:The removal result.</para>
    /// </returns>
    public DynamicAppOperationResult RemoveTarget(DynamicModVersionKey key)
    {
        lock (_syncRoot)
        {
            using var fileLock = _options.SaveOnChange ? DynamicAppFileSnapshotLock.Acquire(_filePath) : null;
            if (_options.SaveOnChange) Load();
            var matching = _targets.Values.Where(item => item.Key == key).ToArray();
            if (matching.Length == 0)
            {
                return DynamicAppOperationResult.Failure($"Target `{key}` is not registered.");
            }

            foreach (var target in matching)
            {
                _targets.Remove(BuildTargetKey(target));
            }

            RebuildActiveVersions();
            SaveIfNeeded();
            return DynamicAppOperationResult.Success($"Target `{key}` removed.");
        }
    }

    /// <summary>
    /// <para>zh-cn:删除指定部署实例而不影响其他部署。</para>
    /// <para>en-us:Removes the specified deployment without affecting other deployments.</para>
    /// </summary>
    /// <param name="key">
    /// <para>zh-cn:运行实例键。</para>
    /// <para>en-us:The runtime-instance key.</para>
    /// </param>
    /// <returns>
    /// <para>zh-cn:删除结果。</para>
    /// <para>en-us:The removal result.</para>
    /// </returns>
    public DynamicAppOperationResult RemoveTarget(DynamicAppRuntimeKey key)
    {
        lock (_syncRoot)
        {
            using var fileLock = _options.SaveOnChange ? DynamicAppFileSnapshotLock.Acquire(_filePath) : null;
            if (_options.SaveOnChange) Load();
            if (!_targets.Remove(key.ToString()))
            {
                return DynamicAppOperationResult.Failure($"Target `{key}` is not registered.");
            }

            RebuildActiveVersions();
            SaveIfNeeded();
            return DynamicAppOperationResult.Success($"Target `{key}` removed.");
        }
    }

    /// <summary>
    /// <para>zh-cn:尝试读取 Active 且 Healthy 的当前目标。</para>
    /// <para>en-us:Attempts to read the current Active and Healthy target.</para>
    /// </summary>
    /// <param name="dynamicAppId">
    /// <para>zh-cn:DynamicApp 标识。</para>
    /// <para>en-us:DynamicApp identifier.</para>
    /// </param>
    /// <param name="modId">
    /// <para>zh-cn:Mod 标识。</para>
    /// <para>en-us:Mod identifier.</para>
    /// </param>
    /// <param name="target">
    /// <para>zh-cn:活动目标副本。</para>
    /// <para>en-us:A copy of the active target.</para>
    /// </param>
    /// <returns>
    /// <para>zh-cn:是否存在可转发目标。</para>
    /// <para>en-us:Whether a forwardable target exists.</para>
    /// </returns>
    public bool TryGetActiveTarget(DynamicAppId dynamicAppId, DynamicModId modId, out DynamicAppRouteTarget? target)
    {
        lock (_syncRoot)
        {
            using var fileLock = _options.SaveOnChange ? DynamicAppFileSnapshotLock.Acquire(_filePath) : null;
            if (_options.SaveOnChange) Load();
            target = FindActiveTargets(dynamicAppId, modId).FirstOrDefault();
            return target != null;
        }
    }

    /// <summary>
    /// <para>zh-cn:列出当前版本全部可转发部署实例。</para>
    /// <para>en-us:Lists all forwardable deployments of the current active package version.</para>
    /// </summary>
    /// <param name="dynamicAppId">
    /// <para>zh-cn:DynamicApp 标识。</para>
    /// <para>en-us:DynamicApp identifier.</para>
    /// </param>
    /// <param name="modId">
    /// <para>zh-cn:Mod 标识。</para>
    /// <para>en-us:Mod identifier.</para>
    /// </param>
    /// <returns>
    /// <para>zh-cn:Active 且 Healthy 的部署实例。</para>
    /// <para>en-us:Active and Healthy deployment instances.</para>
    /// </returns>
    public IReadOnlyCollection<DynamicAppRouteTarget> ListActiveTargets(DynamicAppId dynamicAppId, DynamicModId modId)
    {
        lock (_syncRoot)
        {
            using var fileLock = _options.SaveOnChange ? DynamicAppFileSnapshotLock.Acquire(_filePath) : null;
            if (_options.SaveOnChange) Load();
            return FindActiveTargets(dynamicAppId, modId);
        }
    }

    private IReadOnlyCollection<DynamicAppRouteTarget> FindActiveTargets(DynamicAppId dynamicAppId, DynamicModId modId)
    {
        if (!_activeVersions.TryGetValue(BuildActiveKey(dynamicAppId, modId), out var key))
        {
            return Array.Empty<DynamicAppRouteTarget>();
        }

        return _targets.Values
            .Where(item => item.Key == key && item.State == DynamicModLifecycleState.Active && item.HealthStatus == DynamicModHealthStatus.Healthy)
            .Select(Clone)
            .ToArray();
    }

    /// <summary>
    /// <para>zh-cn:列出所有目标快照。</para>
    /// <para>en-us:Lists snapshots of all targets.</para>
    /// </summary>
    /// <returns>
    /// <para>zh-cn:目标快照集合。</para>
    /// <para>en-us:The target snapshot collection.</para>
    /// </returns>
    public IReadOnlyCollection<DynamicAppRouteTarget> ListTargets()
    {
        lock (_syncRoot)
        {
            using var fileLock = _options.SaveOnChange ? DynamicAppFileSnapshotLock.Acquire(_filePath) : null;
            if (_options.SaveOnChange) Load();
            return _targets.Values.Select(Clone).ToList();
        }
    }

    private void Load()
    {
        _targets.Clear();
        _activeVersions.Clear();
        if (!File.Exists(_filePath))
        {
            return;
        }

        StateSnapshot? snapshot;
        try
        {
            snapshot = JsonSerializer.Deserialize<StateSnapshot>(File.ReadAllText(_filePath), JsonOptions);
        }
        catch (JsonException)
        {
            // 损坏的状态文件不应阻止 Host 启动；后续状态写入会生成新的完整快照。
            // A corrupt state file must not prevent Host startup; the next state write creates a new complete snapshot.
            return;
        }
        catch (IOException)
        {
            return;
        }

        if (snapshot?.Targets == null)
        {
            return;
        }

        foreach (var target in snapshot.Targets)
        {
            _targets[BuildTargetKey(target)] = Clone(target);
        }

        RebuildActiveVersions();
    }

    private void SaveIfNeeded()
    {
        if (!_options.SaveOnChange)
        {
            return;
        }

        var directory = Path.GetDirectoryName(_filePath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var snapshot = new StateSnapshot
        {
            Targets = _targets.Values.Select(Clone).ToList()
        };
        DynamicAppAtomicJsonFile.Write(_filePath, JsonSerializer.Serialize(snapshot, JsonOptions));
    }

    private void RebuildActiveVersions()
    {
        _activeVersions.Clear();
        foreach (var target in _targets.Values.Where(target => target.State == DynamicModLifecycleState.Active && target.HealthStatus == DynamicModHealthStatus.Healthy))
        {
            _activeVersions[BuildActiveKey(target.Key.DynamicAppId, target.Key.ModId)] = target.Key;
        }
    }

    private static string BuildActiveKey(DynamicAppId dynamicAppId, DynamicModId modId) => $"{dynamicAppId.Value}/{modId.Value}";

    private static string BuildTargetKey(DynamicAppRouteTarget target)
        => new DynamicAppRuntimeKey(target.Key, NormalizeDeploymentId(target.DeploymentId)).ToString();

    private static string NormalizeDeploymentId(string? deploymentId)
        => string.IsNullOrWhiteSpace(deploymentId) ? "default" : deploymentId.Trim();

    private static string ResolveFilePath(string? filePath, string defaultFileName)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            return Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, defaultFileName));
        }

        return Path.IsPathFullyQualified(filePath)
            ? Path.GetFullPath(filePath)
            : Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, filePath));
    }

    private static DynamicAppRouteTarget Clone(DynamicAppRouteTarget target) => new()
    {
        Key = target.Key,
        DeploymentId = NormalizeDeploymentId(target.DeploymentId),
        DeploymentDirectory = target.DeploymentDirectory,
        DeploymentEntryFilePath = target.DeploymentEntryFilePath,
        BaseAddress = target.BaseAddress,
        HealthPath = target.HealthPath,
        HealthProbeTimeoutSeconds = target.HealthProbeTimeoutSeconds,
        State = target.State,
        HealthStatus = target.HealthStatus,
        InstanceId = target.InstanceId,
        LastStatusMessage = target.LastStatusMessage,
        CreatedAtUtc = target.CreatedAtUtc,
        UpdatedAtUtc = target.UpdatedAtUtc,
        DrainingSinceUtc = target.DrainingSinceUtc
    };

    private sealed class StateSnapshot
    {
        public List<DynamicAppRouteTarget> Targets { get; set; } = new();
    }
}
