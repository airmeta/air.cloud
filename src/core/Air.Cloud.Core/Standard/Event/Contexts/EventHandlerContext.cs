using System.Reflection;
using Air.Cloud.Core.Standard.Event.Attributes;

namespace Air.Cloud.Core.Standard.Event.Contexts;

/// <summary>
/// 事件处理程序上下文
/// </summary>
public abstract class EventHandlerContext
{
    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="eventSource">事件源（事件承载对象）</param>
    /// <param name="properties">共享上下文数据</param>
    /// <param name="handlerMethod">触发的方法</param>
    /// <param name="attribute">订阅特性</param>
    internal EventHandlerContext(IEventSource eventSource
        , IDictionary<object, object> properties
        , MethodInfo handlerMethod
        , EventDescriptionAttribute attribute)
    {
        Source = eventSource;
        Properties = properties;
        HandlerMethod = handlerMethod;
        Attribute = attribute;
    }
    /// <summary>
    /// 覆盖原有的服务
    /// </summary>
    /// <param name="serviceProvider"></param>
    public void CoverEventSerivce(IServiceProvider serviceProvider)
    {
        EventSerivce = serviceProvider;
    }
    /// <summary>
    /// 
    /// </summary>
    public IServiceProvider EventSerivce { get; private set; }

    /// <summary>
    /// 事件源（事件承载对象）
    /// </summary>
    public IEventSource Source { get; }

    /// <summary>
    /// 共享上下文数据
    /// </summary>
    public IDictionary<object, object> Properties { get; set; }

    /// <summary>
    /// 触发的方法
    /// </summary>
    /// <remarks>如果是动态订阅，可能为 null</remarks>
    public MethodInfo HandlerMethod { get; }

    /// <summary>
    /// 订阅特性
    /// </summary>
    /// <remarks><remarks>如果是动态订阅，可能为 null</remarks></remarks>
    public EventDescriptionAttribute Attribute { get; }
    /// <summary>
    /// 获取携带数据
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public virtual T GetPayLoad<T>() where T : class => Source.Payload as T;
}