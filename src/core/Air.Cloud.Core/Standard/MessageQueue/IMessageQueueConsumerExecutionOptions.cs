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
    /// <para>zh-cn:定义消息队列消费者执行策略标准，用于描述单条消息处理超时、最大重试次数等与具体队列平台无关的执行规则。</para>
    /// <para>en-us:Defines the message-queue consumer execution options contract used to describe provider-neutral execution rules such as per-message handling timeout and maximum retry count.</para>
    /// </summary>
    public interface IMessageQueueConsumerExecutionOptions
    {
        /// <summary>
        /// <para>zh-cn:获取单条消息业务处理超时时间；为 null 时表示不由框架限制单条消息处理时间。</para>
        /// <para>en-us:Gets the business handling timeout for a single message. A null value means the framework does not limit per-message handling time.</para>
        /// </summary>
        public TimeSpan? MessageHandlingTimeout { get; }

        /// <summary>
        /// <para>zh-cn:获取单条消息最大重试次数；具体消息队列模块可以将该值映射到本地重试、重试队列或死信策略。</para>
        /// <para>en-us:Gets the maximum retry count for a single message. Concrete message-queue modules may map this value to local retry, retry queues, or dead-letter policies.</para>
        /// </summary>
        public int MaxRetryCount { get; }
    }
}
