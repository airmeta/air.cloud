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
namespace Air.Cloud.Modules.Kafka.Topic
{
    public class TopicCreateResult
    {
        /// <summary>
        /// topic 创建 结果
        /// </summary>
        public bool State { get; set; } = false;
        /// <summary>
        /// 提示信息
        /// </summary>
        public string Message { get; set; } = "创建失败";

        /// <summary>
        /// 创建成功之后的topic信息
        /// </summary>
        public TopicInfo Info { get; set; } = null;
    }
}
