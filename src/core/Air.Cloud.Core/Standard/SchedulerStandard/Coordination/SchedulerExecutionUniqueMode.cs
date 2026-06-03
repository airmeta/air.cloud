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
namespace Air.Cloud.Core.Standard.SchedulerStandard.Coordination
{
    /// <summary>
    /// <para>zh-cn:调度执行唯一性模式。</para>
    /// <para>en-us:Scheduler execution uniqueness mode.</para>
    /// </summary>
    public enum SchedulerExecutionUniqueMode
    {
        /// <summary>
        /// <para>zh-cn:不启用唯一执行裁决。</para>
        /// <para>en-us:Disables unique execution coordination.</para>
        /// </summary>
        Disabled = 0,

        /// <summary>
        /// <para>zh-cn:同一任务任意时刻只允许一个实例执行。</para>
        /// <para>en-us:Allows only one instance to execute the same job at any time.</para>
        /// </summary>
        PerJob = 1,

        /// <summary>
        /// <para>zh-cn:同一任务的同一计划触发时间只允许一个实例执行。</para>
        /// <para>en-us:Allows only one instance for the same scheduled fire time of the same job.</para>
        /// </summary>
        PerFireTime = 2
    }
}
