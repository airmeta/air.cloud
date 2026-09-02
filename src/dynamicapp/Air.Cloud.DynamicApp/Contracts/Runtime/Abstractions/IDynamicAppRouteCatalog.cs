namespace Air.Cloud.DynamicApp.Contracts.Runtime;

/// <summary>
/// <para>zh-cn:DynamicApp 网关路由目录接口。</para>
/// <para>en-us:DynamicApp gateway route catalog interface.</para>
/// </summary>
public interface IDynamicAppRouteCatalog
{
    /// <summary>
    /// <para>zh-cn:按独立动态路由规则解析请求、目标和明确失败原因。</para>
    /// <para>en-us:Resolves the request, target, and explicit failure reason through independent dynamic route rules.</para>
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
    /// <para>zh-cn:动态路由解析结果。</para>
    /// <para>en-us:Dynamic route resolution result.</para>
    /// </returns>
    ValueTask<DynamicAppGatewayRouteResolution> ResolveAsync(DynamicAppRouteRequest request, CancellationToken cancellationToken = default);
}
