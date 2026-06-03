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
namespace Air.Cloud.Core.Standard.MessageQueue.Enums
{
    /// <summary>
    /// <para>zh-cn:消息队列消费者中断原因，使用通用语义描述消费通道异常，不绑定 Kafka rebalance、RabbitMQ channel 或 Redis Stream 等具体平台概念。</para>
    /// <para>en-us:Message-queue consumer interruption reason. It describes consumer-channel interruptions with provider-neutral semantics and does not bind to provider-specific concepts such as Kafka rebalance, RabbitMQ channel, or Redis Stream.</para>
    /// </summary>
    public enum MessageQueueConsumerInterruptedReason
    {
        /// <summary>
        /// <para>zh-cn:未知中断原因。</para>
        /// <para>en-us:Unknown interruption reason.</para>
        /// </summary>
        Unknown = 0,

        /// <summary>
        /// <para>zh-cn:消费者所属资源被撤销或重新分配，例如 Kafka 分区撤销。</para>
        /// <para>en-us:The consumer-owned resource was revoked or reassigned, for example Kafka partition revocation.</para>
        /// </summary>
        ResourceRevoked = 1,

        /// <summary>
        /// <para>zh-cn:消费者所属资源已丢失，例如 Kafka 分区丢失；该场景通常比撤销更保守，不应假设仍可安全确认消息。</para>
        /// <para>en-us:The consumer-owned resource was lost, for example Kafka partition loss. This is usually more conservative than revocation and should not assume messages can still be safely acknowledged.</para>
        /// </summary>
        ResourceLost = 2,

        /// <summary>
        /// <para>zh-cn:底层连接或消费循环发生异常。</para>
        /// <para>en-us:The underlying connection or consume loop encountered an exception.</para>
        /// </summary>
        ConnectionError = 3,

        /// <summary>
        /// <para>zh-cn:消费者被框架或宿主主动停止。</para>
        /// <para>en-us:The consumer was actively stopped by the framework or host.</para>
        /// </summary>
        Stopped = 4
    }
}
