namespace Air.Cloud.Core.Standard.Event;


/// <summary>
/// 事件处理器依赖接口
/// </summary>
/// <remarks>
/// <para>可自定义事件处理方法，但须符合 Func{EventSubscribeExecutingContext, Task} 签名</para>
/// <para>通常只做依赖查找，不做服务调用</para>
/// </remarks>
public interface IEventHandler
{
    /*
     * // 事件处理程序定义规范
     * [EventDescriptionAttribute(YourEventID)]
     * public Task YourHandler(EventHandlerExecutingContext context)
     * {
     *     // To Do...
     * }
     */
}