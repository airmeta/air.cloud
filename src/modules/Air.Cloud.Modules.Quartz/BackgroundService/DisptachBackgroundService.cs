using Air.Cloud.Core;
using Air.Cloud.Core.Standard.SchedulerStandard;
using Air.Cloud.Modules.Quartz.Job;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Air.Cloud.Modules.Quartz.BackgroundServices
{
    public  class DisptachBackgroundService:IHostedService
    {
        private readonly IServiceProvider _provider;
        private readonly QuartzJobService _quartzJob;
        public DisptachBackgroundService(QuartzJobService quartzJob, IServiceProvider provider)
        {
            this._quartzJob = quartzJob;
            _provider = provider;
        }
        public async Task StartAsync(CancellationToken cancellationToken)
        {
            AppRealization.Output.Print(new Core.Standard.Print.AppPrintInformation()
            {
                State = true,
                AdditionalParams = null,
                Content = "定时任务开始挂载",
                Level = Core.Standard.Print.AppPrintInformation.AppPrintLevel.Information,
                Title = "App:Dispatch"
            });
            using (var scope = _provider.CreateScope())
            {
                // 解析你的作用域服务
                IEnumerable<ISchedulerStandard> servicelist = scope.ServiceProvider.GetServices<ISchedulerStandard>();
                foreach (var item in servicelist)
                {
                    //自动恢复任务机制a
                    try
                    {
                        var result = await _quartzJob.RunAsync(item);
                        if (result)
                        {
                            AppRealization.Output.Print(new Core.Standard.Print.AppPrintInformation()
                            {
                                State = true,
                                AdditionalParams = null,
                                Content = "定时任务挂载成功",
                                Level = Core.Standard.Print.AppPrintInformation.AppPrintLevel.Information,
                                Title = "App:Dispatch"
                            });
                        }
                    }
                    catch (Exception ex)
                    {
                        AppRealization.Output.Print(new Core.Standard.Print.AppPrintInformation()
                        {
                            State = true,
                            AdditionalParams = new Dictionary<string, object>()
                            {
                                {"error",ex }
                            },
                            Content = "定时任务挂载失败",
                            Level = Core.Standard.Print.AppPrintInformation.AppPrintLevel.Error,
                            Title = "App:Dispatch"
                        });
                    }
                }
            }
        }
        public async  Task StopAsync(CancellationToken cancellationToken)
        {
            using (var scope = _provider.CreateScope())
            {
                // 解析你的作用域服务
                IEnumerable<ISchedulerStandard> servicelist = scope.ServiceProvider.GetServices<ISchedulerStandard>();
                foreach (var item in servicelist)
                {
                    //自动恢复任务机制a
                    try
                    {
                        var result = await _quartzJob.CloseAsync(item);
                        if (result)
                        {
                            AppRealization.Output.Print(new Core.Standard.Print.AppPrintInformation()
                            {
                                State = true,
                                AdditionalParams = null,
                                Content = "定时任务取消挂载成功",
                                Level = Core.Standard.Print.AppPrintInformation.AppPrintLevel.Information,
                                Title = "App:Dispatch"
                            });
                        }
                    }
                    catch (Exception ex)
                    {
                        AppRealization.Output.Print(new Core.Standard.Print.AppPrintInformation()
                        {
                            State = true,
                            AdditionalParams = new Dictionary<string, object>()
                            {
                                {"error",ex }
                            },
                            Content = "定时任务取消挂载失败",
                            Level = Core.Standard.Print.AppPrintInformation.AppPrintLevel.Error,
                            Title = "App:Dispatch"
                        });
                    }
                }
            }
        }
    }
}
