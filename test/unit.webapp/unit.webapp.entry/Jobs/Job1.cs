﻿/*
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
using Air.Cloud.Core;
using Air.Cloud.Core.App;
using Air.Cloud.Core.Dependencies;
using Air.Cloud.Core.Standard.SchedulerStandard;
using Air.Cloud.Core.Standard.SchedulerStandard.Attributes;
using Air.Cloud.Modules.Quartz.Options;
using unit.webapp.model.Domains;

namespace unit.webapp.entry.Jobs
{

    [SchedulerInformationAttribute(CronExpression = "0/5 * * * * ? ", Name = "测试定时任务", Id = "job_test1", Description = "测试定时任务")]
    public class Job1 : ISchedulerStandard<QuartzSchedulerStandardOptions>, ISingleton
    {
        private readonly IServiceProvider _provider;
        public CancellationToken CancellationToken { get; set; }
        public SchedulerStatusEnum SchedulerStatus { get; set; }
        public QuartzSchedulerStandardOptions Options { get; set; }

        public Job1() { }
        public string Do()
        {
            return "123132";
        }
        public Job1(
            ISchedulerStandardFactory<QuartzSchedulerStandardOptions> schedulerStandardFactory1, 
            IServiceProvider _provider)
        {
            this.Options = schedulerStandardFactory1.GetSchedulerConfiguration<Job1>();
            this._provider = _provider;

        }
        public Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                using (var scope = _provider.CreateScope())
                {
                    Console.WriteLine("123132");
                    Console.WriteLine(scope==null);
                    Console.WriteLine(scope.ServiceProvider == null);
                    
                    var testDomain = scope.ServiceProvider.GetService<ITestDomain>();
                    Console.WriteLine(testDomain == null);
                    var data = testDomain.Search(s => s.UserId == "a09cdb089b7f48498090d1f7f11c0e7b");
                    Console.WriteLine(data == null);
                    AppRealization.Output.Print(new Air.Cloud.Core.Standard.Print.AppPrintInformation()
                    {
                        State = true,
                        AdditionalParams = null,
                        Content = AppRealization.JSON.Serialize(data),
                        Level = Air.Cloud.Core.Standard.Print.AppPrintInformation.AppPrintLevel.Information,
                        Title = "air.cloud.scheduler"
                    });
                    AppRealization.Output.Print(new Air.Cloud.Core.Standard.Print.AppPrintInformation()
                    {
                        State = true,
                        AdditionalParams = null,
                        Content = $"[{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}]定时任务执行成功",
                        Level = Air.Cloud.Core.Standard.Print.AppPrintInformation.AppPrintLevel.Information,
                        Title = "air.cloud.scheduler"
                    });
                    this.CancellationToken = stoppingToken;
                }
            }
            catch (Exception ex)
            {

                throw ex;
            }

            return Task.CompletedTask;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            AppRealization.Output.Print(new Air.Cloud.Core.Standard.Print.AppPrintInformation()
            {
                State = true,
                AdditionalParams = null,
                Content = "定时任务Job1开始运行",
                Level = Air.Cloud.Core.Standard.Print.AppPrintInformation.AppPrintLevel.Information,
                Title = "air.cloud.scheduler"
            });
          
            await Task.CompletedTask;
        }

        public async Task StopAsync()
        {
            AppRealization.Output.Print(new Air.Cloud.Core.Standard.Print.AppPrintInformation()
            {
                State = true,
                AdditionalParams = null,
                Content = "定时任务Job1结束运行",
                Level = Air.Cloud.Core.Standard.Print.AppPrintInformation.AppPrintLevel.Information,
                Title = "air.cloud.scheduler"
            });
            File.WriteAllText($"{AppConst.ApplicationPath}/job_stop_log.txt", AppRealization.JSON.Serialize(new Air.Cloud.Core.Standard.Print.AppPrintInformation()
            {
                State = true,
                AdditionalParams = null,
                Content = "定时任务Job1结束运行",
                Level = Air.Cloud.Core.Standard.Print.AppPrintInformation.AppPrintLevel.Information,
                Title = "air.cloud.scheduler"
            }));
            await Task.CompletedTask;
        }


    }
}
