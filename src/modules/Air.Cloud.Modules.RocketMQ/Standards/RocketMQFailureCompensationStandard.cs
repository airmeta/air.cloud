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
    /// <para>zh-cn:RocketMQ 消息失败补偿默认实现，用于记录单条消息失败事件；默认不主动 Ack、不写死信、不转发重试。</para>
    /// <para>en-us:Default RocketMQ message failure compensation implementation used to record per-message failures. It does not actively acknowledge, dead-letter, or forward retries by default.</para>
    /// </summary>
    public class RocketMQFailureCompensationStandard : IMessageQueueFailureCompensationStandard
    {
        /// <summary>
        /// <para>zh-cn:消息处理失败时记录默认追踪日志。</para>
        /// <para>en-us:Writes the default trace log when message handling fails.</para>
        /// </summary>
        /// <param name="context">
        /// <para>zh-cn:消息处理失败上下文。</para>
        /// <para>en-us:The message handling failure context.</para>
        /// </param>
        /// <returns>
        /// <para>zh-cn:异步任务。</para>
        /// <para>en-us:The asynchronous task.</para>
        /// </returns>
        public Task OnFailedAsync(IMessageQueueFailureContext context)
        {
            AppRealization.TraceLog.Write(new DefaultTraceLogContent()
            {
                Title = "rocketmq-message-failed",
                Content = context.Exception?.Message ?? "RocketMQ message handling failed.",
                AdditionalParams = new Dictionary<string, object>()
                {
                    { "TopicName", context.TopicName },
                    { "ProviderName", context.ProviderName },
                    { "FailureReason", context.FailureReason.ToString() },
                    { "IsTimeout", context.IsTimeout },
                    { "HandlingTimeout", context.HandlingTimeout?.ToString() },
                    { "HandlingElapsed", context.HandlingElapsed.ToString() },
                    { "RetryCount", context.RetryCount },
                    { "Exception", context.Exception?.Message },
                    { "StackTrace", context.Exception?.StackTrace }
                },
                Tags = "rocketmq_message_failed"
            });
            return Task.CompletedTask;
        }
    }
}
