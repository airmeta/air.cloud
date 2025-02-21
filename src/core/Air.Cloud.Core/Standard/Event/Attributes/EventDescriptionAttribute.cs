using Air.Cloud.Core.Standard.Event.Extensions;

namespace Air.Cloud.Core.Standard.Event.Attributes;

/// <summary>
/// 事件处理程序特性
/// </summary>
/// <remarks>
/// <para>作用于 <see cref="IEventHandler"/> 实现类实例方法</para>
/// <para>支持多个事件 Id 触发同一个事件处理程序</para>
/// </remarks>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = false)]
public sealed class EventDescriptionAttribute : Attribute
{
    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="eventName">事件 Id</param>
    /// <remarks>只支持事件类型和 Enum 类型</remarks>
    public EventDescriptionAttribute(object eventName)
    {
        if (eventName is string)
        {
            EventName = eventName as string;
        }
        else if (eventName is Enum)
        {
            EventName = (eventName as Enum).ParseToString();
        }
        else throw new ArgumentException("Only support string or Enum data type.");
    }

    /// <summary>
    /// 事件 Id
    /// </summary>
    public string EventName { get; set; }

    /// <summary>
    /// 是否启用模糊匹配消息
    /// </summary>
    /// <remarks>支持正则表达式，bool 类型，默认为 null</remarks>
    public object FuzzyMatch { get; set; } = null;

    /// <summary>
    /// 是否启用执行完成触发 GC 回收
    /// </summary>
    /// <remarks>bool 类型，默认为 null</remarks>
    public object GCCollect { get; set; } = null;

    /// <summary>
    /// 重试次数
    /// </summary>
    public int NumRetries { get; set; } = 0;

    /// <summary>
    /// 重试间隔时间
    /// </summary>
    /// <remarks>默认1000毫秒</remarks>
    public int RetryTimeout { get; set; } = 1000;

    /// <summary>
    /// 可以指定特定异常类型才重试
    /// </summary>
    public Type[] ExceptionTypes { get; set; }

    /// <summary>
    /// 重试失败行为规则配置
    /// </summary>
    /// <remarks>如果没有注册，必须通过 options.AddFallbackRule(type) 注册</remarks>
    public Type FallbackRule { get; set; }

    /// <summary>
    /// 排序
    /// </summary>
    /// <remarks>数值越大的先执行</remarks>
    public int Order { get; set; } = 0;
}