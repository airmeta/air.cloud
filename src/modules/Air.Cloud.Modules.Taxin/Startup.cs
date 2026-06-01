using Air.Cloud.Core.App.Startups;
using Air.Cloud.Core.Standard.Taxin.Options;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace Air.Cloud.Modules.Taxin
{
    /// <summary>
    /// <para>zh-cn:Taxin 模块启动配置，负责注册远程调用配置选项。</para>
    /// <para>en-us:Taxin module startup configuration that registers remote call configuration options.</para>
    /// </summary>
    public class Startup : AppStartup
    {
        /// <summary>
        /// <para>zh-cn:配置 Taxin 模块的应用管道。</para>
        /// <para>en-us:Configures the application pipeline for the Taxin module.</para>
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
        /// <para>zh-cn:注册 Taxin 配置选项。</para>
        /// <para>en-us:Registers Taxin configuration options.</para>
        /// </summary>
        /// <param name="services">
        /// <para>zh-cn:服务集合。</para>
        /// <para>en-us:The service collection.</para>
        /// </param>
        public override void ConfigureServices(IServiceCollection services)
        {
            services.AddOptions<TaxinOptions>()
               .BindConfiguration("TaxinSettings")
               .ValidateDataAnnotations()
               .PostConfigure(options =>
               { });
        }
    }
}
