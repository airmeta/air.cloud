﻿/*
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
    /// 主题订阅配置
    /// </summary>
    public interface ITopicSubscribeConfig<TTopicSubscribeConfig> where TTopicSubscribeConfig:class
    {
        public TTopicSubscribeConfig Config { get; set; }
        public string TopicName { get; set; }
    }

}
