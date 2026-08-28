using System.Collections.Concurrent;

namespace Air.Cloud.Modules.ReverseTunnel.Server;

/// <summary>
/// <para>zh-cn:线程安全的内存 Enrollment replay store。适合单 Server 进程；多实例部署应替换为共享原子存储。</para>
/// <para>en-us:Thread-safe in-memory enrollment replay store. It is suitable for a single Server process; multi-instance deployments should replace it with a shared atomic store.</para>
/// </summary>
public sealed class InMemoryReverseTunnelEnrollmentReplayStore : IReverseTunnelEnrollmentReplayStore
{
    private readonly ConcurrentDictionary<string, DateTimeOffset> claims = new(StringComparer.OrdinalIgnoreCase);

    /// <inheritdoc />
    public bool TryClaim(string agentId, Guid guid, DateTimeOffset utcNow, TimeSpan replayWindow)
    {
        if (string.IsNullOrWhiteSpace(agentId) || guid == Guid.Empty || replayWindow <= TimeSpan.Zero)
        {
            return false;
        }

        var cutoff = utcNow - replayWindow;
        foreach (var existing in claims)
        {
            if (existing.Value < cutoff)
            {
                claims.TryRemove(new KeyValuePair<string, DateTimeOffset>(existing.Key, existing.Value));
            }
        }

        // The nonce is globally unique; keying only by GUID also blocks cross-Agent replay.
        var key = guid.ToString("N");
        return claims.TryAdd(key, utcNow);
    }
}
