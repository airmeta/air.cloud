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
using Air.Cloud.Core.Standard.SchedulerStandard;
using Air.Cloud.Core.Standard.SchedulerStandard.Extensions;
using Air.Cloud.Modules.Quartz.Factory;
using Air.Cloud.Modules.Quartz.Job;
using Air.Cloud.Modules.Quartz.Options;

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using Quartz;
using Quartz.AspNetCore;
using Quartz.Impl;
using Quartz.Impl.Matchers;

namespace Air.Cloud.Modules.Quartz.Extensions
{
    /// <summary>
    /// <para>zh-cn:提供 Quartz 调度服务注册、启动、停止与查询相关的扩展方法。</para>
    /// <para>en-us:Provides extension methods for registering, starting, stopping, and querying Quartz scheduler services.</para>
    /// </summary>
    public static  class QuartzExtensions
    {

        /// <summary>
        /// <para>zh-cn:启动并挂载当前应用中注册的 Quartz 调度任务。</para>
        /// <para>en-us:Starts and mounts the Quartz scheduled jobs registered in the current application.</para>
        /// </summary>
        /// <typeparam name="TSchedulerOption">
        /// <para>zh-cn:调度任务使用的配置类型。</para>
        /// <para>en-us:The configuration type used by the scheduled jobs.</para>
        /// </typeparam>
        /// <param name="_quartzJob">
        /// <para>zh-cn:负责执行任务挂载与关闭逻辑的 Quartz 作业服务。</para>
        /// <para>en-us:The Quartz job service responsible for mounting and closing jobs.</para>
        /// </param>
        /// <param name="_provider">
        /// <para>zh-cn:用于解析调度标准实现的服务提供器。</para>
        /// <para>en-us:The service provider used to resolve scheduler standard implementations.</para>
        /// </param>
        /// <returns>
        /// <para>zh-cn:表示任务启动与挂载流程的异步任务。</para>
        /// <para>en-us:A task that represents the job startup and mounting workflow.</para>
        /// </returns>
        public static  async Task StartAsync<TSchedulerOption>(this QuartzJobService<TSchedulerOption> _quartzJob, IServiceProvider _provider) where TSchedulerOption : class, ISchedulerStandardOptions, new()
        {
            AppRealization.Output.Print(new AppPrintInformation()
            {
                State = true,
                AdditionalParams = null,
                Content = "定时任务开始挂载",
                Level = AppPrintLevel.Information,
                Title = "定时任务"
            });
            // 解析你的作用域服务
            IEnumerable<ISchedulerStandard<TSchedulerOption>> servicelist = _provider.GetServices<ISchedulerStandard<TSchedulerOption>>();
            foreach (var item in servicelist)
            {
                //自动恢复任务机制a
                try
                {
                    var result = await _quartzJob.RunAsync(item);
                    if (result)
                    {
                        AppRealization.Output.Print(new AppPrintInformation()
                        {
                            State = true,
                            AdditionalParams = null,
                            Content = "定时任务挂载成功",
                            Level = AppPrintLevel.Information,
                            Title = "定时任务"
                        });
                    }
                }
                catch (Exception ex)
                {
                    AppRealization.Output.Print(new AppPrintInformation()
                    {
                        State = true,
                        AdditionalParams = new Dictionary<string, object>()
                            {
                                {"error",ex }
                            },
                        Content = "定时任务挂载失败",
                        Level = AppPrintLevel.Error,
                        Title = "定时任务"
                    });
                }
            }
        }
        
        /// <summary>
        /// <para>zh-cn:停止并取消挂载当前应用中注册的 Quartz 调度任务。</para>
        /// <para>en-us:Stops and unmounts the Quartz scheduled jobs registered in the current application.</para>
        /// </summary>
        /// <typeparam name="TSchedulerOption">
        /// <para>zh-cn:调度任务使用的配置类型。</para>
        /// <para>en-us:The configuration type used by the scheduled jobs.</para>
        /// </typeparam>
        /// <param name="_quartzJob">
        /// <para>zh-cn:负责执行任务挂载与关闭逻辑的 Quartz 作业服务。</para>
        /// <para>en-us:The Quartz job service responsible for mounting and closing jobs.</para>
        /// </param>
        /// <param name="_provider">
        /// <para>zh-cn:用于创建服务作用域并解析调度标准实现的服务提供器。</para>
        /// <para>en-us:The service provider used to create a service scope and resolve scheduler standard implementations.</para>
        /// </param>
        /// <returns>
        /// <para>zh-cn:表示任务停止与取消挂载流程的异步任务。</para>
        /// <para>en-us:A task that represents the job stopping and unmounting workflow.</para>
        /// </returns>
        public static  async Task StopAsync<TSchedulerOption>(this QuartzJobService<TSchedulerOption> _quartzJob, IServiceProvider _provider) where TSchedulerOption : class, ISchedulerStandardOptions, new()
        {
            using (var scope = _provider.CreateScope())
            {
                // 解析你的作用域服务
                IEnumerable<ISchedulerStandard<TSchedulerOption>> servicelist = scope.ServiceProvider.GetServices<ISchedulerStandard<TSchedulerOption>>();
                foreach (var item in servicelist)
                {
                    //自动恢复任务机制a
                    try
                    {
                        var result = await _quartzJob.CloseAsync(item);
                        if (result)
                        {
                            AppRealization.Output.Print(new AppPrintInformation()
                            {
                                State = true,
                                AdditionalParams = null,
                                Content = "定时任务取消挂载成功",
                                Level = AppPrintLevel.Information,
                                Title = "App:Dispatch"
                            });
                        }
                    }
                    catch (Exception ex)
                    {
                        AppRealization.Output.Print(new AppPrintInformation()
                        {
                            State = true,
                            AdditionalParams = new Dictionary<string, object>()
                            {
                                {"error",ex }
                            },
                            Content = "定时任务取消挂载失败",
                            Level = AppPrintLevel.Error,
                            Title = "App:Dispatch"
                        });
                    }
                }
            }
        }

        /// <summary>
        /// <para>zh-cn:获取指定名称的 Quartz 调度器；未指定名称时返回默认调度器。</para>
        /// <para>en-us:Gets the Quartz scheduler with the specified name, or the default scheduler when no name is specified.</para>
        /// </summary>
        /// <typeparam name="TSchedulerOption">
        /// <para>zh-cn:调度任务使用的配置类型。</para>
        /// <para>en-us:The configuration type used by the scheduled jobs.</para>
        /// </typeparam>
        /// <param name="schedulerStandard">
        /// <para>zh-cn:当前调度标准实例，用于承载扩展方法调用。</para>
        /// <para>en-us:The current scheduler standard instance that hosts the extension method call.</para>
        /// </param>
        /// <param name="Scheduler">
        /// <para>zh-cn:可选的调度器名称。</para>
        /// <para>en-us:The optional scheduler name.</para>
        /// </param>
        /// <returns>
        /// <para>zh-cn:解析得到的 Quartz 调度器实例。</para>
        /// <para>en-us:The resolved Quartz scheduler instance.</para>
        /// </returns>
        public static async Task<IScheduler> GetScheduler<TSchedulerOption>(this ISchedulerStandard<TSchedulerOption> schedulerStandard,string Scheduler=null) where TSchedulerOption : class, ISchedulerStandardOptions, new()
        {
            ISchedulerFactory schedulerFactory = AppCore.GetService<ISchedulerFactory>();
            return  string.IsNullOrEmpty(Scheduler) ? await schedulerFactory.GetScheduler() : await schedulerFactory.GetScheduler(Scheduler);
        }

        /// <summary>
        /// <para>zh-cn:获取触发器</para>
        /// <para>en-us:Get trigger</para>
        /// </summary>
        /// <param name="schedulerStandard">
        ///  <para>zh-cn:调度作业信息</para>
        ///  <para>en-us:Scheduler information</para>
        /// </param>
        /// <param name="scheduler">
        /// <para>zh-cn:调度器</para>
        /// <para>en-us:Scheduler</para>
        /// </param>
        /// <returns></returns>
        public static async Task<ITrigger> GetTrigger<TSchedulerOption>(
            this ISchedulerStandard<TSchedulerOption> schedulerStandard,
            IScheduler scheduler=null) where TSchedulerOption : class, ISchedulerStandardOptions, new()
        {
            if (scheduler == null) scheduler = await schedulerStandard.GetScheduler<TSchedulerOption>();

            var jobKeys = (await scheduler.GetJobKeys(GroupMatcher<JobKey>.GroupEquals(schedulerStandard.Options.GroupName))).ToList().FirstOrDefault();
            if (jobKeys == null)
            {
                AppRealization.Output.Print(new AppPrintInformation()
                {
                    State = true,
                    AdditionalParams = null,
                    Content = "未查询到定时任务信息",
                    Level = AppPrintLevel.Information,
                    Title = "定时任务"
                });
                return null;
            }
            var triggers = await scheduler.GetTriggersOfJob(jobKeys);
            ITrigger trigger = triggers?.Where(x => x.JobKey.Name == schedulerStandard.Options.Name)?.FirstOrDefault();
            if (trigger == null)
            {
                AppRealization.Output.Print(new AppPrintInformation()
                {
                    State = true,
                    AdditionalParams = null,
                    Content = "未查询到定时任务触发器信息",
                    Level = AppPrintLevel.Information,
                    Title = "定时任务"
                });
                return null;
            }
            return trigger;
        }

        /// <summary>
        /// <para>zh-cn:引入Air.Cloud.Modules.Quartz模块</para>
        /// <para>en-us:Use QuartzServices</para>
        /// </summary>
        /// <param name="app"></param>
        /// <typeparam name="TSchedulerOption"></typeparam>
        /// <returns></returns>
        public static void UseQuartzServices<TSchedulerOption>(this IApplicationBuilder app) where TSchedulerOption : class, ISchedulerStandardOptions, new()
        {
            // 获取主机生命周期管理接口
            var lifetime = app.ApplicationServices.GetRequiredService<IHostApplicationLifetime>();
            // 应用程序终止时，注销服务
            lifetime.ApplicationStopping.Register(() =>
            {
                var QuartJobService = AppCore.GetService<QuartzJobService<TSchedulerOption>>();
                var ServiceProvider = AppCore.GetService<IServiceProvider>();
                QuartJobService.StopAsync<TSchedulerOption>(ServiceProvider).GetAwaiter().GetResult();
            });
        }
        /// <summary>
        /// <para>zh-cn:添加Air.Cloud.Modules.Quartz的服务</para>
        /// <para>en-us:Use QuartzServices</para>
        /// </summary>
        /// <typeparam name="TSchedulerOption">
        /// <para>zh-cn:调度配置实现</para>
        /// <para>en-us:SchedulerOptions Dependency</para>
        /// </typeparam>
        /// <param name="services">
        /// <para>zh-cn:服务接口</para>
        /// <para>en-us:ServiceCollections</para>
        /// </param>
        /// <param name="configure">
        ///  Quartz配置
        /// </param>
        /// <param name="quartzHostedServiceConfigure">
        ///  Quartz服务配置
        /// </param>
        public static void AddQuartzService<TSchedulerOption>(this IServiceCollection services, 
            Action<IServiceCollectionQuartzConfigurator>? configure = null, 
            Action<QuartzHostedServiceOptions>? quartzHostedServiceConfigure = null) where TSchedulerOption : class, ISchedulerStandardOptions, new()
        {
            services.AddSingleton<ISchedulerStandardFactory<TSchedulerOption>, SchedulerStandardFactory<TSchedulerOption>>();
            services.AddTransient<ISchedulerFactory, StdSchedulerFactory>();
            services.AddSingleton<QuartzJobService<TSchedulerOption>>();
            services.AddSchedulerStandard<TSchedulerOption>();
            if (configure == null)
            {
                configure = ConfigureDefaultQuartz;
            }
            if (quartzHostedServiceConfigure == null)
            {
                quartzHostedServiceConfigure= q =>
                {
                    q.WaitForJobsToComplete = true;
                };
            }
            services.AddQuartz(configure);
            services.AddQuartzServer(quartzHostedServiceConfigure);
            var QuartJobService = AppCore.GetService<QuartzJobService<TSchedulerOption>>();
            var ServiceProvider = AppCore.GetService<IServiceProvider>();
            QuartJobService.StartAsync<TSchedulerOption>(ServiceProvider).GetAwaiter().GetResult();
        }

        /// <summary>
        /// <para>zh-cn:配置默认 Quartz 选项。</para>
        /// <para>en-us:Configure default Quartz options.</para>
        /// </summary>
        /// <param name="quartz">
        /// <para>zh-cn:Quartz 配置器。</para>
        /// <para>en-us:Quartz configurator.</para>
        /// </param>
        public static void ConfigureDefaultQuartz(IServiceCollectionQuartzConfigurator quartz)
        {
        }
    }
}
