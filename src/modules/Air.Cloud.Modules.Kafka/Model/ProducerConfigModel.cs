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
    /// 生产配置类
    /// </summary>
    public sealed class ProducerConfigModel: ITopicPublishConfig<ProducerConfig>
    {
        /// <summary>
        /// 生产配置
        /// </summary>
        public ProducerConfig Config { get; set; }
        /// <summary>
        /// 主题信息
        /// </summary>
        public string TopicName { get; set; }
    }
}
