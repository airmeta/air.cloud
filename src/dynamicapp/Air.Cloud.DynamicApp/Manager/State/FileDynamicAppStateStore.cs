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
            var now = DateTimeOffset.UtcNow;
            if (_targets.TryGetValue(target.Key.ToString(), out var existing))
            {
                var updated = Clone(target);
                updated.CreatedAtUtc = existing.CreatedAtUtc == default ? now : existing.CreatedAtUtc;
                updated.UpdatedAtUtc = now;
                updated.DrainingSinceUtc = updated.State == DynamicModLifecycleState.Draining
                    ? updated.DrainingSinceUtc ?? existing.DrainingSinceUtc ?? now
                    : null;
                _targets[target.Key.ToString()] = updated;
            }
            else
            {
                var created = Clone(target);
                created.CreatedAtUtc = created.CreatedAtUtc == default ? now : created.CreatedAtUtc;
                created.UpdatedAtUtc = now;
                _targets[target.Key.ToString()] = created;
            }

            RebuildActiveVersions();
            SaveIfNeeded();
        }
    }

    /// <summary>
    /// <para>zh-cn:激活指定版本，将同一 DynamicApp/Mod 的其他版本标记为 Draining，并持久化状态。</para>
    /// <para>en-us:Activates the specified version, marks other versions for the same DynamicApp/Mod as Draining, and persists state.</para>
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
    {
        lock (_syncRoot)
        {
            if (!_targets.ContainsKey(key.ToString()))
            {
                return DynamicAppOperationResult.Failure($"Target `{key}` is not registered.");
            }

            var now = DateTimeOffset.UtcNow;
            foreach (var item in _targets.Values.Where(item => item.Key.DynamicAppId == key.DynamicAppId && item.Key.ModId == key.ModId).ToArray())
            {
                var next = Clone(item);
                if (next.Key == key)
                {
                    next.State = DynamicModLifecycleState.Active;
                    next.HealthStatus = DynamicModHealthStatus.Healthy;
                    next.DrainingSinceUtc = null;
                    next.LastStatusMessage = $"Target `{key}` is active.";
                }
                else
                {
                    next.State = DynamicModLifecycleState.Draining;
                    next.HealthStatus = DynamicModHealthStatus.Draining;
                    next.DrainingSinceUtc ??= now;
                    next.LastStatusMessage = $"Target `{next.Key}` is draining because `{key}` is active.";
                }

                next.UpdatedAtUtc = now;
                _targets[next.Key.ToString()] = next;
            }

            RebuildActiveVersions();
            SaveIfNeeded();
            return DynamicAppOperationResult.Success($"Target `{key}` is active.");
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
            if (!_targets.TryGetValue(key.ToString(), out var target))
            {
                return DynamicAppOperationResult.Failure($"Target `{key}` is not registered.");
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
            target = null;
            if (!_activeVersions.TryGetValue(BuildActiveKey(dynamicAppId, modId), out var key))
            {
                return false;
            }

            if (!_targets.TryGetValue(key.ToString(), out var stored)
                || stored.State != DynamicModLifecycleState.Active
                || stored.HealthStatus != DynamicModHealthStatus.Healthy)
            {
                return false;
            }

            target = Clone(stored);
            return true;
        }
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
            return _targets.Values.Select(Clone).ToList();
        }
    }

    private void Load()
    {
        if (!File.Exists(_filePath))
        {
            return;
        }

        var snapshot = JsonSerializer.Deserialize<StateSnapshot>(File.ReadAllText(_filePath), JsonOptions);
        if (snapshot?.Targets == null)
        {
            return;
        }

        foreach (var target in snapshot.Targets)
        {
            _targets[target.Key.ToString()] = Clone(target);
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
        File.WriteAllText(_filePath, JsonSerializer.Serialize(snapshot, JsonOptions));
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
        BaseAddress = target.BaseAddress,
        RoutePrefix = target.RoutePrefix,
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
