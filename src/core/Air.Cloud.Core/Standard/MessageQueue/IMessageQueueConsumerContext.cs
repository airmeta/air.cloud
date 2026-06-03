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
using Air.Cloud.Core.Standard.MessageQueue.Enums;

namespace Air.Cloud.Core.Standard.MessageQueue
{
    /// <summary>
    /// <para>zh-cn:定义消息队列消费者生命周期上下文标准，用于在消费者中断或恢复时传递通用运行时信息。</para>
    /// <para>en-us:Defines the message-queue consumer lifecycle context contract used to pass common runtime information when a consumer is interrupted or recovered.</para>
    /// </summary>
    public interface IMessageQueueConsumerContext
    {
        /// <summary>
        /// <para>zh-cn:获取当前消费者订阅的主题或队列名称。</para>
        /// <para>en-us:Gets the topic or queue name subscribed by the current consumer.</para>
        /// </summary>
        public string TopicName { get; }

        /// <summary>
        /// <para>zh-cn:获取当前消费者组编号；不支持消费者组的平台可以返回 null。</para>
        /// <para>en-us:Gets the current consumer group id. Providers that do not support consumer groups may return null.</para>
        /// </summary>
        public string GroupId { get; }

        /// <summary>
        /// <para>zh-cn:获取当前消息队列提供方名称，例如 Kafka、RabbitMQ 或 RedisStream。</para>
        /// <para>en-us:Gets the current message-queue provider name, such as Kafka, RabbitMQ, or RedisStream.</para>
        /// </summary>
        public string ProviderName { get; }

        /// <summary>
        /// <para>zh-cn:获取消费者中断原因；消费者恢复事件可返回 Unknown。</para>
        /// <para>en-us:Gets the consumer interruption reason. Consumer recovery events may return Unknown.</para>
        /// </summary>
        public MessageQueueConsumerInterruptedReason InterruptedReason { get; }

        /// <summary>
        /// <para>zh-cn:获取触发当前消费者生命周期事件的异常；如果不是异常触发则为 null。</para>
        /// <para>en-us:Gets the exception that triggered the current consumer lifecycle event. Returns null when the event was not triggered by an exception.</para>
        /// </summary>
        public Exception Exception { get; }
    }
}
