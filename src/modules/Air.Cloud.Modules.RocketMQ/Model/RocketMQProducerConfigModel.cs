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
using Air.Cloud.Core.Standard.MessageQueue.Config;

namespace Air.Cloud.Modules.RocketMQ.Model
{
    /// <summary>
    /// <para>zh-cn:RocketMQ 生产配置模型，绑定 Air.Cloud 发布标准中的 TopicName、Config 和 KeyType。</para>
    /// <para>en-us:RocketMQ producer configuration model that binds TopicName, Config, and KeyType from the Air.Cloud publish standard.</para>
    /// </summary>
    public sealed class RocketMQProducerConfigModel : ITopicPublishConfig<RocketMQProducerConfig>
    {
        /// <summary>
        /// <para>zh-cn:RocketMQ 生产者配置。</para>
        /// <para>en-us:RocketMQ producer configuration.</para>
        /// </summary>
        public RocketMQProducerConfig Config { get; set; }

        /// <summary>
        /// <para>zh-cn:发布目标主题名称。</para>
        /// <para>en-us:Target topic name to publish to.</para>
        /// </summary>
        public string TopicName { get; set; }

        /// <summary>
        /// <para>zh-cn:消息 Key 类型；RocketMQ SDK 使用字符串 Key 集合，因此默认并建议使用 string。</para>
        /// <para>en-us:Message key type. The RocketMQ SDK uses a string key collection, so string is the default and recommended type.</para>
        /// </summary>
        public Type KeyType { get; set; } = typeof(string);
    }
}
