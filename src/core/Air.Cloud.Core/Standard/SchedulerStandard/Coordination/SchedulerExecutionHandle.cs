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
    /// <para>zh-cn:调度执行句柄。</para>
    /// <para>en-us:Scheduler execution handle.</para>
    /// </summary>
    public sealed class SchedulerExecutionHandle
    {
        /// <summary>
        /// <para>zh-cn:执行记录主键。</para>
        /// <para>en-us:Execution record primary key.</para>
        /// </summary>
        public string ExecutionId { get; set; } = string.Empty;

        /// <summary>
        /// <para>zh-cn:执行唯一键。</para>
        /// <para>en-us:Execution unique key.</para>
        /// </summary>
        public string ExecutionKey { get; set; } = string.Empty;

        /// <summary>
        /// <para>zh-cn:执行所有者实例标识。</para>
        /// <para>en-us:Execution owner instance identifier.</para>
        /// </summary>
        public string OwnerId { get; set; } = string.Empty;

        /// <summary>
        /// <para>zh-cn:租约时长。</para>
        /// <para>en-us:Lease duration.</para>
        /// </summary>
        public TimeSpan LeaseTime { get; set; } = TimeSpan.FromMinutes(5);

        /// <summary>
        /// <para>zh-cn:执行记录实体类型。</para>
        /// <para>en-us:Execution record entity type.</para>
        /// </summary>
        public Type? ExecutionRecordType { get; set; }
    }
}
