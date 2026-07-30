namespace Air.Cloud.Modules.ReverseTunnel.Common.Protocol;

/// <summary>
/// <para>zh-cn:TCP 穿透连接打开帧负载。Server 只携带逻辑服务键和监听器策略，Agent 必须根据本地配置解析真实目标地址。</para>
/// <para>en-us:Payload for opening a TCP tunnel connection. The Server carries only the logical service key and listener policy; the Agent must resolve the real target address from local configuration.</para>
/// </summary>
public sealed class ReverseTunnelTcpOpenPayload
{
    /// <summary><para>zh-cn:逻辑服务键。</para><para>en-us:Logical service key.</para></summary>
    public string ServiceKey { get; set; } = string.Empty;

    /// <summary><para>zh-cn:Server 侧监听器名称，仅用于日志和诊断。</para><para>en-us:Server-side listener name, used only for logs and diagnostics.</para></summary>
    public string ListenerName { get; set; } = string.Empty;

    /// <summary><para>zh-cn:Agent 连接本地 TCP 目标的超时秒数。</para><para>en-us:Timeout in seconds for the Agent to connect to the local TCP target.</para></summary>
    public int ConnectTimeoutSeconds { get; set; } = 5;

    /// <summary><para>zh-cn:TCP 穿透连接空闲超时秒数。</para><para>en-us:Idle timeout in seconds for the TCP tunnel connection.</para></summary>
    public int IdleTimeoutSeconds { get; set; } = 300;
}
