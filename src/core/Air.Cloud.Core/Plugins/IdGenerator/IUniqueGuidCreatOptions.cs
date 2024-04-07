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
namespace Air.Cloud.Core.Plugins.IdGenerator
{
    /// <summary>
    /// 序列生成配置信息
    /// </summary>
    public interface IUniqueGuidCreatOptions
    {
        /// <summary>
        /// 当前时间
        /// </summary>
        DateTimeOffset? TimeNow { get; set; }
        /// <summary>
        /// LittleEndianBinary 16 格式化
        /// </summary>
        bool LittleEndianBinary16Format { get; set; }
    }
}
