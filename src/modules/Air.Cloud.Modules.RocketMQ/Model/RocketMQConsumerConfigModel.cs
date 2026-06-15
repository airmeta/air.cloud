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
    /// <para>zh-cn:RocketMQ 消费配置模型，绑定 Air.Cloud 订阅标准中的 TopicName、Config 和 KeyType。</para>
    /// <para>en-us:RocketMQ consumer configuration model that binds TopicName, Config, and KeyType from the Air.Cloud subscription standard.</para>
    /// </summary>
    public sealed class RocketMQConsumerConfigModel : ITopicSubscribeConfig<RocketMQConsumerConfig>
    {
        /// <summary>
        /// <para>zh-cn:RocketMQ 消费者配置。</para>
        /// <para>en-us:RocketMQ consumer configuration.</para>
        /// </summary>
        public RocketMQConsumerConfig Config { get; set; }

        /// <summary>
        /// <para>zh-cn:订阅目标主题名称。</para>
        /// <para>en-us:Target topic name to subscribe to.</para>
        /// </summary>
        public string TopicName { get; set; }

        /// <summary>
        /// <para>zh-cn:消息 Key 类型；RocketMQ 消费侧不反序列化强类型 Key，此属性保留用于符合标准配置模型。</para>
        /// <para>en-us:Message key type. RocketMQ consumers do not deserialize strongly typed keys; this property is retained to satisfy the standard configuration model.</para>
        /// </summary>
        public Type KeyType { get; set; } = typeof(string);
    }
}
