namespace Air.Cloud.DynamicApp.Contracts.Runtime;

/// <summary>
/// <para>zh-cn:DynamicApp 独立入口路由规则存储接口；规则变更不触碰实例进程、包目录或 Manifest。</para>
/// <para>en-us:Store for independent DynamicApp entry-route rules; route changes do not touch runtime processes, package directories, or manifests.</para>
/// </summary>
public interface IDynamicAppGatewayRouteStore
{
    /// <summary>
    /// <para>zh-cn:获取当前路由快照。</para>
    /// <para>en-us:Gets the current route snapshot.</para>
    /// </summary>
    IReadOnlyCollection<DynamicAppGatewayRoute> List();

    /// <summary>
    /// <para>zh-cn:按 RouteId 获取路由。</para>
    /// <para>en-us:Gets a route by RouteId.</para>
    /// </summary>
    DynamicAppGatewayRoute? Get(string routeId);

    /// <summary>
    /// <para>zh-cn:创建路由；RouteId、模板和父子顺序必须通过校验。</para>
    /// <para>en-us:Creates a route after validating RouteId, templates, and parent-child ordering.</para>
    /// </summary>
    Task<DynamicAppOperationResult> CreateAsync(DynamicAppGatewayRoute route, CancellationToken cancellationToken = default);

    /// <summary>
    /// <para>zh-cn:更新路由，可使用 UpdatedAtUtc 做乐观并发校验。</para>
    /// <para>en-us:Updates a route and may use UpdatedAtUtc for optimistic concurrency.</para>
    /// </summary>
    Task<DynamicAppOperationResult> UpdateAsync(DynamicAppGatewayRoute route, DateTimeOffset? expectedUpdatedAtUtc = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// <para>zh-cn:删除路由规则，不影响目标实例。</para>
    /// <para>en-us:Removes a route rule without affecting its target instances.</para>
    /// </summary>
    Task<DynamicAppOperationResult> RemoveAsync(string routeId, CancellationToken cancellationToken = default);

    /// <summary>
    /// <para>zh-cn:启用或禁用路由规则。</para>
    /// <para>en-us:Enables or disables a route rule.</para>
    /// </summary>
    Task<DynamicAppOperationResult> SetEnabledAsync(string routeId, bool enabled, DateTimeOffset? expectedUpdatedAtUtc = null, CancellationToken cancellationToken = default);
}
