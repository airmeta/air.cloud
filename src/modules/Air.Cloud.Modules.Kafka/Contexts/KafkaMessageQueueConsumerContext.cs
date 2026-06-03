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
using Air.Cloud.Core.Standard.MessageQueue.Enums;

namespace Air.Cloud.Modules.Kafka.Contexts
{
    /// <summary>
    /// <para>zh-cn:Kafka 消费者生命周期上下文，用于将 Kafka rebalance、消费循环异常和消费者恢复等事件桥接到标准层恢复接口。</para>
    /// <para>en-us:Kafka consumer lifecycle context used to bridge Kafka events such as rebalance, consume-loop exceptions, and consumer recovery to the standard recovery interface.</para>
    /// </summary>
    public sealed class KafkaMessageQueueConsumerContext : IMessageQueueConsumerContext
    {
        /// <summary>
        /// <para>zh-cn:获取或初始化 Kafka Topic 名称。</para>
        /// <para>en-us:Gets or initializes the Kafka topic name.</para>
        /// </summary>
        public string TopicName { get; init; }

        /// <summary>
        /// <para>zh-cn:获取或初始化 Kafka Consumer GroupId。</para>
        /// <para>en-us:Gets or initializes the Kafka consumer GroupId.</para>
        /// </summary>
        public string GroupId { get; init; }

        /// <summary>
        /// <para>zh-cn:获取当前消息队列提供方名称。</para>
        /// <para>en-us:Gets the current message-queue provider name.</para>
        /// </summary>
        public string ProviderName => "Kafka";

        /// <summary>
        /// <para>zh-cn:获取或初始化消费者中断原因；恢复事件通常使用 Unknown。</para>
        /// <para>en-us:Gets or initializes the consumer interruption reason. Recovery events usually use Unknown.</para>
        /// </summary>
        public MessageQueueConsumerInterruptedReason InterruptedReason { get; init; }

        /// <summary>
        /// <para>zh-cn:获取或初始化触发消费者生命周期事件的异常。</para>
        /// <para>en-us:Gets or initializes the exception that triggered the consumer lifecycle event.</para>
        /// </summary>
        public Exception Exception { get; init; }

        /// <summary>
        /// <para>zh-cn:获取或初始化 Kafka 原生消费者对象；为避免标准层依赖 Kafka 类型，这里以 object 暴露。</para>
        /// <para>en-us:Gets or initializes the native Kafka consumer object. It is exposed as object to avoid Kafka type dependency in the standard layer.</para>
        /// </summary>
        public object NativeConsumer { get; init; }

        /// <summary>
        /// <para>zh-cn:获取或初始化 Kafka 原生事件参数，例如分区集合或异常对象。</para>
        /// <para>en-us:Gets or initializes native Kafka event arguments, such as partition collections or exception objects.</para>
        /// </summary>
        public object NativeEventArgs { get; init; }
    }
}
