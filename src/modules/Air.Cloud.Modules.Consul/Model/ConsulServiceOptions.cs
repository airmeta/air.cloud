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
using Air.Cloud.Core.Attributes;

using crypto;

namespace Air.Cloud.Modules.Consul.Model
{
    /// <summary>
    /// <para>zh-cn:Consul 服务配置</para>
    /// <para>en-us: Consul service config options</para>
    /// </summary>
    /// <remarks>
    ///  <para>zh-cn:Consul 模块当前未支持ACL认证 因为通常来说这个服务是在内网中进行安装部署,防火墙向集群内机器IP开放即可,可以最大程度降低安全风险,如需开启,则需要你进行相关支持开发</para>
    ///  <para>en-us: The Consul modules currently does not support ACL authentication because it is usually installed and deployed within the intranet, and the firewall can be opened to the IP addresses of machines in the cluster to minimize security risks.If you need to enable it, you will need to provide relevant support and development</para>
    /// </remarks>
    [ConfigurationInfo("ConsulServiceOptions")]
    public class ConsulServiceOptions
    {
        #region  Consul配置(ConsulConfig)
        /// <summary>
        /// <para>zh-cn:Consul地址</para>
        /// <para>en-us:Consul address</para>
        /// </summary>
        public string ConsulAddress { get; set; }

        #endregion

        #region 服务配置(ServiceConfig)
        /// <summary>
        /// <para>zh-cn:当前服务ID</para>
        /// <para>en-us:Current service id</para>
        /// </summary>
        /// <remarks>
        ///  <para>zh-cn:这个ID为PID,只有这样才能保持唯一</para>
        ///  <para>en-us: This id is same as PID</para>
        /// </remarks>
        public string ServiceId { get; set; } = AppRealization.PID.Get();
        /// <summary>
        /// <para>zh-cn:服务名称</para>
        /// <para>en-us:Service name</para>
        /// </summary>
        public string ServiceName { get; set; } = null;

        /// <summary>
        /// <para>zh-cn:服务地址</para>
        /// <para>en-us:Service address</para>
        /// </summary>
        public string ServiceAddress { get; set; } = null;

        /// <summary>
        /// <para>zh-cn:是否服务名忽略项</para>
        /// <para>en-us:Ignore service key</para>
        /// </summary>
        public bool IsIgnoreServiceNameKey { get; internal set; } = true;

        /// <summary>
        /// <para>zh-cn:忽略项名称</para>
        /// <para>en-us:IgnoreKey</para>
        /// </summary>
        public string  IgnoreKey { get; internal set; } = ".Entry";

        #endregion

        #region 连接配置(ConnectConfig)
        /// <summary>
        /// <para>zh-cn:注册超时时间 (秒)</para>
        /// <para>en-us:</para>
        /// </summary>
        public int ConnectTimeout { get; set; } = 5;

        /// <summary>
        /// <para>zh-cn:服务停止多久后注销服务 (秒)</para>
        /// <para>en-us:How long does it take to cancel the service after it stops(seconds)</para>
        /// </summary>
        public int DeregisterCriticalServiceAfter { get; set; } = 5;

        #endregion

        #region 健康检查配置(HealthCheckConfig)
        /// <summary>
        /// <para>zh-cn:健康检查地址</para>
        /// <para>en-us:Health check route</para>
        /// </summary>
        public string HealthCheckRoute { get; set; } = "/Health";
        /// <summary>
        /// <para>zh-cn:健康检查间隔 (秒)</para>
        /// <para>en-us:Health check time step(seconds)</para>
        /// </summary>
        public int HealthCheckTimeStep { get; set; } = 5;

        #endregion

        #region  公共配置文件配置(CommonOptionsConfig)
        /// <summary>
        /// <para>zh-cn:是否加载公共配置文件</para>
        /// <para>en-is:Should universal configuration be loaded</para>
        /// </summary>
        public bool EnableCommonConfig { get; set; } = true;
        /// <summary>
        /// <para>zh-cn:公共配置文件路由地址</para>
        /// <para>en-us:Common config file route</para>
        /// </summary>
        /// <remarks>
        ///  公共配置文件路由地址在Common路径下
        /// </remarks>
        public string CommonConfigFileRoute { get; set; } = "Common";
        #endregion

    }
}
