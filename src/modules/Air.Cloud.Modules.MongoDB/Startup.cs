using Air.Cloud.Core.App.Startups;
using Air.Cloud.Core.Standard.DataBase.Options;
using Air.Cloud.Modules.MongoDB.Extensions;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace Air.Cloud.Modules.MongoDB
{
    /// <summary>
    /// <para>zh-cn:MongoDB 模块启动配置，负责注册 MongoDB 选项和仓储服务。</para>
    /// <para>en-us:MongoDB module startup configuration that registers MongoDB options and repository services.</para>
    /// </summary>
    public class Startup : AppStartup
    {
        /// <summary>
        /// <para>zh-cn:配置 MongoDB 模块的应用管道。</para>
        /// <para>en-us:Configures the application pipeline for the MongoDB module.</para>
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
        /// <para>zh-cn:注册 MongoDB 模块依赖服务和数据库配置项。</para>
        /// <para>en-us:Registers MongoDB module dependencies and database options.</para>
        /// </summary>
        /// <param name="services">
        /// <para>zh-cn:服务集合。</para>
        /// <para>en-us:The service collection.</para>
        /// </param>
        public override void ConfigureServices(IServiceCollection services)
        {
            services.AddOptions<DataBaseOptions>()
               .BindConfiguration("DataBaseSettings")
               .ValidateDataAnnotations()
               .PostConfigure(options =>
               { });
            services.AddMongoDB();
        }
    }
}
