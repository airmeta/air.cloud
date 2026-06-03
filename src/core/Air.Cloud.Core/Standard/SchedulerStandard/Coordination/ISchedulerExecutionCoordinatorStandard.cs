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
    /// <para>zh-cn:调度执行协调器标准，由业务或应用层实现，用于在多实例部署下裁决单次任务触发是否允许执行。</para>
    /// <para>en-us:Scheduler execution coordinator standard implemented by business or application layer to decide whether a fired job can run in multi-instance deployments.</para>
    /// <para>zh-cn:如需落库，业务实体可实现 <see cref="ISchedulerExecutionRecordStandard"/> 并自行维护表结构、唯一索引和迁移。</para>
    /// <para>en-us:For persistence, business entities can implement <see cref="ISchedulerExecutionRecordStandard"/> and own tables, unique indexes, and migrations.</para>
    /// </summary>
    public interface ISchedulerExecutionCoordinatorStandard : IStandard
    {
        /// <summary>
        /// <para>zh-cn:尝试开始一次调度执行；只有返回允许执行的实例才能真正执行业务任务。</para>
        /// <para>en-us:Attempts to begin one scheduler execution; only the granted instance should run the business job.</para>
        /// </summary>
        Task<SchedulerExecutionDecision> TryBeginAsync(SchedulerExecutionContext context, CancellationToken cancellationToken = default);

        /// <summary>
        /// <para>zh-cn:标记一次调度执行成功完成。</para>
        /// <para>en-us:Marks a scheduler execution as completed successfully.</para>
        /// </summary>
        Task CompleteAsync(SchedulerExecutionHandle handle, CancellationToken cancellationToken = default);

        /// <summary>
        /// <para>zh-cn:标记一次调度执行失败。</para>
        /// <para>en-us:Marks a scheduler execution as failed.</para>
        /// </summary>
        Task FailAsync(SchedulerExecutionHandle handle, Exception exception, CancellationToken cancellationToken = default);

        /// <summary>
        /// <para>zh-cn:刷新执行租约，用于长任务证明当前执行者仍然存活。</para>
        /// <para>en-us:Refreshes the execution lease so long-running jobs can prove the owner is alive.</para>
        /// </summary>
        Task HeartbeatAsync(SchedulerExecutionHandle handle, CancellationToken cancellationToken = default);
    }
}
