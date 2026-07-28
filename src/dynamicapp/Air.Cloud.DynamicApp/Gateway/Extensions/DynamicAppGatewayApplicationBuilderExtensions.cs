using Microsoft.AspNetCore.Builder;

namespace Air.Cloud.DynamicApp.Gateway.Extensions;

/// <summary>
/// <para>zh-cn:DynamicApp Gateway 应用管道扩展。</para>
/// <para>en-us:Application pipeline extensions for DynamicApp Gateway.</para>
/// </summary>
public static class DynamicAppGatewayApplicationBuilderExtensions
{
    /// <summary>
    /// <para>zh-cn:启用 DynamicApp Gateway 转发中间件；应放在认证授权之后、业务端点之前。</para>
    /// <para>en-us:Enables the DynamicApp Gateway forwarding middleware; place it after authentication/authorization and before business endpoints.</para>
    /// </summary>
    /// <param name="app">
    /// <para>zh-cn:应用构建器。</para>
    /// <para>en-us:The application builder.</para>
    /// </param>
    /// <returns>
    /// <para>zh-cn:原应用构建器。</para>
    /// <para>en-us:The original application builder.</para>
    /// </returns>
    public static IApplicationBuilder UseDynamicAppGateway(this IApplicationBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);
        return app.UseMiddleware<DynamicAppGatewayMiddleware>();
    }
}
