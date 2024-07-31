/*
 * Copyright (c) 2024 星曳数据
 *
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at https://mozilla.org/MPL/2.0/.
 *
 * This file is provided under the Mozilla Public License Version 2.0,
 * and the "NO WARRANTY" clause of the MPL is hereby expressly
 * acknowledged.
 */
using Air.Cloud.Core;
using Air.Cloud.Core.App;
using Air.Cloud.Core.Enums;
using Air.Cloud.Core.Standard.AppInject;
using Air.Cloud.Modules.Consul.Model;
using Air.Cloud.Modules.Consul.Resolver;
using Air.Cloud.Modules.Consul.Service;
using Air.Cloud.Modules.Consul.Util;

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using System.Reflection;
namespace Air.Cloud.Modules.Consul.Extensions
{
    /// <summary>
    /// <para>zh-cn:Consul 模块扩展</para>
    /// <para>en-us:Consul module extensions</para>
    /// </summary>
    /// <remarks>
    ///  <para>zh-cn:不支持控制台模式使用该类库的扩展方法</para>
    ///  <para>en-us:Extension methods using this class library in console mode are not supported.</para>
    /// </remarks>
    public static class ConsulModuleExtensions
    {
        /// <summary>
        /// 从远程加载配置文件并注册当前服务到Consul
        /// </summary>
        /// <remarks>
        /// 是否注册到Consul 依赖于appsettings.json 里面的EnableConsul
        /// </remarks>
        /// <param name="builder">WebApplication构建器</param>
        /// <returns></returns>
        public static WebApplication WebInjectInConsul(this WebApplicationBuilder builder)
        {
            AppConst.LoadConfigurationTypeEnum = LoadConfigurationTypeEnum.Remote;
            AppConst.ApplicationName = Assembly.GetCallingAssembly().GetName().Name;
            //加载远程配置文件
            var Config = new ConfigurationLoader(Assembly.GetCallingAssembly()).LoadRemoteConfiguration();
            if (Config.Item2 != null)
            {
                AppConfigurationLoader.SetPublicConfiguration(Config.Item2);
                builder.Configuration.AddConfiguration(Config.Item2);
            }
            var InjectionType = AppCore.StandardTypes.Where(s=>s.GetInterfaces().Contains(typeof(IAppInjectStandard))).FirstOrDefault();
            Assembly assembly = Assembly.GetAssembly(InjectionType);
            IAppInjectStandard appInject= assembly.CreateInstance(InjectionType.FullName) as IAppInjectStandard;
            AppRealization.SetDependency(appInject);
            AppConfigurationLoader.SetExternalConfiguration(Config.Item1);
            builder.Configuration.AddConfiguration(Config.Item1);
            builder = AppRealization.Injection.Inject(builder);
            //使用健康检查
            builder.Services.AddConulService();
            AppConst.ApplicationInstanceName = $"{AppConst.ApplicationName}_{AppRealization.PID.Get()}";
            var app = builder.Build();
            //添加Consul支持
            app.UseConsul(Assembly.GetCallingAssembly());
            return app;
        }

        /// <summary>
        /// 从远程加载配置文件
        /// </summary>
        /// <param name="builder">WebApplication构建器</param>
        /// <returns></returns>
        public static IHostBuilder HostInjectInConsul(this IHostBuilder builder)
        {
            //加载远程配置文件
            AppConst.LoadConfigurationTypeEnum = LoadConfigurationTypeEnum.Remote;
            AppConst.ApplicationName = Assembly.GetCallingAssembly().GetName().Name;
            AppConst.ApplicationInstanceName = $"{AppConst.ApplicationName}_{AppRealization.PID.Get()}";
            var Config = new ConfigurationLoader(Assembly.GetCallingAssembly()).LoadRemoteConfiguration();
            var InjectionType = AppCore.StandardTypes.Where(s => s.GetInterfaces().Contains(typeof(IAppInjectStandard))).FirstOrDefault();
            Assembly assembly = Assembly.GetAssembly(InjectionType);
            IAppInjectStandard appInject = assembly.CreateInstance(InjectionType.FullName) as IAppInjectStandard;
            AppRealization.SetDependency(appInject);
            builder = builder.ConfigureAppConfiguration(a =>
            {
                if (Config.Item2 != null)
                {
                    a.AddConfiguration(Config.Item2);
                    AppConfigurationLoader.SetPublicConfiguration(Config.Item2);
                }
                a.AddConfiguration(Config.Item1);
                AppConfigurationLoader.SetExternalConfiguration(Config.Item1);
            });
            builder = AppRealization.Injection.Inject(builder,true);
            return builder;
        }

        /// <summary>
        ///  <para>zh-cn:接入注册中心</para>
        ///  <para>en-us:Use consul</para>
        /// </summary>
        /// <param name="app"></param>
        /// <param name="assembly"></param>
        /// <remarks>
        /// IIS模式目前只需要一个注册中心地址即可,当前服务地址信息为即时发现
        /// Docker模式目前需要配置注册中心地址以及当前服务的地址信息
        /// </remarks>
        /// <returns></returns>
        private static IApplicationBuilder UseConsul(this IApplicationBuilder app, Assembly assembly = null)
        {
            var serviceOptions = AppCore.Configuration.GetConfig<ConsulServiceOptions>();
            //开发环境剔除此参数
            if (AppEnvironment.IsDevelopment) return app;
            AppRuntimeOptions? info = AppCore.GetOptions<AppRuntimeOptions>();
            serviceOptions = ConsulServiceOptions.GetConsulServiceOptions(info, serviceOptions);
            #region 初始化一些参数 如果可以自动获取 将忽略配置文件
            // 服务ID为当前应用程序唯一标识
            serviceOptions.ServiceId = serviceOptions.ServiceId ?? AppRealization.PID.Get();
            //获取当前程序的注册名
            serviceOptions.ServiceName = app.GetCurrentProjectConsulServiceName(serviceOptions.ServiceName, assembly);
            #endregion
            var ConsulHelpers = new ConsulOperatorHelper(serviceOptions.ConsulAddress);
            var result = ConsulHelpers.InitConsulRegistration(serviceOptions);
            if (!result.Item1) return app;
            #region 注册服务
            ConsulServerCenterDependency dependency = new ConsulServerCenterDependency();
            var r = dependency.Register(new ConsulServerCenterServiceRegisterOptions()
            {
                ServiceAddress = serviceOptions.ServiceAddress,
                ServiceName = serviceOptions.ServiceName,
                ServiceKey = serviceOptions.ServiceId,
                HealthCheckTimeStep = new TimeSpan(0, 0, 10),
                HealthCheckRoute = serviceOptions.HealthCheck,
                Timeout = new TimeSpan(0, 0, 5),
                DeregisterCriticalServiceAfter = new TimeSpan(0, 1, 0)
            }).Result;
            // 获取主机生命周期管理接口
            var lifetime = app.ApplicationServices.GetRequiredService<IHostApplicationLifetime>();
            // 应用程序终止时，注销服务
            lifetime.ApplicationStopping.Register(() =>
            {
                ConsulHelpers.GetConsulClient().Agent.ServiceDeregister(serviceOptions.ServiceId).Wait();
            });

            lifetime.ApplicationStarted.Register(() =>
            {
                //开始进行初次健康检查
                using (HttpClient client = new HttpClient())
                {
                    Uri uris = new Uri(new Uri(serviceOptions.ServiceAddress), serviceOptions.HealthCheck);
                    _ = client.GetAsync(uris).Result;
                }
            });
            #endregion
            app.UseHealthChecks(serviceOptions.HealthCheck);
            return app;
        }

        /// <summary>
        /// <para>zh-cn:注册Consul服务</para>
        /// <para>en-us:Add conul service</para>
        /// </summary>
        /// <param name="services"></param>
        private static void AddConulService(this IServiceCollection services)
        {
            //开发环境剔除此参数
            if (AppEnvironment.IsDevelopment) return;
            services.AddHealthChecks();
        }
    }
}
