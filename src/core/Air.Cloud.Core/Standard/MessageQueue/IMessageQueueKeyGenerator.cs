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
    /// <para>zh-cn:定义消息队列 Key 生成器的非泛型标准，用于运行时发现生成器支持的 Key 类型并以统一方式生成 Key。</para>
    /// <para>en-us:Defines the non-generic message-queue key generator contract used to discover the supported key type at runtime and generate keys in a unified way.</para>
    /// </summary>
    /// <remarks>
    /// <para>zh-cn:消息队列实现可以通过依赖注入获取该接口集合，并根据发布或订阅配置中的 KeyType 选择合适的生成器。</para>
    /// <para>en-us:Message-queue implementations may resolve this interface collection from dependency injection and select the proper generator by the KeyType configured for publishing or subscribing.</para>
    /// </remarks>
    public interface IMessageQueueKeyGenerator
    {
        /// <summary>
        /// <para>zh-cn:获取当前生成器支持生成的消息 Key 类型。</para>
        /// <para>en-us:Gets the message key type supported by the current generator.</para>
        /// </summary>
        /// <returns>
        /// <para>zh-cn:消息 Key 类型。</para>
        /// <para>en-us:The message key type.</para>
        /// </returns>
        public Type GetKeyType();

        /// <summary>
        /// <para>zh-cn:根据消息队列 Key 生成上下文生成消息 Key。</para>
        /// <para>en-us:Generates a message key from the message-queue key generation context.</para>
        /// </summary>
        /// <param name="context">
        /// <para>zh-cn:消息队列 Key 生成上下文。</para>
        /// <para>en-us:The message-queue key generation context.</para>
        /// </param>
        /// <returns>
        /// <para>zh-cn:生成后的消息 Key，返回类型应与 GetKeyType() 保持一致。</para>
        /// <para>en-us:The generated message key. Its runtime type should be consistent with GetKeyType().</para>
        /// </returns>
        public object Generate(IMessageQueueKeyGenerationContext context);
    }

    /// <summary>
    /// <para>zh-cn:定义消息队列 Key 生成器的强类型标准，用于为指定 TKey 类型生成消息 Key。</para>
    /// <para>en-us:Defines the strongly typed message-queue key generator contract used to generate message keys for the specified TKey type.</para>
    /// </summary>
    /// <typeparam name="TKey">
    /// <para>zh-cn:消息 Key 类型，例如 int、string 或 Guid。</para>
    /// <para>en-us:The message key type, such as int, string, or Guid.</para>
    /// </typeparam>
    public interface IMessageQueueKeyGenerator<TKey> : IMessageQueueKeyGenerator
    {
        /// <summary>
        /// <para>zh-cn:获取当前泛型生成器支持的消息 Key 类型，默认返回 typeof(TKey)。</para>
        /// <para>en-us:Gets the message key type supported by the current generic generator. The default implementation returns typeof(TKey).</para>
        /// </summary>
        /// <returns>
        /// <para>zh-cn:TKey 对应的 Type。</para>
        /// <para>en-us:The Type that corresponds to TKey.</para>
        /// </returns>
        Type IMessageQueueKeyGenerator.GetKeyType()
        {
            return typeof(TKey);
        }

        /// <summary>
        /// <para>zh-cn:根据消息队列 Key 生成上下文生成强类型消息 Key。</para>
        /// <para>en-us:Generates a strongly typed message key from the message-queue key generation context.</para>
        /// </summary>
        /// <param name="context">
        /// <para>zh-cn:消息队列 Key 生成上下文。</para>
        /// <para>en-us:The message-queue key generation context.</para>
        /// </param>
        /// <returns>
        /// <para>zh-cn:生成后的强类型消息 Key。</para>
        /// <para>en-us:The generated strongly typed message key.</para>
        /// </returns>
        public new TKey Generate(IMessageQueueKeyGenerationContext context);

        /// <summary>
        /// <para>zh-cn:生成 object 形式的消息 Key，默认调用强类型 Generate 方法，减少业务侧实现负担。</para>
        /// <para>en-us:Generates a message key as object. The default implementation calls the strongly typed Generate method to reduce the implementation burden for business code.</para>
        /// </summary>
        /// <param name="context">
        /// <para>zh-cn:消息队列 Key 生成上下文。</para>
        /// <para>en-us:The message-queue key generation context.</para>
        /// </param>
        /// <returns>
        /// <para>zh-cn:装箱后的消息 Key。</para>
        /// <para>en-us:The boxed message key.</para>
        /// </returns>
        object IMessageQueueKeyGenerator.Generate(IMessageQueueKeyGenerationContext context)
        {
            return Generate(context);
        }
    }
}
