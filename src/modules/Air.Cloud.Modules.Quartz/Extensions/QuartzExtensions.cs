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
    public static  class QuartzExtensions
    {

        /// <summary>
        /// 启动服务
        /// </summary>
        /// <param name="_quartzJob"></param>
        /// <param name="_provider"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
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
        /// 停止服务
        /// </summary>
        /// <param name="_quartzJob"></param>
        /// <param name="_provider"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
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
                configure = q =>
                {
                    q.UseMicrosoftDependencyInjectionJobFactory();
                };
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
    }
}
