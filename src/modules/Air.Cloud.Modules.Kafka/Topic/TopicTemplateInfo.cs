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
using Confluent.Kafka;
using Confluent.Kafka.Admin;

namespace Air.Cloud.Modules.Kafka.Topic
{
    /// <summary>
    /// topic模板配置
    /// </summary>
    public class TopicTemplateInfo
    {
        /// <summary>
        /// 主题信息
        /// </summary>
        public TopicSpecification Topic { get; set; } = new TopicSpecification();

        /// <summary>
        /// 连接信息
        /// </summary>
        public AdminClientConfig Config { get; set; } = new AdminClientConfig();
    }
}
