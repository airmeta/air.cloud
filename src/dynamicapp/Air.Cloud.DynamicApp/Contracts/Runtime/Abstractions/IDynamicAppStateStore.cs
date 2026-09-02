using Air.Cloud.DynamicApp.Contracts.Identifiers;
using Air.Cloud.DynamicApp.Contracts.Lifecycle;

namespace Air.Cloud.DynamicApp.Contracts.Runtime;

/// <summary>
/// <para>zh-cn:DynamicApp 运行状态存储接口，提供目标登记、激活、状态更新、删除和查询能力。</para>
/// <para>en-us:DynamicApp runtime state store interface providing target registration, activation, state updates, removal, and query capabilities.</para>
/// </summary>
public interface IDynamicAppStateStore
{
    /// <summary>
    /// <para>zh-cn:新增或更新一个运行目标；实现方应维护创建时间和更新时间。</para>
    /// <para>en-us:Adds or updates a runtime target; implementations should maintain created and updated timestamps.</para>
    /// </summary>
    /// <param name="target">
    /// <para>zh-cn:运行目标。</para>
    /// <para>en-us:The runtime target.</para>
    /// </param>
    void UpsertTarget(DynamicAppRouteTarget target);

    /// <summary>
    /// <para>zh-cn:激活指定版本，并将同一 DynamicApp/Mod 的其他版本排空。</para>
    /// <para>en-us:Activates the specified version and drains other versions for the same DynamicApp/Mod.</para>
    /// </summary>
    /// <param name="key">
    /// <para>zh-cn:目标版本键。</para>
    /// <para>en-us:Target version key.</para>
    /// </param>
    /// <returns>
    /// <para>zh-cn:激活结果。</para>
    /// <para>en-us:The activation result.</para>
    /// </returns>
    DynamicAppOperationResult Activate(DynamicModVersionKey key);

    /// <summary>
    /// <para>zh-cn:激活同一包版本下的全部部署实例，并排空其他版本；同版本实例不会互相排空。</para>
    /// <para>en-us:Activates all deployments of one package version and drains other versions; deployments of the same version do not drain one another.</para>
    /// </summary>
    /// <param name="key">
    /// <para>zh-cn:运行实例键。</para>
    /// <para>en-us:The runtime-instance key.</para>
    /// </param>
    /// <returns>
    /// <para>zh-cn:激活结果。</para>
    /// <para>en-us:The activation result.</para>
    /// </returns>
    DynamicAppOperationResult Activate(DynamicAppRuntimeKey key)
        => Activate(key.PackageKey);

    /// <summary>
    /// <para>zh-cn:只激活指定部署实例，并排空其他包版本；同版本其他实例保持各自状态。</para>
    /// <para>en-us:Activates only the specified deployment and drains other package versions; other deployments of the same version retain their own state.</para>
    /// </summary>
    /// <param name="key">
    /// <para>zh-cn:运行实例键。</para>
    /// <para>en-us:The runtime-instance key.</para>
    /// </param>
    /// <returns>
    /// <para>zh-cn:实例激活结果。</para>
    /// <para>en-us:The instance activation result.</para>
    /// </returns>
    DynamicAppOperationResult ActivateInstance(DynamicAppRuntimeKey key)
        => Activate(key);

    /// <summary>
    /// <para>zh-cn:更新目标生命周期和健康状态，并记录可读状态消息；非法状态转换或矛盾健康组合会返回失败且不改变快照。</para>
    /// <para>en-us:Updates target lifecycle and health state with a readable status message; illegal transitions or contradictory health combinations return failure without changing the snapshot.</para>
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
    /// <para>zh-cn:可选状态消息；未指定时由实现方生成默认消息。</para>
    /// <para>en-us:Optional status message; implementations generate a default message when omitted.</para>
    /// </param>
    /// <returns>
    /// <para>zh-cn:更新结果。</para>
    /// <para>en-us:The update result.</para>
    /// </returns>
    DynamicAppOperationResult UpdateState(DynamicModVersionKey key, DynamicModLifecycleState state, DynamicModHealthStatus healthStatus, string? message = null);

    /// <summary>
    /// <para>zh-cn:只更新指定部署实例的生命周期和健康状态；非法状态转换或矛盾健康组合会返回失败且不改变快照。</para>
    /// <para>en-us:Updates lifecycle and health state for only the specified deployment; illegal transitions or contradictory health combinations return failure without changing the snapshot.</para>
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
    DynamicAppOperationResult UpdateState(DynamicAppRuntimeKey key, DynamicModLifecycleState state, DynamicModHealthStatus healthStatus, string? message = null)
        => UpdateState(key.PackageKey, state, healthStatus, message);

    /// <summary>
    /// <para>zh-cn:删除一个运行目标；常用于排空结束后的清理。</para>
    /// <para>en-us:Removes a runtime target, commonly used after draining cleanup completes.</para>
    /// </summary>
    /// <param name="key">
    /// <para>zh-cn:目标版本键。</para>
    /// <para>en-us:Target version key.</para>
    /// </param>
    /// <returns>
    /// <para>zh-cn:删除结果。</para>
    /// <para>en-us:The removal result.</para>
    /// </returns>
    DynamicAppOperationResult RemoveTarget(DynamicModVersionKey key);

    /// <summary>
    /// <para>zh-cn:删除指定部署实例而不影响同一包版本的其他实例。</para>
    /// <para>en-us:Removes the specified deployment without affecting other deployments of the same package version.</para>
    /// </summary>
    /// <param name="key">
    /// <para>zh-cn:运行实例键。</para>
    /// <para>en-us:The runtime-instance key.</para>
    /// </param>
    /// <returns>
    /// <para>zh-cn:删除结果。</para>
    /// <para>en-us:The removal result.</para>
    /// </returns>
    DynamicAppOperationResult RemoveTarget(DynamicAppRuntimeKey key)
        => RemoveTarget(key.PackageKey);

    /// <summary>
    /// <para>zh-cn:尝试获取当前可转发的活动目标；仅 Active 且 Healthy 的目标应返回给网关。</para>
    /// <para>en-us:Attempts to get the current forwardable active target; only Active and Healthy targets should be returned to the gateway.</para>
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
    /// <para>zh-cn:活动目标输出。</para>
    /// <para>en-us:Active target output.</para>
    /// </param>
    /// <returns>
    /// <para>zh-cn:是否找到活动目标。</para>
    /// <para>en-us:Whether an active target was found.</para>
    /// </returns>
    bool TryGetActiveTarget(DynamicAppId dynamicAppId, DynamicModId modId, out DynamicAppRouteTarget? target);

    /// <summary>
    /// <para>zh-cn:获取同一应用和 Mod 下所有 Active 且 Healthy 的部署实例。</para>
    /// <para>en-us:Gets all Active and Healthy deployments for one application and Mod.</para>
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
    /// <para>zh-cn:可转发的部署实例快照。</para>
    /// <para>en-us:Forwardable deployment snapshots.</para>
    /// </returns>
    IReadOnlyCollection<DynamicAppRouteTarget> ListActiveTargets(DynamicAppId dynamicAppId, DynamicModId modId)
        => TryGetActiveTarget(dynamicAppId, modId, out var target) && target != null
            ? new[] { target }
            : Array.Empty<DynamicAppRouteTarget>();

    /// <summary>
    /// <para>zh-cn:列出所有运行目标快照。</para>
    /// <para>en-us:Lists snapshots of all runtime targets.</para>
    /// </summary>
    /// <returns>
    /// <para>zh-cn:运行目标集合。</para>
    /// <para>en-us:The runtime target collection.</para>
    /// </returns>
    IReadOnlyCollection<DynamicAppRouteTarget> ListTargets();
}
