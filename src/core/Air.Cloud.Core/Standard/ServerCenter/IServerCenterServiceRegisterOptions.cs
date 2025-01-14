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

namespace Air.Cloud.Core.Standard.ServerCenter
{
    /// <summary>
    /// <para>zh-cn:服务注册配置</para>
    /// <para>en-us:Server register options</para>
    /// </summary>
    public interface IServerCenterServiceRegisterOptions
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
        /// 服务唯一编号
        /// </summary>
        public string ServiceKey { get; set; }
        /// <summary>
        /// 注册超时时间
        /// </summary>
        public TimeSpan Timeout { get; set; }

        #region HealthCheck
        /// <summary>
        /// 服务注销多久后删除
        /// </summary>
        public TimeSpan DeregisterCriticalServiceAfter { get; set; }

        /// <summary>
        /// 健康检查路由地址
        /// </summary>
        public string HealthCheckRoute { get; set; }

        /// <summary>
        /// 健康检查时间间隔
        /// </summary>
        public TimeSpan HealthCheckTimeStep { get; set; }
        #endregion

    }
}
