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
    /// <para>zh-cn:调度执行状态。</para>
    /// <para>en-us:Scheduler execution status.</para>
    /// </summary>
    public enum SchedulerExecutionStatus
    {
        /// <summary>
        /// <para>zh-cn:运行中。</para>
        /// <para>en-us:Running.</para>
        /// </summary>
        Running = 0,

        /// <summary>
        /// <para>zh-cn:执行成功。</para>
        /// <para>en-us:Succeeded.</para>
        /// </summary>
        Succeeded = 1,

        /// <summary>
        /// <para>zh-cn:执行失败。</para>
        /// <para>en-us:Failed.</para>
        /// </summary>
        Failed = 2,

        /// <summary>
        /// <para>zh-cn:已被其他实例接管。</para>
        /// <para>en-us:Taken over by another instance.</para>
        /// </summary>
        TakenOver = 3
    }
}
