using Air.Cloud.Core.Standard.Event.Attributes;
using Air.Cloud.Core.Standard.Event.Builders;
using System.Reflection;
using System.Text.RegularExpressions;

namespace Air.Cloud.Modules.Nexus.Wrappers;

/// <summary>
/// 事件处理程序包装类
/// </summary>
/// <remarks>主要用于主机服务启动时将所有处理程序和事件 Id 进行包装绑定</remarks>
internal sealed class EventHandlerWrapper
{
    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="eventName">事件Id</param>
    internal EventHandlerWrapper(string eventName)
    {
        EventName = eventName;
    }

    /// <summary>
    /// 事件 Id
    /// </summary>
    internal string EventName { get; set; }

    /// <summary>
    /// 事件处理程序
    /// </summary>
    internal EventHandlerDelegate Handler { get; set; }
    /// <summary>
    /// 触发的方法
    /// </summary>
    internal MethodInfo HandlerMethod { get; set; }

    /// <summary>
    /// 订阅特性
    /// </summary>
    internal EventDescriptionAttribute Attribute { get; set; }

    /// <summary>
    /// 正则表达式
    /// </summary>
    internal Regex Pattern { get; set; }

    /// <summary>
    /// 是否启用执行完成触发 GC 回收
    /// </summary>
    public bool GCCollect { get; set; }

    /// <summary>
    /// 排序
    /// </summary>
    /// <remarks>数值越大的先执行</remarks>
    public int Order { get; set; } = 0;

    /// <summary>
    /// 是否符合条件执行处理程序
    /// </summary>
    /// <remarks>支持正则表达式</remarks>
    /// <param name="eventName">事件 Id</param>
    /// <returns><see cref="bool"/></returns>
    internal bool ShouldRun(string eventName)
    {
        return EventName == eventName || (Pattern?.IsMatch(eventName) ?? false);
    }
    /// <summary>
    /// 创建
    /// </summary>
    /// <param name="eventHandlerWrapper"></param>
    public static List<EventHandlerWrapper> CreateList(params EventHandlerWrapper[] eventHandlerWrappers)
    {
        return eventHandlerWrappers.Where(e => e != null).ToList();
    }
}