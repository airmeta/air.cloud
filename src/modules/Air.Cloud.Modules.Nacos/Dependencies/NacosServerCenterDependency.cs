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
using Air.Cloud.Core.Extensions;
using Air.Cloud.Core.Plugins.LogFiltering;
using Air.Cloud.Core.Standard.ServerCenter;
using Air.Cloud.Modules.Nacos.Model;

using Mapster;

using Nacos.V2;
using Nacos.V2.Naming.Dtos;

namespace Air.Cloud.Modules.Nacos.Service
{
    /// <summary>
    /// <para>zh-cn:Nacos 服务中心标准实现，负责服务注册、发现和按服务名读取实例详情。</para>
    /// <para>en-us>Nacos server-center standard implementation for service registration, discovery, and service-name detail lookup.</para>
    /// </summary>
    public class NacosServerCenterDependency : IServerCenterStandard
    {
        private readonly INacosNamingService _namingService;
        private readonly NacosServiceOptions _serviceOptions;
        private readonly IAppLogFilterPlugin _logFilter;

        /// <summary>
        /// <para>zh-cn:创建 Nacos 服务中心实现，依赖官方 Nacos Naming 客户端和 Air.Cloud 配置。</para>
        /// <para>en-us>Creates a Nacos server-center implementation with the official Nacos naming client and Air.Cloud options.</para>
        /// </summary>
        /// <param name="namingService">
        /// <para>zh-cn:Nacos 服务发现客户端。</para>
        /// <para>en-us>The Nacos naming service client.</para>
        /// </param>
        /// <param name="logFilter">
        /// <para>zh-cn:Air.Cloud 日志过滤插件，用于登记最终健康检查路由；为空时会尝试从框架服务容器读取。</para>
        /// <para>en-us>The Air.Cloud log filtering plugin used to register the final health-check route; when null, the framework service container is used as a fallback.</para>
        /// </param>
        public NacosServerCenterDependency(INacosNamingService namingService, IAppLogFilterPlugin logFilter = null)
        {
            _namingService = namingService;
            _logFilter = logFilter;
            _serviceOptions = AppConfigurationLoader.InnerConfiguration.GetConfig<NacosServiceOptions>() ?? new NacosServiceOptions();
        }

        /// <inheritdoc/>
        public async Task<IList<T>> QueryAsync<T>() where T : IServerCenterServiceOptions, new()
        {
            var services = await _namingService.GetServicesOfServer(1, 1000, NormalizeGroup(_serviceOptions.ServiceGroup));
            return services.Data.Select(serviceName => new NacosServerCenterServiceOptions
            {
                ServiceName = serviceName,
                ServiceKey = serviceName,
                ServiceValues = new[] { serviceName }
            }.Adapt<T>()).ToList();
        }

        /// <inheritdoc/>
        public async Task<object> GetAsync(string Key)
        {
            var instances = await _namingService.GetAllInstances(Key, NormalizeGroup(_serviceOptions.ServiceGroup));
            return new NacosServerCenterServiceOptions
            {
                ServiceName = Key,
                ServiceKey = Key,
                ServiceValues = instances.Select(GetInstanceId).ToArray(),
                ServerDetails = instances.Select(instance => new NacosServerDetailOptions
                {
                    ServiceID = GetInstanceId(instance),
                    ServiceName = Key,
                    ServiceAddress = instance.Ip,
                    ServicePort = instance.Port,
                    ClusterName = instance.ClusterName,
                    Weight = instance.Weight,
                    Healthy = instance.Healthy,
                    Enabled = instance.Enabled,
                    Ephemeral = instance.Ephemeral,
                    ServiceMeta = instance.Metadata
                }).ToList()
            };
        }

        /// <inheritdoc/>
        public async Task<bool> RegisterAsync<T>(T serverCenterServiceInformation) where T : class, IServerCenterServiceRegisterOptions, new()
        {
            var uri = new Uri(serverCenterServiceInformation.ServiceAddress);
            if (serverCenterServiceInformation.HealthCheckRoute.IsNullOrEmpty())
            {
                serverCenterServiceInformation.HealthCheckRoute = _serviceOptions.HealthCheckRoute;
            }

            if (!serverCenterServiceInformation.HealthCheckRoute.IsNullOrEmpty())
            {
                serverCenterServiceInformation.HealthCheckRoute = serverCenterServiceInformation.HealthCheckRoute.StartsWith("/")
                    ? serverCenterServiceInformation.HealthCheckRoute
                    : $"/{serverCenterServiceInformation.HealthCheckRoute}";
            }

            (_logFilter ?? AppCore.GetService<IAppLogFilterPlugin>())?.AddIgnorePath(serverCenterServiceInformation.HealthCheckRoute);

            var groupName = serverCenterServiceInformation is NacosServerCenterServiceRegisterOptions nacosOptions
                ? NormalizeGroup(nacosOptions.GroupName)
                : NormalizeGroup(_serviceOptions.ServiceGroup);

            var instance = new Instance
            {
                InstanceId = serverCenterServiceInformation.ServiceKey,
                Ip = uri.Host,
                Port = uri.Port,
                ServiceName = serverCenterServiceInformation.ServiceName,
                ClusterName = serverCenterServiceInformation is NacosServerCenterServiceRegisterOptions registerOptions && !string.IsNullOrWhiteSpace(registerOptions.ClusterName)
                    ? registerOptions.ClusterName
                    : _serviceOptions.ClusterName,
                Weight = serverCenterServiceInformation is NacosServerCenterServiceRegisterOptions weightOptions ? weightOptions.Weight : _serviceOptions.Weight,
                Healthy = true,
                Enabled = true,
                Ephemeral = serverCenterServiceInformation is NacosServerCenterServiceRegisterOptions ephemeralOptions ? ephemeralOptions.Ephemeral : _serviceOptions.Ephemeral,
                Metadata = BuildMetadata(serverCenterServiceInformation)
            };

            await _namingService.RegisterInstance(serverCenterServiceInformation.ServiceName, groupName, instance);
            return true;
        }

        /// <summary>
        /// <para>zh-cn:从 Nacos 注销指定服务实例。实例不存在时 SDK 按 Nacos 服务端语义处理，不额外抛出业务异常。</para>
        /// <para>en-us>Deregisters a service instance from Nacos. Missing instances are handled by the SDK and Nacos server semantics without extra business exceptions.</para>
        /// </summary>
        /// <param name="serviceName">
        /// <para>zh-cn:服务名称。</para>
        /// <para>en-us>The service name.</para>
        /// </param>
        /// <param name="serviceId">
        /// <para>zh-cn:实例标识。</para>
        /// <para>en-us>The service instance identifier.</para>
        /// </param>
        /// <param name="serviceAddress">
        /// <para>zh-cn:服务地址，用于解析实例 IP 和端口。</para>
        /// <para>en-us>The service address used to resolve instance IP and port.</para>
        /// </param>
        /// <param name="groupName">
        /// <para>zh-cn:Nacos 服务分组。</para>
        /// <para>en-us>The Nacos service group.</para>
        /// </param>
        /// <returns>
        /// <para>zh-cn:注销是否已提交。</para>
        /// <para>en-us>Whether deregistration was submitted.</para>
        /// </returns>
        public async Task<bool> UnregisterAsync(string serviceName, string serviceId, string serviceAddress, string groupName = null)
        {
            var uri = new Uri(serviceAddress);
            var instance = new Instance
            {
                InstanceId = serviceId,
                Ip = uri.Host,
                Port = uri.Port,
                ServiceName = serviceName
            };
            await _namingService.DeregisterInstance(serviceName, NormalizeGroup(groupName), instance);
            return true;
        }

        private string NormalizeGroup(string group)
        {
            return string.IsNullOrWhiteSpace(group) ? _serviceOptions.ServiceGroup : group;
        }

        private static Dictionary<string, string> BuildMetadata(IServerCenterServiceRegisterOptions registerOptions)
        {
            var metadata = registerOptions is NacosServerCenterServiceRegisterOptions nacosOptions
                ? new Dictionary<string, string>(nacosOptions.Metadata ?? new Dictionary<string, string>())
                : new Dictionary<string, string>();

            if (!string.IsNullOrWhiteSpace(registerOptions.ServiceKey))
            {
                metadata["ServiceKey"] = registerOptions.ServiceKey;
            }

            if (!string.IsNullOrWhiteSpace(registerOptions.HealthCheckRoute))
            {
                metadata["HealthCheckRoute"] = registerOptions.HealthCheckRoute;
            }

            return metadata;
        }

        private static string GetInstanceId(Instance instance)
        {
            if (!string.IsNullOrWhiteSpace(instance.InstanceId))
            {
                return instance.InstanceId;
            }

            if (instance.Metadata != null && instance.Metadata.TryGetValue("ServiceKey", out var serviceKey))
            {
                return serviceKey;
            }

            return $"{instance.Ip}:{instance.Port}";
        }
    }
}
