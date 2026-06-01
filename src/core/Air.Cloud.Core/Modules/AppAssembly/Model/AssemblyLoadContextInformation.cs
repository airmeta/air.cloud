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
using System.Reflection;
using System.Runtime.Loader;

namespace Air.Cloud.Core.Modules.AppAssembly.Model
{
    /// <summary>
    /// <para>zh-cn:表示应用程序集装载上下文信息，用于记录程序集路径、程序集标识、装载上下文和装载时间。</para>
    /// <para>en-us:Represents application assembly load-context information, recording assembly path, assembly identity, load context, and load time.</para>
    /// </summary>
    public  class AssemblyLoadContextInformation
    {
        /// <summary>
        /// <para>zh-cn:获取或设置装载记录名称。</para>
        /// <para>en-us:Gets or sets the load record name.</para>
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// <para>zh-cn:获取或设置程序集文件路径。</para>
        /// <para>en-us:Gets or sets the assembly file path.</para>
        /// </summary>
        public string AssemblyPath { get; set; }

        /// <summary>
        /// <para>zh-cn:获取或设置程序集名称信息。</para>
        /// <para>en-us:Gets or sets the assembly name information.</para>
        /// </summary>
        public AssemblyName AssemblyName { get; set; }

        /// <summary>
        /// <para>zh-cn:获取或设置已装载的程序集实例。</para>
        /// <para>en-us:Gets or sets the loaded assembly instance.</para>
        /// </summary>
        public Assembly Assembly{ get; set; }

        /// <summary>
        /// <para>zh-cn:获取或设置承载该程序集的装载上下文。</para>
        /// <para>en-us:Gets or sets the load context that hosts the assembly.</para>
        /// </summary>
        public AssemblyLoadContext Context { get; set; }

        /// <summary>
        /// <para>zh-cn:获取或设置程序集装载时间。</para>
        /// <para>en-us:Gets or sets the assembly load time.</para>
        /// </summary>
        public DateTime LoadTime { get; set; }
    }
}
