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
using Air.Cloud.Core.Modules.AppPrint;
using Air.Cloud.Core.Standard.Print;
using Air.Cloud.Core.Standard.SchedulerStandard;
using Air.Cloud.Core.Standard.TraceLog.Defaults;
using Air.Cloud.Modules.Quartz.Options;
using Quartz;

using System.Reflection;

using System;

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
            string JobId = context.MergedJobDataMap.Get("TasksId")?.ToString();
            ISchedulerStandard<TSchedulerOption> Scheduler = ISchedulerStandardFactory<TSchedulerOption>.SchedulerPool.Get(JobId);
            switch (Scheduler.SchedulerStatus)
            {
                case SchedulerStatusEnum.Created:
                case SchedulerStatusEnum.Mounted:
                    if (Scheduler.SchedulerStatus == SchedulerStatusEnum.Created)
                    {
                        AppRealization.Output.Print(new AppPrintInformation
                        {
                            Title = "定时任务",
                            Level = AppPrintLevel.Warn,
                            Content = $"当前调度任务未挂载,本次调度将会正常执行,并使其完成挂载",
                            State = true
                        });
                    }
                    Scheduler.CancellationToken = new CancellationTokenSource().Token;
                    Scheduler.CancellationToken.Register(async () =>
                    {
                        await Scheduler.StopAsync();
                    });
                    await Scheduler.StartAsync(Scheduler.CancellationToken);
                    Scheduler.SchedulerStatus = SchedulerStatusEnum.Running;
                    ISchedulerStandardFactory<TSchedulerOption>.SchedulerPool.Set(Scheduler);
                    break;
                case SchedulerStatusEnum.Stopped:
                    break;

                case SchedulerStatusEnum.Running:
                default:
                    // Do nothing
                    break;
            }
            try
            {
                await Scheduler.ExecuteAsync(Scheduler.CancellationToken);
            }
            catch (Exception exception)
            {
                DefaultTraceLogContent appPrintInformation = new DefaultTraceLogContent(
                    "定时任务异常",
                   $"在执行[{Scheduler.Options.Name}]任务时出现异常,已记录异常信息",
                  new Dictionary<string, object>()
                  {
                         {"source",exception.Source },
                         {"stace",exception.StackTrace }
                  }, DefaultTraceLogContent.EVENT_TAG, DefaultTraceLogContent.ERROR_TAG);
                AppRealization.TraceLog.Write(appPrintInformation);
            }
            

        }
    }
}
