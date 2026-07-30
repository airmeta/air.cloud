namespace Air.Cloud.Modules.ReverseTunnel.Common.Protocol;

/// <summary>
/// <para>zh-cn:UDP 数据报穿透帧负载。UDP 保持数据报语义，不提供顺序、可靠性或重传。</para>
/// <para>en-us:Payload for a UDP datagram tunnel frame. UDP keeps datagram semantics and does not provide ordering, reliability, or retransmission.</para>
/// </summary>
public sealed class ReverseTunnelUdpDatagramPayload
{
    /// <summary><para>zh-cn:逻辑服务键。</para><para>en-us:Logical service key.</para></summary>
    public string ServiceKey { get; set; } = string.Empty;

    /// <summary><para>zh-cn:Server 侧监听器名称，仅用于日志和诊断。</para><para>en-us:Server-side listener name, used only for logs and diagnostics.</para></summary>
    public string ListenerName { get; set; } = string.Empty;

    /// <summary><para>zh-cn:单个 UDP 数据报内容。</para><para>en-us:Single UDP datagram content.</para></summary>
    public byte[] Data { get; set; } = Array.Empty<byte>();
}
