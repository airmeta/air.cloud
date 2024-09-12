using Air.Cloud.Core;
using Air.Cloud.Core.Standard.SchedulerStandard;
using Air.Cloud.Modules.Quartz.Job;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

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
            List<ISchedulerStandard> servicelist = new();
            using (var scope = _provider.CreateScope())
            {
                // 解析你的作用域服务
                var service = scope.ServiceProvider.GetService<ISchedulerStandard>();
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
        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            throw new NotImplementedException();
        }
    }
}
