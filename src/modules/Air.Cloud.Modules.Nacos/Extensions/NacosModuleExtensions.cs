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
using Air.Cloud.Core.App;
using Air.Cloud.Core.Enums;
using Air.Cloud.Core.Plugins.LogFiltering;
using Air.Cloud.Core.Standard.KVCenter;
using Air.Cloud.Core.Standard.ServerCenter;
using Air.Cloud.Modules.Nacos.Model;
using Air.Cloud.Modules.Nacos.Service;
using Air.Cloud.Modules.Nacos.Util;

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using Nacos.Microsoft.Extensions.Configuration;
using Nacos.V2.DependencyInjection;

namespace Air.Cloud.Modules.Nacos.Extensions
{
    /// <summary>
    /// <para>zh-cn:Nacos 模块扩展方法，提供依赖注册、远程配置加载和服务注册入口。</para>
    /// <para>en-us>Nacos module extension methods for dependency registration, remote configuration loading, and service registration.</para>
    /// </summary>
    public static class NacosModuleExtensions
    {
        /// <summary>
        /// <para>zh-cn:注册 Nacos SDK、Air.Cloud 服务中心标准和 KV 中心标准实现。</para>
        /// <para>en-us>Registers Nacos SDK plus Air.Cloud server-center and KV-center standard implementations.</para>
        /// </summary>
        /// <param name="services">
        /// <para>zh-cn:服务集合。</para>
        /// <para>en-us>The service collection.</para>
        /// </param>
        /// <param name="configuration">
        /// <para>zh-cn:应用配置，用于绑定 NacosServiceOptions。</para>
        /// <para>en-us>The application configuration used to bind NacosServiceOptions.</para>
        /// </param>
        /// <param name="action">
        /// <para>zh-cn:调用方自定义配置委托，会在绑定配置后执行。</para>
        /// <para>en-us>The caller customization delegate, executed after configuration binding.</para>
        /// </param>
        /// <returns>
        /// <para>zh-cn:原始服务集合，便于链式调用。</para>
        /// <para>en-us>The original service collection for chaining.</para>
        /// </returns>
        public static IServiceCollection AddNacosModule(this IServiceCollection services, IConfiguration configuration, Action<NacosServiceOptions> action = null)
        {
            var serviceOptions = GetNacosServiceOptions(configuration, action);
            services.AddSingleton(serviceOptions);
            services.Configure<AppLogFilterOptions>(options => options.IgnorePaths.Add(serviceOptions.HealthCheckRoute));
            services.AddNacosV2Config(NacosOptionsBuilder.Build(serviceOptions));
            services.AddNacosV2Naming(NacosOptionsBuilder.Build(serviceOptions));
            services.AddTransient<IServerCenterStandard, NacosServerCenterDependency>();
            services.AddTransient<IKVCenterStandard, NacosKVCenterDependency>();
            return services;
        }

        /// <summary>
        /// <para>zh-cn:从 Nacos 远程加载配置，并完成 Nacos 依赖注册和 WebApplication 构建。</para>
        /// <para>en-us>Loads remote configuration from Nacos, then registers Nacos dependencies and builds the WebApplication.</para>
        /// </summary>
        /// <param name="builder">
        /// <para>zh-cn:WebApplication 构建器。</para>
        /// <para>en-us>The WebApplication builder.</para>
        /// </param>
        /// <param name="action">
        /// <para>zh-cn:调用方自定义 Nacos 配置。</para>
        /// <para>en-us>The caller customization for Nacos options.</para>
        /// </param>
        /// <returns>
        /// <para>zh-cn:构建后的 WebApplication。</para>
        /// <para>en-us>The built WebApplication.</para>
        /// </returns>
        public static WebApplication WebInjectInNacos(this WebApplicationBuilder builder, Action<NacosServiceOptions> action = null)
        {
            var serviceOptions = GetNacosServiceOptions(builder.Configuration, action);
            LoadRemoteConfiguration(builder.Configuration, serviceOptions);
            builder.Services.AddNacosModule(builder.Configuration, options => CopyOptions(serviceOptions, options));
            AppConst.LoadConfigurationTypeEnum = LoadConfigurationTypeEnum.Remote;
            return builder.Build();
        }

        /// <summary>
        /// <para>zh-cn:为通用主机从 Nacos 远程加载配置，并注册 Nacos 模块依赖。</para>
        /// <para>en-us>Loads remote configuration from Nacos for a generic host and registers Nacos module dependencies.</para>
        /// </summary>
        /// <param name="builder">
        /// <para>zh-cn:主机构建器。</para>
        /// <para>en-us>The host builder.</para>
        /// </param>
        /// <param name="action">
        /// <para>zh-cn:调用方自定义 Nacos 配置。</para>
        /// <para>en-us>The caller customization for Nacos options.</para>
        /// </param>
        /// <returns>
        /// <para>zh-cn:原始主机构建器，便于链式调用。</para>
        /// <para>en-us>The original host builder for chaining.</para>
        /// </returns>
        public static IHostBuilder HostInjectInNacos(this IHostBuilder builder, Action<NacosServiceOptions> action = null)
        {
            builder.ConfigureAppConfiguration((_, configurationBuilder) =>
            {
                var root = configurationBuilder.Build();
                var serviceOptions = GetNacosServiceOptions(root, action);
                LoadRemoteConfiguration(configurationBuilder, serviceOptions);
            });

            builder.ConfigureServices((context, services) =>
            {
                services.AddNacosModule(context.Configuration, action);
            });

            AppConst.LoadConfigurationTypeEnum = LoadConfigurationTypeEnum.Remote;
            return builder;
        }

        private static NacosServiceOptions GetNacosServiceOptions(IConfiguration configuration, Action<NacosServiceOptions> action)
        {
            var serviceOptions = configuration.GetConfig<NacosServiceOptions>() ?? new NacosServiceOptions();
            action?.Invoke(serviceOptions);
            return serviceOptions;
        }

        private static void LoadRemoteConfiguration(IConfigurationBuilder builder, NacosServiceOptions serviceOptions)
        {
            var configuration = BuildNacosConfigurationSection(serviceOptions);
            builder.AddNacosV2Configuration(configuration.GetSection("NacosConfig"));
        }

        private static IConfiguration BuildNacosConfigurationSection(NacosServiceOptions serviceOptions)
        {
            var data = new Dictionary<string, string>
            {
                ["NacosConfig:ContextPath"] = string.IsNullOrWhiteSpace(serviceOptions.ContextPath) ? "nacos" : serviceOptions.ContextPath,
                ["NacosConfig:DefaultTimeOut"] = serviceOptions.DefaultTimeOut.ToString(),
                ["NacosConfig:Namespace"] = serviceOptions.Namespace ?? string.Empty,
                ["NacosConfig:UserName"] = serviceOptions.UserName ?? string.Empty,
                ["NacosConfig:Password"] = serviceOptions.Password ?? string.Empty,
                ["NacosConfig:Listeners:0:DataId"] = string.IsNullOrWhiteSpace(serviceOptions.ConfigDataId) ? AppConst.SystemEnvironmentConfigFileFullName : serviceOptions.ConfigDataId,
                ["NacosConfig:Listeners:0:Group"] = serviceOptions.ConfigGroup,
                ["NacosConfig:Listeners:0:Optional"] = "true"
            };

            var index = 0;
            foreach (var address in GetServerAddresses(serviceOptions))
            {
                data[$"NacosConfig:ServerAddresses:{index}"] = address;
                index++;
            }

            if (serviceOptions.EnableCommonConfig)
            {
                data["NacosConfig:Listeners:1:DataId"] = serviceOptions.CommonConfigDataId;
                data["NacosConfig:Listeners:1:Group"] = serviceOptions.ConfigGroup;
                data["NacosConfig:Listeners:1:Optional"] = "true";
            }

            return new ConfigurationBuilder().AddInMemoryCollection(data).Build();
        }

        private static IEnumerable<string> GetServerAddresses(NacosServiceOptions serviceOptions)
        {
            foreach (var address in serviceOptions.ServerAddresses ?? Array.Empty<string>())
            {
                if (!string.IsNullOrWhiteSpace(address))
                {
                    yield return address;
                }
            }

            if (!string.IsNullOrWhiteSpace(serviceOptions.ServerAddress))
            {
                yield return serviceOptions.ServerAddress;
            }
        }

        private static void CopyOptions(NacosServiceOptions source, NacosServiceOptions target)
        {
            target.ServerAddresses = source.ServerAddresses?.ToList() ?? new List<string>();
            target.ServerAddress = source.ServerAddress;
            target.Namespace = source.Namespace;
            target.ContextPath = source.ContextPath;
            target.UserName = source.UserName;
            target.Password = source.Password;
            target.ConfigGroup = source.ConfigGroup;
            target.ServiceGroup = source.ServiceGroup;
            target.ConfigTimeoutMs = source.ConfigTimeoutMs;
            target.DefaultTimeOut = source.DefaultTimeOut;
            target.ServiceName = source.ServiceName;
            target.ServiceAddress = source.ServiceAddress;
            target.HealthCheckRoute = source.HealthCheckRoute;
            target.ClusterName = source.ClusterName;
            target.Weight = source.Weight;
            target.Ephemeral = source.Ephemeral;
            target.EnableServiceRegister = source.EnableServiceRegister;
            target.EnableCommonConfig = source.EnableCommonConfig;
            target.CommonConfigDataId = source.CommonConfigDataId;
            target.ConfigDataId = source.ConfigDataId;
        }
    }
}
