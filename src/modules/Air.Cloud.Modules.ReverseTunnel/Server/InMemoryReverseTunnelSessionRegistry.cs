using Air.Cloud.Modules.ReverseTunnel.Common.Diagnostics;
using Air.Cloud.Modules.ReverseTunnel.Common.Options;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;

namespace Air.Cloud.Modules.ReverseTunnel.Server;

/// <summary>
/// <para>zh-cn:线程安全的本地 Agent 会话注册表。注册表按会话标识保存当前进程内的活跃连接，同一个 AgentId 可以注册多条物理连接，会话选择策略决定某个 ServiceKey 的转发目标。</para>
/// <para>en-us:Thread-safe local Agent-session registry. The registry stores active connections in the current process by session identifier; the same AgentId may register multiple physical connections, and the session-selection strategy chooses the forwarding target for a ServiceKey.</para>
/// </summary>
public sealed class InMemoryReverseTunnelSessionRegistry : IReverseTunnelSessionRegistry
{
    private readonly ConcurrentDictionary<string, ReverseTunnelSession> sessions = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<string, long> roundRobinCounters = new(StringComparer.OrdinalIgnoreCase);
    private readonly IOptions<ReverseTunnelOptions> options;

    /// <summary>
    /// <para>zh-cn:创建内存会话注册表。空闲摘除阈值从 ReverseTunnelSettings:SessionIdleTimeoutSeconds 读取。</para>
    /// <para>en-us:Creates an in-memory session registry. The idle-eviction threshold is read from ReverseTunnelSettings:SessionIdleTimeoutSeconds.</para>
    /// </summary>
    /// <param name="options">
    /// <para>zh-cn:反向隧道配置选项。</para>
    /// <para>en-us:Reverse tunnel configuration options.</para>
    /// </param>
    public InMemoryReverseTunnelSessionRegistry(IOptions<ReverseTunnelOptions> options) => this.options = options;

    /// <inheritdoc />
    public void Register(ReverseTunnelSession session)
    {
        if (string.IsNullOrWhiteSpace(session.SessionId) || string.IsNullOrWhiteSpace(session.AgentId)) throw new ArgumentException("Reverse tunnel session and agent identifiers are required.", nameof(session));
        sessions[session.SessionId] = session;
    }

    /// <inheritdoc />
    public bool TryGet(string sessionId, out ReverseTunnelSession? session) => sessions.TryGetValue(sessionId, out session);

    /// <inheritdoc />
    public bool TryGetForServiceKey(string serviceKey, out ReverseTunnelSession? session)
    {
        session = null;
        if (string.IsNullOrWhiteSpace(serviceKey))
        {
            return false;
        }

        var normalizedServiceKey = serviceKey.Trim();
        var candidates = sessions.Values
            .Where(item => item.ServiceKeys.Contains(normalizedServiceKey))
            .ToArray();

        session = SelectSession(normalizedServiceKey, candidates);

        return session is not null;
    }

    /// <inheritdoc />
    public bool Remove(string sessionId)
    {
        return sessions.TryRemove(sessionId, out _);
    }

    /// <inheritdoc />
    public int RemoveExpired(DateTimeOffset utcNow)
    {
        var threshold = TimeSpan.FromSeconds(options.Value.SessionIdleTimeoutSeconds);
        return sessions.Values.Where(session => utcNow - session.LastSeenUtc >= threshold).Count(session => Remove(session.SessionId));
    }

    /// <inheritdoc />
    public IReadOnlyList<ReverseTunnelSessionDiagnosticsSnapshot> GetSnapshot(DateTimeOffset utcNow)
    {
        return sessions.Values
            .OrderBy(session => session.AgentId, StringComparer.OrdinalIgnoreCase)
            .ThenBy(session => session.SessionId, StringComparer.Ordinal)
            .Select(session =>
            {
                var lastSeenUtc = session.LastSeenUtc;
                return new ReverseTunnelSessionDiagnosticsSnapshot
                {
                    SessionId = session.SessionId,
                    AgentId = session.AgentId,
                    ServiceKeys = session.ServiceKeys.OrderBy(key => key, StringComparer.OrdinalIgnoreCase).ToArray(),
                    ConnectedAtUtc = session.ConnectedAtUtc,
                    LastSeenUtc = lastSeenUtc,
                    IdleSeconds = Math.Max(0, (utcNow - lastSeenUtc).TotalSeconds),
                    ActiveStreamCount = session.ActiveStreamCount
                };
            })
            .ToArray();
    }

    private ReverseTunnelSession? SelectSession(string serviceKey, IReadOnlyList<ReverseTunnelSession> candidates)
    {
        if (candidates.Count == 0)
        {
            return null;
        }

        return options.Value.SessionSelectionStrategy switch
        {
            ReverseTunnelSessionSelectionStrategy.OldestActive => candidates
                .OrderBy(item => item.LastSeenUtc)
                .ThenBy(item => item.SessionId, StringComparer.Ordinal)
                .FirstOrDefault(),
            ReverseTunnelSessionSelectionStrategy.RoundRobin => SelectRoundRobin(serviceKey, candidates),
            _ => candidates
                .OrderByDescending(item => item.LastSeenUtc)
                .ThenBy(item => item.SessionId, StringComparer.Ordinal)
                .FirstOrDefault()
        };
    }

    private ReverseTunnelSession SelectRoundRobin(string serviceKey, IReadOnlyList<ReverseTunnelSession> candidates)
    {
        var orderedCandidates = candidates
            .OrderBy(item => item.SessionId, StringComparer.Ordinal)
            .ToArray();
        var next = roundRobinCounters.AddOrUpdate(serviceKey, 0, (_, current) => unchecked(current + 1));
        var index = (int)(next % orderedCandidates.Length);
        if (index < 0)
        {
            index += orderedCandidates.Length;
        }

        return orderedCandidates[index];
    }
}
