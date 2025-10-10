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
using Air.Cloud.Core.Standard.SchedulerStandard.Attributes;

namespace Air.Cloud.Core.Standard.SchedulerStandard
{
    /// <summary>
    /// <para>zh-cn:调度标准配置选项</para>
    /// <para>en-us:Scheduler standard options</para>
    /// </summary>
    public interface ISchedulerStandardOptions
    {
        /// <summary>
        /// <para>zh-cn:编号</para>
        /// <para>en-us:Id</para>
        /// </summary>
        public string Id { get; set; }
        /// <summary>
        /// <para>zh-cn:名称</para>
        /// <para>en-us:MainAssemblyName</para>
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// <para>zh-cn:描述</para>
        /// <para>en-us:Description</para>
        /// </summary>
        public string Description { get; set; }
        /// <summary>
        /// <para>zh-cn:表达式</para>
        /// <para>en-us:CronExpression</para>
        /// </summary>
        public string CronExpression { get; set; }

        /// <summary>
        /// <para>zh-cn:任务组</para>
        /// <para>en-us:Group name</para>
        /// </summary>
        public string GroupName { get; set; }

        /// <summary>
        /// <para>zh-cn:根据配置的特性来生成配置项</para>
        /// <para>en-us:Set property by SchedulerInformationAttribute</para>
        /// </summary>
        /// <param name="schedulerInformationAttribute">
        /// <para>zh-cn:配置特性</para>
        /// <para>en-us:Attribute</para>
        /// </param>
        public void Setter(SchedulerInformationAttribute schedulerInformationAttribute);

    }
}
