namespace Air.Cloud.Modules.ReverseTunnel.Common.Diagnostics;

/// <summary>
/// <para>zh-cn:按 ServiceKey 和 AgentId 聚合的 AirTunnel 指标快照。</para>
/// <para>en-us:AirTunnel metric snapshot aggregated by ServiceKey and AgentId.</para>
/// </summary>
public sealed class ReverseTunnelMetricDimensionSnapshot
{
    /// <summary><para>zh-cn:逻辑服务键。</para><para>en-us:Logical service key.</para></summary>
    public string ServiceKey { get; init; } = "unknown";

    /// <summary><para>zh-cn:Agent 稳定身份。</para><para>en-us:Stable Agent identity.</para></summary>
    public string AgentId { get; init; } = "unknown";

    /// <summary><para>zh-cn:该 ServiceKey/AgentId 维度下的指标计数。</para><para>en-us:Metric counters for this ServiceKey/AgentId dimension.</para></summary>
    public ReverseTunnelMetricCountersSnapshot Counters { get; init; } = new();
}
