namespace Air.Cloud.Modules.ReverseTunnel.Common.Diagnostics;

/// <summary>
/// <para>zh-cn:AirTunnel 诊断快照读取服务。实现应只读取当前进程内状态，不主动连接外部观测平台或远程 Gateway。</para>
/// <para>en-us:AirTunnel diagnostic snapshot reader. Implementations should only read current-process state and must not actively connect to external observability platforms or remote Gateways.</para>
/// </summary>
public interface IReverseTunnelDiagnosticsService
{
    /// <summary>
    /// <para>zh-cn:读取完整诊断快照，包含模块配置摘要、指标快照和当前进程内 Agent 会话。</para>
    /// <para>en-us:Reads the full diagnostic snapshot, including module configuration summary, metric snapshot, and current-process Agent sessions.</para>
    /// </summary>
    /// <returns>
    /// <para>zh-cn:完整诊断快照。</para>
    /// <para>en-us:The full diagnostic snapshot.</para>
    /// </returns>
    ReverseTunnelDiagnosticsSnapshot GetSnapshot();

    /// <summary>
    /// <para>zh-cn:读取当前进程内的指标快照。</para>
    /// <para>en-us:Reads the current-process metric snapshot.</para>
    /// </summary>
    /// <returns>
    /// <para>zh-cn:指标快照。</para>
    /// <para>en-us:The metric snapshot.</para>
    /// </returns>
    ReverseTunnelMetricsSnapshot GetMetrics();

    /// <summary>
    /// <para>zh-cn:读取当前进程内的 Agent 会话快照。</para>
    /// <para>en-us:Reads current-process Agent session snapshots.</para>
    /// </summary>
    /// <returns>
    /// <para>zh-cn:会话快照集合。</para>
    /// <para>en-us:Collection of session snapshots.</para>
    /// </returns>
    IReadOnlyList<ReverseTunnelSessionDiagnosticsSnapshot> GetSessions();
}
