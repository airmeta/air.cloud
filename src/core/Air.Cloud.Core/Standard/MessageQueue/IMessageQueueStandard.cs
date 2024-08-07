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
using Air.Cloud.Core.Standard.MessageQueue.Model;
using Air.Cloud.Core.Standard.MessageQueue.Provider;

namespace Air.Cloud.Core.Standard.MessageQueue
{
    /// <summary>
    /// 主题行为
    /// </summary>
    public interface IMessageQueueStandard
    {
        /// <summary>
        /// 发布消息
        /// </summary>
        /// <param name="producerConfigModel">发布配置</param>
        /// <param name="Content">消息内容约定</param>
        public void Publish<TTopicPublishConfig, TMessageContentStandard>
                (ITopicPublishConfig<TTopicPublishConfig> producerConfigModel, TMessageContentStandard Content)
                where TTopicPublishConfig : class
                where TMessageContentStandard : class, new();
        /// <summary>
        /// 订阅消息
        /// </summary>
        /// <param name="subscribeConfig">订阅配置</param>
        /// <param name="action">订阅操作</param>
        /// <param name="GroupId">组信息</param>
        public void Subscribe<TTopicSubscribeConfig, TMessageContentStandard>
            (ITopicSubscribeConfig<TTopicSubscribeConfig> subscribeConfig,
            Action<TMessageContentStandard> action,string GroupId=null)
               where TTopicSubscribeConfig : class
                where TMessageContentStandard : class, new();
    }
}
