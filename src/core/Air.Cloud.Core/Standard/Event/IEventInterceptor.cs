using Air.Cloud.Core.Standard.Event.Contexts;

namespace Air.Cloud.Core.Standard.Event;

/// <summary>
/// 事件拦截器标准接口
/// </summary>
/// <remarks>
/// 该接口定义在事件处理器执行前进行拦截的异步操作，<br/>
/// 允许实现类对事件处理流程进行自定义控制（如验证、日志记录、参数修改等）
/// </remarks>
/// <example>
/// //这是一个简单的示例
/// public class ExampleInterceptor : IEventInterceptor
/// {
///     private readonly EventHandlerDelegate _next;
///    public ExampleInterceptor(EventHandlerDelegate next)
///     {
///         _next = next;
///     }
/// 
///     public async Task ExcuteAsync(EventHandlerExecutingContext context)
///     {
///         //前置执行 todo:
///         //....
///         await _next.Invoke(context);
/// 
///         //后置执行 todo:
///         //....
///     }
/// }
/// </example>
public interface IEventInterceptor
{
    /// <summary>
    /// 异步执行事件拦截逻辑
    /// </summary>
    /// <param name="context">
    /// 事件处理器执行上下文，包含：<br/>
    /// - 当前执行时间 <see cref="EventHandlerExecutingContext.ExecutingTime"/><br/>
    /// </param>
    /// <returns>表示异步操作的任务对象</returns>
    /// <example>
    /// 典型实现：
    /// <code>
    /// public async Task ExcuteAsync(EventHandlerExecutingContext context)
    /// {
    ///     // 前置拦截逻辑（如参数校验）
    ///     if(!Validate(context.EventArgs)) 
    ///         throw new ValidationException();
    ///     
    ///     // 执行后续处理器链
    ///     await context.Next();
    /// }
    /// </code>
    /// </example>
    Task ExcuteAsync(EventHandlerExecutingContext context);
}
