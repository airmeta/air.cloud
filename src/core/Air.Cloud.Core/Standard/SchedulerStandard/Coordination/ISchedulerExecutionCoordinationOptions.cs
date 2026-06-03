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
    /// <para>zh-cn:调度执行协调配置。</para>
    /// <para>en-us:Scheduler execution coordination options.</para>
    /// </summary>
    public interface ISchedulerExecutionCoordinationOptions
    {
        /// <summary>
        /// <para>zh-cn:是否启用执行协调。</para>
        /// <para>en-us:Whether execution coordination is enabled.</para>
        /// </summary>
        bool EnableExecutionCoordination { get; set; }

        /// <summary>
        /// <para>zh-cn:唯一执行策略。</para>
        /// <para>en-us:Unique execution policy.</para>
        /// </summary>
        SchedulerExecutionUniqueMode UniqueMode { get; set; }

        /// <summary>
        /// <para>zh-cn:执行租约秒数。</para>
        /// <para>en-us:Execution lease seconds.</para>
        /// </summary>
        int LeaseSeconds { get; set; }

        /// <summary>
        /// <para>zh-cn:执行记录实体类型；为空时由协调器自动扫描唯一实现。</para>
        /// <para>en-us:Execution record entity type; when empty, the coordinator scans the single implementation automatically.</para>
        /// </summary>
        Type? ExecutionRecordType { get; set; }
    }
}
