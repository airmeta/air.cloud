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
using Air.Cloud.Core.Standard.MessageQueue.Enums;

namespace Air.Cloud.Core.Standard.MessageQueue
{
    /// <summary>
    /// <para>zh-cn:定义消息处理失败上下文标准，用于在单条消息处理失败、超时或反序列化失败时传递补偿所需的通用信息。</para>
    /// <para>en-us:Defines the message handling failure context contract used to pass common compensation information when a single message fails, times out, or cannot be deserialized.</para>
    /// </summary>
    public interface IMessageQueueFailureContext
    {
        /// <summary>
        /// <para>zh-cn:获取消息所属主题或队列名称。</para>
        /// <para>en-us:Gets the topic or queue name that owns the message.</para>
        /// </summary>
        public string TopicName { get; }

        /// <summary>
        /// <para>zh-cn:获取当前消息队列提供方名称，例如 Kafka。</para>
        /// <para>en-us:Gets the current message-queue provider name, such as Kafka.</para>
        /// </summary>
        public string ProviderName { get; }

        /// <summary>
        /// <para>zh-cn:获取处理失败的消息内容；如果消息反序列化失败，该值可能为 null。</para>
        /// <para>en-us:Gets the message content that failed to be handled. This value may be null when message deserialization failed.</para>
        /// </summary>
        public object MessageContent { get; }

        /// <summary>
        /// <para>zh-cn:获取导致处理失败的异常；超时失败时可以是 TimeoutException。</para>
        /// <para>en-us:Gets the exception that caused the handling failure. Timeout failures may use TimeoutException.</para>
        /// </summary>
        public Exception Exception { get; }

        /// <summary>
        /// <para>zh-cn:获取当前失败是否由处理超时导致。</para>
        /// <para>en-us:Gets whether the current failure was caused by handling timeout.</para>
        /// </summary>
        public bool IsTimeout { get; }

        /// <summary>
        /// <para>zh-cn:获取本次处理使用的超时配置；为 null 时表示框架未限制单条消息处理时间。</para>
        /// <para>en-us:Gets the timeout configuration used by the current handling operation. A null value means the framework did not limit per-message handling time.</para>
        /// </summary>
        public TimeSpan? HandlingTimeout { get; }

        /// <summary>
        /// <para>zh-cn:获取本次处理已消耗时间。</para>
        /// <para>en-us:Gets the elapsed time consumed by the current handling operation.</para>
        /// </summary>
        public TimeSpan HandlingElapsed { get; }

        /// <summary>
        /// <para>zh-cn:获取当前消息已重试次数；具体模块可以根据自身能力维护该值。</para>
        /// <para>en-us:Gets the retry count of the current message. Concrete modules may maintain this value based on their own capabilities.</para>
        /// </summary>
        public int RetryCount { get; }

        /// <summary>
        /// <para>zh-cn:获取消息处理失败原因。</para>
        /// <para>en-us:Gets the message handling failure reason.</para>
        /// </summary>
        public MessageQueueFailureReason FailureReason { get; }
    }
}
