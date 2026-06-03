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
    /// <para>zh-cn:定义消息队列消费者恢复标准，用于在消费者中断和恢复时执行日志、告警、状态清理或业务自定义恢复逻辑。</para>
    /// <para>en-us:Defines the message-queue consumer recovery contract used to execute logging, alerting, state cleanup, or business-specific recovery logic when a consumer is interrupted or recovered.</para>
    /// </summary>
    /// <remarks>
    /// <para>zh-cn:该接口只表达通用生命周期事件；Kafka 模块会将 rebalance、分区撤销、分区恢复、消费循环异常等具体事件映射为该标准。</para>
    /// <para>en-us:This interface only expresses provider-neutral lifecycle events. The Kafka module maps concrete events such as rebalance, partition revocation, partition assignment, and consume-loop exceptions to this standard.</para>
    /// </remarks>
    public interface IMessageQueueConsumerRecoveryStandard
    {
        /// <summary>
        /// <para>zh-cn:当消费者中断或消费能力暂时不可用时触发。</para>
        /// <para>en-us:Triggered when the consumer is interrupted or temporarily unable to consume messages.</para>
        /// </summary>
        /// <param name="context">
        /// <para>zh-cn:消费者生命周期上下文。</para>
        /// <para>en-us:The consumer lifecycle context.</para>
        /// </param>
        /// <returns>
        /// <para>zh-cn:异步任务。</para>
        /// <para>en-us>The asynchronous task.</para>
        /// </returns>
        public Task OnConsumerInterruptedAsync(IMessageQueueConsumerContext context);

        /// <summary>
        /// <para>zh-cn:当消费者恢复或消费能力重新可用时触发。</para>
        /// <para>en-us:Triggered when the consumer is recovered or able to consume messages again.</para>
        /// </summary>
        /// <param name="context">
        /// <para>zh-cn:消费者生命周期上下文。</para>
        /// <para>en-us:The consumer lifecycle context.</para>
        /// </param>
        /// <returns>
        /// <para>zh-cn:异步任务。</para>
        /// <para>en-us>The asynchronous task.</para>
        /// </returns>
        public Task OnConsumerRecoveredAsync(IMessageQueueConsumerContext context);
    }
}
