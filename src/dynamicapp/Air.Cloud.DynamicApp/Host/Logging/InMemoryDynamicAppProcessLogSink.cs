using System.Collections.Concurrent;

using Air.Cloud.DynamicApp.Contracts.Runtime;

namespace Air.Cloud.DynamicApp.Host;

/// <summary>
/// <para>zh-cn:进程内 DynamicApp 进程日志 Sink，适合 Compact Host 默认运行和开发调试。</para>
/// <para>en-us:In-process DynamicApp process log sink suitable for default Compact Host runs and development diagnostics.</para>
/// </summary>
public sealed class InMemoryDynamicAppProcessLogSink : IDynamicAppProcessLogSink
{
    private readonly ConcurrentDictionary<string, ConcurrentQueue<DynamicAppProcessLogEntry>> _entries = new();
    private readonly int _maxEntriesPerTarget;

    /// <summary>
    /// <para>zh-cn:创建进程内日志 Sink。</para>
    /// <para>en-us:Creates the in-process log sink.</para>
    /// </summary>
    /// <param name="maxEntriesPerTarget">
    /// <para>zh-cn:单个目标最多保留日志条数，默认 1000，最小值 1。</para>
    /// <para>en-us:Maximum retained log entries per target; defaults to 1000 and has a minimum of 1.</para>
    /// </param>
    public InMemoryDynamicAppProcessLogSink(int maxEntriesPerTarget = 1000)
    {
        _maxEntriesPerTarget = Math.Max(1, maxEntriesPerTarget);
    }

    /// <summary>
    /// <para>zh-cn:写入单条日志，并裁剪超出保留上限的旧日志。</para>
    /// <para>en-us:Writes one log entry and trims older entries beyond the retention limit.</para>
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
    /// <para>zh-cn:已完成的写入任务。</para>
    /// <para>en-us:A completed write task.</para>
    /// </returns>
    public Task WriteAsync(DynamicAppProcessLogEntry entry, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entry);
        cancellationToken.ThrowIfCancellationRequested();

        var queue = _entries.GetOrAdd(entry.Key.ToString(), _ => new ConcurrentQueue<DynamicAppProcessLogEntry>());
        queue.Enqueue(entry);
        while (queue.Count > _maxEntriesPerTarget && queue.TryDequeue(out _))
        {
        }

        return Task.CompletedTask;
    }

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
    /// <para>zh-cn:按写入时间升序返回的日志。</para>
    /// <para>en-us:Log entries returned in ascending write order.</para>
    /// </returns>
    public Task<IReadOnlyCollection<DynamicAppProcessLogEntry>> QueryAsync(DynamicModVersionKey key, int take = 200, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!_entries.TryGetValue(key.ToString(), out var queue))
        {
            return Task.FromResult<IReadOnlyCollection<DynamicAppProcessLogEntry>>(Array.Empty<DynamicAppProcessLogEntry>());
        }

        var count = Math.Max(1, take);
        return Task.FromResult<IReadOnlyCollection<DynamicAppProcessLogEntry>>(queue
            .Reverse()
            .Take(count)
            .Reverse()
            .ToList());
    }
}
