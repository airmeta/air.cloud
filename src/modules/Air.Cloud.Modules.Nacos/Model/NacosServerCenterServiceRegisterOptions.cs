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
    /// <para>zh-cn:Nacos 服务注册选项。</para>
    /// <para>en-us>Nacos service registration options.</para>
    /// </summary>
    public class NacosServerCenterServiceRegisterOptions : IServerCenterServiceRegisterOptions
    {
        /// <inheritdoc/>
        public string ServiceAddress { get; set; }

        /// <inheritdoc/>
        public string ServiceName { get; set; }

        /// <inheritdoc/>
        public string ServiceKey { get; set; }

        /// <inheritdoc/>
        public TimeSpan Timeout { get; set; }

        /// <inheritdoc/>
        public TimeSpan DeregisterCriticalServiceAfter { get; set; }

        /// <inheritdoc/>
        public string HealthCheckRoute { get; set; }

        /// <inheritdoc/>
        public TimeSpan HealthCheckTimeStep { get; set; }

        /// <summary>
        /// <para>zh-cn:Nacos 服务分组。</para>
        /// <para>en-us>The Nacos service group.</para>
        /// </summary>
        public string GroupName { get; set; }

        /// <summary>
        /// <para>zh-cn:Nacos 集群名称。</para>
        /// <para>en-us>The Nacos cluster name.</para>
        /// </summary>
        public string ClusterName { get; set; }

        /// <summary>
        /// <para>zh-cn:实例权重。</para>
        /// <para>en-us>The instance weight.</para>
        /// </summary>
        public double Weight { get; set; } = 1D;

        /// <summary>
        /// <para>zh-cn:是否临时实例。</para>
        /// <para>en-us>Whether the instance is ephemeral.</para>
        /// </summary>
        public bool Ephemeral { get; set; } = true;

        /// <summary>
        /// <para>zh-cn:实例元数据。</para>
        /// <para>en-us>The instance metadata.</para>
        /// </summary>
        public IDictionary<string, string> Metadata { get; set; } = new Dictionary<string, string>();
    }
}
