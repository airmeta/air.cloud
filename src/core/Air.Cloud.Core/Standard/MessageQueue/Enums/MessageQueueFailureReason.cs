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
    /// <para>zh-cn:消息处理失败原因，使用通用语义描述单条消息失败，不绑定任何具体消息队列平台的失败模型。</para>
    /// <para>en-us:Message handling failure reason. It describes per-message failures with provider-neutral semantics and does not bind to any provider-specific failure model.</para>
    /// </summary>
    public enum MessageQueueFailureReason
    {
        /// <summary>
        /// <para>zh-cn:未知失败原因。</para>
        /// <para>en-us:Unknown failure reason.</para>
        /// </summary>
        Unknown = 0,

        /// <summary>
        /// <para>zh-cn:业务处理抛出异常。</para>
        /// <para>en-us:The business handler threw an exception.</para>
        /// </summary>
        Exception = 1,

        /// <summary>
        /// <para>zh-cn:业务处理超过框架配置的单条消息处理超时时间。</para>
        /// <para>en-us:The business handler exceeded the framework-configured per-message handling timeout.</para>
        /// </summary>
        Timeout = 2,

        /// <summary>
        /// <para>zh-cn:消息反序列化失败。</para>
        /// <para>en-us:Message deserialization failed.</para>
        /// </summary>
        DeserializeFailed = 3,

        /// <summary>
        /// <para>zh-cn:消费者中断导致消息处理无法继续。</para>
        /// <para>en-us:The consumer interruption prevented message handling from continuing.</para>
        /// </summary>
        ConsumerInterrupted = 4
    }
}
