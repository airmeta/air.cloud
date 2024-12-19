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
    /// Consul 中的服务中心服务选项
    /// </summary>
    public class ConsulServerCenterServiceOptions : IServerCenterServiceOptions
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
        /// <para>zh-cn:服务详细信息</para>
        /// <para>en-us:Server details</para>
        /// </summary>
        public IList<object> ServerDetails { get; set; }

    }
    /// <summary>
    /// 服务详细信息
    /// </summary>
    public class ServerDetailOptions
    {
        /// <summary>
        /// Node
        /// </summary>
        public string Node { get; set; }
        /// <summary>
        /// Address
        /// </summary>
        public string Address { get; set; }
        /// <summary>
        /// ServiceID
        /// </summary>
        public string ServiceID { get; set; }
        /// <summary>
        /// ServiceName
        /// </summary>
        public string ServiceName { get; set; }
        /// <summary>
        /// ServiceAddress
        /// </summary>
        public string ServiceAddress { get; set; }
        /// <summary>
        /// ServiceTags
        /// </summary>
        public string[] ServiceTags { get; set; }
        /// <summary>
        /// ServicePort
        /// </summary>
        public int ServicePort { get; set; }
        /// <summary>
        /// ServiceTaggedAddresses
        /// </summary>
        public IList<KeyValuePair<string, string>> ServiceTaggedAddresses { get; set; }
        /// <summary>
        /// ServiceEnableTagOverride
        /// </summary>
        public bool ServiceEnableTagOverride { get; set; }
        /// <summary>
        /// ServiceMeta
        /// </summary>
        public IDictionary<string, string> ServiceMeta { get; set; }
    }
}
