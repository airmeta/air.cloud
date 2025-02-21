namespace Air.Cloud.Core.Standard.Event;

/// <summary>
/// 事件处理程序事件参数
/// </summary>
public sealed class EventHandlerEventArgs : EventArgs
{
    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="eventSource">事件源（事件承载对象）</param>
    /// <param name="success">任务处理委托调用结果</param>
    public EventHandlerEventArgs(IEventSource eventSource, bool success)
    {
        Source = eventSource;
        Status = success ? "SUCCESS" : "FAIL";
    }

    /// <summary>
    /// 事件源（事件承载对象）
    /// </summary>
    public IEventSource Source { get; }

    /// <summary>
    /// 执行状态
    /// </summary>
    public string Status { get; }

    /// <summary>
    /// 异常信息
    /// </summary>
    public Exception Exception { get; set; }
}
