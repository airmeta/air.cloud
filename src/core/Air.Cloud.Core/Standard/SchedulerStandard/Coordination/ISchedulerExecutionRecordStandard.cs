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
    /// <para>zh-cn:调度执行记录标准，业务层可用实体实现该接口并自行决定数据库表、索引和迁移。</para>
    /// <para>en-us:Scheduler execution record standard that business-layer entities can implement while owning tables, indexes, and migrations.</para>
    /// </summary>
    public interface ISchedulerExecutionRecordStandard
    {
        /// <summary>
        /// <para>zh-cn:执行记录标识。</para>
        /// <para>en-us:Execution record identifier.</para>
        /// </summary>
        string ExecutionId { get; set; }

        /// <summary>
        /// <para>zh-cn:执行唯一键，建议在业务表中建立唯一索引。</para>
        /// <para>en-us:Execution unique key; a unique index is recommended in the business table.</para>
        /// </summary>
        string ExecutionKey { get; set; }

        /// <summary>
        /// <para>zh-cn:服务名称，用于隔离不同应用或微服务。</para>
        /// <para>en-us:Service name used to isolate applications or microservices.</para>
        /// </summary>
        string ServiceName { get; set; }

        /// <summary>
        /// <para>zh-cn:任务唯一标识。</para>
        /// <para>en-us:Unique job identifier.</para>
        /// </summary>
        string JobId { get; set; }

        /// <summary>
        /// <para>zh-cn:任务分组名称。</para>
        /// <para>en-us:Job group name.</para>
        /// </summary>
        string GroupName { get; set; }

        /// <summary>
        /// <para>zh-cn:任务名称。</para>
        /// <para>en-us:Job name.</para>
        /// </summary>
        string JobName { get; set; }

        /// <summary>
        /// <para>zh-cn:计划触发时间，按 UTC 保存。</para>
        /// <para>en-us:Scheduled fire time stored as UTC.</para>
        /// </summary>
        DateTimeOffset? ScheduledFireTimeUtc { get; set; }

        /// <summary>
        /// <para>zh-cn:调度框架本次触发实例标识。</para>
        /// <para>en-us:Scheduler framework fire instance identifier.</para>
        /// </summary>
        string FireInstanceId { get; set; }

        /// <summary>
        /// <para>zh-cn:当前执行所有者实例标识。</para>
        /// <para>en-us:Current execution owner instance identifier.</para>
        /// </summary>
        string OwnerId { get; set; }

        /// <summary>
        /// <para>zh-cn:执行状态。</para>
        /// <para>en-us:Execution status.</para>
        /// </summary>
        SchedulerExecutionStatus Status { get; set; }

        /// <summary>
        /// <para>zh-cn:开始时间，按 UTC 保存。</para>
        /// <para>en-us:Start time stored as UTC.</para>
        /// </summary>
        DateTimeOffset StartedAtUtc { get; set; }

        /// <summary>
        /// <para>zh-cn:完成时间，按 UTC 保存。</para>
        /// <para>en-us:Completion time stored as UTC.</para>
        /// </summary>
        DateTimeOffset? CompletedAtUtc { get; set; }

        /// <summary>
        /// <para>zh-cn:租约过期时间，按 UTC 保存。</para>
        /// <para>en-us:Lease expiration time stored as UTC.</para>
        /// </summary>
        DateTimeOffset LeaseExpiresAtUtc { get; set; }

        /// <summary>
        /// <para>zh-cn:失败时的异常消息。</para>
        /// <para>en-us:Error message when execution fails.</para>
        /// </summary>
        string? ErrorMessage { get; set; }
    }
}
