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

using System.Reflection;

namespace Air.Cloud.Modules.Quartz.Factory
{
    /// <summary>
    ///  <para>zh-cn: 调度工厂</para>
    ///  <para>en-us: Scheduler factory</para>
    /// </summary>
    /// <typeparam name="TSchedulerStandardOptions">
    ///  <para>zh-cn:需要创建的类型配置</para>
    ///  <para>en-us:Scheduler standard options</para>
    /// </typeparam>
    public class SchedulerStandardFactory<TSchedulerStandardOptions> : ISchedulerStandardFactory<TSchedulerStandardOptions> where TSchedulerStandardOptions : class, ISchedulerStandardOptions, new()
    {
       /// <inheritdoc/>
        public TSchedulerStandardOptions GetSchedulerConfiguration<TScheduler>() where TScheduler : class, ISchedulerStandard<TSchedulerStandardOptions>
        {
            var attr = typeof(TScheduler).GetCustomAttribute<SchedulerInformationAttribute>();
            if (attr == null) throw new Exception("SchedulerInformationAttribute is not found");
            TSchedulerStandardOptions quartzSchedulerStandardOptions = new TSchedulerStandardOptions();
            quartzSchedulerStandardOptions.Setter(attr);
            return quartzSchedulerStandardOptions;
        }
    }
}
