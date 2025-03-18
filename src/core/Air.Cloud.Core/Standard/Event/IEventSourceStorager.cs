namespace Air.Cloud.Core.Standard.Event;

/// <summary>
/// 事件源存储器（核心存储抽象层）
/// </summary>
/// <remarks>
/// <para>实现事件消息的持久化存储与读取，支持多介质扩展</para>
/// <para>📦 默认实现：<see cref="System.Threading.Channels.Channel"/>（内存通道队列）</para>
/// <para>🔌 可扩展实现：Kafka、SQL Server、Redis 等分布式存储</para>
/// </remarks>
public interface IEventSourceStorager
{
    /// <summary>
    /// 异步写入事件源
    /// </summary>
    /// <param name="eventSource">待存储的事件源对象</param>
    /// <param name="cancellationToken">异步取消令牌（传播取消操作）</param>
    /// <returns>表示写入操作的异步任务</returns>
    /// <exception cref="ArgumentNullException">当 eventSource 为 null 时抛出</exception>
    /// <example>
    /// await storager.WriteAsync(new UserCreatedEvent(), cancellationToken);
    /// </example>
    ValueTask WriteAsync(IEventSource eventSource, CancellationToken cancellationToken);

    /// <summary>
    /// 异步读取单个事件源
    /// </summary>
    /// <param name="cancellationToken">异步取消令牌（支持读取中断）</param>
    /// <returns>包含事件源对象的异步任务（可能返回 null 表示无数据）</returns>
    /// <remarks>
    /// ⚠ 注意：实现类需明确无数据时的处理逻辑（阻塞/返回null/抛出异常）
    /// </remarks>
    ValueTask<IEventSource> ReadAsync(CancellationToken cancellationToken);
}