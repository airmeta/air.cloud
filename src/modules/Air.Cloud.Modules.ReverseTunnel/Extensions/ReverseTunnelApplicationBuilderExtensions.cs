using Air.Cloud.Core.Attributes;

using Microsoft.AspNetCore.Builder;

namespace Air.Cloud.Modules.ReverseTunnel.Extensions;

/// <summary>
/// <para>zh-cn:反向隧道 Server 应用管道扩展。该扩展适合没有集中 UseEndpoints 回调的宿主。</para>
/// <para>en-us:Reverse tunnel Server application-pipeline extensions. This extension is intended for hosts that do not already centralize endpoint mapping in a UseEndpoints callback.</para>
/// </summary>
[IgnoreScanning]
public static class ReverseTunnelApplicationBuilderExtensions
{
    /// <summary>
    /// <para>zh-cn:通过独立 UseEndpoints 调用映射反向隧道 Server gRPC 服务。已有集中 endpoint 映射的宿主应优先在该回调内调用 MapReverseTunnelServer，以保持路由顺序清晰。</para>
    /// <para>en-us:Maps the reverse tunnel Server gRPC service through a standalone UseEndpoints call. Hosts that already centralize endpoint mapping should prefer calling MapReverseTunnelServer inside that callback to keep route ordering clear.</para>
    /// </summary>
    /// <param name="app">
    /// <para>zh-cn:应用程序构建器，必须已经在前面调用 UseRouting。</para>
    /// <para>en-us:Application builder; UseRouting must have been called earlier in the pipeline.</para>
    /// </param>
    /// <returns>
    /// <para>zh-cn:原始应用程序构建器。</para>
    /// <para>en-us:The original application builder.</para>
    /// </returns>
    /// <exception cref="InvalidOperationException">
    /// <para>zh-cn:Server endpoint 配置未通过验证或 CA 挂载无法加载。</para>
    /// <para>en-us:Server endpoint configuration does not pass validation or the mounted CA cannot be loaded.</para>
    /// </exception>
    public static IApplicationBuilder UseReverseTunnelServer(this IApplicationBuilder app)
    {
        if (app is null)
        {
            throw new ArgumentNullException(nameof(app));
        }

        app.UseEndpoints(endpoints => endpoints.MapReverseTunnelServer());
        return app;
    }

    /// <summary>
    /// <para>zh-cn:通过独立 UseEndpoints 调用映射 AirTunnel 诊断 HTTP API。该扩展不要求当前进程包含 Server 角色，适合 Agent、Gateway 或 Server 宿主在业务中间件之前开放只读诊断入口；调用前必须已经执行 UseRouting。</para>
    /// <para>en-us:Maps AirTunnel diagnostic HTTP APIs through a standalone UseEndpoints call. This extension does not require the current process to include the Server role, so Agent, Gateway, or Server hosts can expose the read-only diagnostic entry before business middlewares; UseRouting must have already run.</para>
    /// </summary>
    /// <param name="app">
    /// <para>zh-cn:应用程序构建器，必须已经在前面调用 UseRouting。</para>
    /// <para>en-us:Application builder; UseRouting must have been called earlier.</para>
    /// </param>
    /// <returns>
    /// <para>zh-cn:原始应用程序构建器。</para>
    /// <para>en-us:The original application builder.</para>
    /// </returns>
    public static IApplicationBuilder UseReverseTunnelDiagnostics(this IApplicationBuilder app)
    {
        if (app is null)
        {
            throw new ArgumentNullException(nameof(app));
        }

        app.UseEndpoints(endpoints => endpoints.MapReverseTunnelDiagnostics());
        return app;
    }
}
