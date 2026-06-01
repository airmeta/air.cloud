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
using Air.Cloud.Modules.Consul.Model;

using Consul;

using System.Net;

namespace Air.Cloud.Modules.Consul.Service
{
    /// <summary>
    /// <para>zh-cn:Consul 操作辅助类，负责创建 Consul 客户端并校验服务注册信息。</para>
    /// <para>en-us:Consul operation helper that creates the Consul client and validates service registration information.</para>
    /// </summary>
    public class ConsulOperatorHelper
    {
        /// <summary>
        /// <para>zh-cn:当前 Consul 客户端实例，用于访问 Consul 服务发现和健康检查接口。</para>
        /// <para>en-us:Current Consul client instance used to access service discovery and health check APIs.</para>
        /// </summary>
        public static ConsulClient ConsulClient = null;

        /// <summary>
        /// <para>zh-cn:使用 Consul 集群地址初始化操作辅助类，并创建 Consul 客户端。</para>
        /// <para>en-us:Initializes the helper with a Consul cluster address and creates the Consul client.</para>
        /// </summary>
        /// <param name="ConsulClusterAddress">
        /// <para>zh-cn:Consul 集群中任意可访问节点的地址。</para>
        /// <para>en-us:The address of any reachable node in the Consul cluster.</para>
        /// </param>
        public ConsulOperatorHelper(string ConsulClusterAddress)
        {
            ConsulClient = new ConsulClient(configuration =>
            {
                //服务注册的地址，集群中任意一个地址
                configuration.Address = new Uri(ConsulClusterAddress);
            });
        }

        /// <summary>
        /// <para>zh-cn:获取当前 Consul 客户端实例。</para>
        /// <para>en-us:Gets the current Consul client instance.</para>
        /// </summary>
        /// <returns>
        /// <para>zh-cn:用于访问 Consul 集群的客户端。</para>
        /// <para>en-us:The client used to access the Consul cluster.</para>
        /// </returns>
        public ConsulClient GetConsulClient()
        {
            return ConsulClient;
        }

        /// <summary>
        /// <para>zh-cn:初始化服务注册前的 Consul 检查，验证服务地址和服务标识是否与集群中已有实例冲突。</para>
        /// <para>en-us:Initializes Consul checks before service registration and validates whether the service address or identifier conflicts with existing cluster instances.</para>
        /// </summary>
        /// <param name="serviceOptions">
        /// <para>zh-cn:待注册服务的 Consul 配置。</para>
        /// <para>en-us:The Consul options for the service to register.</para>
        /// </param>
        /// <returns>
        /// <para>zh-cn:检查结果以及可能被修正后的服务配置。</para>
        /// <para>en-us:The validation result and the service options that may have been adjusted.</para>
        /// </returns>
        public Tuple<bool, ConsulServiceOptions> InitConsulRegistration(ConsulServiceOptions serviceOptions)
        {
            #region  检查Consul服务集群
            var services = ConsulClient.Health.Service(serviceOptions.ServiceName, null, false, null).Result.Response;//健康的服务

            #region 1.检查注册的IP地址是否冲突
            var serviceUrls = services.Select(p => new
            {
                Address = $"http://{p.Service.Address + ":" + p.Service.Port}",
                p.Service
            }).ToList();
            bool CheckSuccess = true;
            //如果有相同IP地址的服务,则不注册
            if (serviceUrls.Count(s => serviceOptions.ServiceAddress.Contains(s.Address) || s.Address.Contains(serviceOptions.ServiceAddress)) > 0)
            {
                var ServicesInfo = serviceUrls.FirstOrDefault(s => s.Service.ID == ConsulServiceOptions.ServiceId);
                //排除自己重启的情况
                if (ServicesInfo != null && ServicesInfo.Service.ID != ConsulServiceOptions.ServiceId)
                {
                    CheckSuccess = false;
                }
            }
            if (!CheckSuccess)
            {
                AppRealization.Output.Error(new HttpListenerException(1219,"当前目录下的应用程序IP地址已被使用"));
            }
            #endregion

            #region 2.检测当前ID 是否在集群里面冲突 冲突(指的是当前ID绑定了其他端口的项目 并且那个项目正在使用)
            if (serviceUrls.Count(s => s.Service.ID == ConsulServiceOptions.ServiceId) > 0)
            {
                var ServicesInfo = serviceUrls.FirstOrDefault(s => s.Service.ID == ConsulServiceOptions.ServiceId);
                //判断是否为自己重启 重启不更换PID
                if (!(serviceOptions.ServiceAddress.Contains(ServicesInfo.Address) 
                    || ServicesInfo.Address.Contains(serviceOptions.ServiceAddress)))
                {
                    //表示ID冲突 需要重新生成
                    //出现这个情况是因为 该项目文件是在已注册的服务环境复制出来的 需要进行更换
                    ConsulServiceOptions.ServiceId = AppRealization.PID.Get();
                }
            }
            #endregion

            #endregion
            return new Tuple<bool, ConsulServiceOptions>(true, serviceOptions);
        }
    }
}
