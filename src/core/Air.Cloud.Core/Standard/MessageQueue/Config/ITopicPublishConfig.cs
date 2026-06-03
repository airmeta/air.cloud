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
using Air.Cloud.Core.Standard.MessageQueue.Model;

namespace Air.Cloud.Core.Standard.MessageQueue.Config
{
    /// <summary>
    /// <para>zh-cn:定义主题发布配置标准，绑定具体发布配置对象和主题名称。</para>
    /// <para>en-us:Defines the topic publish configuration contract, binding a concrete publish configuration object and topic name.</para>
    /// </summary>
    /// <typeparam name="TConfig">
    /// <para>zh-cn:具体消息队列实现使用的发布配置类型。</para>
    /// <para>en-us:The publish configuration type used by a concrete message-queue implementation.</para>
    /// </typeparam>
    public interface ITopicPublishConfig<TConfig> where TConfig:class
    {
        /// <summary>
        /// <para>zh-cn:获取或设置具体消息队列实现的发布配置对象。</para>
        /// <para>en-us:Gets or sets the publish configuration object for the concrete message-queue implementation.</para>
        /// </summary>
        public TConfig Config { get; set; }
        /// <summary>
        /// <para>zh-cn:获取或设置发布目标主题名称。</para>
        /// <para>en-us:Gets or sets the target topic name to publish to.</para>
        /// </summary>
        public string TopicName { get; set; }

        /// <summary>
        /// <para>zh-cn:获取或设置发布消息使用的 Key 类型；消息队列实现会据此选择对应的 Key 生成器和底层生产者类型。</para>
        /// <para>en-us:Gets or sets the key type used by published messages. Message-queue implementations use this value to select the corresponding key generator and underlying producer type.</para>
        /// </summary>
        /// <remarks>
        /// <para>zh-cn:当未显式设置时，具体消息队列模块可以使用自身默认值，例如 Kafka 模块默认使用 int。</para>
        /// <para>en-us:When this value is not explicitly set, a concrete message-queue module may use its own default value, for example int in the Kafka module.</para>
        /// </remarks>
        public Type KeyType { get; set; }
    }
}
