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

namespace Air.Cloud.Modules.Kafka.Options
{
    /// <summary>
    /// <para>zh-cn:Kafka 消费者执行策略默认配置，用于控制单条消息处理超时和最大重试次数等框架级规则。</para>
    /// <para>en-us:Default Kafka consumer execution options used to control framework-level rules such as per-message handling timeout and maximum retry count.</para>
    /// </summary>
    /// <remarks>
    /// <para>zh-cn:MessageHandlingTimeout 是业务处理超时，不等同于 Kafka ConsumerConfig.MaxPollIntervalMs；推荐让业务处理超时小于 MaxPollIntervalMs。</para>
    /// <para>en-us:MessageHandlingTimeout is the business handling timeout and is not the same as Kafka ConsumerConfig.MaxPollIntervalMs. It is recommended to keep the business handling timeout smaller than MaxPollIntervalMs.</para>
    /// </remarks>
    public class KafkaConsumerExecutionOptions : IMessageQueueConsumerExecutionOptions
    {
        /// <summary>
        /// <para>zh-cn:单条消息业务处理超时时间；默认 30 秒，超过后由 Kafka 模块触发失败补偿。</para>
        /// <para>en-us:Business handling timeout for a single message. The default is 30 seconds. When exceeded, the Kafka module triggers failure compensation.</para>
        /// </summary>
        public TimeSpan? MessageHandlingTimeout { get; set; } = TimeSpan.FromSeconds(30);

        /// <summary>
        /// <para>zh-cn:单条消息最大重试次数；当前默认补偿实现仅记录该值，后续 retry topic 或 dead-letter topic 可使用它。</para>
        /// <para>en-us:Maximum retry count for a single message. The current default compensation implementation only records this value. Future retry-topic or dead-letter-topic implementations may use it.</para>
        /// </summary>
        public int MaxRetryCount { get; set; } = 3;
    }
}
