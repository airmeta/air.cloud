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
using Air.Cloud.Core.Standard.ServerCenter;

namespace Air.Cloud.Modules.Consul.Model
{
    /// <summary>
    /// Consul 中的服务注册选项
    /// </summary>
    public class ConsulServerCenterServiceRegisterOptions: IServerCenterServiceRegisterOptions
    {
        /// <summary>
        /// <para>zh-cn:服务地址</para>
        /// <para>en-us:Service address</para>
        /// </summary>
        public string ServiceAddress { get; set; }
        /// <summary>
        /// <para>zh-cn:服务名称</para>
        /// <para>en-us:Service name</para>
        /// </summary>
        public string ServiceName { get; set; }
        /// <summary>
        /// <para>zh-cn:服务键</para>
        /// <para>en-us:ServiceKey</para>
        /// </summary>
        /// <remarks>
        ///  <para>zh-cn:服务有多个实例时,不建议使用PID来生成(默认使用PID),你需要掺入其他的参数,比如机器名,你可以在配置文件中主动指定该值,并在多个实例中保持不通</para> 
        /// </remarks>
        public string ServiceKey { get; set; }
        /// <summary>
        /// <para>zh-cn:健康检查超时时间</para>
        /// <para>en-us:Health check timeout</para>
        /// </summary>
        public TimeSpan Timeout { get; set; }
        /// <summary>
        /// <para>zh-cn:健康检查失败后多久注销服务</para>
        /// <para>en-us:DeregisterCriticalServiceAfter</para>
        /// </summary>
        public TimeSpan DeregisterCriticalServiceAfter { get; set; }
        /// <summary>
        /// <para>zh-cn:健康检查地址</para>
        /// <para>en-us:HealthCheckRoute</para>
        /// </summary>
        public string HealthCheckRoute { get; set; }
        /// <summary>
        /// <para>zh-cn:健康检查步长</para>
        /// <para>en-us:HealthCheckTimeStep</para>
        /// </summary>
        public TimeSpan HealthCheckTimeStep { get; set; }
    }
}
