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
namespace Air.Cloud.Core.Standard.MessageQueue.Config
{
    /// <summary>
    /// <para>zh-cn:定义消息队列主题配置标准，描述主题名称、资源标识、引导服务器、状态和原始配置内容。</para>
    /// <para>en-us:Defines the message-queue topic configuration contract, describing topic name, resource identifier, bootstrap server, status, and raw configuration content.</para>
    /// </summary>
    public interface ITopicConfig
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
        /// <para>zh-cn:获取或设置主题原始配置内容或序列化配置文本。</para>
        /// <para>en-us:Gets or sets the raw topic configuration content or serialized configuration text.</para>
        /// </summary>
        public string TopicConfig { get; set; }
    }

}
