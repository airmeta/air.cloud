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

using Confluent.Kafka;

namespace Air.Cloud.Modules.Kafka.Contexts
{
    /// <summary>
    /// <para>zh-cn:Kafka 消息 Key 生成上下文，向 Key 生成器提供 Kafka 发布时可用的运行时信息。</para>
    /// <para>en-us:Kafka message key generation context that provides runtime information available during Kafka publishing to key generators.</para>
    /// </summary>
    /// <remarks>
    /// <para>zh-cn:业务侧可以只依赖 IMessageQueueKeyGenerationContext 编写通用生成器；如果需要 Kafka 细节，可判断并转换为该类型读取 ProducerConfig。</para>
    /// <para>en-us:Business code may depend only on IMessageQueueKeyGenerationContext to write provider-neutral generators. If Kafka-specific details are required, it may cast the context to this type and read ProducerConfig.</para>
    /// </remarks>
    public sealed class KafkaMessageQueueKeyGenerationContext : IMessageQueueKeyGenerationContext
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
        /// <para>zh-cn:获取或初始化 Kafka 原始发布配置对象。</para>
        /// <para>en-us:Gets or initializes the raw Kafka publish configuration object.</para>
        /// </summary>
        public object PublishConfig { get; init; }

        /// <summary>
        /// <para>zh-cn:获取消息内容对象的运行时类型；当消息内容为空时返回 null。</para>
        /// <para>en-us:Gets the runtime type of the message content object. Returns null when the message content is null.</para>
        /// </summary>
        public Type MessageContentType => MessageContent?.GetType();

        /// <summary>
        /// <para>zh-cn:获取发布配置对象的运行时类型；当发布配置为空时返回 null。</para>
        /// <para>en-us:Gets the runtime type of the publish configuration object. Returns null when the publish configuration is null.</para>
        /// </summary>
        public Type PublishConfigType => PublishConfig?.GetType();

        /// <summary>
        /// <para>zh-cn:获取或初始化本次 Kafka 发布期望使用的消息 Key 类型。</para>
        /// <para>en-us:Gets or initializes the message key type expected by the current Kafka publish operation.</para>
        /// </summary>
        public Type KeyType { get; init; }

        /// <summary>
        /// <para>zh-cn:获取当前消息队列提供方名称。</para>
        /// <para>en-us:Gets the current message-queue provider name.</para>
        /// </summary>
        public string ProviderName => "Kafka";

        /// <summary>
        /// <para>zh-cn:获取 Kafka 强类型生产者配置。</para>
        /// <para>en-us:Gets the strongly typed Kafka producer configuration.</para>
        /// </summary>
        public ProducerConfig ProducerConfig => PublishConfig as ProducerConfig;
    }
}
