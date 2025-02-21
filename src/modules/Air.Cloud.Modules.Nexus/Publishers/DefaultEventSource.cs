using Air.Cloud.Core.Standard.Event;
using Air.Cloud.Core.Standard.Event.Extensions;
using Newtonsoft.Json;

namespace Air.Cloud.Modules.Nexus.Publishers;

/// <summary>
/// 内存通道事件源（事件承载对象）
/// </summary>
public class DefaultEventSource : IEventSource
{
    /// <summary>
    /// 构造函数
    /// </summary>
    public DefaultEventSource()
    {
    }

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="eventName">事件 Id</param>
    public DefaultEventSource(string eventName)
    {
        EventName = eventName;
    }

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="eventName">事件 Id</param>
    /// <param name="payload">事件承载（携带）数据</param>
    public DefaultEventSource(string eventName, object payload)
        : this(eventName)
    {
        Payload = payload;
    }

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="eventName">事件 Id</param>
    /// <param name="payload">事件承载（携带）数据</param>
    /// <param name="cancellationToken">取消任务 Token</param>
    public DefaultEventSource(string eventName, object payload, CancellationToken cancellationToken)
        : this(eventName, payload)
    {
        CancellationToken = cancellationToken;
    }

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="eventName">事件 Id</param>
    public DefaultEventSource(Enum eventName)
        : this(eventName.ParseToString())
    {
    }

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="eventName">事件 Id</param>
    /// <param name="payload">事件承载（携带）数据</param>
    public DefaultEventSource(Enum eventName, object payload)
        : this(eventName.ParseToString(), payload)
    {
    }

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="eventName">事件 Id</param>
    /// <param name="payload">事件承载（携带）数据</param>
    /// <param name="cancellationToken">取消任务 Token</param>
    public DefaultEventSource(Enum eventName, object payload, CancellationToken cancellationToken)
        : this(eventName.ParseToString(), payload, cancellationToken)
    {
    }

    /// <summary>
    /// 事件 Id
    /// </summary>
    public string EventName { get; set; }

    /// <summary>
    /// 事件承载（携带）数据
    /// </summary>
    public object Payload { get; set; }

    /// <summary>
    /// 事件创建时间
    /// </summary>
    public DateTime CreatedTime { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 消息是否只消费一次
    /// </summary>
    public bool IsConsumOnce { get; set; }

    /// <summary>
    /// 取消任务 Token
    /// </summary>
    /// <remarks>用于取消本次消息处理</remarks>
    [JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    public CancellationToken CancellationToken { get; set; }

    /// <summary>
    /// 是否忽略未找到
    /// </summary>
    public bool IgnoreNotFound { get; set; } = false;
}