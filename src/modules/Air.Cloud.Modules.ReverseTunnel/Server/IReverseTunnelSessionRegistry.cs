using Air.Cloud.Modules.ReverseTunnel.Common.Diagnostics;

namespace Air.Cloud.Modules.ReverseTunnel.Server;

/// <summary>
/// <para>zh-cn:管理当前 Server 进程中 Agent 活跃会话的注册表。该接口只表达本进程会话状态，不承诺跨 Gateway 共享。</para>
/// <para>en-us:Registry that manages active Agent sessions in the current Server process. This interface represents only local process state and does not promise cross-Gateway sharing.</para>
/// </summary>
public interface IReverseTunnelSessionRegistry
{
    /// <summary>
    /// <para>zh-cn:注册一个活跃 Agent 会话。同一个 AgentId 可以同时拥有多条物理连接；只有相同 SessionId 会被后续注册覆盖。</para>
    /// <para>en-us:Registers an active Agent session. The same AgentId may own multiple physical connections at the same time; only a later registration with the same SessionId replaces the existing entry.</para>
    /// </summary>
    /// <param name="session">
    /// <para>zh-cn:需要注册的 Agent 会话快照。</para>
    /// <para>en-us:Agent session snapshot to register.</para>
    /// </param>
    void Register(ReverseTunnelSession session);

    /// <summary>
    /// <para>zh-cn:按会话标识查询活跃会话。未找到时输出 session 为 null。</para>
    /// <para>en-us:Gets an active session by session identifier. When no session is found, the output session is null.</para>
    /// </summary>
    /// <param name="sessionId">
    /// <para>zh-cn:Server 分配或 Agent Hello 帧提供的会话标识。</para>
    /// <para>en-us:Session identifier assigned by the Server or supplied by the Agent Hello frame.</para>
    /// </param>
    /// <param name="session">
    /// <para>zh-cn:找到的会话快照。</para>
    /// <para>en-us:Resolved session snapshot.</para>
    /// </param>
    /// <returns>
    /// <para>zh-cn:找到活跃会话返回 true，否则返回 false。</para>
    /// <para>en-us:Returns true when an active session is found; otherwise false.</para>
    /// </returns>
    bool TryGet(string sessionId, out ReverseTunnelSession? session);

    /// <summary>
    /// <para>zh-cn:按逻辑服务键查找当前进程内可处理该服务的活跃 Agent 会话。首版只返回本进程会话，不跨 Gateway 查找。</para>
    /// <para>en-us:Finds an active Agent session in the current process that can handle the logical service key. The first version returns only local-process sessions and does not search across Gateways.</para>
    /// </summary>
    /// <param name="serviceKey">
    /// <para>zh-cn:需要转发的逻辑服务键。</para>
    /// <para>en-us:Logical service key to forward.</para>
    /// </param>
    /// <param name="session">
    /// <para>zh-cn:找到的活跃 Agent 会话。</para>
    /// <para>en-us:Resolved active Agent session.</para>
    /// </param>
    /// <returns>
    /// <para>zh-cn:找到可处理该服务的会话时返回 true。</para>
    /// <para>en-us:Returns true when a session that can handle the service is found.</para>
    /// </returns>
    bool TryGetForServiceKey(string serviceKey, out ReverseTunnelSession? session);

    /// <summary>
    /// <para>zh-cn:移除会话；连接正常断开、异常结束或被空闲摘除时调用。</para>
    /// <para>en-us:Removes a session; called when a connection ends normally, fails, or is evicted as idle.</para>
    /// </summary>
    /// <param name="sessionId">
    /// <para>zh-cn:需要移除的会话标识。</para>
    /// <para>en-us:Session identifier to remove.</para>
    /// </param>
    /// <returns>
    /// <para>zh-cn:实际移除会话返回 true；会话不存在返回 false。</para>
    /// <para>en-us:Returns true when a session was removed; false when it did not exist.</para>
    /// </returns>
    bool Remove(string sessionId);

    /// <summary>
    /// <para>zh-cn:移除超过配置空闲阈值的会话。空闲阈值来自 ReverseTunnelSettings:SessionIdleTimeoutSeconds。</para>
    /// <para>en-us:Removes sessions that exceed the configured idle threshold. The threshold comes from ReverseTunnelSettings:SessionIdleTimeoutSeconds.</para>
    /// </summary>
    /// <param name="utcNow">
    /// <para>zh-cn:用于判断空闲时间的 UTC 当前时间。</para>
    /// <para>en-us:Current UTC time used to evaluate idle duration.</para>
    /// </param>
    /// <returns>
    /// <para>zh-cn:本次移除的会话数量。</para>
    /// <para>en-us:Number of sessions removed by this call.</para>
    /// </returns>
    int RemoveExpired(DateTimeOffset utcNow);

    /// <summary>
    /// <para>zh-cn:读取当前进程内所有 Agent 会话的只读诊断快照。返回值不跨 Gateway 聚合，也不包含证书私钥或一次性凭据。</para>
    /// <para>en-us:Reads read-only diagnostic snapshots for all Agent sessions in the current process. The result is not aggregated across Gateways and contains no certificate private keys or one-time credentials.</para>
    /// </summary>
    /// <param name="utcNow">
    /// <para>zh-cn:用于计算会话空闲时长的 UTC 当前时间。</para>
    /// <para>en-us:Current UTC time used to calculate session idle duration.</para>
    /// </param>
    /// <returns>
    /// <para>zh-cn:当前进程内会话快照集合。</para>
    /// <para>en-us:Collection of session snapshots from the current process.</para>
    /// </returns>
    IReadOnlyList<ReverseTunnelSessionDiagnosticsSnapshot> GetSnapshot(DateTimeOffset utcNow);
}
