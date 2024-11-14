using Air.Cloud.Core.Standard.SchedulerStandard;

using Quartz;
using Quartz.Impl.Matchers;

namespace Air.Cloud.Modules.Quartz.Job
{
    /// <summary>
    /// 任务调度服务
    /// </summary>
    public class QuartzJobService
    {
        private readonly ISchedulerFactory _schedulerFactory;
        private readonly ResultfulApiJobFactory _resultfulApiJobFactory;
        public QuartzJobService(ISchedulerFactory schedulerFactory, ResultfulApiJobFactory resultfulApiJobFactory)
        {
            _schedulerFactory = schedulerFactory;
            _resultfulApiJobFactory = resultfulApiJobFactory;
        }
        /// <summary>
        /// 开始运行一个调度器
        /// </summary>
        /// <param name="tasks"></param>
        /// <returns></returns>
        public async Task<bool> RunAsync(ISchedulerStandard tasks)
        {
            //1、通过调度工厂获得调度器
            var scheduler = await _schedulerFactory.GetScheduler();
            var taskName = $"{tasks.Id}>{tasks.Name}";
            //2、创建一个触发器
            var trigger = TriggerBuilder.Create()
                .WithIdentity(taskName, taskName)
                .StartNow()
                .WithDescription(tasks.Description)
                .WithCronSchedule(tasks.CronExpression)
                .Build();
            //3、创建任务
            var jobDetail = JobBuilder.Create<InternalJob>()
                            .WithIdentity(taskName, taskName)
                            .UsingJobData("TasksId", tasks.Id.ToString())
                            .Build();
            //4、写入 Job 实例工厂 解决 Job 中取 ioc 对象
            scheduler.JobFactory = _resultfulApiJobFactory;
            //5、将触发器和任务器绑定到调度器中
            await scheduler.ScheduleJob(jobDetail, trigger);
            //6、开启调度器
            await scheduler.Start();
            return await Task.FromResult(true);
        }
        /// <summary>
        /// 关闭调度器
        /// </summary>
        /// <param name="tasks"></param>
        /// <returns></returns>
        public async Task<bool> CloseAsync(ISchedulerStandard tasks)
        {
            IScheduler scheduler = await _schedulerFactory.GetScheduler();
            var taskName = $"{tasks.Id}>{tasks.Name}";
            var jobKeys = (await scheduler
                .GetJobKeys(GroupMatcher<JobKey>.GroupEquals(taskName)))
                .ToList().FirstOrDefault();
            if (jobKeys == null)
            {
                Console.WriteLine($"未找到任务:{taskName}");
            }
            var triggers = await scheduler.GetTriggersOfJob(jobKeys);
            ITrigger trigger = triggers?.Where(x => x.JobKey.Name == taskName).FirstOrDefault();
            if (trigger == null)
            {
                Console.WriteLine($"未找到触发器:{taskName}");
            }
            await scheduler.PauseTrigger(trigger.Key);
            await scheduler.UnscheduleJob(trigger.Key);// 移除触发器
            await scheduler.DeleteJob(trigger.JobKey);
            return await Task.FromResult(true);
        }
    }
}
