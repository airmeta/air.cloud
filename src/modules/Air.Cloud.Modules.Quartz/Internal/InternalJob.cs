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
    [DisallowConcurrentExecution]
    [PersistJobDataAfterExecution]
    public class InternalJob<TSchedulerOption> : IJob where TSchedulerOption : class,ISchedulerStandardOptions,new()
    {
        /// <summary>
        /// <para>zh-cn:执行任务</para>
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
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
                            Title = "air.cloud.scheduler",
                            Level = AppPrintLevel.Warning,
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
