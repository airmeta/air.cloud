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
using Air.Cloud.Core.Standard.SchedulerStandard.Pool;

namespace Air.Cloud.Core.Standard.SchedulerStandard
{
    /// <summary>
    ///  <para>zh-cn: 调度工厂</para>
    ///  <para>en-us: Scheduler factory</para>
    /// </summary>
    /// <typeparam name="TScheduler">
    ///  <para>zh-cn:需要创建的类型</para>
    ///  <para>en-us:Types that need to be created</para>
    /// </typeparam>
    public interface ISchedulerStandardFactory<TSchedulerStandardOptions>
        //where TScheduler : class,ISchedulerStandard<TSchedulerStandardOptions>,new()
        where TSchedulerStandardOptions : class,ISchedulerStandardOptions
    {
        /// <summary>
        /// <para>zh-cn:调度池</para>
        /// <para>en-us:Scheduler pool</para>
        /// </summary>
        public static SchedulerPool<TSchedulerStandardOptions> SchedulerPool =new SchedulerPool<TSchedulerStandardOptions>();
        /// <summary>
        /// <para>zh-cn:创建调度</para>
        /// <para>en-us:Create scheduler</para>
        /// </summary>
        /// <returns></returns>
        public TSchedulerStandardOptions GetSchedulerConfiguration<TScheduler>() where TScheduler : class, ISchedulerStandard<TSchedulerStandardOptions>;

        /// <summary>
        /// <para>zh-cn:创建调度</para>
        /// <para>en-us:Create scheduler</para>
        /// </summary>
        /// <returns>
        /// <para>zh-cn:调度服务</para>
        /// <para>en-us:Scheduler service</para>
        /// </returns>
        //public TScheduler CreateScheduler();
    }
}
