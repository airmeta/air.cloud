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

namespace Air.Cloud.Modules.RocketMQ.Options
{
    /// <summary>
    /// <para>zh-cn:RocketMQ 消费者执行策略默认配置，用于控制单条消息业务处理超时和最大重试次数。</para>
    /// <para>en-us:Default RocketMQ consumer execution options used to control per-message business handling timeout and maximum retry count.</para>
    /// </summary>
    public class RocketMQConsumerExecutionOptions : IMessageQueueConsumerExecutionOptions
    {
        /// <summary>
        /// <para>zh-cn:单条消息业务处理超时时间；默认 30 秒，超时后触发失败补偿且不 Ack。</para>
        /// <para>en-us:Business handling timeout for a single message. The default is 30 seconds. When exceeded, failure compensation is triggered and the message is not acknowledged.</para>
        /// </summary>
        public TimeSpan? MessageHandlingTimeout { get; set; } = TimeSpan.FromSeconds(30);

        /// <summary>
        /// <para>zh-cn:单条消息最大重试次数；默认 3，当前默认补偿实现仅记录该值，死信或重试队列策略由业务自定义补偿实现负责。</para>
        /// <para>en-us:Maximum retry count for a single message. The default is 3. The current default compensation implementation only records this value; dead-letter or retry-queue policies are handled by custom business compensation implementations.</para>
        /// </summary>
        public int MaxRetryCount { get; set; } = 3;
    }
}
