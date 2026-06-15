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
    /// <para>zh-cn:RocketMQ 消息处理失败上下文，用于向失败补偿接口传递业务异常、业务超时、反序列化失败和原生消息视图。</para>
    /// <para>en-us:RocketMQ message handling failure context used to pass business exceptions, business timeouts, deserialization failures, and native message views to the failure compensation interface.</para>
    /// </summary>
    public sealed class RocketMQMessageQueueFailureContext : IMessageQueueFailureContext
    {
        /// <summary>
        /// <para>zh-cn:获取或初始化 RocketMQ Topic 名称。</para>
        /// <para>en-us:Gets or initializes the RocketMQ topic name.</para>
        /// </summary>
        public string TopicName { get; init; }

        /// <summary>
        /// <para>zh-cn:获取当前消息队列提供方名称。</para>
        /// <para>en-us:Gets the current message-queue provider name.</para>
        /// </summary>
        public string ProviderName => "RocketMQ";

        /// <summary>
        /// <para>zh-cn:获取或初始化处理失败的消息内容；反序列化失败时可能为空。</para>
        /// <para>en-us:Gets or initializes the message content that failed to be handled. It may be null when deserialization failed.</para>
        /// </summary>
        public object MessageContent { get; init; }

        /// <summary>
        /// <para>zh-cn:获取或初始化导致处理失败的异常。</para>
        /// <para>en-us:Gets or initializes the exception that caused the handling failure.</para>
        /// </summary>
        public Exception Exception { get; init; }

        /// <summary>
        /// <para>zh-cn:获取或初始化当前失败是否由业务处理超时导致。</para>
        /// <para>en-us:Gets or initializes whether the current failure was caused by business handling timeout.</para>
        /// </summary>
        public bool IsTimeout { get; init; }

        /// <summary>
        /// <para>zh-cn:获取或初始化本次处理使用的超时配置。</para>
        /// <para>en-us:Gets or initializes the timeout configuration used by the current handling operation.</para>
        /// </summary>
        public TimeSpan? HandlingTimeout { get; init; }

        /// <summary>
        /// <para>zh-cn:获取或初始化本次处理已消耗时间。</para>
        /// <para>en-us:Gets or initializes the elapsed time consumed by the current handling operation.</para>
        /// </summary>
        public TimeSpan HandlingElapsed { get; init; }

        /// <summary>
        /// <para>zh-cn:获取或初始化当前消息已重试次数，默认来自 RocketMQ DeliveryAttempt。</para>
        /// <para>en-us:Gets or initializes the retry count of the current message, typically from RocketMQ DeliveryAttempt.</para>
        /// </summary>
        public int RetryCount { get; init; }

        /// <summary>
        /// <para>zh-cn:获取或初始化消息处理失败原因。</para>
        /// <para>en-us:Gets or initializes the message handling failure reason.</para>
        /// </summary>
        public MessageQueueFailureReason FailureReason { get; init; }

        /// <summary>
        /// <para>zh-cn:获取或初始化 RocketMQ 原生 MessageView；以 object 暴露以避免标准层依赖 RocketMQ 类型。</para>
        /// <para>en-us:Gets or initializes the native RocketMQ MessageView. It is exposed as object to avoid RocketMQ type dependency in the standard layer.</para>
        /// </summary>
        public object NativeMessage { get; init; }
    }
}
