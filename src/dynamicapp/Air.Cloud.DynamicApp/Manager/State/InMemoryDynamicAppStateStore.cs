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
            target.Key.ToString(),
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
    /// <para>zh-cn:激活指定版本，并将同一 DynamicApp/Mod 下的其他版本标记为 Draining。</para>
    /// <para>en-us:Activates the specified version and marks other versions under the same DynamicApp/Mod as Draining.</para>
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
    {
        if (!_targets.ContainsKey(key.ToString()))
        {
            return DynamicAppOperationResult.Failure($"Target `{key}` is not registered.");
        }

        var now = DateTimeOffset.UtcNow;
        foreach (var item in _targets.Values.Where(item => item.Key.DynamicAppId == key.DynamicAppId && item.Key.ModId == key.ModId))
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

        _activeVersions[BuildActiveKey(key.DynamicAppId, key.ModId)] = key;
        return DynamicAppOperationResult.Success($"Target `{key}` is active.");
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
}
