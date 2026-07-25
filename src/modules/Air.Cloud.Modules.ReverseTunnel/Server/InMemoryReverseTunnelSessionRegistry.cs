using Air.Cloud.Modules.ReverseTunnel.Common.Options;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;

namespace Air.Cloud.Modules.ReverseTunnel.Server;

/// <summary>
/// <para>zh-cn:线程安全的本地 Agent 会话注册表。多 Gateway 高可用将在后续阶段通过 Agent 多连接或专用接入层解决。</para>
/// <para>en-us:Thread-safe local Agent-session registry. A later phase will address multi-Gateway high availability through Agent multi-connection or a dedicated ingress tier.</para>
/// </summary>
public sealed class InMemoryReverseTunnelSessionRegistry : IReverseTunnelSessionRegistry
{
    private readonly ConcurrentDictionary<string, ReverseTunnelSession> sessions = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<string, string> agentSessions = new(StringComparer.OrdinalIgnoreCase);
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
        if (agentSessions.TryGetValue(session.AgentId, out var previousSessionId) && previousSessionId != session.SessionId) sessions.TryRemove(previousSessionId, out _);
        sessions[session.SessionId] = session;
        agentSessions[session.AgentId] = session.SessionId;
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

        session = sessions.Values
            .OrderByDescending(item => item.LastSeenUtc)
            .FirstOrDefault(item => item.ServiceKeys.Contains(serviceKey.Trim()));

        return session is not null;
    }

    /// <inheritdoc />
    public bool Remove(string sessionId)
    {
        if (!sessions.TryRemove(sessionId, out var session)) return false;
        if (agentSessions.TryGetValue(session.AgentId, out var activeSessionId) && activeSessionId == sessionId) agentSessions.TryRemove(session.AgentId, out _);
        return true;
    }

    /// <inheritdoc />
    public int RemoveExpired(DateTimeOffset utcNow)
    {
        var threshold = TimeSpan.FromSeconds(options.Value.SessionIdleTimeoutSeconds);
        return sessions.Values.Where(session => utcNow - session.LastSeenUtc >= threshold).Count(session => Remove(session.SessionId));
    }
}
