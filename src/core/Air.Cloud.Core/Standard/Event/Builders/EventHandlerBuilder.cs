using Air.Cloud.Core.Standard.Event.Contexts;

namespace Air.Cloud.Core.Standard.Event.Builders;
/// <summary>
/// 事件处理器构建器（采用中间件管道模式）
/// </summary>
/// <remarks>
/// 实现基于责任链模式的拦截器管道构建，通过Use()方法注册的中间件将按照注册顺序的逆序执行
/// </remarks>
public class EventHandlerBuilder
{
    /// <summary>
    /// 中间件委托列表（存储顺序即执行顺序）
    /// </summary>
    private readonly IList<Func<EventHandlerDelegate, EventHandlerDelegate>> _interceptors
        = new List<Func<EventHandlerDelegate, EventHandlerDelegate>>();

    /// <summary>
    /// 构建事件处理管道
    /// </summary>
    /// <returns>由多个中间件组成的最终委托链</returns>
    /// <remarks>
    /// 构建过程采用反向遍历策略，确保中间件的注册顺序与执行顺序符合：
    /// 最后注册的中间件最先执行
    /// </remarks>
    public EventHandlerDelegate Build()
    {
        // 初始化终端节点（空任务处理）
        EventHandlerDelegate next = context => Task.CompletedTask;

        // 逆序组合中间件（最后添加的中间件最先执行）
        foreach (var interceptor in _interceptors.Reverse())
        {
            next = interceptor.Invoke(next);
        }

        return next;
    }

    /// <summary>
    /// 注册中间件到处理管道
    /// </summary>
    /// <param name="eventMiddleware">
    /// 中间件委托，格式：Func&lt;EventHandlerDelegate, EventHandlerDelegate&gt; <br/>
    /// 输入参数为下一个处理节点，返回包装后的处理节点
    /// </param>
    /// <returns>当前构建器实例（支持链式调用）</returns>
    /// <example>
    /// builder.Use(next => async context => {
    ///     // 前置处理
    ///     await next(context); 
    ///     // 后置处理
    /// });
    /// </example>
    public EventHandlerBuilder Use(Func<EventHandlerDelegate, EventHandlerDelegate> eventMiddleware)
    {
        _interceptors.Add(eventMiddleware);
        return this;
    }
}

/// <summary>
/// 事件处理委托（异步模式）
/// </summary>
/// <param name="context">
/// 包含以下要素的执行上下文：<br/>
/// - 当前时间执行时间 <see cref="EventHandlerExecutingContext.ExecutingTime"/> <br/>
/// </param>
/// <returns>表示异步操作的任务对象</returns>
/// <remarks>
/// 该委托构成中间件管道的基本执行单元，每个中间件通过调用next委托传递控制权
/// </remarks>
public delegate Task EventHandlerDelegate(EventHandlerExecutingContext context);

