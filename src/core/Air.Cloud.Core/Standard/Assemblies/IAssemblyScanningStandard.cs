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
using System.Collections.Concurrent;

namespace Air.Cloud.Core.Standard.Assemblies
{
    /// <summary>
    /// 类库扫描标准
    /// </summary>
    public interface IAssemblyScanningStandard
    {
        /// <summary>
        /// 扫描程序集类型时的动作
        /// </summary>
        public static ConcurrentDictionary<string, Action<Type>> Evensts = new ConcurrentDictionary<string, Action<Type>>();
        /// <summary>
        /// 开始执行扫描
        /// </summary>
        public void Scanning();
        /// <summary>
        /// 添加动作
        /// </summary>
        public void Add(KeyValuePair<string,Action<Type>> keyValuePair);
    }
}
