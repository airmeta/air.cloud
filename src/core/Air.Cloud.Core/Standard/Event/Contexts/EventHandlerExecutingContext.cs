using System.Reflection;
using Air.Cloud.Core.Standard.Event.Attributes;

namespace Air.Cloud.Core.Standard.Event.Contexts;

/// <summary>
/// 事件处理程序执行前上下文
/// </summary>
public sealed class EventHandlerExecutingContext : EventHandlerContext
{
    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="eventSource">事件源（事件承载对象）</param>
    /// <param name="properties">共享上下文数据</param>
    /// <param name="handlerMethod">触发的方法</param>
    /// <param name="attribute">订阅特性</param>
    public  EventHandlerExecutingContext(IEventSource eventSource
        , IDictionary<object, object> properties
        , MethodInfo handlerMethod
        , EventDescriptionAttribute attribute)
        : base(eventSource, properties, handlerMethod, attribute)
    {
    }

    /// <summary>
    /// 执行前时间
    /// </summary>
    public DateTime ExecutingTime { get;  set; }
}