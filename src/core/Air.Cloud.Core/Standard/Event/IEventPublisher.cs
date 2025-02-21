using Air.Cloud.Core.Standard.Event;
/// <summary>
/// 事件发布服务依赖接口
/// </summary>
public interface IEventPublisher
{
    /// <summary>
    /// 事件处理程序事件
    /// </summary>
    event EventHandler<EventHandlerEventArgs> OnExecuted;

    /// <summary>
    /// 发布一条消息
    /// </summary>
    /// <param name="eventSource">事件源</param>
    /// <returns><see cref="Task"/> 实例</returns>
    Task PublishAsync(IEventSource eventSource);

    /// <summary>
    /// 延迟发布一条消息
    /// </summary>
    /// <param name="eventSource">事件源</param>
    /// <param name="span">延迟数</param>
    /// <returns><see cref="Task"/> 实例</returns>
    Task PublishDelayAsync(IEventSource eventSource, TimeSpan span);
    /// <summary>
    /// 延迟发布一条消息
    /// </summary>
    /// <param name="eventSource">事件源</param>
    /// <param name="delay">延迟数（毫秒）</param>
    /// <returns><see cref="Task"/> 实例</returns>
    Task PublishDelayAsync(IEventSource eventSource, long delay);

    /// <summary>
    /// 发布一条消息
    /// </summary>
    /// <param name="eventName">事件 Id</param>
    /// <param name="payload">事件承载（携带）数据</param>
    /// <param name="cancellationToken"> 取消任务 Token</param>
    /// <returns></returns>
    Task PublishAsync(string eventName, object payload = default, CancellationToken cancellationToken = default);

    /// <summary>
    /// 发布一条消息
    /// </summary>
    /// <param name="eventName">事件 Id</param>
    /// <param name="payload">事件承载（携带）数据</param>
    /// <param name="cancellationToken"> 取消任务 Token</param>
    /// <returns></returns>
    Task PublishAsync(Enum eventName, object payload = default, CancellationToken cancellationToken = default);

    /// <summary>
    /// 延迟发布一条消息
    /// </summary>
    /// <param name="eventName">事件 Id</param>
    /// <param name="delay">延迟数（毫秒）</param>
    /// <param name="payload">事件承载（携带）数据</param>
    /// <param name="cancellationToken"> 取消任务 Token</param>
    /// <returns><see cref="Task"/> 实例</returns>
    Task PublishDelayAsync(string eventName, long delay, object payload = default, CancellationToken cancellationToken = default);

    /// <summary>
    /// 延迟发布一条消息
    /// </summary>
    /// <param name="eventName">事件 Id</param>
    /// <param name="span">延迟数</param>
    /// <param name="payload">事件承载（携带）数据</param>
    /// <param name="cancellationToken"> 取消任务 Token</param>
    /// <returns><see cref="Task"/> 实例</returns>
    Task PublishDelayAsync(string eventName, TimeSpan delay, object payload = default, CancellationToken cancellationToken = default);

    /// <summary>
    /// 延迟发布一条消息
    /// </summary>
    /// <param name="eventName">事件 Id</param>
    /// <param name="delay">延迟数（毫秒）</param>
    /// <param name="payload">事件承载（携带）数据</param>
    /// <param name="cancellationToken"> 取消任务 Token</param>
    /// <returns><see cref="Task"/> 实例</returns>
    Task PublishDelayAsync(Enum eventName, long delay, object payload = default, CancellationToken cancellationToken = default);

    /// <summary>
    /// 触发事件处理程序事件
    /// </summary>
    /// <param name="args">事件参数</param>
    void InvokeEvents(EventHandlerEventArgs args);
}
