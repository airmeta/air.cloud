using Microsoft.Extensions.Hosting;

namespace Air.Cloud.Core.Standard.SchedulerStandard
{
    /// <summary>
    /// <para>zh-cn:调度任务状态</para>
    /// <para>en-us:Scheduler status enum</para>
    /// </summary>
    public enum SchedulerStatusEnum
    {
        /// <summary>
        /// <para>zh-cn:已挂载</para>
        /// <para>en-us:Mounted</para>
        /// </summary>
        Mounted,
        /// <summary>
        /// <para>zh-cn:已运行</para>
        /// <para>en-us:Running</para>
        /// </summary>
        Running,
        /// <summary>
        /// <para>zh-cn:已结束</para>
        /// <para>en-us:Stopping</para>
        /// </summary>
        Stopped,
    }

    /// <summary>
    /// <para>zh-cn:调度标准</para>
    /// <para>en-us:Scheduler standard</para>
    /// </summary>
    public interface ISchedulerStandard
    {
        /// <summary>
        /// <para>zh-cn:调度名称</para>
        /// <para>en-us:Name</para>
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// <para>zh-cn:调度描述</para>
        /// <para>en-us:Description</para>
        /// </summary>
        public string  Description { get; set; }

        /// <summary>
        /// <para>zh-cn:调度编号</para>
        /// <para>en-us:Id</para>
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// <para>zh-cn:令牌</para>
        /// <para>en-us:CancellationToken</para>
        /// </summary>
        public CancellationToken CancellationToken { get; set; }

        /// <summary>
        /// <para>zh-cn:调度名称</para>
        /// <para>en-us:Name</para>
        /// </summary>
        public SchedulerStatusEnum SchedulerStatus { get; set; }    

        /// <summary>
        /// <para>zh-cn:调度规则</para>
        /// <para>en-us:</para>
        /// </summary>
        public string CronExpression { get; set; }

        /// <summary>
        /// <para>zh-cn:设置循环调度规则</para>
        /// <para>en-us:Set a round-robin scheduling rule</para>
        /// </summary>
        /// <param name="expression"></param>
        /// <returns></returns>
        public bool SetExpression(string expression);

        /// <summary>
        /// This method is called when the <see cref="IHostedService"/> starts. The implementation should return a task that represents
        /// the lifetime of the long running operation(s) being performed.
        /// </summary>
        /// <param name="stoppingToken">Triggered when <see cref="IHostedService.StopAsync(CancellationToken)"/> is called.</param>
        /// <returns>A <see cref="Task"/> that represents the long running operations.</returns>
        public abstract Task ExecuteAsync(CancellationToken stoppingToken);

        /// <summary>
        /// Triggered when the application host is ready to start the service.
        /// </summary>
        /// <param name="cancellationToken">Indicates that the start process has been aborted.</param>
        public Task StartAsync(CancellationToken cancellationToken);
        //{
        //    // Create linked token to allow cancelling executing task from provided token
        //    _stoppingCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        //    // Store the task we're executing
        //    _executeTask = ExecuteAsync(_stoppingCts.Token);

        //    // If the task is completed then return it, this will bubble cancellation and failure to the caller
        //    if (_executeTask.IsCompleted)
        //    {
        //        return _executeTask;
        //    }

        //    // Otherwise it's running
        //    return Task.CompletedTask;
        //}

        /// <summary>
        /// Triggered when the application host is performing a graceful shutdown.
        /// </summary>
        /// <param name="cancellationToken">Indicates that the shutdown process should no longer be graceful.</param>
        public Task StopAsync();
        //{
        //    // Stop called without start
        //    if (_executeTask == null)
        //    {
        //        return;
        //    }

        //    try
        //    {
        //        // Signal cancellation to the executing method
        //        _stoppingCts.Cancel();
        //    }
        //    finally
        //    {
        //        // Wait until the task completes or the stop token triggers
        //        await Task.WhenAny(_executeTask, Task.Delay(Timeout.Infinite, cancellationToken)).ConfigureAwait(false);
        //    }
        //}
    }
}
