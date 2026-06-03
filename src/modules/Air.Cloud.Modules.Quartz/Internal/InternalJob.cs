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
using Air.Cloud.Core;
using Air.Cloud.Core.App;
using Air.Cloud.Core.Modules.AppPrint;
using Air.Cloud.Core.Standard.Print;
using Air.Cloud.Core.Standard.SchedulerStandard;
using Air.Cloud.Core.Standard.SchedulerStandard.Coordination;
using Air.Cloud.Core.Standard.TraceLog.Defaults;
using Air.Cloud.Modules.Quartz.Options;
using Quartz;

using System.Reflection;

using System;
using System.Net;

namespace Air.Cloud.Modules.Quartz.Internal
{
    /// <summary>
    /// <para>zh-cn:Quartz 内部作业执行器，用于从调度池恢复调度标准并执行任务。</para>
    /// <para>en-us:Quartz internal job executor that restores the scheduler standard from the scheduler pool and runs the job.</para>
    /// </summary>
    /// <typeparam name="TSchedulerOption">
    /// <para>zh-cn:调度任务使用的配置类型。</para>
    /// <para>en-us:The configuration type used by the scheduled job.</para>
    /// </typeparam>
    [DisallowConcurrentExecution]
    [PersistJobDataAfterExecution]
    public class InternalJob<TSchedulerOption> : IJob where TSchedulerOption : class,ISchedulerStandardOptions,new()
    {
        /// <summary>
        /// <para>zh-cn:执行 Quartz 触发的调度任务，并在异常时写入追踪日志。</para>
        /// <para>en-us:Executes the scheduled job triggered by Quartz and writes trace logs when exceptions occur.</para>
        /// </summary>
        /// <param name="context">
        /// <para>zh-cn:Quartz 作业执行上下文，包含任务标识和触发器数据。</para>
        /// <para>en-us:The Quartz job execution context containing the task identifier and trigger data.</para>
        /// </param>
        /// <returns>
        /// <para>zh-cn:表示作业执行流程的异步任务。</para>
        /// <para>en-us:A task that represents the job execution workflow.</para>
        /// </returns>
        public async Task Execute(IJobExecutionContext context)
        {
            string jobId = context.MergedJobDataMap.Get("TasksId")?.ToString();
            ISchedulerStandard<TSchedulerOption> scheduler = ISchedulerStandardFactory<TSchedulerOption>.SchedulerPool.Get(jobId);
            await EnsureSchedulerStartedAsync(scheduler);

            SchedulerExecutionHandle? executionHandle = null;
            try
            {
                var decision = await TryBeginExecutionAsync(scheduler, context);
                if (!decision.CanExecute)
                {
                    PrintSkippedExecution(scheduler, decision);
                    return;
                }

                executionHandle = decision.Handle;
                await scheduler.ExecuteAsync(scheduler.CancellationToken);
                await CompleteExecutionAsync(executionHandle, context.CancellationToken);
            }
            catch (Exception exception)
            {
                await FailExecutionAsync(executionHandle, exception, context.CancellationToken);
                WriteExecutionException(scheduler, exception);
            }
        }

        /// <summary>
        /// <para>zh-cn:确保调度任务进入可执行状态。</para>
        /// <para>en-us:Ensures the scheduler job is in an executable state.</para>
        /// </summary>
        private static async Task EnsureSchedulerStartedAsync(ISchedulerStandard<TSchedulerOption> scheduler)
        {
            switch (scheduler.SchedulerStatus)
            {
                case SchedulerStatusEnum.Created:
                case SchedulerStatusEnum.Mounted:
                    await StartSchedulerAsync(scheduler);
                    break;
                case SchedulerStatusEnum.Stopped:
                    break;
                case SchedulerStatusEnum.Running:
                default:
                    break;
            }
        }

        /// <summary>
        /// <para>zh-cn:启动调度任务并回写调度池。</para>
        /// <para>en-us:Starts the scheduler job and writes it back to the scheduler pool.</para>
        /// </summary>
        private static async Task StartSchedulerAsync(ISchedulerStandard<TSchedulerOption> scheduler)
        {
            if (scheduler.SchedulerStatus == SchedulerStatusEnum.Created)
            {
                AppRealization.Output.Print(new AppPrintInformation
                {
                    Title = "定时任务",
                    Level = AppPrintLevel.Warn,
                    Content = "当前调度任务未挂载,本次调度将会正常执行,并使其完成挂载",
                    State = true
                });
            }

            scheduler.CancellationToken = new CancellationTokenSource().Token;
            scheduler.CancellationToken.Register(async () =>
            {
                await scheduler.StopAsync();
            });
            await scheduler.StartAsync(scheduler.CancellationToken);
            scheduler.SchedulerStatus = SchedulerStatusEnum.Running;
            ISchedulerStandardFactory<TSchedulerOption>.SchedulerPool.Set(scheduler);
        }

        /// <summary>
        /// <para>zh-cn:尝试获取本次触发的分布式执行权。</para>
        /// <para>en-us:Attempts to acquire the distributed execution right for this fire.</para>
        /// </summary>
        private static Task<SchedulerExecutionDecision> TryBeginExecutionAsync(
            ISchedulerStandard<TSchedulerOption> scheduler,
            IJobExecutionContext quartzContext)
        {
            if (scheduler.Options is not ISchedulerExecutionCoordinationOptions coordinationOptions
                || !coordinationOptions.EnableExecutionCoordination)
            {
                return Task.FromResult(new SchedulerExecutionDecision { CanExecute = true, Reason = "Execution coordination is not enabled." });
            }

            var coordinator = AppCore.GetService<ISchedulerExecutionCoordinatorStandard>();
            if (coordinator == null)
            {
                return Task.FromResult(new SchedulerExecutionDecision { CanExecute = false, Reason = "ISchedulerExecutionCoordinatorStandard was not registered." });
            }

            return coordinator.TryBeginAsync(BuildExecutionContext(scheduler, quartzContext, coordinationOptions), quartzContext.CancellationToken);
        }

        /// <summary>
        /// <para>zh-cn:构建调度执行上下文。</para>
        /// <para>en-us:Builds the scheduler execution context.</para>
        /// </summary>
        private static SchedulerExecutionContext BuildExecutionContext(
            ISchedulerStandard<TSchedulerOption> scheduler,
            IJobExecutionContext quartzContext,
            ISchedulerExecutionCoordinationOptions coordinationOptions)
        {
            var executionContext = new SchedulerExecutionContext
            {
                ServiceName = string.IsNullOrWhiteSpace(AppConst.ApplicationName) ? "air.cloud" : AppConst.ApplicationName,
                JobId = scheduler.Options.Id,
                GroupName = scheduler.Options.GroupName,
                JobName = scheduler.Options.Name,
                ScheduledFireTimeUtc = quartzContext.ScheduledFireTimeUtc,
                FireInstanceId = quartzContext.FireInstanceId,
                OwnerId = BuildOwnerId(),
                LeaseTime = TimeSpan.FromSeconds(Math.Max(1, coordinationOptions.LeaseSeconds)),
                UniqueMode = coordinationOptions.UniqueMode,
                ExecutionRecordType = coordinationOptions.ExecutionRecordType
            };
            executionContext.ExecutionKey = SchedulerExecutionKeyBuilder.Build(executionContext);
            return executionContext;
        }

        /// <summary>
        /// <para>zh-cn:构建当前服务实例标识。</para>
        /// <para>en-us:Builds the current service instance identifier.</para>
        /// </summary>
        private static string BuildOwnerId()
        {
            if (!string.IsNullOrWhiteSpace(AppConst.ApplicationInstanceName))
            {
                return AppConst.ApplicationInstanceName;
            }

            return $"{Environment.MachineName}_{Dns.GetHostName()}_{Environment.ProcessId}";
        }

        /// <summary>
        /// <para>zh-cn:完成执行记录。</para>
        /// <para>en-us:Completes the execution record.</para>
        /// </summary>
        private static async Task CompleteExecutionAsync(SchedulerExecutionHandle? handle, CancellationToken cancellationToken)
        {
            if (handle == null)
            {
                return;
            }

            var coordinator = AppCore.GetService<ISchedulerExecutionCoordinatorStandard>();
            if (coordinator != null)
            {
                await coordinator.CompleteAsync(handle, cancellationToken);
            }
        }

        /// <summary>
        /// <para>zh-cn:失败执行记录。</para>
        /// <para>en-us:Fails the execution record.</para>
        /// </summary>
        private static async Task FailExecutionAsync(SchedulerExecutionHandle? handle, Exception exception, CancellationToken cancellationToken)
        {
            if (handle == null)
            {
                return;
            }

            var coordinator = AppCore.GetService<ISchedulerExecutionCoordinatorStandard>();
            if (coordinator != null)
            {
                await coordinator.FailAsync(handle, exception, cancellationToken);
            }
        }

        /// <summary>
        /// <para>zh-cn:输出跳过执行日志。</para>
        /// <para>en-us:Prints skipped execution log.</para>
        /// </summary>
        private static void PrintSkippedExecution(ISchedulerStandard<TSchedulerOption> scheduler, SchedulerExecutionDecision decision)
        {
            AppRealization.Output.Print(new AppPrintInformation
            {
                Title = "定时任务",
                Level = AppPrintLevel.Information,
                Content = $"跳过[{scheduler.Options.Name}]任务执行,{decision.Reason}",
                State = true
            });
        }

        /// <summary>
        /// <para>zh-cn:写入调度任务异常追踪日志。</para>
        /// <para>en-us:Writes scheduler job exception trace log.</para>
        /// </summary>
        private static void WriteExecutionException(ISchedulerStandard<TSchedulerOption> scheduler, Exception exception)
        {
            DefaultTraceLogContent appPrintInformation = new DefaultTraceLogContent(
                "定时任务异常",
                $"在执行[{scheduler.Options.Name}]任务时出现异常,已记录异常信息",
                new Dictionary<string, object>()
                {
                    {"source", exception.Source },
                    {"stace", exception.StackTrace }
                }, DefaultTraceLogContent.EVENT_TAG, DefaultTraceLogContent.ERROR_TAG);
            AppRealization.TraceLog.Write(appPrintInformation);

        }
    }
}
