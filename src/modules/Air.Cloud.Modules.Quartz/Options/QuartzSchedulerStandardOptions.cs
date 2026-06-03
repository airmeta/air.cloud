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
using Air.Cloud.Core.Standard.SchedulerStandard;
using Air.Cloud.Core.Standard.SchedulerStandard.Attributes;
using Air.Cloud.Core.Standard.SchedulerStandard.Coordination;

namespace Air.Cloud.Modules.Quartz.Options
{
    /// <summary>
    /// <para>zh-cn:Quartz 调度器标准配置项，保存任务标识、名称、描述、Cron 表达式和分组信息。</para>
    /// <para>en-us:Quartz scheduler standard options that store job identity, name, description, cron expression, and group information.</para>
    /// </summary>
    public class QuartzSchedulerStandardOptions : ISchedulerStandardOptions, ISchedulerExecutionCoordinationOptions
    {
        /// <summary>
        /// <para>zh-cn:调度任务唯一标识，用于区分不同的调度定义。</para>
        /// <para>en-us:Unique scheduler job identifier used to distinguish different scheduler definitions.</para>
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// <para>zh-cn:调度任务名称，用于日志、展示和调度器内部识别。</para>
        /// <para>en-us:Scheduler job name used for logging, display, and scheduler-side identification.</para>
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// <para>zh-cn:调度任务描述，用于说明任务用途、触发场景或业务含义。</para>
        /// <para>en-us:Scheduler job description that explains the purpose, trigger scenario, or business meaning.</para>
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// <para>zh-cn:Cron 表达式，用于定义任务触发周期。</para>
        /// <para>en-us:Cron expression that defines the job trigger schedule.</para>
        /// </summary>
        public string CronExpression { get; set; }

        /// <summary>
        /// <para>zh-cn:调度任务分组名称，用于按业务域或运行场景归类任务。</para>
        /// <para>en-us:Scheduler job group name used to classify jobs by business domain or runtime scenario.</para>
        /// </summary>
        public string GroupName { get; set; }

        /// <summary>
        /// <para>zh-cn:是否启用分布式执行协调；启用后需要注册 ISchedulerExecutionCoordinatorStandard。</para>
        /// <para>en-us:Whether distributed execution coordination is enabled; requires ISchedulerExecutionCoordinatorStandard registration.</para>
        /// </summary>
        public bool EnableExecutionCoordination { get; set; }

        /// <summary>
        /// <para>zh-cn:分布式唯一执行策略，默认按同一计划触发时间去重。</para>
        /// <para>en-us:Distributed unique execution policy, deduplicated by scheduled fire time by default.</para>
        /// </summary>
        public SchedulerExecutionUniqueMode UniqueMode { get; set; } = SchedulerExecutionUniqueMode.PerFireTime;

        /// <summary>
        /// <para>zh-cn:执行租约秒数，长任务可通过心跳延长租约。</para>
        /// <para>en-us:Execution lease seconds; long-running jobs can extend it through heartbeat.</para>
        /// </summary>
        public int LeaseSeconds { get; set; } = 300;

        /// <summary>
        /// <para>zh-cn:执行记录实体类型；为空时由协调器自动扫描唯一实现。</para>
        /// <para>en-us:Execution record entity type; when empty, the coordinator scans the single implementation automatically.</para>
        /// </summary>
        public Type? ExecutionRecordType { get; set; }

        /// <summary>
        /// <para>zh-cn:初始化 Quartz 调度器标准配置项。</para>
        /// <para>en-us:Initializes the Quartz scheduler standard options.</para>
        /// </summary>
        public QuartzSchedulerStandardOptions() { }

        /// <summary>
        /// <para>zh-cn:使用显式任务信息初始化 Quartz 调度器标准配置项。</para>
        /// <para>en-us:Initializes the Quartz scheduler standard options with explicit job information.</para>
        /// </summary>
        /// <param name="id">
        /// <para>zh-cn:调度任务唯一标识。</para>
        /// <para>en-us:Unique scheduler job identifier.</para>
        /// </param>
        /// <param name="name">
        /// <para>zh-cn:调度任务名称。</para>
        /// <para>en-us:Scheduler job name.</para>
        /// </param>
        /// <param name="description">
        /// <para>zh-cn:调度任务描述。</para>
        /// <para>en-us:Scheduler job description.</para>
        /// </param>
        /// <param name="cronExpression">
        /// <para>zh-cn:任务触发 Cron 表达式。</para>
        /// <para>en-us:Cron expression used to trigger the job.</para>
        /// </param>
        public QuartzSchedulerStandardOptions(string id, string name, string description, string cronExpression)
        {
            Id = id;
            Name = name;
            Description = description;
            CronExpression = cronExpression;
        }
        /// <summary>
        /// <para>zh-cn:使用SchedulerInformationAttribute初始化</para>
        /// <para>en-us:Initialize using SchedulerInformationAttribute</para>
        /// </summary>
        /// <param name="attribute">
        /// 调度配置信息
        /// </param>
        public QuartzSchedulerStandardOptions(SchedulerInformationAttribute attribute)
        {
            Id = attribute.Id;
            Name = attribute.Name;
            Description = attribute.Description;
            CronExpression = attribute.CronExpression;
            GroupName = attribute.GroupName;
        }
        /// <summary>
        /// <para>zh-cn:使用SchedulerInformationAttribute初始化</para>
        /// <para>en-us:Initialize using SchedulerInformationAttribute</para>
        /// </summary>
        /// <param name="attribute">
        /// 调度配置信息
        /// </param>
        public void Setter(SchedulerInformationAttribute attribute)
        {
            Id = attribute.Id;
            Name = attribute.Name;
            Description = attribute.Description;
            CronExpression = attribute.CronExpression;
            GroupName = attribute.GroupName;
        }
    }
}
