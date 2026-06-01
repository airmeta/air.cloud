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
namespace Air.Cloud.Core.Modules.DynamicApp.Model
{
    /// <summary>
    /// <para>zh-cn:动态模块配置模型，描述入口程序集、依赖程序集和模块设置。</para>
    /// <para>en-us:Dynamic module configuration model describing the entry assembly, dependent assemblies, and module settings.</para>
    /// </summary>
    public class ModInformation
    {
        /// <summary>
        /// 入口程序集
        /// </summary>
        public string Entry { get; set; }
        /// <summary>
        /// 需要加载的程序集
        /// </summary>
        public List<ModAssemblyInformation> Assemblies { get; set; }
        /// <summary>
        /// 模组配置
        /// </summary>
        public ModSettings Settings { get; set; }
    }
}
