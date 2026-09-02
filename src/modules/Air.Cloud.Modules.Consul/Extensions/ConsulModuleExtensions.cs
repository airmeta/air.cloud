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
using Air.Cloud.Core.Plugins.LogFiltering;
using Air.Cloud.Core.Plugins.PID;
using Air.Cloud.Core.Standard.AppInject;
using Air.Cloud.Modules.Consul.Dependencies;
using Air.Cloud.Modules.Consul.Model;
using Air.Cloud.Modules.Consul.Plugins;
using Air.Cloud.Modules.Consul.Service;
using Air.Cloud.Modules.Consul.Standard;
using Air.Cloud.Modules.Consul.Util;

using Consul;

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
        /// <para>zh-cn:从 Consul 配置中心加载配置，并按配置完成当前 Web 服务的 Consul 注册流程。</para>
        /// <para>en-us:Loads configuration from the Consul configuration center and completes the current Web service Consul registration flow according to options.</para>
        /// </summary>
        /// <remarks>
        /// <para>zh-cn:该入口保留为兼容的统一启动入口；内部会依次调用配置加载、服务信息注册和健康检查注册三个独立方法。</para>
        /// <para>en-us:This entry point remains the unified startup entry; internally it calls the separate configuration loading, service information registration, and health-check registration methods in order.</para>
        /// </remarks>
        /// <param name="builder">
        /// <para>zh-cn:WebApplication 构建器。</para>
        /// <para>en-us:The WebApplication builder.</para>
        /// </param>
        /// <param name="action">
        /// <para>zh-cn:自定义构建行为</para>
        /// <para>en-us:Configure action</para>
        /// </param>
        /// <returns>
        /// <para>zh-cn:构建完成的 WebApplication。</para>
        /// <para>en-us:The built WebApplication.</para>
        /// </returns>
        public static WebApplication WebInjectInConsul(this WebApplicationBuilder builder, Action<ConsulServiceOptions> action = null)
        {
            return WebInjectInConsul<DefaultConsulServiceOptionsConfigureDependency>(builder,action);
        }

        /// <summary>
        /// <para>zh-cn:使用默认 Consul 配置规整器从 Consul 配置中心加载服务配置，并完成 Air.Cloud 注入初始化。</para>
        /// <para>en-us:Loads service configuration from the Consul configuration center with the default Consul options normalizer and initializes Air.Cloud injection.</para>
        /// </summary>
        /// <param name="builder">
        /// <para>zh-cn:WebApplication 构建器。</para>
        /// <para>en-us:The WebApplication builder.</para>
        /// </param>
        /// <param name="action">
        /// <para>zh-cn:读取配置后执行的自定义配置委托。</para>
        /// <para>en-us:Custom configuration delegate executed after base options are read.</para>
        /// </param>
        /// <returns>
        /// <para>zh-cn:规整后的 Consul 服务配置。</para>
        /// <para>en-us:The normalized Consul service options.</para>
        /// </returns>
        public static ConsulServiceOptions LoadConfigurationFromConsul(this WebApplicationBuilder builder,
            Action<ConsulServiceOptions> action = null)
        {
            return builder.LoadConfigurationFromConsul<DefaultConsulServiceOptionsConfigureDependency>(action);
        }

        /// <summary>
        /// <para>zh-cn:从远程加载配置文件并注册当前服务到Consul</para>
        /// <para>en-us:Load configuration from remote and register the current service to Consul</para>
        /// </summary>
        /// <param name="builder">WebApplication构建器</param>
        /// <param name="action">
        /// <para>zh-cn:自定义构建行为</para>
        /// <para>en-us:Configure action</para>
        /// </param>
        /// <typeparam name="TConsulServiceOptionsConfigureDependency">
        ///  <para>zh-cn:Consul配置构建器</para>
        ///   <para>en-us: Consul options configure</para>
        /// </typeparam>
        /// <returns>
        /// <para>zh-cn:构建完成的 WebApplication。</para>
        /// <para>en-us:The built WebApplication.</para>
        /// </returns>
        public static WebApplication WebInjectInConsul<TConsulServiceOptionsConfigureDependency>(this WebApplicationBuilder builder,
            Action<ConsulServiceOptions> action = null)
            where TConsulServiceOptionsConfigureDependency:class,IConsulServiceOptionsConfigureStandard,new()
        {
            var consulServiceOptions = builder.LoadConfigurationFromConsul<TConsulServiceOptionsConfigureDependency>(action);
            builder.Services.Configure<AppLogFilterOptions>(options => options.IgnorePaths.Add(consulServiceOptions.HealthCheckRoute));
            builder.Services.RegisterConsulHealthCheckServices(consulServiceOptions);
            var app = builder.Build();
            app.RegisterConsulServiceInformation(consulServiceOptions);
            app.RegisterConsulHealthCheck(consulServiceOptions);
            return app;
        }

        /// <summary>
        /// <para>zh-cn:从 Consul 配置中心加载服务配置，并完成 Air.Cloud 注入初始化；当 EnableConfigurationCenter 为 false 时只保留本地配置。</para>
        /// <para>en-us:Loads service configuration from the Consul configuration center and initializes Air.Cloud injection; when EnableConfigurationCenter is false, only local configuration is kept.</para>
        /// </summary>
        /// <param name="builder">
        /// <para>zh-cn:WebApplication 构建器。</para>
        /// <para>en-us:The WebApplication builder.</para>
        /// </param>
        /// <param name="action">
        /// <para>zh-cn:读取配置后执行的自定义配置委托。</para>
        /// <para>en-us:Custom configuration delegate executed after base options are read.</para>
        /// </param>
        /// <typeparam name="TConsulServiceOptionsConfigureDependency">
        /// <para>zh-cn:Consul 配置规整器类型。</para>
        /// <para>en-us:The Consul options normalizer type.</para>
        /// </typeparam>
        /// <returns>
        /// <para>zh-cn:规整后的 Consul 服务配置。</para>
        /// <para>en-us:The normalized Consul service options.</para>
        /// </returns>
        public static ConsulServiceOptions LoadConfigurationFromConsul<TConsulServiceOptionsConfigureDependency>(this WebApplicationBuilder builder,
            Action<ConsulServiceOptions> action = null)
            where TConsulServiceOptionsConfigureDependency : class, IConsulServiceOptionsConfigureStandard, new()
        {
            InitAppInject();
            var consulServiceOptions = GetConsulService<TConsulServiceOptionsConfigureDependency>(action);
            var config = ConfigurationLoader.LoadRemoteConfiguration(consulServiceOptions);
            PrintConfigurationLoadSummary(consulServiceOptions, config);
            if (config.Item2 != null)
            {
                AppConfigurationLoader.SetPublicConfiguration(config.Item2);
                builder.Configuration.AddConfiguration(config.Item2);
            }

            AppConfigurationLoader.SetExternalConfiguration(config.Item1);
            builder.Configuration.AddConfiguration(config.Item1);
            AppRealization.Injection.Inject(builder);
            return consulServiceOptions;
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
        /// <para>zh-cn:从 Consul 配置中心加载 HostBuilder 配置，并执行 Air.Cloud 主机注入流程。</para>
        /// <para>en-us:Loads HostBuilder configuration from the Consul configuration center and runs the Air.Cloud host injection flow.</para>
        /// </summary>
        /// <param name="builder">
        /// <para>zh-cn:Host 构建器。</para>
        /// <para>en-us:The host builder.</para>
        /// </param>
        /// <param name="action">
        /// <para>zh-cn:自定义构建行为</para>
        /// <para>en-us:Configure action</para>
        /// </param>
        /// <returns>
        /// <para>zh-cn:注入后的 Host 构建器。</para>
        /// <para>en-us:The injected host builder.</para>
        /// </returns>
        public static IHostBuilder HostInjectInConsul(this IHostBuilder builder, Action<ConsulServiceOptions> action = null)
        {
            return HostInjectInConsul<DefaultConsulServiceOptionsConfigureDependency>(builder, action);
        }
        /// <summary>
        /// <para>zh-cn:使用指定配置规整器从 Consul 配置中心加载 HostBuilder 配置，并执行 Air.Cloud 主机注入流程。</para>
        /// <para>en-us:Loads HostBuilder configuration from the Consul configuration center with the specified options normalizer and runs the Air.Cloud host injection flow.</para>
        /// </summary>
        /// <param name="builder">
        /// <para>zh-cn:Host 构建器。</para>
        /// <para>en-us:The host builder.</para>
        /// </param>
        /// <param name="action">
        /// <para>zh-cn:自定义构建行为</para>
        /// <para>en-us:Configure action</para>
        /// </param>
        /// <typeparam name="TConsulServiceOptionsConfigureDependency">
        /// <para>zh-cn:Consul 配置规整器类型。</para>
        /// <para>en-us:The Consul options normalizer type.</para>
        /// </typeparam>
        /// <returns>
        /// <para>zh-cn:注入后的 Host 构建器。</para>
        /// <para>en-us:The injected host builder.</para>
        /// </returns>
        public static IHostBuilder HostInjectInConsul<TConsulServiceOptionsConfigureDependency>(this IHostBuilder builder, Action<ConsulServiceOptions> action = null)
             where TConsulServiceOptionsConfigureDependency : class, IConsulServiceOptionsConfigureStandard, new()
        {
            InitAppInject();
            ConsulServiceOptions consulServiceOptions = GetConsulService<TConsulServiceOptionsConfigureDependency>(action);
            var Config = ConfigurationLoader.LoadRemoteConfiguration(consulServiceOptions);
            PrintConfigurationLoadSummary(consulServiceOptions, Config);
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
            builder.ConfigureServices(services =>
            {
                services.Configure<AppLogFilterOptions>(options => options.IgnorePaths.Add(consulServiceOptions.HealthCheckRoute));
            });
            return builder;
        }

        private static void PrintConfigurationLoadSummary(ConsulServiceOptions serviceOptions, (IConfiguration, IConfiguration) configurations)
        {
            if (!serviceOptions.EnableConfigurationCenter) return;
            var serviceConfigurationLocation = $"{serviceOptions.ConfigurationServiceName.Replace(".", "/")}/{AppConst.SystemEnvironmentConfigFileFullName}";
            ConfigurationLoadSummary.Print("Consul", serviceOptions.ConsulAddress, serviceConfigurationLocation, configurations.Item1);

            if (configurations.Item2 != null)
            {
                var commonConfigurationLocation = $"{serviceOptions.CommonConfigFileRoute}/{AppConst.CommonEnvironmentConfigFileFullName}";
                ConfigurationLoadSummary.Print("Consul", serviceOptions.ConsulAddress, commonConfigurationLocation, configurations.Item2);
            }
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
        /// <returns>
        /// <para>zh-cn:原应用管道构建器。</para>
        /// <para>en-us:The original application pipeline builder.</para>
        /// </returns>
        public static IApplicationBuilder RegisterConsulServiceInformation(
                    this IApplicationBuilder app,
                    ConsulServiceOptions serviceOptions)
        {
            if (!ShouldRegisterServiceToConsul(serviceOptions)) return app;
            var ConsulHelpers = new ConsulOperatorHelper(serviceOptions.ConsulAddress);
            var result = ConsulHelpers.InitConsulRegistration(serviceOptions);
            if (!result.Item1) return app;
            ConsulServerCenterDependency dependency = new ConsulServerCenterDependency();
            var r = dependency.RegisterAsync(new ConsulServerCenterServiceRegisterOptions()
            {
                ServiceAddress = serviceOptions.ServiceAddress,
                ServiceName = serviceOptions.RegistryServiceName,
                ServiceKey = ConsulServiceOptions.ServiceId,
                EnableHealthCheck = false,
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

            return app;
        }

        /// <summary>
        /// <para>zh-cn:注册本地健康检查端点，并在允许注册中心注册时把该健康检查关联到 Consul 服务实例。</para>
        /// <para>en-us:Registers the local health-check endpoint and, when registry registration is allowed, associates the health check with the Consul service instance.</para>
        /// </summary>
        /// <param name="app">
        /// <para>zh-cn:应用管道构建器。</para>
        /// <para>en-us:The application pipeline builder.</para>
        /// </param>
        /// <param name="serviceOptions">
        /// <para>zh-cn:Consul 服务配置。</para>
        /// <para>en-us:The Consul service options.</para>
        /// </param>
        /// <returns>
        /// <para>zh-cn:原应用管道构建器。</para>
        /// <para>en-us:The original application pipeline builder.</para>
        /// </returns>
        public static IApplicationBuilder RegisterConsulHealthCheck(
                    this IApplicationBuilder app,
                    ConsulServiceOptions serviceOptions)
        {
            if (AppEnvironment.IsDevelopment || !serviceOptions.EnableHealthCheck) return app;
            app.ApplicationServices.GetService<IAppLogFilterPlugin>()?.AddIgnorePath(serviceOptions.HealthCheckRoute);
            app.UseHealthChecks(serviceOptions.HealthCheckRoute);
            if (!ShouldRegisterServiceToConsul(serviceOptions)) return app;

            var uri = new Uri(serviceOptions.ServiceAddress);
            if (!serviceOptions.HealthCheckRoute.StartsWith("/"))
            {
                serviceOptions.HealthCheckRoute = $"/{serviceOptions.HealthCheckRoute}";
            }

            var helper = new ConsulOperatorHelper(serviceOptions.ConsulAddress);
            helper.GetConsulClient().Agent.CheckRegister(new AgentCheckRegistration
            {
                ID = $"{ConsulServiceOptions.ServiceId}:health",
                Name = $"Service '{serviceOptions.RegistryServiceName}' health check",
                ServiceID = ConsulServiceOptions.ServiceId,
                HTTP = $"{uri.Scheme}://{uri.Host}:{uri.Port}{serviceOptions.HealthCheckRoute}",
                Interval = new TimeSpan(0, 0, serviceOptions.HealthCheckTimeStep),
                Timeout = new TimeSpan(0, 0, serviceOptions.ConnectTimeout),
                DeregisterCriticalServiceAfter = new TimeSpan(0, 0, serviceOptions.DeregisterCriticalServiceAfter)
            }).Wait();

            return app;
        }

        /// <summary>
        /// <para>zh-cn:按健康检查配置注册 ASP.NET Core 健康检查服务；开发环境或关闭健康检查时不会添加额外服务。</para>
        /// <para>en-us:Registers ASP.NET Core health-check services according to options; development mode or disabled health checks add no extra services.</para>
        /// </summary>
        /// <param name="services">
        /// <para>zh-cn:服务集合。</para>
        /// <para>en-us:The service collection.</para>
        /// </param>
        /// <param name="serviceOptions">
        /// <para>zh-cn:Consul 服务配置。</para>
        /// <para>en-us:The Consul service options.</para>
        /// </param>
        /// <returns>
        /// <para>zh-cn:原服务集合。</para>
        /// <para>en-us:The original service collection.</para>
        /// </returns>
        public static IServiceCollection RegisterConsulHealthCheckServices(this IServiceCollection services, ConsulServiceOptions serviceOptions)
        {
            //开发环境剔除此参数
            if (AppEnvironment.IsDevelopment || !serviceOptions.EnableHealthCheck) return services;
            services.AddHealthChecks();
            return services;
        }

        /// <summary>
        /// <para>zh-cn:判断当前进程是否应该向 Consul 注册服务信息；开发环境、关闭服务注册或 DynamicApp 托管进程都会返回 false。</para>
        /// <para>en-us:Determines whether the current process should register service information into Consul; development mode, disabled registration, and DynamicApp-managed processes return false.</para>
        /// </summary>
        /// <param name="serviceOptions">
        /// <para>zh-cn:Consul 服务配置。</para>
        /// <para>en-us:The Consul service options.</para>
        /// </param>
        /// <returns>
        /// <para>zh-cn:true 表示允许注册到 Consul；false 表示跳过注册。</para>
        /// <para>en-us:true means registration into Consul is allowed; false means registration is skipped.</para>
        /// </returns>
        public static bool ShouldRegisterServiceToConsul(ConsulServiceOptions serviceOptions)
        {
            return !AppEnvironment.IsDevelopment
                && serviceOptions.EnableServiceRegistration
                && !serviceOptions.DynamicAppManaged;
        }
    }
}
