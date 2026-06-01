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
using Air.Cloud.Core.Standard.MessageQueue.Config;

using Air.Cloud.Core.Standard.MessageQueue.Provider;

namespace Air.Cloud.Core.Standard.MessageQueue.Model
{
    /// <summary>
    /// <para>zh-cn:定义消息队列主题运行时信息标准，描述主题名称、资源标识、连接地址、状态和配置对象。</para>
    /// <para>en-us:Defines the message-queue topic runtime information contract, describing topic name, resource identifier, connection address, status, and configuration object.</para>
    /// </summary>
    public interface ITopicInfo
    {
        /// <summary>
        /// <para>zh-cn:获取或设置主题名称。</para>
        /// <para>en-us:Gets or sets the topic name.</para>
        /// </summary>
        public string TopicName { get; set; }
        /// <summary>
        /// <para>zh-cn:获取或设置主题资源标识，例如云消息队列中的 ARN 或等价标识。</para>
        /// <para>en-us:Gets or sets the topic resource identifier, such as an ARN or equivalent identifier in a cloud message queue.</para>
        /// </summary>
        public string TopicArn { get; set; }
        /// <summary>
        /// <para>zh-cn:获取或设置主题所在消息队列服务的引导服务器地址。</para>
        /// <para>en-us:Gets or sets the bootstrap server address of the message-queue service that hosts the topic.</para>
        /// </summary>
        public string BootStrapServer { get; set; }
        /// <summary>
        /// <para>zh-cn:获取或设置主题当前状态。</para>
        /// <para>en-us:Gets or sets the current topic status.</para>
        /// </summary>
        public string TopicStatus { get; set; }
        /// <summary>
        /// <para>zh-cn:获取或设置主题配置对象。</para>
        /// <para>en-us:Gets or sets the topic configuration object.</para>
        /// </summary>
        public ITopicConfig TopicConfig { get; set; }
    }
}
