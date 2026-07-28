namespace Air.Cloud.Modules.ReverseTunnel.Common.Diagnostics;

/// <summary>
/// <para>zh-cn:AirTunnel 当前进程内指标快照，包含总量聚合和按 ServiceKey/AgentId 聚合的数据。</para>
/// <para>en-us:AirTunnel current-process metric snapshot containing total counters and ServiceKey/AgentId dimensional counters.</para>
/// </summary>
public sealed class ReverseTunnelMetricsSnapshot
{
    /// <summary><para>zh-cn:快照生成的 UTC 时间。</para><para>en-us:UTC time when the snapshot was generated.</para></summary>
    public DateTimeOffset GeneratedAtUtc { get; init; }

    /// <summary><para>zh-cn:AirTunnel 指标 Meter 名称。</para><para>en-us:AirTunnel metric Meter name.</para></summary>
    public string MeterName { get; init; } = ReverseTunnelMetrics.MeterName;

    /// <summary><para>zh-cn:当前进程内所有维度汇总后的指标计数。</para><para>en-us:Metric counters aggregated across all dimensions in the current process.</para></summary>
    public ReverseTunnelMetricCountersSnapshot Total { get; init; } = new();

    /// <summary><para>zh-cn:按 ServiceKey 和 AgentId 拆分的指标计数。</para><para>en-us:Metric counters split by ServiceKey and AgentId.</para></summary>
    public IReadOnlyList<ReverseTunnelMetricDimensionSnapshot> Dimensions { get; init; } = [];
}
