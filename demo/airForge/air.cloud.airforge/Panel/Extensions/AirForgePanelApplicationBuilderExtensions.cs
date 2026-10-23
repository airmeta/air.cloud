using Microsoft.AspNetCore.Builder;

namespace Air.Cloud.AirForge.Panel.Extensions;

/// <summary>
/// <para>zh-cn:AirForge 管理面板应用管道扩展。</para>
/// <para>en-us:Application pipeline extensions for the AirForge management panel.</para>
/// </summary>
public static class AirForgePanelApplicationBuilderExtensions
{
    /// <summary>
    /// <para>zh-cn:启用 AirForge 管理面板中间件；应放在 Manager API 和 Gateway 转发中间件之前。</para>
    /// <para>en-us:Enables the AirForge management panel middleware; it should be placed before Manager API and Gateway forwarding middleware.</para>
    /// </summary>
    /// <param name="app">
    /// <para>zh-cn:应用管道构建器。</para>
    /// <para>en-us:The application builder.</para>
    /// </param>
    /// <returns>
    /// <para>zh-cn:原应用管道构建器。</para>
    /// <para>en-us:The original application builder.</para>
    /// </returns>
    public static IApplicationBuilder UseAirForgePanel(this IApplicationBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);
        return app.UseMiddleware<AirForgePanelMiddleware>();
    }
}


