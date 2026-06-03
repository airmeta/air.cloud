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
    /// <para>zh-cn:定义消息处理失败补偿标准，用于在单条消息处理异常、超时或反序列化失败后执行重试、死信、日志、告警等补偿动作。</para>
    /// <para>en-us:Defines the message handling failure compensation contract used to execute compensation actions such as retry, dead-lettering, logging, and alerting after a single message fails, times out, or cannot be deserialized.</para>
    /// </summary>
    /// <remarks>
    /// <para>zh-cn:默认实现应保持保守，例如只记录日志而不主动重投递，避免和底层消息队列的未确认消息重投递机制产生重复消息。</para>
    /// <para>en-us:The default implementation should stay conservative, for example logging only without active redelivery, to avoid duplicate messages caused by overlapping with the underlying queue's unacknowledged-message redelivery mechanism.</para>
    /// </remarks>
    public interface IMessageQueueFailureCompensationStandard
    {
        /// <summary>
        /// <para>zh-cn:当单条消息处理失败时触发。</para>
        /// <para>en-us:Triggered when a single message handling operation fails.</para>
        /// </summary>
        /// <param name="context">
        /// <para>zh-cn:消息处理失败上下文。</para>
        /// <para>en-us:The message handling failure context.</para>
        /// </param>
        /// <returns>
        /// <para>zh-cn:异步任务。</para>
        /// <para>en-us:The asynchronous task.</para>
        /// </returns>
        public Task OnFailedAsync(IMessageQueueFailureContext context);
    }
}
