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
namespace Air.Cloud.Core.Enums
{
    /// <summary>
    /// 加载配置文件的方式
    /// </summary>
    public enum LoadConfigurationTypeEnum
    {
        /// <summary>
        /// 加载本地配置文件
        /// </summary>
        File,
        /// <summary>
        /// 从远程加载配置文件
        /// 核心库中未实现 请使用相关的配置中心模组
        /// </summary>
        Remote
    }
}
