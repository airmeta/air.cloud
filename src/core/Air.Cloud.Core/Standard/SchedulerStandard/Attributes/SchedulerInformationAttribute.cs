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
using Air.Cloud.Core.Standard.SchedulerStandard.Extensions;

namespace Air.Cloud.Core.Standard.SchedulerStandard.Attributes
{
    /// <summary>
    /// <para>zh-cn:通过特性来显示配置定时调度的运行计划</para>
    /// <para>en-us:Display the running plan for configuring scheduled scheduling through features</para>
    /// </summary>
    public class SchedulerInformationAttribute : Attribute
    {
        /// <summary>
        /// <para>zh-cn:编号</para>
        /// <para>en-us:Id</para>
        /// </summary>
        public string Id { get; set; }
        /// <summary>
        /// <para>zh-cn:名称</para>
        /// <para>en-us:Name</para>
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// <para>zh-cn:描述</para>
        /// <para>en-us:Description</para>
        /// </summary>
        public string Description { get; set; }
        /// <summary>
        /// <para>zh-cn:定时任务表达式</para>
        /// <para>en-us:CronExpression</para>
        /// </summary>
        public string CronExpression { get; set; }

        /// <summary>
        /// <para>zh-cn:任务组</para>
        /// <para>en-us:Group name</para>
        /// </summary>
        public string GroupName { get; set; } = "default";
        /// <summary>
        /// <para>zh-cn:构造函数</para>
        /// <para>en-us:Constructor</para>
        /// </summary>
        /// <param name="id">
        /// <para>zh-cn:编号</para>
        /// <para>en-us:Id</para>
        /// </param>
        /// <param name="name">
        /// <para>zh-cn:名称</para>
        /// <para>en-us:Name</para>
        /// </param>
        /// <param name="groupName">
        /// <para>zh-cn:任务组</para>
        /// <para>en-us:Group name</para>
        /// </param>
        /// <param name="description">
        /// <para>zh-cn:描述</para>
        /// <para>en-us:Description</para>
        /// </param>
        /// <param name="cronExpression">
        /// <para>zh-cn:定时任务表达式</para>
        /// <para>en-us:CronExpression</para>
        /// </param>
        public SchedulerInformationAttribute(string id="air.cloud.schduler.001", string name="Scheduler", string groupName = "default", string description = "Scheduler", string cronExpression =null)
        {
            Id = id;
            Name = name;
            Description = description;
            CronExpression = cronExpression;
            GroupName= groupName;
            if (CronExpression.IsNullOrEmpty()) AppRealization.Output.Error(new Exception("调度任务参数配置错误"));
        }
        /// <summary>
        /// <para>zh-cn:默认构造函数</para>
        /// <para>en-us:default constractor</para>
        /// </summary>
        public SchedulerInformationAttribute() { }
    }
}
