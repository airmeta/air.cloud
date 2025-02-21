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
using Air.Cloud.Core.Standard.ServerCenter;
using Air.Cloud.Modules.Consul.Model;

using Consul;

namespace Air.Cloud.Modules.Consul.Service
{
    public class ConsulServerCenterDependency : IServerCenterStandard
    {

        public static ConsulClient ConsulClient { get; set; }

        static ConsulServerCenterDependency()
        {
            var serviceOptions = AppConfigurationLoader.InnerConfiguration.GetConfig<ConsulServiceOptions>();
            ConsulClient = new ConsulClient(configuration =>
            {
                //服务注册的地址，集群中任意一个地址
                configuration.Address = new Uri(serviceOptions.ConsulAddress);
            });
        }
        /// <inheritdoc/>
        public async Task<IList<T>> QueryAsync<T>() where T : IServerCenterServiceOptions, new()
        {
            var services = await ConsulClient.Catalog.Services();
            if (services.StatusCode == System.Net.HttpStatusCode.OK)
            {
                return (IList<T>)services.Response.Select(s =>
                {
                    return new ConsulServerCenterServiceOptions()
                    {
                        ServiceKey = s.Key,
                        ServiceName = s.Key.ToString(),
                        ServiceValues = s.Value
                    };
                }).ToList();
            }
            return new List<T>();
        }
        /// <inheritdoc/>
        public async Task<object> GetAsync(string Key)
        {
            var services = await ConsulClient.Catalog.Service(Key);
            return new
            {
                services.AddressTranslationEnabled,
                services.KnownLeader,
                services.LastContact,
                services.LastIndex,
                services.RequestTime,
                ServerDetails = services.Response.Select(s => new ServerDetailOptions
                {
                    ServicePort = s.ServicePort,
                    ServiceID = s.ServiceID,
                    ServiceName = s.ServiceName,
                    ServiceTags = s.ServiceTags,
                    Address = s.Address,
                    Node = s.Node,
                    ServiceAddress = s.ServiceAddress,
                    ServiceEnableTagOverride = s.ServiceEnableTagOverride,
                    ServiceMeta = s.ServiceMeta,
                    ServiceTaggedAddresses = s.ServiceTaggedAddresses?.Select(ss => new KeyValuePair<string, string>(ss.Key, ss.Value.Address + ":" + ss.Value.Port)).ToList()
                })
            };
        }
        /// <inheritdoc/>
        public async Task<bool> RegisterAsync<T>(T serverCenterServiceInformation) where T : class, IServerCenterServiceRegisterOptions, new()
        {
            #region  注册服务
            //组装请求信息
            var uri = new Uri(serverCenterServiceInformation.ServiceAddress);
            if (!serverCenterServiceInformation.HealthCheckRoute.IsNullOrEmpty())
                serverCenterServiceInformation.HealthCheckRoute = serverCenterServiceInformation.HealthCheckRoute.StartsWith("/") ? serverCenterServiceInformation.HealthCheckRoute : $"/{serverCenterServiceInformation.HealthCheckRoute}";
            // 节点服务注册对象
            var registration = new AgentServiceRegistration()
            {
                ID = serverCenterServiceInformation.ServiceKey,
                Name = serverCenterServiceInformation.ServiceName,// 服务名
                Address = uri.Host,
                Port = uri.Port, // 服务端口
                Check = new AgentServiceCheck
                {
                    // 注册超时
                    Timeout = serverCenterServiceInformation.Timeout,
                    // 服务停止多久后注销服务
                    DeregisterCriticalServiceAfter = serverCenterServiceInformation.DeregisterCriticalServiceAfter,
                    // 健康检查地址
                    HTTP = $"{uri.Scheme}://{uri.Host}:{uri.Port}{serverCenterServiceInformation.HealthCheckRoute}",
                    // 健康检查时间间隔
                    Interval = serverCenterServiceInformation.HealthCheckTimeStep,
                }
            };

            // 注册服务
            await ConsulClient.Agent.ServiceRegister(registration);
            #endregion
            return true;
        }
        /// <inheritdoc/>
        public async Task<bool> Unregister(string ServiceId)
        {
            await ConsulClient.Agent.ServiceDeregister(ServiceId);
            return true;
        }
    }
}
