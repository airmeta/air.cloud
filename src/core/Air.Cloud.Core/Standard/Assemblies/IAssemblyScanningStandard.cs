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
using Air.Cloud.Core.Collections;
using Air.Cloud.Core.Standard.Assemblies.Model;

using System.Collections.Concurrent;

namespace Air.Cloud.Core.Standard.Assemblies
{
    /// <summary>
    /// 类库扫描标准
    /// </summary>
    public interface IAssemblyScanningStandard
    {
        /// <summary>
        /// <para>zh-cn:扫描事件集合</para>
        /// <para>en-us:Assembly scanning event collection</para>
        /// </summary>
        public static ConcurrentList<AssemblyScanningEvent> Evensts = new ConcurrentList<AssemblyScanningEvent>();
        /// <summary>
        /// <para>zh-cn:执行扫描</para>
        /// <para>en-us:Execute scanning</para>
        /// </summary>
        public void Execute();
        /// <summary>
        /// <para>zh-cn:添加扫描事件</para>
        /// <para>en-us:Add assembly scanning event</para>
        /// </summary>
        public void Add(AssemblyScanningEvent Event);
    }
}
