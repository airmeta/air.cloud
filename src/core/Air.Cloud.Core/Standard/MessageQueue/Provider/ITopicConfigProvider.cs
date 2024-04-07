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

namespace Air.Cloud.Core.Standard.MessageQueue.Provider
{
    /// <summary>
    /// 主题配置行为
    /// </summary>
    public interface ITopicConfigProvider
    {
        /// <summary>
        /// 设置主题的配置
        /// </summary>
        /// <param name="topicConfig">主题配置</param>
        /// <param name="topicInfo">主题信息</param>

        public void SetConfig(ITopicInfo topicInfo, ITopicConfig topicConfig);
        /// <summary>
        ///获取主题的配置
        /// </summary>
        /// <param name="topicConfig">主题配置</param>
        /// <param name="topicInfo">主题信息</param>
        public void GetConfig(ITopicInfo topicInfo, ITopicConfig topicConfig);
    }

}
