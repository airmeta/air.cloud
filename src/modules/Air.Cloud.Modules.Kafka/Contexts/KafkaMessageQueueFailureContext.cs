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

namespace Air.Cloud.Modules.Kafka.Contexts
{
    /// <summary>
    /// <para>zh-cn:Kafka 消息处理失败上下文，用于将业务异常、业务超时、反序列化失败等单条消息失败事件传递给标准层补偿接口。</para>
    /// <para>en-us:Kafka message handling failure context used to pass per-message failure events such as business exceptions, business timeouts, and deserialization failures to the standard compensation interface.</para>
    /// </summary>
    public sealed class KafkaMessageQueueFailureContext : IMessageQueueFailureContext
    {
        /// <summary>
        /// <para>zh-cn:获取或初始化 Kafka Topic 名称。</para>
        /// <para>en-us:Gets or initializes the Kafka topic name.</para>
        /// </summary>
        public string TopicName { get; init; }

        /// <summary>
        /// <para>zh-cn:获取当前消息队列提供方名称。</para>
        /// <para>en-us:Gets the current message-queue provider name.</para>
        /// </summary>
        public string ProviderName => "Kafka";

        /// <summary>
        /// <para>zh-cn:获取或初始化处理失败的消息内容；反序列化失败时可能为 null。</para>
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
        /// <para>zh-cn:获取或初始化当前消息已重试次数；当前 Kafka 默认实现先保留该字段，后续可接入 retry topic 或 header 计数。</para>
        /// <para>en-us:Gets or initializes the retry count of the current message. The current Kafka default implementation keeps this field for future retry-topic or header-count integration.</para>
        /// </summary>
        public int RetryCount { get; init; }

        /// <summary>
        /// <para>zh-cn:获取或初始化消息处理失败原因。</para>
        /// <para>en-us:Gets or initializes the message handling failure reason.</para>
        /// </summary>
        public MessageQueueFailureReason FailureReason { get; init; }

        /// <summary>
        /// <para>zh-cn:获取或初始化 Kafka 原生消费结果；为避免标准层依赖 Kafka 类型，这里以 object 暴露。</para>
        /// <para>en-us:Gets or initializes the native Kafka consume result. It is exposed as object to avoid Kafka type dependency in the standard layer.</para>
        /// </summary>
        public object NativeConsumeResult { get; init; }
    }
}
