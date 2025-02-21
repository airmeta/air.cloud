namespace Air.Cloud.Core.Standard.Event;
/// <summary>
/// 事件总线执行引擎接口
/// </summary>
/// <remarks>
/// 定义事件总线核心执行逻辑，典型功能包括：
/// 1. 事件订阅者的动态注册与卸载
/// 2. 消息队列的消费协调
/// 3. 背压控制与错误重试策略
/// </remarks>
/// <example>
/// ASP.NET Core集成示例：
/// </example>
public interface IEventBusExecutor
{
    /// <summary>
    /// 启动事件总线异步执行引擎
    /// </summary>
    /// <param name="stoppingToken">
    /// 取消令牌，传播服务停止通知，用法示例：
    /// <code>
    /// while (!stoppingToken.IsCancellationRequested) 
    /// {
    ///     // 处理消息
    /// }
    /// </code>
    /// </param>
    /// <remarks>
    /// 实现要求：
    /// 1. 支持优雅关闭（收到取消请求后完成当前消息处理）
    /// 2. 异常处理需记录错误上下文
    /// 3. 建议采用异步流（Async Streams）处理消息序列
    /// </remarks>
    Task ExecuteAsync(CancellationToken stoppingToken);
}

