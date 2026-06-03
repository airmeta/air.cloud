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
    /// <para>zh-cn:调度执行裁决结果。</para>
    /// <para>en-us:Scheduler execution decision result.</para>
    /// </summary>
    public sealed class SchedulerExecutionDecision
    {
        /// <summary>
        /// <para>zh-cn:当前实例是否可以执行任务。</para>
        /// <para>en-us:Whether the current instance can execute the job.</para>
        /// </summary>
        public bool CanExecute { get; set; }

        /// <summary>
        /// <para>zh-cn:执行句柄，用于完成、失败和心跳回写。</para>
        /// <para>en-us:Execution handle used for completion, failure, and heartbeat updates.</para>
        /// </summary>
        public SchedulerExecutionHandle? Handle { get; set; }

        /// <summary>
        /// <para>zh-cn:裁决原因。</para>
        /// <para>en-us:Decision reason.</para>
        /// </summary>
        public string Reason { get; set; } = string.Empty;
    }
}
