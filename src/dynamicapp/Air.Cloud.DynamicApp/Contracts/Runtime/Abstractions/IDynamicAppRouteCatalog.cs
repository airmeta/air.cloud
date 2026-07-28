namespace Air.Cloud.DynamicApp.Contracts.Runtime;

/// <summary>
/// <para>zh-cn:DynamicApp 网关路由目录接口。</para>
/// <para>en-us:DynamicApp gateway route catalog interface.</para>
/// </summary>
public interface IDynamicAppRouteCatalog
{
    /// <summary>
    /// <para>zh-cn:解析请求对应的活动目标。</para>
    /// <para>en-us:Resolves the active target for a request.</para>
    /// </summary>
    /// <param name="request">
    /// <para>zh-cn:路由请求。</para>
    /// <para>en-us:The route request.</para>
    /// </param>
    /// <param name="cancellationToken">
    /// <para>zh-cn:取消令牌。</para>
    /// <para>en-us:Cancellation token.</para>
    /// </param>
    /// <returns>
    /// <para>zh-cn:活动目标；不存在时返回 null。</para>
    /// <para>en-us:The active target, or null when none exists.</para>
    /// </returns>
    ValueTask<DynamicAppRouteTarget?> ResolveAsync(DynamicAppRouteRequest request, CancellationToken cancellationToken = default);
}
