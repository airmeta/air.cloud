using System.Collections.Concurrent;

using Air.Cloud.DynamicApp.Contracts.Identifiers;
using Air.Cloud.DynamicApp.Contracts.Lifecycle;
using Air.Cloud.DynamicApp.Contracts.Runtime;

namespace Air.Cloud.DynamicApp.Manager;

/// <summary>
/// <para>zh-cn:进程内 DynamicApp 状态存储，适合 Compact Host、单机调试和持久化实现的参考基线。</para>
/// <para>en-us:In-process DynamicApp state store suitable for Compact Host, local debugging, and as a reference baseline for persistent stores.</para>
/// </summary>
public sealed class InMemoryDynamicAppStateStore : IDynamicAppStateStore
{
    private readonly ConcurrentDictionary<string, DynamicModVersionKey> _activeVersions = new();
    private readonly ConcurrentDictionary<string, DynamicAppRouteTarget> _targets = new();

    /// <summary>
    /// <para>zh-cn:登记或更新运行目标；更新已有目标时保留首次创建时间，并刷新更新时间。</para>
    /// <para>en-us:Registers or updates a runtime target; existing targets keep their created timestamp while the updated timestamp is refreshed.</para>
    /// </summary>
    /// <param name="target">
    /// <para>zh-cn:要保存的运行目标。</para>
    /// <para>en-us:The runtime target to save.</para>
    /// </param>
    public void UpsertTarget(DynamicAppRouteTarget target)
    {
        ArgumentNullException.ThrowIfNull(target);
        var now = DateTimeOffset.UtcNow;
        _targets.AddOrUpdate(
            BuildTargetKey(target),
            _ =>
            {
                var created = Clone(target);
                created.CreatedAtUtc = target.CreatedAtUtc == default ? now : target.CreatedAtUtc;
                created.UpdatedAtUtc = now;
                return created;
            },
            (_, existing) =>
            {
                var updated = Clone(target);
                updated.CreatedAtUtc = existing.CreatedAtUtc == default ? now : existing.CreatedAtUtc;
                updated.UpdatedAtUtc = now;
                updated.DrainingSinceUtc = updated.State == DynamicModLifecycleState.Draining
                    ? updated.DrainingSinceUtc ?? existing.DrainingSinceUtc ?? now
                    : null;
                return updated;
            });

        RebuildActiveVersions();
    }

    /// <summary>
    /// <para>zh-cn:激活指定版本，并将同一 DynamicApp/Mod 下仍可运行的其他版本标记为 Draining；已停止或失败目标保持原状态。</para>
    /// <para>en-us:Activates the specified version and drains other runnable versions under the same DynamicApp/Mod; already stopped or failed targets retain their state.</para>
    /// </summary>
    /// <param name="key">
    /// <para>zh-cn:要激活的目标键。</para>
    /// <para>en-us:The target key to activate.</para>
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
    /// <para>zh-cn:只激活指定部署实例，并排空同一 DynamicApp/Mod 下其他包版本。</para>
    /// <para>en-us:Activates only the specified deployment and drains other package versions under the same DynamicApp/Mod.</para>
    /// </summary>
    public DynamicAppOperationResult ActivateInstance(DynamicAppRuntimeKey key)
    {
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
        foreach (var item in _targets.Values.Where(item => item.Key.DynamicAppId == packageKey.DynamicAppId && item.Key.ModId == packageKey.ModId))
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
                next.LastStatusMessage = $"Target `{next.Key}` deployment `{next.DeploymentId}` is draining because `{packageKey}` deployment `{key.DeploymentId}` is active.";
            }

            next.UpdatedAtUtc = now;
            _targets[BuildTargetKey(next)] = next;
        }

        _activeVersions[BuildActiveKey(packageKey.DynamicAppId, packageKey.ModId)] = packageKey;
        return DynamicAppOperationResult.Success($"Target `{key}` is active.");
    }

    private DynamicAppOperationResult ActivatePackageVersion(DynamicModVersionKey packageKey)
    {
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
        foreach (var item in _targets.Values.Where(item => item.Key.DynamicAppId == packageKey.DynamicAppId && item.Key.ModId == packageKey.ModId))
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

        _activeVersions[BuildActiveKey(packageKey.DynamicAppId, packageKey.ModId)] = packageKey;
        return DynamicAppOperationResult.Success($"Target `{packageKey}` is active with {matching.Length} deployment(s).");
    }

    /// <summary>
    /// <para>zh-cn:更新目标生命周期和健康状态；用于健康探测、排空、退出监控和停止流程发布状态。</para>
    /// <para>en-us:Updates target lifecycle and health status for health probes, draining, exit monitoring, and stop flows.</para>
    /// </summary>
    /// <param name="key">
    /// <para>zh-cn:目标键。</para>
    /// <para>en-us:The target key.</para>
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

        DynamicAppOperationResult result = DynamicAppOperationResult.Success($"Target `{key}` state updated.");
        foreach (var target in matching)
        {
            result = UpdateState(new DynamicAppRuntimeKey(key, target.DeploymentId), state, healthStatus, message);
        }

        return result;
    }

    /// <summary>
    /// <para>zh-cn:只更新指定部署实例的状态，避免集群中一个实例异常时污染同包其他实例。</para>
    /// <para>en-us:Updates only the specified deployment state so one failed cluster instance does not contaminate other instances of the same package.</para>
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
        var targetKey = key.ToString();
        if (!_targets.TryGetValue(targetKey, out var target))
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
        _targets[targetKey] = next;
        RebuildActiveVersions();
        return DynamicAppOperationResult.Success(next.LastStatusMessage);
    }

    /// <summary>
    /// <para>zh-cn:删除一个运行目标，并在必要时同步清理活动版本索引。</para>
    /// <para>en-us:Removes one runtime target and cleans the active-version index when needed.</para>
    /// </summary>
    /// <param name="key">
    /// <para>zh-cn:目标键。</para>
    /// <para>en-us:The target key.</para>
    /// </param>
    /// <returns>
    /// <para>zh-cn:删除结果。</para>
    /// <para>en-us:The removal result.</para>
    /// </returns>
    public DynamicAppOperationResult RemoveTarget(DynamicModVersionKey key)
    {
        var matching = _targets.Values.Where(item => item.Key == key).ToArray();
        if (matching.Length == 0)
        {
            return DynamicAppOperationResult.Failure($"Target `{key}` is not registered.");
        }

        foreach (var target in matching)
        {
            _targets.TryRemove(BuildTargetKey(target), out _);
        }

        RebuildActiveVersions();
        return DynamicAppOperationResult.Success($"Target `{key}` removed.");
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
        if (!_targets.TryRemove(key.ToString(), out _))
        {
            return DynamicAppOperationResult.Failure($"Target `{key}` is not registered.");
        }

        RebuildActiveVersions();
        return DynamicAppOperationResult.Success($"Target `{key}` removed.");
    }

    /// <summary>
    /// <para>zh-cn:获取活动目标；只有 Active 且 Healthy 的目标会返回给 Gateway。</para>
    /// <para>en-us:Gets the active target; only Active and Healthy targets are returned to Gateway.</para>
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
        target = ListActiveTargets(dynamicAppId, modId).FirstOrDefault();
        return target != null;
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
    /// <para>zh-cn:列出所有目标的快照副本，调用方修改返回值不会改变存储状态。</para>
    /// <para>en-us:Lists snapshot copies of all targets; changing returned values does not mutate store state.</para>
    /// </summary>
    /// <returns>
    /// <para>zh-cn:目标快照集合。</para>
    /// <para>en-us:A collection of target snapshots.</para>
    /// </returns>
    public IReadOnlyCollection<DynamicAppRouteTarget> ListTargets() => _targets.Values.Select(Clone).ToList();

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
}
