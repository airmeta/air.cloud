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

using System.Reflection;

namespace Air.Cloud.Core.Modules.DynamicApp.Model
{
    /// <summary>
    /// <para>zh-cn:动态应用信息</para>
    /// <para>en-us: Dynamic application information</para>
    /// </summary>
    public class DynamicAppInformation
    {
        /// <summary>
        /// <para>zh-cn:动态应用名称</para>
        /// <para>en-us: Dynamic application name</para>
        /// </summary>
        public Type Type { get; set; }
        /// <summary>
        /// <para>zh-cn:动态应用用途</para>
        /// <para>en-us: Dynamic application usage</para>
        /// </summary>
        public DynamicAppUsage Usage { get; set; }
        /// <summary>
        /// <para>zh-cn:动态应用加载顺序类型</para>
        /// <para>en-us: Dynamic application load order type</para>
        /// </summary>
        public DynamicPluginOrder OrderType { get; set; }

        /// <summary>
        /// <para>zh-cn:动态应用加载顺序</para>
        /// <para>en-us: Dynamic application load order</para>
        /// </summary>
        public int OrderNumber { get; set; }

        /// <summary>
        /// <para>zh-cn:动态应用程序集名称</para>
        /// <para>en-us: Dynamic application assembly name</para>
        /// </summary>
        public AssemblyName Assembly { get; set; }
    }
}
