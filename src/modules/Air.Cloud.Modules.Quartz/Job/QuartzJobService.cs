/*
 * Copyright (c) 2024 星曳数据
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
using Air.Cloud.Modules.Quartz.Extensions;
using Air.Cloud.Modules.Quartz.Internal;

using Quartz;
using Quartz.Spi;

namespace Air.Cloud.Modules.Quartz.Job
{
    /// <summary>
    /// 任务调度服务
    /// </summary>
    public class QuartzJobService<TSchedulerOption> where TSchedulerOption:class,ISchedulerStandardOptions,new()
    {
        private readonly ISchedulerFactory _schedulerFactory;
        private readonly IJobFactory _resultfulApiJobFactory;
        public QuartzJobService(ISchedulerFactory schedulerFactory, IJobFactory resultfulApiJobFactory)
        {
            _schedulerFactory = schedulerFactory;
            _resultfulApiJobFactory = resultfulApiJobFactory;
        }
        /// <summary>
        /// 开始运行一个调度器
        /// </summary>
        /// <param name="tasks"></param>
        /// <returns></returns>
        public async Task<bool> RunAsync(ISchedulerStandard<TSchedulerOption> tasks)
        {
            //1、通过调度工厂获得调度器
            var scheduler = await _schedulerFactory.GetScheduler();
            //6、开启调度器
            await scheduler.Start();
            //2、创建一个触发器
            var trigger = TriggerBuilder.Create()
                .WithIdentity(tasks.Options.Name, tasks.Options.GroupName)
                .StartNow()
                .WithDescription(tasks.Options.Description)
                .WithCronSchedule(tasks.Options.CronExpression)
                .Build();
            //3、创建任务
            var jobDetail = JobBuilder.Create<InternalJob<TSchedulerOption>>()
                             .WithIdentity(tasks.Options.Name, tasks.Options.GroupName)
                            .UsingJobData("TasksId", tasks.Options.Id.ToString())
                            .Build();
            tasks.SchedulerStatus = SchedulerStatusEnum.Mounted;
            ISchedulerStandardFactory<TSchedulerOption>.SchedulerPool.Set(tasks);
            //4、写入 Job 实例工厂 解决 Job 中取 ioc 对象
            scheduler.JobFactory = _resultfulApiJobFactory;
            //5、将触发器和任务器绑定到调度器中
            await scheduler.ScheduleJob(jobDetail, trigger);
            await scheduler.ResumeTrigger(trigger.Key);
            return await Task.FromResult(true);
        }
        /// <summary>
        /// 关闭调度器
        /// </summary>
        /// <param name="tasks"></param>
        /// <returns></returns>
        public async Task<bool> CloseAsync(ISchedulerStandard<TSchedulerOption> tasks)
        {
            IScheduler scheduler = await _schedulerFactory.GetScheduler();
            ITrigger? trigger = await tasks.GetTrigger(scheduler);
            await tasks.StopAsync();
            await scheduler.PauseTrigger(trigger.Key);
            await scheduler.UnscheduleJob(trigger.Key);// 移除触发器
            await scheduler.DeleteJob(trigger.JobKey);
            return await Task.FromResult(true);
        }
    }
}
