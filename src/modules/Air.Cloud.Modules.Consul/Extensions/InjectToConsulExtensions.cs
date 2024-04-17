using Air.Cloud.Core.App;
using Air.Cloud.Core.Enums;
using Air.Cloud.Modules.Consul.Util;

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Air.Cloud.WebApp.Extensions;
using System.Reflection;
using Air.Cloud.HostApp;
using Air.Cloud.Core;
using Air.Cloud.Core.Standard.AppInject;
using System.Dynamic;
namespace Air.Cloud.Modules.Consul.Extensions
{
    public static class InjectToConsulExtensions
    {
        /// <summary>
        /// 从远程加载配置文件并注册当前服务到Consul
        /// </summary>
        /// <remarks>
        /// 是否注册到Consul 依赖于appsettings.json 里面的EnableConsul
        /// </remarks>
        /// <param name="builder">WebApplication构建器</param>
        /// <returns></returns>
        public static WebApplication WebInjectInCon1sul(this WebApplicationBuilder builder)
        {
            AppConst.LoadConfigurationTypeEnum = LoadConfigurationTypeEnum.Remote;
            AppConst.ApplicationName = Assembly.GetCallingAssembly().GetName().Name;
            //加载远程配置文件
            var Config = new ConfigurationLoader(Assembly.GetCallingAssembly()).LoadRemoteConfiguration();
            if (Config.Item2 != null)
            {
                AppConfigurationLoader.SetCommonConfiguration(Config.Item2);
                builder.Configuration.AddConfiguration(Config.Item2);
            }
            AppConfigurationLoader.SetExternalConfiguration(Config.Item1);
            builder.Configuration.AddConfiguration(Config.Item1);
            builder = AppRealization.Injection.Inject(builder);
            //使用健康检查
            builder.Services.AddConulService();
            var app = builder.Build();
            //添加Consul支持
            app.UseConsul(Assembly.GetCallingAssembly());
            return app;
        }


        /// <summary>
        /// 从远程加载配置文件并注册当前服务到Consul
        /// </summary>
        /// <remarks>
        /// 是否注册到Consul 依赖于appsettings.json 里面的EnableConsul
        /// </remarks>
        /// <param name="builder">WebApplication构建器</param>
        /// <returns></returns>
        public static IHostBuilder HostInjectInConsul(this IHostBuilder builder)
        {
            //加载远程配置文件
            AppConst.LoadConfigurationTypeEnum = LoadConfigurationTypeEnum.Remote;
            AppConst.ApplicationName = Assembly.GetCallingAssembly().GetName().Name;
            var Config = new ConfigurationLoader(Assembly.GetCallingAssembly()).LoadRemoteConfiguration();
            builder = builder.ConfigureAppConfiguration(a =>
            {
                if (Config.Item2 != null)
                {
                    a.AddConfiguration(Config.Item2);
                    AppConfigurationLoader.SetCommonConfiguration(Config.Item2);
                }
                a.AddConfiguration(Config.Item1);
                AppConfigurationLoader.SetExternalConfiguration(Config.Item1);
            });
            builder = AppRealization.Injection.Inject(builder,true);
            return builder;
        }

    }
}
