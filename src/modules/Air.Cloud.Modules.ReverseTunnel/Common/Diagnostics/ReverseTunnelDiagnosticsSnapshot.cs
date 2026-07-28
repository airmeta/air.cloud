namespace Air.Cloud.Modules.ReverseTunnel.Common.Diagnostics;

/// <summary>
/// <para>zh-cn:AirTunnel 诊断 API 返回的完整快照，合并当前进程内指标、会话和配置入口信息。</para>
/// <para>en-us:Full snapshot returned by AirTunnel diagnostic APIs, combining current-process metrics, sessions, and configuration entry information.</para>
/// </summary>
public sealed class ReverseTunnelDiagnosticsSnapshot
{
    /// <summary><para>zh-cn:快照生成的 UTC 时间。</para><para>en-us:UTC time when the snapshot was generated.</para></summary>
    public DateTimeOffset GeneratedAtUtc { get; init; }

    /// <summary><para>zh-cn:AirTunnel 模块是否启用。</para><para>en-us:Whether the AirTunnel module is enabled.</para></summary>
    public bool Enabled { get; init; }

    /// <summary><para>zh-cn:当前进程启用的 AirTunnel 角色。</para><para>en-us:AirTunnel roles enabled in the current process.</para></summary>
    public string Roles { get; init; } = string.Empty;

    /// <summary><para>zh-cn:当前进程内同 ServiceKey 会话选择策略。</para><para>en-us:Session selection strategy for sessions sharing the same ServiceKey in the current process.</para></summary>
    public string SessionSelectionStrategy { get; init; } = string.Empty;

    /// <summary><para>zh-cn:单个 Agent 会话允许的最大并发 HTTP 流数量。</para><para>en-us:Maximum concurrent HTTP streams allowed for one Agent session.</para></summary>
    public int MaxConcurrentStreams { get; init; }

    /// <summary><para>zh-cn:单个 Agent 进程配置的物理隧道连接数量。</para><para>en-us:Number of physical tunnel connections configured for one Agent process.</para></summary>
    public int AgentConnectionCount { get; init; }

    /// <summary><para>zh-cn:诊断 HTTP API 当前是否开启。</para><para>en-us:Whether diagnostic HTTP APIs are currently enabled.</para></summary>
    public bool DiagnosticsEnabled { get; init; }

    /// <summary><para>zh-cn:诊断快照后台采集周期秒数。</para><para>en-us:Background diagnostic snapshot collection interval in seconds.</para></summary>
    public int SnapshotCollectionIntervalSeconds { get; init; }

    /// <summary><para>zh-cn:当前进程内指标快照。</para><para>en-us:Current-process metric snapshot.</para></summary>
    public ReverseTunnelMetricsSnapshot Metrics { get; init; } = new();

    /// <summary><para>zh-cn:当前进程内 Agent 会话快照集合。</para><para>en-us:Current-process Agent session snapshots.</para></summary>
    public IReadOnlyList<ReverseTunnelSessionDiagnosticsSnapshot> Sessions { get; init; } = [];
}
