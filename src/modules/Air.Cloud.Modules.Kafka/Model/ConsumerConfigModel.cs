/*
 * Copyright (c) 2024 星曳数据
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
    /// 消费配置类
    /// </summary>
    public class ConsumerConfigModel:ITopicSubscribeConfig<ConsumerConfig>
    {
        /// <summary>
        /// 消费配置
        /// </summary>
        public ConsumerConfig Config { get; set; }

        /// <summary>
        /// topic名称
        /// </summary>
        public string TopicName { get; set; }
    }
}
