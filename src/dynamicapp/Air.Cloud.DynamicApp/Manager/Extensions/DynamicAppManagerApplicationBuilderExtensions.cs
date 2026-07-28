using Microsoft.AspNetCore.Builder;

namespace Air.Cloud.DynamicApp.Manager.Extensions;

/// <summary>
/// <para>zh-cn:DynamicApp Manager 应用管道扩展。</para>
/// <para>en-us:Application pipeline extensions for DynamicApp Manager.</para>
/// </summary>
public static class DynamicAppManagerApplicationBuilderExtensions
{
    /// <summary>
    /// <para>zh-cn:启用内部 Manager API 中间件；应仅挂载在可信控制面端口或受保护网络中。</para>
    /// <para>en-us:Enables the internal Manager API middleware; mount it only on a trusted control-plane port or protected network.</para>
    /// </summary>
    /// <param name="app">
    /// <para>zh-cn:应用构建器。</para>
    /// <para>en-us:The application builder.</para>
    /// </param>
    /// <returns>
    /// <para>zh-cn:原应用构建器。</para>
    /// <para>en-us:The original application builder.</para>
    /// </returns>
    public static IApplicationBuilder UseDynamicAppManagerApi(this IApplicationBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);
        return app.UseMiddleware<DynamicAppManagerApiMiddleware>();
    }
}
