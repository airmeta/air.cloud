/*
 * Copyright (c) 2024 星曳数据
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
using Air.Cloud.Core.Standard.SchedulerStandard.Pool;
using Air.Cloud.Modules.Quartz.Options;
using System.Reflection;

namespace Air.Cloud.Modules.Quartz.Factory
{
    public class SchedulerStandardFactory : ISchedulerStandardFactory<QuartzSchedulerStandardOptions>
    {
        public QuartzSchedulerStandardOptions GetSchedulerConfiguration<TScheduler>() where TScheduler : class, ISchedulerStandard<QuartzSchedulerStandardOptions>
        {
            var attr = typeof(TScheduler).GetCustomAttribute<SchedulerInformationAttribute>();

            if (attr == null) throw new Exception("SchedulerInformationAttribute is not found");

            QuartzSchedulerStandardOptions quartzSchedulerStandardOptions = new QuartzSchedulerStandardOptions(attr);

            return quartzSchedulerStandardOptions;
        }
    }
}
