/*
 * Copyright (c) 2024-2030 星曳数据
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
using Air.Cloud.Core.Plugins;
using Air.Cloud.Core.Plugins.Banner;
using Air.Cloud.Core.Plugins.PID;
using Air.Cloud.Core.Standard.AppInject;
using Air.Cloud.Modules.Consul.Dependencies;
using Air.Cloud.Modules.Consul.Model;
using Air.Cloud.Modules.Consul.Plugins;
using Air.Cloud.Modules.Consul.Service;
using Air.Cloud.Modules.Consul.Standard;
using Air.Cloud.Modules.Consul.Util;

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using System;
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
        /// <param name="action">
        /// <para>zh-cn:自定义构建行为</para>
        /// <para>en-us:Configure action</para>
        /// </param>
        /// <returns></returns>
        public static WebApplication WebInjectInConsul(this WebApplicationBuilder builder, Action<ConsulServiceOptions> action = null)
        {
            return WebInjectInConsul<DefaultConsulServiceOptionsConfigureDependency>(builder,action);
        }
        /// <summary>
        /// 从远程加载配置文件并注册当前服务到Consul
        /// </summary>
        /// <remarks>
        /// 是否注册到Consul 依赖于appsettings.json 里面的EnableConsul
        /// </remarks>
        /// <param name="builder">WebApplication构建器</param>
        /// <param name="action">
        /// <para>zh-cn:自定义构建行为</para>
        /// <para>en-us:Configure action</para>
        /// </param>
        /// <typeparam name="TConsulServiceOptionsConfigureDependency">
        ///  <para>zh-cn:Consul配置构建器</para>
        ///   <para>en-us: Consul options configure</para>
        /// </typeparam>
        /// <returns></returns>
        public static WebApplication WebInjectInConsul<TConsulServiceOptionsConfigureDependency>(this WebApplicationBuilder builder,
            Action<ConsulServiceOptions> action = null)
            where TConsulServiceOptionsConfigureDependency:class,IConsulServiceOptionsConfigureStandard,new()
        {
            InitAppInject();
            ConsulServiceOptions consulServiceOptions = GetConsulService<TConsulServiceOptionsConfigureDependency>(action);
            //加载远程配置文件
            var Config = ConfigurationLoader.LoadRemoteConfiguration(consulServiceOptions);
            if (Config.Item2 != null)
            {
                AppConfigurationLoader.SetPublicConfiguration(Config.Item2);
                builder.Configuration.AddConfiguration(Config.Item2);
            }
            AppConfigurationLoader.SetExternalConfiguration(Config.Item1);
            builder.Configuration.AddConfiguration(Config.Item1);

            builder = AppRealization.Injection.Inject(builder);
            //注册Consul服务
            builder.Services.AddConulService();
            var app = builder.Build();
            //添加Consul支持
            app.UseConsul(consulServiceOptions);
            return app;
        }

        /// <summary>
        /// <para>zh-cn:读取Consul服务配置</para>
        /// <para>en-us:Get consul service options</para>
        /// </summary>
        /// <typeparam name="TConsulServiceOptionsConfigureDependency">
        ///  <para>zh-cn:Consul配置构建器</para>
        ///   <para>en-us: Consul options configure</para>
        /// </typeparam>
        /// <param name="action">
        /// <para>zh-cn:自定义构建行为</para>
        /// <para>en-us:Configure action</para>
        /// </param>
        /// <returns>
        ///  <para>zh-cn:服务配置</para>
        ///  <para>en-us:Consul service options</para>
        /// </returns>
        private static ConsulServiceOptions GetConsulService<TConsulServiceOptionsConfigureDependency>(Action<ConsulServiceOptions> action = null)
            where TConsulServiceOptionsConfigureDependency : class, IConsulServiceOptionsConfigureStandard, new()
        {
            var serviceOptions = AppConfigurationLoader.InnerConfiguration.GetConfig<ConsulServiceOptions>();
            TConsulServiceOptionsConfigureDependency OptionsConfigure = new TConsulServiceOptionsConfigureDependency();
            serviceOptions = OptionsConfigure?.Configure(serviceOptions, action) ?? serviceOptions;
            return serviceOptions;

        }

        /// <summary>
        ///  <para>zh-cn:初始化注入</para>
        ///  <para>en-us:Init app inject</para>
        /// </summary>
        private static void InitAppInject() {
            #region  初始化注入
            var InjectionType = AppCore.StandardTypes.Where(s => s.GetInterfaces().Contains(typeof(IAppInjectStandard))).FirstOrDefault();
                Assembly assembly = Assembly.GetAssembly(InjectionType);
                IAppInjectStandard appInject = assembly.CreateInstance(InjectionType.FullName) as IAppInjectStandard;
                AppRealization.SetDependency(appInject);
            #endregion
            AppRealization.SetPlugin<IPIDPlugin>(new ConsulPIDPluginDependency());
            AppConst.LoadConfigurationTypeEnum = LoadConfigurationTypeEnum.Remote;
            AppConst.ApplicationName = Assembly.GetEntryAssembly().GetName().Name;
            AppConst.ApplicationInstanceName = $"{AppConst.ApplicationName}_{AppRealization.PID.Get()}";
        }
        /// <summary>
        /// 从远程加载配置文件
        /// </summary>
        /// <param name="builder">WebApplication构建器</param>
        /// <param name="action">
        /// <para>zh-cn:自定义构建行为</para>
        /// <para>en-us:Configure action</para>
        /// </param>
        /// <returns></returns>
        public static IHostBuilder HostInjectInConsul(this IHostBuilder builder, Action<ConsulServiceOptions> action = null)
        {
            return HostInjectInConsul<DefaultConsulServiceOptionsConfigureDependency>(builder, action);
        }
        /// <summary>
        /// 从远程加载配置文件
        /// </summary>
        /// <param name="builder">WebApplication构建器</param>
        /// <param name="action">
        /// <para>zh-cn:自定义构建行为</para>
        /// <para>en-us:Configure action</para>
        /// </param>
        /// <returns></returns>
        public static IHostBuilder HostInjectInConsul<TConsulServiceOptionsConfigureDependency>(this IHostBuilder builder, Action<ConsulServiceOptions> action = null)
             where TConsulServiceOptionsConfigureDependency : class, IConsulServiceOptionsConfigureStandard, new()
        {
            InitAppInject();
            ConsulServiceOptions consulServiceOptions = GetConsulService<TConsulServiceOptionsConfigureDependency>(action);
            var Config = ConfigurationLoader.LoadRemoteConfiguration(consulServiceOptions);
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
            builder = AppRealization.Injection.Inject(builder, true);
            return builder;
        }
        /// <summary>
        ///  <para>zh-cn:接入注册中心</para>
        ///  <para>en-us:Use consul</para>
        /// </summary>
        /// <param name="app">
        ///     application builder
        /// </param>
        /// <param name="serviceOptions">
        /// <para>zh-cn:服务注册配置</para>
        /// <para>en-us:Service options</para>
        /// </param>
        /// <remarks>
        /// IIS模式目前只需要一个注册中心地址即可,当前服务地址信息为即时发现
        /// Docker模式目前需要配置注册中心地址以及当前服务的地址信息
        /// </remarks>
        /// <returns></returns>
        private static IApplicationBuilder UseConsul(
                    this IApplicationBuilder app,
                    ConsulServiceOptions serviceOptions)
        {
            //开发环境剔除此参数
            if (AppEnvironment.IsDevelopment) return app;
            var ConsulHelpers = new ConsulOperatorHelper(serviceOptions.ConsulAddress);
            var result = ConsulHelpers.InitConsulRegistration(serviceOptions);
            if (!result.Item1) return app;
            #region 注册服务
            ConsulServerCenterDependency dependency = new ConsulServerCenterDependency();
            var r = dependency.RegisterAsync(new ConsulServerCenterServiceRegisterOptions()
            {
                ServiceAddress = serviceOptions.ServiceAddress,
                ServiceName = serviceOptions.ServiceName,
                ServiceKey = ConsulServiceOptions.ServiceId,
                HealthCheckTimeStep = new TimeSpan(0, 0, serviceOptions.HealthCheckTimeStep),
                HealthCheckRoute = serviceOptions.HealthCheckRoute,
                Timeout = new TimeSpan(0, 0, serviceOptions.ConnectTimeout),
                DeregisterCriticalServiceAfter = new TimeSpan(0, 0, serviceOptions.DeregisterCriticalServiceAfter)
            }).Result;
            // 获取主机生命周期管理接口
            var lifetime = app.ApplicationServices.GetRequiredService<IHostApplicationLifetime>();
            // 应用程序终止时，注销服务
            lifetime.ApplicationStopping.Register(() =>
            {
                ConsulHelpers.GetConsulClient().Agent.ServiceDeregister(ConsulServiceOptions.ServiceId).Wait();
            });

            lifetime.ApplicationStarted.Register(() =>
            {
                //开始进行初次健康检查
                using (HttpClient client = new HttpClient())
                {
                    Uri uris = new Uri(new Uri(serviceOptions.ServiceAddress), serviceOptions.HealthCheckRoute);
                    _ = client.GetAsync(uris).Result;
                }
            });
            #endregion
            app.UseHealthChecks(serviceOptions.HealthCheckRoute);
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
