namespace Air.Cloud.Modules.ReverseTunnel.Common.Options;

/// <summary>
/// <para>zh-cn:同一个 ServiceKey 存在多个可用 Agent 会话时，Server 侧会话注册表采用的选择策略。</para>
/// <para>en-us:Selection strategy used by the Server-side session registry when multiple available Agent sessions declare the same ServiceKey.</para>
/// </summary>
public enum ReverseTunnelSessionSelectionStrategy
{
    /// <summary>
    /// <para>zh-cn:选择最近活跃的会话。该值保持向后兼容，适用于本地联调和单活 Agent 场景。</para>
    /// <para>en-us:Selects the most recently active session. This value preserves backward compatibility and fits local debugging or single-active-Agent scenarios.</para>
    /// </summary>
    NewestActive = 0,

    /// <summary>
    /// <para>zh-cn:选择最早活跃的会话。适用于需要固定倾向较老连接、避免新连接立即接管流量的场景。</para>
    /// <para>en-us:Selects the least recently active session. This fits scenarios that prefer older connections and avoid immediate traffic takeover by a new connection.</para>
    /// </summary>
    OldestActive = 1,

    /// <summary>
    /// <para>zh-cn:按 ServiceKey 在当前进程内轮询多个会话。该策略不跨 Gateway 进程共享状态。</para>
    /// <para>en-us:Round-robins multiple sessions per ServiceKey within the current process. This strategy does not share state across Gateway processes.</para>
    /// </summary>
    RoundRobin = 2
}
