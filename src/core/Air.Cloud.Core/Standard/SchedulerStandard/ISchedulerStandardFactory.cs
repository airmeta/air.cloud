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
using Air.Cloud.Core.Standard.SchedulerStandard.Pool;

namespace Air.Cloud.Core.Standard.SchedulerStandard
{
    /// <summary>
    ///  <para>zh-cn: 调度工厂</para>
    ///  <para>en-us: Scheduler factory</para>
    /// </summary>
    /// <typeparam name="TSchedulerStandardOptions">
    ///  <para>zh-cn:需要创建的类型配置</para>
    ///  <para>en-us:Scheduler standard options</para>
    /// </typeparam>
    public interface ISchedulerStandardFactory<TSchedulerStandardOptions>
        where TSchedulerStandardOptions : class,ISchedulerStandardOptions,new()
    {
        /// <summary>
        /// <para>zh-cn:调度池</para>
        /// <para>en-us:Scheduler pool</para>
        /// </summary>
        public static SchedulerPool<TSchedulerStandardOptions> SchedulerPool =new SchedulerPool<TSchedulerStandardOptions>();
        /// <summary>
        /// <para>
        /// zh-cn: 获取调度配置
        /// </para>
        /// <para>en-us:GetSchedulerConfiguration</para>
        /// </summary>
        /// <typeparam name="TScheduler">
        ///  <para>zh-cn:调度标准实现类</para>
        ///  <para>en-us:Scheduler standard dependency</para>
        /// </typeparam>
        /// <returns></returns>
        /// <exception cref="Exception">
        ///  SchedulerInformationAttribute is not found
        /// </exception>
        public TSchedulerStandardOptions GetSchedulerConfiguration<TScheduler>() where TScheduler : class, ISchedulerStandard<TSchedulerStandardOptions>;
    }
}
