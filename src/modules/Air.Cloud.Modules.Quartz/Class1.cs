using Air.Cloud.Core.Standard.SchedulerStandard;

using Microsoft.Extensions.Logging;

using Quartz;
using Quartz.Impl.Matchers;
using Quartz.Impl.Triggers;
using Quartz.Spi;

using System.Diagnostics;
using System.Threading;

namespace Air.Cloud.Modules.Quartz
{

    public static class JobConvert
    {
        public static  IJob ToJob(this ISchedulerStandard scheduler)
        {
            return new InternalJob(scheduler);
        }
    }

    public class InternalJob : IJob
    {
        private CancellationToken _cancellationToken;
        private ISchedulerStandard Scheduler;
        public InternalJob(ISchedulerStandard scheduler)
        {
            this.Scheduler= scheduler;
            _cancellationToken = new CancellationTokenSource().Token;
            _cancellationToken.Register(async () =>
            {
                await scheduler.StopAsync();
            });
            scheduler.StartAsync(_cancellationToken).GetAwaiter().GetResult();
        }
        public async  Task Execute(IJobExecutionContext context)
        {
            await Scheduler.ExecuteAsync(_cancellationToken);
        }
    }



    /// <summary>
    /// IJob 对象无法构造注入 需要此类实现 返回 注入后得 Job 实例
    /// </summary>
    public class ResultfulApiJobFactory : IJobFactory
    {
        private readonly IServiceProvider _serviceProvider;
        public ResultfulApiJobFactory(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }
        public IJob NewJob(TriggerFiredBundle bundle, IScheduler scheduler)
        {
            //Job类型
            Type jobType = bundle.JobDetail.JobType;
            return _serviceProvider.GetService(jobType) as IJob;
        }
        public void ReturnJob(IJob job)
        {
            var disposable = job as IDisposable;
            disposable?.Dispose();
        }
    }
      
}
