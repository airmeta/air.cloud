using Air.Cloud.Core.Standard.Event;

namespace Air.Cloud.Modules.Nexus.Publishers;

/// <summary>
/// 基于内存通道事件发布者（默认实现）
/// </summary>
public class DefaultEventPublisher : IEventPublisher
{
    /// <summary>
    /// 事件处理程序事件
    /// </summary>
    public event EventHandler<EventHandlerEventArgs> OnExecuted;

    /// <summary>
    /// 事件源存储器
    /// </summary>
    private readonly IEventSourceStorager _eventSourceStorer;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="eventSourceStorer">事件源存储器</param>
    public DefaultEventPublisher(IEventSourceStorager eventSourceStorer)
    {
        _eventSourceStorer = eventSourceStorer;
    }

    /// <summary>
    /// 发布一条消息
    /// </summary>
    /// <param name="eventSource">事件源</param>
    /// <returns><see cref="Task"/> 实例</returns>
    public virtual async Task PublishAsync(IEventSource eventSource)
    {
        await _eventSourceStorer.WriteAsync(eventSource, eventSource.CancellationToken);
    }

    /// <summary>
    /// 延迟发布一条消息
    /// </summary>
    /// <param name="eventSource">事件源</param>
    /// <param name="span">延迟数</param>
    /// <returns><see cref="Task"/> 实例</returns>
    public virtual Task PublishDelayAsync(IEventSource eventSource, TimeSpan span)
    {
        // 创建新线程
        Task.Factory.StartNew(async () =>
        {
            // 延迟 delay 毫秒
            await Task.Delay(span, eventSource.CancellationToken);

            await _eventSourceStorer.WriteAsync(eventSource, eventSource.CancellationToken);
        }, eventSource.CancellationToken);

        return Task.CompletedTask;
    }
    /// <summary>
    /// 延迟发布一条消息
    /// </summary>
    /// <param name="eventSource">事件源</param>
    /// <param name="delay">延迟数（毫秒）</param>
    /// <returns><see cref="Task"/> 实例</returns>
    public virtual Task PublishDelayAsync(IEventSource eventSource, long delay)
    {
        // 创建新线程
        Task.Factory.StartNew(async () =>
        {
            // 延迟 delay 毫秒
            await Task.Delay(TimeSpan.FromMilliseconds(delay), eventSource.CancellationToken);

            await _eventSourceStorer.WriteAsync(eventSource, eventSource.CancellationToken);
        }, eventSource.CancellationToken);

        return Task.CompletedTask;
    }

    /// <summary>
    /// 发布一条消息
    /// </summary>
    /// <param name="eventName">事件 Id</param>
    /// <param name="payload">事件承载（携带）数据</param>
    /// <param name="cancellationToken"> 取消任务 Token</param>
    /// <returns></returns>
    public virtual async Task PublishAsync(string eventName, object payload = default, CancellationToken cancellationToken = default)
    {
        await PublishAsync(new DefaultEventSource(eventName, payload, cancellationToken));
    }

    /// <summary>
    /// 发布一条消息
    /// </summary>
    /// <param name="eventName">事件 Id</param>
    /// <param name="payload">事件承载（携带）数据</param>
    /// <param name="cancellationToken"> 取消任务 Token</param>
    /// <returns></returns>
    public virtual async Task PublishAsync(Enum eventName, object payload = default, CancellationToken cancellationToken = default)
    {
        await PublishAsync(new DefaultEventSource(eventName, payload, cancellationToken));
    }

    /// <summary>
    /// 延迟发布一条消息
    /// </summary>
    /// <param name="eventName">事件 Id</param>
    /// <param name="delay">延迟数（毫秒）</param>
    /// <param name="payload">事件承载（携带）数据</param>
    /// <param name="cancellationToken"> 取消任务 Token</param>
    /// <returns><see cref="Task"/> 实例</returns>
    public virtual async Task PublishDelayAsync(string eventName, long delay, object payload = default, CancellationToken cancellationToken = default)
    {
        await PublishDelayAsync(new DefaultEventSource(eventName, payload, cancellationToken), delay);
    }


    /// <summary>
    /// 延迟发布一条消息
    /// </summary>
    /// <param name="eventName">事件 Id</param>
    /// <param name="span">延迟数</param>
    /// <param name="payload">事件承载（携带）数据</param>
    /// <param name="cancellationToken"> 取消任务 Token</param>
    /// <returns><see cref="Task"/> 实例</returns>
    public virtual async Task PublishDelayAsync(string eventName, TimeSpan delay, object payload = default, CancellationToken cancellationToken = default)
    {
        await PublishDelayAsync(new DefaultEventSource(eventName, payload, cancellationToken), delay);
    }
    /// <summary>
    /// 延迟发布一条消息
    /// </summary>
    /// <param name="eventName">事件 Id</param>
    /// <param name="delay">延迟数（毫秒）</param>
    /// <param name="payload">事件承载（携带）数据</param>
    /// <param name="cancellationToken"> 取消任务 Token</param>
    /// <returns><see cref="Task"/> 实例</returns>
    public virtual async Task PublishDelayAsync(Enum eventName, long delay, object payload = default, CancellationToken cancellationToken = default)
    {
        await PublishDelayAsync(new DefaultEventSource(eventName, payload, cancellationToken), delay);
    }

    /// <summary>
    /// 触发事件处理程序事件
    /// </summary>
    /// <param name="args">事件参数</param>
    public void InvokeEvents(EventHandlerEventArgs args)
    {
        try
        {
            OnExecuted?.Invoke(this, args);
        }
        catch { }
    }
}
