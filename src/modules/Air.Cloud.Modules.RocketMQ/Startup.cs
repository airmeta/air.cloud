using Air.Cloud.Core.App.Startups;
using Air.Cloud.Modules.RocketMQ.Config;
using Air.Cloud.Modules.RocketMQ.Extensions;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace Air.Cloud.Modules.RocketMQ
{
    /// <summary>
    /// <para>zh-cn:RocketMQ 模块启动类，负责绑定 RocketMQSettings 配置并注册 RocketMQ 消息队列标准实现。</para>
    /// <para>en-us:RocketMQ module startup that binds RocketMQSettings and registers the RocketMQ message-queue standard implementation.</para>
    /// </summary>
    public class Startup : AppStartup
    {
        /// <summary>
        /// <para>zh-cn:配置应用中间件；RocketMQ 模块不需要 ASP.NET Core 请求管道中间件，因此该方法为空。</para>
        /// <para>en-us:Configures application middleware. The RocketMQ module does not require ASP.NET Core request-pipeline middleware, so this method is empty.</para>
        /// </summary>
        /// <param name="app">
        /// <para>zh-cn:应用构建器。</para>
        /// <para>en-us:The application builder.</para>
        /// </param>
        /// <param name="env">
        /// <para>zh-cn:宿主环境。</para>
        /// <para>en-us:The hosting environment.</para>
        /// </param>
        public override void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
        }

        /// <summary>
        /// <para>zh-cn:注册 RocketMQ 模块服务，绑定 RocketMQSettings，并装载默认消息队列实现、恢复、补偿和 Key 生成器。</para>
        /// <para>en-us:Registers RocketMQ module services, binds RocketMQSettings, and loads the default message-queue implementation, recovery, compensation, and key generator.</para>
        /// </summary>
        /// <param name="services">
        /// <para>zh-cn:服务集合。</para>
        /// <para>en-us:The service collection.</para>
        /// </param>
        public override void ConfigureServices(IServiceCollection services)
        {
            services.AddOptions<RocketMQSettingsOptions>()
                .BindConfiguration("RocketMQSettings")
                .ValidateDataAnnotations()
                .PostConfigure(options => { });

            services.AddRocketMQService();
        }
    }
}
