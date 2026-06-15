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
using Air.Cloud.Core.Standard.MessageQueue;
using Air.Cloud.Modules.RocketMQ.Model;

namespace Air.Cloud.Modules.RocketMQ.Contexts
{
    /// <summary>
    /// <para>zh-cn:RocketMQ 消息 Key 生成上下文，向 Key 生成器提供 Topic、消息内容和 RocketMQ 发布配置。</para>
    /// <para>en-us:RocketMQ message key generation context that provides topic, message content, and RocketMQ publish configuration to key generators.</para>
    /// </summary>
    public sealed class RocketMQMessageQueueKeyGenerationContext : IMessageQueueKeyGenerationContext
    {
        /// <summary>
        /// <para>zh-cn:获取或初始化当前发布目标主题名称。</para>
        /// <para>en-us:Gets or initializes the current target topic name for publishing.</para>
        /// </summary>
        public string TopicName { get; init; }

        /// <summary>
        /// <para>zh-cn:获取或初始化当前准备发布的消息内容对象。</para>
        /// <para>en-us:Gets or initializes the message content object that is going to be published.</para>
        /// </summary>
        public object MessageContent { get; init; }

        /// <summary>
        /// <para>zh-cn:获取或初始化 RocketMQ 发布配置对象。</para>
        /// <para>en-us:Gets or initializes the RocketMQ publish configuration object.</para>
        /// </summary>
        public object PublishConfig { get; init; }

        /// <summary>
        /// <para>zh-cn:获取消息内容对象的运行时类型；消息内容为空时返回 null。</para>
        /// <para>en-us:Gets the runtime type of the message content object. Returns null when the message content is null.</para>
        /// </summary>
        public Type MessageContentType => MessageContent?.GetType();

        /// <summary>
        /// <para>zh-cn:获取发布配置对象的运行时类型；发布配置为空时返回 null。</para>
        /// <para>en-us:Gets the runtime type of the publish configuration object. Returns null when the publish configuration is null.</para>
        /// </summary>
        public Type PublishConfigType => PublishConfig?.GetType();

        /// <summary>
        /// <para>zh-cn:获取或初始化本次 RocketMQ 发布期望使用的消息 Key 类型，通常为 string。</para>
        /// <para>en-us:Gets or initializes the message key type expected by the current RocketMQ publish operation, usually string.</para>
        /// </summary>
        public Type KeyType { get; init; }

        /// <summary>
        /// <para>zh-cn:获取当前消息队列提供方名称。</para>
        /// <para>en-us:Gets the current message-queue provider name.</para>
        /// </summary>
        public string ProviderName => "RocketMQ";

        /// <summary>
        /// <para>zh-cn:获取强类型 RocketMQ 生产者配置。</para>
        /// <para>en-us:Gets the strongly typed RocketMQ producer configuration.</para>
        /// </summary>
        public RocketMQProducerConfig ProducerConfig => PublishConfig as RocketMQProducerConfig;
    }
}
