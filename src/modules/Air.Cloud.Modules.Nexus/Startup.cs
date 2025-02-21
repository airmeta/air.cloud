using Air.Cloud.Core.App.Startups;
using Air.Cloud.Core.Standard.Event;
using Air.Cloud.Modules.Nexus.Extensions;
using Air.Cloud.Modules.Nexus.Publishers.Storers;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Air.Cloud.Modules.Nexus;

/// <summary>
/// 事件总线 行为
/// </summary>
public class Startup:AppStartup
{
    public override void ConfigureServices(IServiceCollection services)
    {
        services.AddEventBus(builder =>
        {
            throw new Exception("Nexus未实现");
            //builder.AddHandlers(AppCore.Assemblies.ToArray());
            builder.ReplaceStorer(services =>
            {
                return new ChannelEventSourceStorer(100);
            });
        });

    }

    public override void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        // 获取主机生命周期管理接口
        var lifetime = app.ApplicationServices.GetRequiredService<IHostApplicationLifetime>();

        lifetime.ApplicationStarted.Register(() =>
        {
            //启动后台进程
            var eventBusHostedService = app.ApplicationServices.GetRequiredService<IEventBusTaskBackGroundService>();
            if (eventBusHostedService == null)
            {
                Console.WriteLine("事件总线启动失败");
            }
            eventBusHostedService.Start();

        });
        lifetime.ApplicationStopped.Register(() =>
        {
            //启动后台进程
            var eventBusHostedService = app.ApplicationServices.GetRequiredService<IEventBusTaskBackGroundService>();
            if (eventBusHostedService == null)
            {
                Console.WriteLine("事件总线启动失败");
            }
            eventBusHostedService.Stop();

        });
    }
}