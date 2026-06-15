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

namespace Air.Cloud.Modules.RocketMQ.Utils
{
    /// <summary>
    /// <para>zh-cn:RocketMQ 默认字符串 Key 生成器，使用 Guid.NewGuid() 生成消息 Key。</para>
    /// <para>en-us:Default RocketMQ string key generator that uses Guid.NewGuid() to create message keys.</para>
    /// </summary>
    public sealed class RocketMQStringMessageQueueKeyGenerator : IMessageQueueKeyGenerator<string>
    {
        /// <summary>
        /// <para>zh-cn:生成 RocketMQ 字符串消息 Key。</para>
        /// <para>en-us:Generates a RocketMQ string message key.</para>
        /// </summary>
        /// <param name="context">
        /// <para>zh-cn:消息队列 Key 生成上下文。</para>
        /// <para>en-us:The message-queue key generation context.</para>
        /// </param>
        /// <returns>
        /// <para>zh-cn:生成后的字符串 Key。</para>
        /// <para>en-us:The generated string key.</para>
        /// </returns>
        public string Generate(IMessageQueueKeyGenerationContext context)
        {
            return Guid.NewGuid().ToString("N");
        }
    }
}
