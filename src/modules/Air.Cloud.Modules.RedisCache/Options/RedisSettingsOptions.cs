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
namespace Air.Cloud.Modules.RedisCache.Options
{
    /// <summary>
    /// Redis配置项
    /// </summary>
    public  class RedisSettingsOptions
    {
        /// <summary>
        /// Redis连接字符串
        /// </summary>
        public string? ConnectionString { get; set; }
    }
}
