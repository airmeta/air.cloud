using Air.Cloud.Core;
using Air.Cloud.Core.App.Startups;
using Air.Cloud.Core.Attributes;
using Air.Cloud.Core.Plugins.PID;
using Air.Cloud.Core.Standard.KVCenter;
using Air.Cloud.Core.Standard.ServerCenter;
using Air.Cloud.Modules.Consul.Plugins;
using Air.Cloud.Modules.Consul.Service;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace Air.Cloud.Modules.Consul
{
    /// <summary>
    /// <para>zh-cn:Consul 模块启动配置，负责注册服务中心和键值中心标准实现。</para>
    /// <para>en-us:Consul module startup configuration that registers server center and key-value center standard implementations.</para>
    /// </summary>
    [AppStartupAttribute(AppName = "Air.Cloud.Core", Order = int.MinValue)]
    public  class Startup : AppStartup
    {
        /// <summary>
        /// <para>zh-cn:配置 Consul 模块的应用管道。</para>
        /// <para>en-us:Configures the application pipeline for the Consul module.</para>
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
        /// <para>zh-cn:注册 Consul 服务中心和键值中心依赖。</para>
        /// <para>en-us:Registers Consul server center and key-value center dependencies.</para>
        /// </summary>
        /// <param name="services">
        /// <para>zh-cn:服务集合。</para>
        /// <para>en-us:The service collection.</para>
        /// </param>
        public override void ConfigureServices(IServiceCollection services)
        {
            services.AddTransient<IServerCenterStandard, ConsulServerCenterDependency>();
            services.AddTransient<IKVCenterStandard, ConsulKVCenterDependency>();
        }
    }
}
