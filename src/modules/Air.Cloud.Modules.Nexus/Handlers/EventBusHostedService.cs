using Air.Cloud.Core.Standard.Event;
using Microsoft.Extensions.Hosting;

namespace Air.Cloud.Modules.Nexus.Handlers;

/// <summary>
/// 事件总线后台服务
/// </summary>
public class EventBusHostedService : BackgroundService
{
    private IEventBusExecutor _eventBusExecutor;

    public EventBusHostedService()
    {

    }
    public EventBusHostedService(IEventBusExecutor eventBusExecutor) : this()
    {
        _eventBusExecutor = eventBusExecutor;
    }
    /// <summary>
    /// 执行事件总线后台任务
    /// </summary>
    /// <param name="stoppingToken">任务取消token</param>
    /// <returns></returns>
    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (_eventBusExecutor == null) return Task.CompletedTask;
        return _eventBusExecutor.ExecuteAsync(stoppingToken);
    }
}
