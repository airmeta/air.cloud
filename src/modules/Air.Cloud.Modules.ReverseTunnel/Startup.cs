using Air.Cloud.Core.App.Startups;
using Air.Cloud.Modules.ReverseTunnel.Extensions;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace Air.Cloud.Modules.ReverseTunnel;

/// <summary>
/// <para>zh-cn:反向隧道模块启动配置，注册协议、路由和后续运行角色共用的基础服务。</para>
/// <para>en-us:Reverse tunnel module startup configuration that registers the protocol, routing, and shared services used by later runtime roles.</para>
/// </summary>
public sealed class Startup : AppStartup
{
    /// <summary>
    /// <para>zh-cn:配置反向隧道模块的应用管道。模块自动启动项只注册依赖，不在这里隐式映射 Server、Gateway 或诊断 HTTP endpoint；宿主必须通过 `MapReverseTunnelServer`、`UseReverseTunnelServer` 或 `MapReverseTunnelDiagnostics` 显式开放入口。</para>
    /// <para>en-us:Configures the reverse tunnel module pipeline. The auto-startup path only registers dependencies and does not implicitly map Server, Gateway, or diagnostic HTTP endpoints here; the host must explicitly expose entries through `MapReverseTunnelServer`, `UseReverseTunnelServer`, or `MapReverseTunnelDiagnostics`.</para>
    /// </summary>
    /// <param name="app">
    /// <para>zh-cn:应用程序构建器。</para>
    /// <para>en-us:The application builder.</para>
    /// </param>
    /// <param name="env">
    /// <para>zh-cn:Web 主机环境。</para>
    /// <para>en-us:The web host environment.</para>
    /// </param>
    public override void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
    }

    /// <summary>
    /// <para>zh-cn:注册反向隧道选项和虚拟主机路由解析器。该调用可安全重复执行，供模块自动扫描和显式注册两种场景共用。</para>
    /// <para>en-us:Registers reverse tunnel options and the virtual-host route resolver. The call is safe to repeat so module scanning and explicit registration can share it.</para>
    /// </summary>
    /// <param name="services">
    /// <para>zh-cn:服务集合。</para>
    /// <para>en-us:The service collection.</para>
    /// </param>
    public override void ConfigureServices(IServiceCollection services)
    {
        services.AddReverseTunnel();
    }
}
