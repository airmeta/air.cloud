//using Air.Cloud.Core;
//using Air.Cloud.Core.Modules.AppPrint;
//using Air.Cloud.Core.Standard.SchedulerStandard;
//using Air.Cloud.Core.Standard.SchedulerStandard.Attributes;

//using Microsoft.Extensions.DependencyInjection;

//namespace Air.Cloud.DataBase.Jobs
//{
//    public  class DatabaseStatusCheckJob
//    {
//        [AutoLoad(false)]
//        [SchedulerInformationAttribute(CronExpression = "0/5 * * * * ? ", Name = "测试定时任务", Id = "job_test1", Description = "测试定时任务")]
//        public class Job1 : ISchedulerStandard<QuartzSchedulerStandardOptions>
//        {
//            private readonly IServiceProvider _provider;
//            public CancellationToken CancellationToken { get; set; }
//            public SchedulerStatusEnum SchedulerStatus { get; set; }
//            public QuartzSchedulerStandardOptions Options { get; set; }

//            public Job1() { }
//            public Job1(
//                ISchedulerStandardFactory<QuartzSchedulerStandardOptions> schedulerStandardFactory1,
//                IServiceProvider _provider)
//            {
//                this.Options = schedulerStandardFactory1.GetSchedulerConfiguration<Job1>();
//                this._provider = _provider;

//            }


//            //[Aspect(typeof(DefaultOutputAspect1))]
//            //[Aspect(typeof(IfNullReferenceException))]
//            public Task ExecuteAsync(CancellationToken stoppingToken)
//            {
//                try
//                {
//                    using (var scope = _provider.CreateScope())
//                    {
//                        var testDomain = scope.ServiceProvider.GetService<ITestDomain>();
//                        Console.WriteLine(testDomain == null);
//                        var data = testDomain.Search("a09cdb089b7f48498090d1f7f11c0e7b");
//                        AppRealization.Output.Print(new AppPrintInformation()
//                        {
//                            State = true,
//                            AdditionalParams = null,
//                            Content = AppRealization.JSON.Serialize(data),
//                            Level = AppPrintLevel.Information,
//                            Title = "定时任务"
//                        });
//                        AppRealization.Output.Print(new AppPrintInformation()
//                        {
//                            State = true,
//                            AdditionalParams = null,
//                            Content = $"[{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}]定时任务执行成功",
//                            Level = AppPrintLevel.Information,
//                            Title = "定时任务"
//                        });
//                        this.CancellationToken = stoppingToken;
//                    }
//                }
//                catch (Exception ex)
//                {

//                    throw ex;
//                }

//                return Task.CompletedTask;
//            }

//            public async Task StartAsync(CancellationToken cancellationToken)
//            {
//                AppRealization.Output.Print(new AppPrintInformation()
//                {
//                    State = true,
//                    AdditionalParams = null,
//                    Content = "定时任务Job1开始运行",
//                    Level = AppPrintLevel.Information,
//                    Title = "定时任务"
//                });

//                await Task.CompletedTask;
//            }

//            public async Task StopAsync()
//            {
//                AppRealization.Output.Print(new AppPrintInformation()
//                {
//                    State = true,
//                    AdditionalParams = null,
//                    Content = "定时任务Job1结束运行",
//                    Level = AppPrintLevel.Information,
//                    Title = "定时任务"
//                });
//                File.WriteAllText($"{AppConst.ApplicationPath}/job_stop_log.txt", AppRealization.JSON.Serialize(new AppPrintInformation()
//                {
//                    State = true,
//                    AdditionalParams = null,
//                    Content = "定时任务Job1结束运行",
//                    Level = AppPrintLevel.Information,
//                    Title = "定时任务"
//                }));
//                await Task.CompletedTask;
//            }
//        }

//    }
//}
