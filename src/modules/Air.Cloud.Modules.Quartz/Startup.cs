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
using Air.Cloud.Core.App;
using Air.Cloud.Core.App.Startups;
using Air.Cloud.Core.Attributes;
using Air.Cloud.Core.Standard.SchedulerStandard;
using Air.Cloud.Core.Standard.SchedulerStandard.Extensions;
using Air.Cloud.Modules.Quartz.Extensions;
using Air.Cloud.Modules.Quartz.Factory;
using Air.Cloud.Modules.Quartz.Job;
using Air.Cloud.Modules.Quartz.Options;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using Quartz;
using Quartz.AspNetCore;
using Quartz.Impl;
namespace Air.Cloud.Modules.Quartz
{
    /// <summary>
    /// <para>zh-cn:启动项</para>
    /// <para>en-us:Startup</para>
    /// </summary>
    [AppStartup(Order = 500)]
    public class Startup : AppStartup
    {
        public static CancellationTokenSource cts = new CancellationTokenSource();
        
        public override void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            // 获取主机生命周期管理接口
            var lifetime = app.ApplicationServices.GetRequiredService<IHostApplicationLifetime>();
            // 应用程序终止时，注销服务
            lifetime.ApplicationStopping.Register(() =>
            {
                var QuartJobService = AppCore.GetService<QuartzJobService>();
                var ServiceProvider = AppCore.GetService<IServiceProvider>();
                QuartJobService.StopAsync(ServiceProvider).GetAwaiter().GetResult();
            });
        }
        public override void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton<ISchedulerStandardFactory<QuartzSchedulerStandardOptions>, SchedulerStandardFactory>();
            services.AddTransient<ISchedulerFactory, StdSchedulerFactory>();
            services.AddSingleton<QuartzJobService>();
            services.AddSchedulerStandard<QuartzSchedulerStandardOptions>();
            services.AddQuartz(q =>
            {
                q.UseMicrosoftDependencyInjectionJobFactory();
            });
            services.AddQuartzServer(options =>
            {
                options.WaitForJobsToComplete = true;
            });
            var QuartJobService = AppCore.GetService<QuartzJobService>();
            var ServiceProvider= AppCore.GetService<IServiceProvider>();
            QuartJobService.StartAsync(ServiceProvider).GetAwaiter().GetResult();
        }
    }
}
