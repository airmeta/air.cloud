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

namespace Air.Cloud.Core.Modules.DynamicApp.Attributes
{
    /// <summary>
    /// <para>zh-cn:插件注入特性</para>
    /// <para>en-us:Plugin injection attribute</para>
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class PluginInjectAttribute : Attribute
    {
        /// <summary>
        ///  <para>zh-cn:当前插件在何处应用</para>
        ///  <para>en-us:Where the plugin is applied</para>
        /// </summary>
        public DynamicAppUsage Usage { get; set; }
        /// <summary>
        ///  <para>zh-cn:同Usage同OrderType的插件之间的顺序</para>
        ///  <para>en-us:The order among plugins with the same Usage and OrderType</para>
        /// </summary>
        public int Order { get; set; }
        /// <summary>
        /// <para>zh-cn:插件排序类型</para>
        /// <para>en-us:Plugin sorting type</para>
        /// </summary>
        public DynamicPluginOrder OrderType { get; set; }

    }
}
