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
using Air.Cloud.Core.Standard.SchedulerStandard;
using Air.Cloud.Core.Standard.SchedulerStandard.Attributes;

namespace Air.Cloud.Modules.Quartz.Options
{
    public class QuartzSchedulerStandardOptions : ISchedulerStandardOptions
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string CronExpression { get; set; }
        public string GroupName { get; set; }
        public QuartzSchedulerStandardOptions() { }
        public QuartzSchedulerStandardOptions(string id, string name, string description, string cronExpression)
        {
            Id = id;
            Name = name;
            Description = description;
            CronExpression = cronExpression;
        }
        /// <summary>
        /// <para>zh-cn:使用SchedulerInformationAttribute初始化</para>
        /// <para>en-us:Initialize using SchedulerInformationAttribute</para>
        /// </summary>
        /// <param name="attribute">
        /// 调度配置信息
        /// </param>
        public QuartzSchedulerStandardOptions(SchedulerInformationAttribute attribute)
        {
            Id = attribute.Id;
            Name = attribute.Name;
            Description = attribute.Description;
            CronExpression = attribute.CronExpression;
            GroupName = attribute.GroupName;
        }
        /// <summary>
        /// <para>zh-cn:使用SchedulerInformationAttribute初始化</para>
        /// <para>en-us:Initialize using SchedulerInformationAttribute</para>
        /// </summary>
        /// <param name="attribute">
        /// 调度配置信息
        /// </param>
        public void Setter(SchedulerInformationAttribute attribute)
        {
            Id = attribute.Id;
            Name = attribute.Name;
            Description = attribute.Description;
            CronExpression = attribute.CronExpression;
            GroupName = attribute.GroupName;
        }
    }
}
