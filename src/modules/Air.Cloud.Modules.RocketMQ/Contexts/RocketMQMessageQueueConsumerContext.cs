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

namespace Air.Cloud.Modules.RocketMQ.Contexts
{
    /// <summary>
    /// <para>zh-cn:RocketMQ 消费者生命周期上下文，用于将订阅恢复、消费循环异常和连接错误桥接到标准恢复接口。</para>
    /// <para>en-us:RocketMQ consumer lifecycle context used to bridge subscription recovery, consume-loop exceptions, and connection errors to the standard recovery interface.</para>
    /// </summary>
    public sealed class RocketMQMessageQueueConsumerContext : IMessageQueueConsumerContext
    {
        /// <summary>
        /// <para>zh-cn:获取或初始化 RocketMQ Topic 名称。</para>
        /// <para>en-us:Gets or initializes the RocketMQ topic name.</para>
        /// </summary>
        public string TopicName { get; init; }

        /// <summary>
        /// <para>zh-cn:获取或初始化 RocketMQ 消费者组。</para>
        /// <para>en-us:Gets or initializes the RocketMQ consumer group.</para>
        /// </summary>
        public string GroupId { get; init; }

        /// <summary>
        /// <para>zh-cn:获取当前消息队列提供方名称。</para>
        /// <para>en-us:Gets the current message-queue provider name.</para>
        /// </summary>
        public string ProviderName => "RocketMQ";

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
        /// <para>zh-cn:获取或初始化 RocketMQ 原生消费者对象；以 object 暴露以避免标准层依赖 RocketMQ 类型。</para>
        /// <para>en-us:Gets or initializes the native RocketMQ consumer object. It is exposed as object to avoid RocketMQ type dependency in the standard layer.</para>
        /// </summary>
        public object NativeConsumer { get; init; }

        /// <summary>
        /// <para>zh-cn:获取或初始化 RocketMQ 原生事件参数。</para>
        /// <para>en-us:Gets or initializes the native RocketMQ event arguments.</para>
        /// </summary>
        public object NativeEventArgs { get; init; }
    }
}
