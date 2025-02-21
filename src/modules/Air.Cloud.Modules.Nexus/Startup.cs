using Air.Cloud.Core.App.Startups;
using Air.Cloud.Modules.Nexus.Handlers;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace Air.Cloud.Modules.Nexus;

/// <summary>
/// 事件总线 行为
/// </summary>
public class Startup : AppStartup
{
    /// <summary>
    /// 配置后台服务
    /// </summary>
    /// <param name="services"></param>
    public override void ConfigureServices(IServiceCollection services)
    {
        // 通过工厂模式创建
        services.AddHostedService<EventBusHostedService>();
    }

    public override void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
    }
}