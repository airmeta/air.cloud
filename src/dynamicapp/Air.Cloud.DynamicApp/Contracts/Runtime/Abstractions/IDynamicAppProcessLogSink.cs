namespace Air.Cloud.DynamicApp.Contracts.Runtime;

/// <summary>
/// <para>zh-cn:Process 模式日志写入接口，供 Host 将 stdout、stderr 和退出事件落到可查询介质。</para>
/// <para>en-us:Process-mode log sink used by Host to persist stdout, stderr, and exit events into a queryable medium.</para>
/// </summary>
public interface IDynamicAppProcessLogSink
{
    /// <summary>
    /// <para>zh-cn:写入单条进程日志。</para>
    /// <para>en-us:Writes one process log entry.</para>
    /// </summary>
    /// <param name="entry">
    /// <para>zh-cn:进程日志条目。</para>
    /// <para>en-us:Process log entry.</para>
    /// </param>
    /// <param name="cancellationToken">
    /// <para>zh-cn:取消令牌。</para>
    /// <para>en-us:Cancellation token.</para>
    /// </param>
    /// <returns>
    /// <para>zh-cn:异步写入任务。</para>
    /// <para>en-us:Asynchronous write task.</para>
    /// </returns>
    Task WriteAsync(DynamicAppProcessLogEntry entry, CancellationToken cancellationToken = default);

    /// <summary>
    /// <para>zh-cn:查询指定目标的近期日志。</para>
    /// <para>en-us:Queries recent logs for the specified target.</para>
    /// </summary>
    /// <param name="key">
    /// <para>zh-cn:目标版本键。</para>
    /// <para>en-us:Target version key.</para>
    /// </param>
    /// <param name="take">
    /// <para>zh-cn:最多返回条数。</para>
    /// <para>en-us:Maximum number of entries to return.</para>
    /// </param>
    /// <param name="cancellationToken">
    /// <para>zh-cn:取消令牌。</para>
    /// <para>en-us:Cancellation token.</para>
    /// </param>
    /// <returns>
    /// <para>zh-cn:按时间升序返回的日志集合。</para>
    /// <para>en-us:Log entries returned in ascending timestamp order.</para>
    /// </returns>
    Task<IReadOnlyCollection<DynamicAppProcessLogEntry>> QueryAsync(DynamicModVersionKey key, int take = 200, CancellationToken cancellationToken = default);

    /// <summary>
    /// <para>zh-cn:查询指定部署实例的近期进程日志，避免同一包版本的多个副本互相串日志。</para>
    /// <para>en-us:Queries recent process logs for one deployment instance so replicas of the same package version do not share log history.</para>
    /// </summary>
    /// <param name="key">
    /// <para>zh-cn:运行实例键。</para>
    /// <para>en-us:The runtime-instance key.</para>
    /// </param>
    /// <param name="take">
    /// <para>zh-cn:最多返回的日志条数。</para>
    /// <para>en-us:The maximum number of entries to return.</para>
    /// </param>
    /// <param name="cancellationToken">
    /// <para>zh-cn:取消令牌。</para>
    /// <para>en-us:Cancellation token.</para>
    /// </param>
    /// <returns>
    /// <para>zh-cn:按写入时间升序返回的日志集合。</para>
    /// <para>en-us:Log entries returned in ascending write order.</para>
    /// </returns>
    Task<IReadOnlyCollection<DynamicAppProcessLogEntry>> QueryAsync(DynamicAppRuntimeKey key, int take = 200, CancellationToken cancellationToken = default)
        => QueryAsync(key.PackageKey, take, cancellationToken);
}
