using Microsoft.AspNetCore.Builder;

namespace Air.Cloud.DynamicApp.Panel.Extensions;

/// <summary>
/// <para>zh-cn:DynamicApp 内置控制面板应用管道扩展。</para>
/// <para>en-us:Application pipeline extensions for the built-in DynamicApp control panel.</para>
/// </summary>
public static class DynamicAppPanelApplicationBuilderExtensions
{
    /// <summary>
    /// <para>zh-cn:启用内置控制面板中间件；应放在 Manager API 和 Gateway 转发中间件之前。</para>
    /// <para>en-us:Enables the built-in control panel middleware; it should be placed before Manager API and Gateway forwarding middleware.</para>
    /// </summary>
    /// <param name="app">
    /// <para>zh-cn:应用构建器。</para>
    /// <para>en-us:The application builder.</para>
    /// </param>
    /// <returns>
    /// <para>zh-cn:原应用构建器。</para>
    /// <para>en-us:The original application builder.</para>
    /// </returns>
    public static IApplicationBuilder UseDynamicAppPanel(this IApplicationBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);
        return app.UseMiddleware<DynamicAppPanelMiddleware>();
    }
}
