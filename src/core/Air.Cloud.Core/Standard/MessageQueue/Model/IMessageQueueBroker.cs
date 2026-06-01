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
using System.Net;

namespace Air.Cloud.Core.Standard.MessageQueue.Model
{
    /// <summary>
    /// <para>zh-cn:定义消息队列 Broker 节点信息标准，描述节点名称、引导服务器地址和 IP 地址。</para>
    /// <para>en-us:Defines the message-queue broker node information contract, describing broker name, bootstrap server address, and IP address.</para>
    /// </summary>
    public interface IMessageQueueBroker
    {
        /// <summary>
        /// <para>zh-cn:获取或设置 Broker 节点名称。</para>
        /// <para>en-us:Gets or sets the broker node name.</para>
        /// </summary>
        public string BrokerName { get; set; }

        /// <summary>
        /// <para>zh-cn:获取或设置消息队列引导服务器地址。</para>
        /// <para>en-us:Gets or sets the message-queue bootstrap server address.</para>
        /// </summary>
        public string BootStrapServer { get; set; }

        /// <summary>
        /// <para>zh-cn:获取或设置 Broker 节点 IP 地址。</para>
        /// <para>en-us:Gets or sets the broker node IP address.</para>
        /// </summary>
        public IPAddress IPAddress { get; set; }
    }
}
