using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Threading.Channels;

using Air.Cloud.DynamicApp.Contracts.Runtime;

namespace Air.Cloud.DynamicApp.Host;

/// <summary>
/// <para>zh-cn:DynamicApp 进程日志广播器，将 Runner 写入的 stdout、stderr 和退出事件广播给实时订阅者。</para>
/// <para>en-us:DynamicApp process-log broadcaster that publishes Runner stdout, stderr, and exit events to live subscribers.</para>
/// </summary>
public sealed class DynamicAppProcessLogBroadcaster
{
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<Guid, Channel<DynamicAppProcessLogEntry>>> _subscriptions = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// <para>zh-cn:发布一条进程日志；没有浏览器订阅时直接丢弃广播副本，历史日志仍由 Log Sink 保留。</para>
    /// <para>en-us:Publishes one process log; when no browser is subscribed, the broadcast copy is discarded while history remains in the log sink.</para>
    /// </summary>
    /// <param name="entry">
    /// <para>zh-cn:进程日志条目。</para>
    /// <para>en-us:Process log entry.</para>
    /// </param>
    public void Publish(DynamicAppProcessLogEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);
        if (!_subscriptions.TryGetValue(BuildRuntimeKey(entry).ToString(), out var subscriptions))
        {
            return;
        }

        foreach (var subscription in subscriptions.Values)
        {
            subscription.Writer.TryWrite(entry);
        }
    }

    /// <summary>
    /// <para>zh-cn:订阅指定目标的实时日志，客户端断开或取消时自动释放订阅。</para>
    /// <para>en-us:Subscribes to live logs for one target and automatically releases the subscription when the client disconnects or cancels.</para>
    /// </summary>
    /// <param name="key">
    /// <para>zh-cn:目标版本键。</para>
    /// <para>en-us:Target version key.</para>
    /// </param>
    /// <param name="cancellationToken">
    /// <para>zh-cn:取消令牌。</para>
    /// <para>en-us:Cancellation token.</para>
    /// </param>
    /// <returns>
    /// <para>zh-cn:实时日志异步序列。</para>
    /// <para>en-us:Asynchronous sequence of live log entries.</para>
    /// </returns>
    public async IAsyncEnumerable<DynamicAppProcessLogEntry> Subscribe(
        DynamicModVersionKey key,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await foreach (var entry in Subscribe(DynamicAppRuntimeKey.Default(key), cancellationToken).ConfigureAwait(false))
        {
            yield return entry;
        }
    }

    /// <summary>
    /// <para>zh-cn:订阅指定部署实例的实时日志，实例之间相互隔离。</para>
    /// <para>en-us:Subscribes to live logs for one deployment instance with replica-level isolation.</para>
    /// </summary>
    /// <param name="key">
    /// <para>zh-cn:运行实例键。</para>
    /// <para>en-us:The runtime-instance key.</para>
    /// </param>
    /// <param name="cancellationToken">
    /// <para>zh-cn:取消令牌。</para>
    /// <para>en-us:Cancellation token.</para>
    /// </param>
    /// <returns>
    /// <para>zh-cn:实时日志异步序列。</para>
    /// <para>en-us:Asynchronous sequence of live log entries.</para>
    /// </returns>
    public async IAsyncEnumerable<DynamicAppProcessLogEntry> Subscribe(
        DynamicAppRuntimeKey key,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var targetKey = key.ToString();
        var subscriptionId = Guid.NewGuid();
        var channel = Channel.CreateUnbounded<DynamicAppProcessLogEntry>();
        var subscriptions = _subscriptions.GetOrAdd(targetKey, _ => new ConcurrentDictionary<Guid, Channel<DynamicAppProcessLogEntry>>());
        subscriptions[subscriptionId] = channel;

        try
        {
            await foreach (var entry in channel.Reader.ReadAllAsync(cancellationToken).ConfigureAwait(false))
            {
                yield return entry;
            }
        }
        finally
        {
            subscriptions.TryRemove(subscriptionId, out _);
            channel.Writer.TryComplete();
            if (subscriptions.IsEmpty)
            {
                _subscriptions.TryRemove(targetKey, out _);
            }
        }
    }

    private static DynamicAppRuntimeKey BuildRuntimeKey(DynamicAppProcessLogEntry entry)
    {
        return new DynamicAppRuntimeKey(
            entry.Key,
            string.IsNullOrWhiteSpace(entry.DeploymentId) ? "default" : entry.DeploymentId.Trim());
    }
}
