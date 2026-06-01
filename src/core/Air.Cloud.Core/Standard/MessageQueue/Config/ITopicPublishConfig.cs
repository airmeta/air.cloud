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
    }
}
