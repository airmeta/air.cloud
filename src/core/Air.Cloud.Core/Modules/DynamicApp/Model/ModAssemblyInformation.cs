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
using Air.Cloud.Core.Modules.DynamicApp.Enums;

namespace Air.Cloud.Core.Modules.DynamicApp.Model
{
    public  class ModAssemblyInformation
    {
        /// <summary>
        /// 程序集名称
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// 程序集类型
        /// </summary>
        public DynamicModAssemblyUseType[] Type { get; set; }
        
    }
}
