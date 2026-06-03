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
namespace Air.Cloud.Core.Standard.MessageQueue
{
    /// <summary>
    /// <para>zh-cn:定义消息队列 Key 生成上下文标准，用于在生成消息 Key 时传递主题、消息内容、发布配置和队列实现等运行时信息。</para>
    /// <para>en-us:Defines the message-queue key generation context contract used to pass runtime information, such as topic, message content, publish configuration, and queue provider, when generating a message key.</para>
    /// </summary>
    /// <remarks>
    /// <para>zh-cn:该接口只描述所有消息队列实现都可以理解的通用信息；Kafka、RabbitMQ、Redis Stream 等模块可以实现该接口并追加自己的强类型扩展属性。</para>
    /// <para>en-us:This interface only describes common information that every message-queue implementation can understand. Modules such as Kafka, RabbitMQ, and Redis Stream may implement this interface and add their own strongly typed extension properties.</para>
    /// </remarks>
    public interface IMessageQueueKeyGenerationContext
    {
        /// <summary>
        /// <para>zh-cn:获取当前发布目标主题名称。</para>
        /// <para>en-us:Gets the current target topic name for publishing.</para>
        /// </summary>
        public string TopicName { get; }

        /// <summary>
        /// <para>zh-cn:获取当前准备发布的消息内容对象。</para>
        /// <para>en-us:Gets the message content object that is going to be published.</para>
        /// </summary>
        public object MessageContent { get; }

        /// <summary>
        /// <para>zh-cn:获取当前消息队列实现使用的原始发布配置对象。</para>
        /// <para>en-us:Gets the raw publish configuration object used by the current message-queue provider.</para>
        /// </summary>
        public object PublishConfig { get; }

        /// <summary>
        /// <para>zh-cn:获取消息内容对象的运行时类型；当消息内容为空时返回 null。</para>
        /// <para>en-us:Gets the runtime type of the message content object. Returns null when the message content is null.</para>
        /// </summary>
        public Type MessageContentType { get; }

        /// <summary>
        /// <para>zh-cn:获取发布配置对象的运行时类型；当发布配置为空时返回 null。</para>
        /// <para>en-us:Gets the runtime type of the publish configuration object. Returns null when the publish configuration is null.</para>
        /// </summary>
        public Type PublishConfigType { get; }

        /// <summary>
        /// <para>zh-cn:获取本次发布期望使用的消息 Key 类型。</para>
        /// <para>en-us:Gets the message key type expected by the current publish operation.</para>
        /// </summary>
        public Type KeyType { get; }

        /// <summary>
        /// <para>zh-cn:获取当前消息队列提供方名称，例如 Kafka。</para>
        /// <para>en-us:Gets the current message-queue provider name, such as Kafka.</para>
        /// </summary>
        public string ProviderName { get; }
    }
}
