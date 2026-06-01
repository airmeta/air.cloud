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
using Air.Cloud.Modules.Quartz.Extensions;
using Air.Cloud.Modules.Quartz.Internal;

using Quartz;
using Quartz.Spi;

namespace Air.Cloud.Modules.Quartz.Job
{
    /// <summary>
    /// <para>zh-cn:任务调度服务，负责挂载、启动、暂停和移除 Quartz 调度任务。</para>
    /// <para>en-us:Scheduled job service responsible for mounting, starting, pausing, and removing Quartz scheduled jobs.</para>
    /// </summary>
    /// <typeparam name="TSchedulerOption">
    /// <para>zh-cn:调度任务使用的配置类型。</para>
    /// <para>en-us:The configuration type used by the scheduled jobs.</para>
    /// </typeparam>
    public class QuartzJobService<TSchedulerOption> where TSchedulerOption:class,ISchedulerStandardOptions,new()
    {
        private readonly ISchedulerFactory _schedulerFactory;
        private readonly IJobFactory _resultfulApiJobFactory;

        /// <summary>
        /// <para>zh-cn:使用调度器工厂和作业工厂初始化任务调度服务。</para>
        /// <para>en-us:Initializes the scheduled job service with the scheduler factory and job factory.</para>
        /// </summary>
        /// <param name="schedulerFactory">
        /// <para>zh-cn:用于创建或获取 Quartz 调度器的工厂。</para>
        /// <para>en-us:The factory used to create or retrieve Quartz schedulers.</para>
        /// </param>
        /// <param name="resultfulApiJobFactory">
        /// <para>zh-cn:用于通过依赖注入创建 Quartz 作业实例的工厂。</para>
        /// <para>en-us:The factory used to create Quartz job instances through dependency injection.</para>
        /// </param>
        public QuartzJobService(ISchedulerFactory schedulerFactory, IJobFactory resultfulApiJobFactory)
        {
            _schedulerFactory = schedulerFactory;
            _resultfulApiJobFactory = resultfulApiJobFactory;
        }
        /// <summary>
        /// <para>zh-cn:开始运行并挂载一个调度任务。</para>
        /// <para>en-us:Starts and mounts a scheduled job.</para>
        /// </summary>
        /// <param name="tasks">
        /// <para>zh-cn:需要挂载到 Quartz 的调度标准实例。</para>
        /// <para>en-us:The scheduler standard instance to mount into Quartz.</para>
        /// </param>
        /// <returns>
        /// <para>zh-cn:表示挂载是否成功的布尔结果。</para>
        /// <para>en-us>A Boolean result indicating whether mounting succeeded.</para>
        /// </returns>
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
        /// <para>zh-cn:关闭并移除一个已挂载的调度任务。</para>
        /// <para>en-us:Closes and removes a mounted scheduled job.</para>
        /// </summary>
        /// <param name="tasks">
        /// <para>zh-cn:需要从 Quartz 中暂停、取消触发并删除的调度标准实例。</para>
        /// <para>en-us:The scheduler standard instance to pause, unschedule, and delete from Quartz.</para>
        /// </param>
        /// <returns>
        /// <para>zh-cn:表示关闭是否成功的布尔结果。</para>
        /// <para>en-us:A Boolean result indicating whether closing succeeded.</para>
        /// </returns>
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
