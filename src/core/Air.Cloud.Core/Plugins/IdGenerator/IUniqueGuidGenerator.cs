
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
using Air.Cloud.Core.Plugins;

namespace Air.Cloud.Core.Plugins.IdGenerator
{
    /// <summary>
    /// 唯一Guid生成器 连续的结果
    /// </summary>
    public interface IUniqueGuidGenerator : IPlugin
    {
        /// <summary>
        /// 生成UniqueGuid 
        /// </summary>
        /// <param name="Options">生成时的参数</param>
        /// <returns></returns>
        object Create<T>(T Options = default) where T : IUniqueGuidCreatOptions, new();
    }
}
