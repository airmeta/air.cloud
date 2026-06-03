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

using Confluent.Kafka;

namespace Air.Cloud.Modules.Kafka.Model
{
    /// <summary>
    /// 生产配置类
    /// </summary>
    public sealed class ProducerConfigModel: ITopicPublishConfig<ProducerConfig>
    {
        /// <summary>
        /// <para>zh-cn:生产配置。</para>
        /// <para>en-us:Producer configuration.</para>
        /// </summary>
        public ProducerConfig Config { get; set; }
        /// <summary>
        /// <para>zh-cn:主题信息。</para>
        /// <para>en-us:Topic information.</para>
        /// </summary>
        public string TopicName { get; set; }

        /// <summary>
        /// <para>zh-cn:消息 Key 类型，默认保持 Kafka 模块原有 int Key 行为；调用方可设置为 string、Guid 等其他 Kafka 支持的 Key 类型。</para>
        /// <para>en-us:Message key type. The default keeps the original int-key behavior of the Kafka module. Callers may set this to other Kafka-supported key types, such as string or Guid.</para>
        /// </summary>
        public Type KeyType { get; set; } = typeof(int);
    }
}
