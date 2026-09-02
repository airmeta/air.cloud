using Air.Cloud.Capabilities.ControlPlaneAuth.Options;

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Air.Cloud.Capabilities.ControlPlaneAuth.Extensions;

/// <summary>
/// <para>zh-cn:控制面认证能力应用管线扩展。</para>
/// <para>en-us:Application-pipeline extensions for the control-plane authentication capability.</para>
/// </summary>
public static class ControlPlaneAuthApplicationBuilderExtensions
{
    /// <summary>
    /// <para>zh-cn:校验控制面认证能力已经注册，并返回原应用构建器。该扩展保留给宿主在管线中显式声明控制面能力，不会自动拦截业务请求。</para>
    /// <para>en-us:Validates that the control-plane authentication capability has been registered and returns the original application builder. This extension lets hosts explicitly declare the control-plane capability in the pipeline and does not automatically intercept business requests.</para>
    /// </summary>
    /// <param name="app"><para>zh-cn:应用构建器。</para><para>en-us:Application builder.</para></param>
    /// <returns><para>zh-cn:原应用构建器，便于链式调用。</para><para>en-us:The original application builder for chaining.</para></returns>
    public static IApplicationBuilder UseControlPlaneAuth(this IApplicationBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);
        _ = app.ApplicationServices.GetRequiredService<IOptionsMonitor<ControlPlaneAuthOptions>>();
        return app;
    }
}
