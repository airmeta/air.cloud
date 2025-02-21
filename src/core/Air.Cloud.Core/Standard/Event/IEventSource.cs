namespace Air.Cloud.Core.Standard.Event;

/// <summary>
/// 事件源（事件承载对象）依赖接口
/// </summary>
public interface IEventSource
{
    /// <summary>
    /// 事件 Id
    /// </summary>
    string EventName { get; }

    /// <summary>
    /// 事件承载（携带）数据
    /// </summary>
    object Payload { get; }

    /// <summary>
    /// 事件创建时间
    /// </summary>
    DateTime CreatedTime { get; }

    /// <summary>
    /// 取消任务 Token
    /// </summary>
    /// <remarks>用于取消本次消息处理</remarks>
    CancellationToken CancellationToken { get; }

    /// <summary>
    /// 消息是否只消费一次
    /// </summary>
    bool IsConsumOnce { get; }

    /// <summary>
    /// 是否忽略未找到
    /// </summary>
    public bool IgnoreNotFound { get; }
}