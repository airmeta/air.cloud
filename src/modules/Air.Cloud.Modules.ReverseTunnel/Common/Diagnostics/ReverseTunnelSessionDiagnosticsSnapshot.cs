namespace Air.Cloud.Modules.ReverseTunnel.Common.Diagnostics;

/// <summary>
/// <para>zh-cn:AirTunnel Server 当前进程内 Agent 会话诊断快照。</para>
/// <para>en-us:Diagnostic snapshot for an Agent session in the current AirTunnel Server process.</para>
/// </summary>
public sealed class ReverseTunnelSessionDiagnosticsSnapshot
{
    /// <summary><para>zh-cn:Server 当前进程内会话标识。</para><para>en-us:Session identifier in the current Server process.</para></summary>
    public string SessionId { get; init; } = string.Empty;

    /// <summary><para>zh-cn:Agent 稳定身份。</para><para>en-us:Stable Agent identity.</para></summary>
    public string AgentId { get; init; } = string.Empty;

    /// <summary><para>zh-cn:该会话声明可处理的逻辑服务键集合。</para><para>en-us:Logical service keys declared by this session.</para></summary>
    public IReadOnlyList<string> ServiceKeys { get; init; } = [];

    /// <summary><para>zh-cn:会话建立 UTC 时间。</para><para>en-us:UTC time when the session was established.</para></summary>
    public DateTimeOffset ConnectedAtUtc { get; init; }

    /// <summary><para>zh-cn:最近一次收到该会话帧的 UTC 时间。</para><para>en-us:UTC time when the latest frame from this session was received.</para></summary>
    public DateTimeOffset LastSeenUtc { get; init; }

    /// <summary><para>zh-cn:从最近活跃时间到快照时间的空闲秒数。</para><para>en-us:Idle seconds from the latest activity time to the snapshot time.</para></summary>
    public double IdleSeconds { get; init; }

    /// <summary><para>zh-cn:该会话当前未完成的 HTTP 请求流数量。</para><para>en-us:Number of unfinished HTTP request streams in this session.</para></summary>
    public int ActiveStreamCount { get; init; }
}
