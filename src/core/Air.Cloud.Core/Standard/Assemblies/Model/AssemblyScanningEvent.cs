
﻿/*
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
namespace Air.Cloud.Core.Standard.Assemblies.Model
{
    /// <summary>
    /// <para>zh-cn:类库扫描事件</para>
    /// <para>en-us:Assembly Execute Event</para>
    /// </summary>
    /// <remarks>
    ///   <para>zh-cn:用于在类库扫描过程中定义特定的事件和操作,TargetType为目标类型,Action为事件动作,组合起来就是扫描到目标类型时执行动作</para>
    ///   <para>en-us:Used to define specific events and actions during the assembly scanning process. TargetType is the target type, and Action is the event action. Together, they execute the action when the target type is scanned.</para>
    /// </remarks>
    public class AssemblyScanningEvent
    {
        /// <summary>
        /// <para>zh-cn:事件键</para>
        /// <para>en-us:Event Key</para>
        /// </summary>
        public string Key { get; set; }

        /// <summary>
        /// <para>zh-cn:事件描述</para>
        /// <para>en-us:Event Description</para>
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// <para>zh-cn:事件动作</para>
        /// <para>en-us:Event Action</para> 
        /// </summary>
        public Action<Type> Action { get; set; }


        /// <summary>
        /// <para>zh-cn:目标类型</para>
        /// <para>en-us:Target Type</para>
        /// </summary>
        public Type TargetType { get; set; }


        /// <summary>
        /// <para>zh-cn:最终动作</para>
        /// <para>en-us:Finally Action</para>
        /// </summary>
        public Action Finally { get; set; }

    }
}
