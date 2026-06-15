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
using Air.Cloud.Core.Standard.ServerCenter;

namespace Air.Cloud.Modules.Nacos.Model
{
    /// <summary>
    /// <para>zh-cn:Nacos 服务中心服务选项。</para>
    /// <para>en-us>Nacos service-center service options.</para>
    /// </summary>
    public class NacosServerCenterServiceOptions : IServerCenterServiceOptions
    {
        /// <inheritdoc/>
        public string ServiceAddress { get; set; }

        /// <inheritdoc/>
        public string ServiceName { get; set; }

        /// <inheritdoc/>
        public string ServiceKey { get; set; }

        /// <inheritdoc/>
        public string[] ServiceValues { get; set; }

        /// <summary>
        /// <para>zh-cn:服务详细信息。</para>
        /// <para>en-us>The service detail collection.</para>
        /// </summary>
        public IList<NacosServerDetailOptions> ServerDetails { get; set; } = new List<NacosServerDetailOptions>();
    }

    /// <summary>
    /// <para>zh-cn:Nacos 服务实例详细信息。</para>
    /// <para>en-us>Nacos service instance details.</para>
    /// </summary>
    public class NacosServerDetailOptions
    {
        /// <summary>
        /// <para>zh-cn:实例标识。</para>
        /// <para>en-us>The instance identifier.</para>
        /// </summary>
        public string ServiceID { get; set; }

        /// <summary>
        /// <para>zh-cn:服务名称。</para>
        /// <para>en-us>The service name.</para>
        /// </summary>
        public string ServiceName { get; set; }

        /// <summary>
        /// <para>zh-cn:服务地址。</para>
        /// <para>en-us>The service address.</para>
        /// </summary>
        public string ServiceAddress { get; set; }

        /// <summary>
        /// <para>zh-cn:服务端口。</para>
        /// <para>en-us>The service port.</para>
        /// </summary>
        public int ServicePort { get; set; }

        /// <summary>
        /// <para>zh-cn:集群名称。</para>
        /// <para>en-us>The cluster name.</para>
        /// </summary>
        public string ClusterName { get; set; }

        /// <summary>
        /// <para>zh-cn:实例权重。</para>
        /// <para>en-us>The instance weight.</para>
        /// </summary>
        public double Weight { get; set; }

        /// <summary>
        /// <para>zh-cn:是否健康。</para>
        /// <para>en-us>Whether the instance is healthy.</para>
        /// </summary>
        public bool Healthy { get; set; }

        /// <summary>
        /// <para>zh-cn:是否启用。</para>
        /// <para>en-us>Whether the instance is enabled.</para>
        /// </summary>
        public bool Enabled { get; set; }

        /// <summary>
        /// <para>zh-cn:是否临时实例。</para>
        /// <para>en-us>Whether the instance is ephemeral.</para>
        /// </summary>
        public bool Ephemeral { get; set; }

        /// <summary>
        /// <para>zh-cn:实例元数据。</para>
        /// <para>en-us>The instance metadata.</para>
        /// </summary>
        public IDictionary<string, string> ServiceMeta { get; set; }
    }
}
