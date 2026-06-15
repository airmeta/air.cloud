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
using Air.Cloud.Core;
using Air.Cloud.Core.Standard.MessageQueue;
using Air.Cloud.Core.Standard.TraceLog.Defaults;

namespace Air.Cloud.Modules.RocketMQ.Standards
{
    /// <summary>
    /// <para>zh-cn:RocketMQ 消费者恢复标准默认实现，用于记录消费者中断和恢复事件，不主动执行业务恢复动作。</para>
    /// <para>en-us:Default RocketMQ consumer recovery implementation used to record consumer interruption and recovery events without actively executing business recovery actions.</para>
    /// </summary>
    public class RocketMQConsumerRecoveryStandard : IMessageQueueConsumerRecoveryStandard
    {
        /// <summary>
        /// <para>zh-cn:消费者中断时记录默认追踪日志。</para>
        /// <para>en-us:Writes the default trace log when the consumer is interrupted.</para>
        /// </summary>
        /// <param name="context">
        /// <para>zh-cn:消费者生命周期上下文。</para>
        /// <para>en-us:The consumer lifecycle context.</para>
        /// </param>
        /// <returns>
        /// <para>zh-cn:异步任务。</para>
        /// <para>en-us:The asynchronous task.</para>
        /// </returns>
        public Task OnConsumerInterruptedAsync(IMessageQueueConsumerContext context)
        {
            AppRealization.TraceLog.Write(new DefaultTraceLogContent()
            {
                Title = "rocketmq-consumer-interrupted",
                Content = context.Exception?.Message ?? "RocketMQ consumer interrupted.",
                AdditionalParams = new Dictionary<string, object>()
                {
                    { "TopicName", context.TopicName },
                    { "GroupId", context.GroupId },
                    { "ProviderName", context.ProviderName },
                    { "InterruptedReason", context.InterruptedReason.ToString() },
                    { "Exception", context.Exception?.Message },
                    { "StackTrace", context.Exception?.StackTrace }
                },
                Tags = "rocketmq_consumer_interrupted"
            });
            return Task.CompletedTask;
        }

        /// <summary>
        /// <para>zh-cn:消费者恢复时记录默认追踪日志。</para>
        /// <para>en-us:Writes the default trace log when the consumer is recovered.</para>
        /// </summary>
        /// <param name="context">
        /// <para>zh-cn:消费者生命周期上下文。</para>
        /// <para>en-us:The consumer lifecycle context.</para>
        /// </param>
        /// <returns>
        /// <para>zh-cn:异步任务。</para>
        /// <para>en-us:The asynchronous task.</para>
        /// </returns>
        public Task OnConsumerRecoveredAsync(IMessageQueueConsumerContext context)
        {
            AppRealization.TraceLog.Write(new DefaultTraceLogContent()
            {
                Title = "rocketmq-consumer-recovered",
                Content = "RocketMQ consumer recovered.",
                AdditionalParams = new Dictionary<string, object>()
                {
                    { "TopicName", context.TopicName },
                    { "GroupId", context.GroupId },
                    { "ProviderName", context.ProviderName }
                },
                Tags = "rocketmq_consumer_recovered"
            });
            return Task.CompletedTask;
        }
    }
}
