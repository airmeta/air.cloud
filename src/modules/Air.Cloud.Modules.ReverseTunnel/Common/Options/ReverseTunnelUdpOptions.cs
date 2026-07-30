namespace Air.Cloud.Modules.ReverseTunnel.Common.Options;

/// <summary>
/// <para>zh-cn:AirTunnel UDP 穿透配置。UDP 保持数据报和流表语义，不模拟可靠连接。</para>
/// <para>en-us:AirTunnel UDP tunneling options. UDP preserves datagram and flow-table semantics and does not emulate a reliable connection.</para>
/// </summary>
public sealed class ReverseTunnelUdpOptions
{
    /// <summary><para>zh-cn:是否启用 UDP 穿透。</para><para>en-us:Whether UDP tunneling is enabled.</para></summary>
    public bool Enabled { get; set; }

    /// <summary><para>zh-cn:Server 侧 UDP 监听器集合。</para><para>en-us:Server-side UDP listener collection.</para></summary>
    public List<ReverseTunnelUdpListenerOptions> Listeners { get; set; } = [];

    /// <summary><para>zh-cn:Agent 可访问的本地 UDP 服务集合。</para><para>en-us:Agent-local UDP service collection.</para></summary>
    public List<ReverseTunnelUdpLocalServiceOptions> LocalServices { get; set; } = [];

    internal void Normalize()
    {
        Listeners ??= [];
        LocalServices ??= [];

        foreach (var listener in Listeners)
        {
            listener?.Normalize();
        }

        foreach (var service in LocalServices)
        {
            service?.Normalize();
        }
    }
}

/// <summary>
/// <para>zh-cn:Server 侧 UDP 监听器配置。每个监听器固定映射到一个逻辑 ServiceKey，并按客户端端点维护流表。</para>
/// <para>en-us:Server-side UDP listener options. Each listener maps to one logical ServiceKey and keeps a flow table by client endpoint.</para>
/// </summary>
public sealed class ReverseTunnelUdpListenerOptions
{
    /// <summary><para>zh-cn:监听器名称，用于日志、诊断和文档识别。</para><para>en-us:Listener name used for logs, diagnostics, and documentation.</para></summary>
    public string Name { get; set; } = string.Empty;

    /// <summary><para>zh-cn:监听器绑定的逻辑服务键。</para><para>en-us:Logical service key bound to the listener.</para></summary>
    public string ServiceKey { get; set; } = string.Empty;

    /// <summary><para>zh-cn:监听地址。为空时绑定 0.0.0.0。</para><para>en-us:Listen address. Empty values bind to 0.0.0.0.</para></summary>
    public string ListenHost { get; set; } = "0.0.0.0";

    /// <summary><para>zh-cn:监听端口。</para><para>en-us:Listen port.</para></summary>
    public int ListenPort { get; set; }

    /// <summary><para>zh-cn:单个监听器允许的最大 UDP 流表数量。</para><para>en-us:Maximum UDP flow entries allowed by one listener.</para></summary>
    public int MaxFlows { get; set; } = 1_000;

    /// <summary><para>zh-cn:UDP 流表空闲过期秒数。</para><para>en-us:UDP flow idle expiration in seconds.</para></summary>
    public int FlowIdleTimeoutSeconds { get; set; } = 60;

    /// <summary><para>zh-cn:单个 UDP 数据报允许的最大字节数。</para><para>en-us:Maximum bytes allowed for one UDP datagram.</para></summary>
    public int MaxDatagramBytes { get; set; } = 1_400;

    internal void Normalize()
    {
        Name = string.IsNullOrWhiteSpace(Name) ? $"{ServiceKey}:{ListenPort}" : Name.Trim();
        ServiceKey = ServiceKey?.Trim() ?? string.Empty;
        ListenHost = string.IsNullOrWhiteSpace(ListenHost) ? "0.0.0.0" : ListenHost.Trim();
        ListenPort = Math.Clamp(ListenPort, 0, 65_535);
        MaxFlows = Math.Clamp(MaxFlows, 1, 1_000_000);
        FlowIdleTimeoutSeconds = Math.Clamp(FlowIdleTimeoutSeconds, 5, 86_400);
        MaxDatagramBytes = Math.Clamp(MaxDatagramBytes, 1, 65_507);
    }
}

/// <summary>
/// <para>zh-cn:Agent 侧本地 UDP 服务配置。真实目标地址只在 Agent 本地声明，不允许外部请求动态传入。</para>
/// <para>en-us:Agent-side local UDP service options. The real target address is declared only on the Agent and cannot be supplied dynamically by external requests.</para>
/// </summary>
public sealed class ReverseTunnelUdpLocalServiceOptions
{
    /// <summary><para>zh-cn:逻辑服务键。</para><para>en-us:Logical service key.</para></summary>
    public string ServiceKey { get; set; } = string.Empty;

    /// <summary><para>zh-cn:Agent 所在网络可访问的本地 UDP 主机。</para><para>en-us:Local UDP host reachable from the Agent network.</para></summary>
    public string Host { get; set; } = "127.0.0.1";

    /// <summary><para>zh-cn:Agent 所在网络可访问的本地 UDP 端口。</para><para>en-us:Local UDP port reachable from the Agent network.</para></summary>
    public int Port { get; set; }

    internal void Normalize()
    {
        ServiceKey = ServiceKey?.Trim() ?? string.Empty;
        Host = string.IsNullOrWhiteSpace(Host) ? "127.0.0.1" : Host.Trim();
        Port = Math.Clamp(Port, 0, 65_535);
    }
}
