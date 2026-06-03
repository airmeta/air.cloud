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

namespace Air.Cloud.Modules.Kafka.Utils
{
    /// <summary>
    /// <para>zh-cn:Kafka 默认 int 类型消息 Key 生成器，保持 Kafka 模块默认使用随机 int Key 的行为。</para>
    /// <para>en-us:Default Kafka int message key generator that keeps the Kafka module behavior of using random int keys by default.</para>
    /// </summary>
    public sealed class KafkaIntMessageQueueKeyGenerator : IMessageQueueKeyGenerator<int>
    {
        /// <summary>
        /// <para>zh-cn:获取当前生成器支持的消息 Key 类型。</para>
        /// <para>en-us:Gets the message key type supported by the current generator.</para>
        /// </summary>
        /// <returns>
        /// <para>zh-cn:int 类型。</para>
        /// <para>en-us:The int type.</para>
        /// </returns>
        public Type GetKeyType()
        {
            return typeof(int);
        }

        /// <summary>
        /// <para>zh-cn:生成 int 类型 Kafka 消息 Key。</para>
        /// <para>en-us:Generates an int Kafka message key.</para>
        /// </summary>
        /// <param name="context">
        /// <para>zh-cn:消息队列 Key 生成上下文；默认随机 int 生成器不依赖上下文内容。</para>
        /// <para>en-us:The message-queue key generation context. The default random int generator does not depend on the context content.</para>
        /// </param>
        /// <returns>
        /// <para>zh-cn:随机 int Key。</para>
        /// <para>en-us:A random int key.</para>
        /// </returns>
        public int Generate(IMessageQueueKeyGenerationContext context)
        {
            return KafkaRandomKey.GetRandom();
        }

        /// <summary>
        /// <para>zh-cn:生成 object 形式的 Kafka 消息 Key，供非泛型运行时调用。</para>
        /// <para>en-us:Generates a Kafka message key as object for non-generic runtime calls.</para>
        /// </summary>
        /// <param name="context">
        /// <para>zh-cn:消息队列 Key 生成上下文。</para>
        /// <para>en-us:The message-queue key generation context.</para>
        /// </param>
        /// <returns>
        /// <para>zh-cn:装箱后的 int Key。</para>
        /// <para>en-us:The boxed int key.</para>
        /// </returns>
        object IMessageQueueKeyGenerator.Generate(IMessageQueueKeyGenerationContext context)
        {
            return Generate(context);
        }
    }
}
