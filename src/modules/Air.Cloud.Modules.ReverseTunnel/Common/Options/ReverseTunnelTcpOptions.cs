namespace Air.Cloud.Modules.ReverseTunnel.Common.Options;

/// <summary>
/// <para>zh-cn:AirTunnel TCP 穿透配置。Server 侧使用监听器接收外部 TCP 连接，Agent 侧使用本地服务声明连接内网目标。</para>
/// <para>en-us:AirTunnel TCP tunneling options. The Server receives external TCP connections through listeners, while the Agent uses local service declarations to connect to internal targets.</para>
/// </summary>
public sealed class ReverseTunnelTcpOptions
{
    /// <summary><para>zh-cn:是否启用 TCP 穿透。</para><para>en-us:Whether TCP tunneling is enabled.</para></summary>
    public bool Enabled { get; set; }

    /// <summary><para>zh-cn:Server 侧 TCP 监听器集合。</para><para>en-us:Server-side TCP listener collection.</para></summary>
    public List<ReverseTunnelTcpListenerOptions> Listeners { get; set; } = [];

    /// <summary><para>zh-cn:Agent 可连接的本地 TCP 服务集合。</para><para>en-us:Agent-local TCP service collection.</para></summary>
    public List<ReverseTunnelTcpLocalServiceOptions> LocalServices { get; set; } = [];

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
/// <para>zh-cn:Server 侧 TCP 监听器配置。每个监听器固定映射到一个逻辑 ServiceKey。</para>
/// <para>en-us:Server-side TCP listener options. Each listener maps to a fixed logical ServiceKey.</para>
/// </summary>
public sealed class ReverseTunnelTcpListenerOptions
{
    /// <summary><para>zh-cn:监听器名称，用于日志、诊断和文档识别。</para><para>en-us:Listener name used for logs, diagnostics, and documentation.</para></summary>
    public string Name { get; set; } = string.Empty;

    /// <summary><para>zh-cn:监听器绑定的逻辑服务键。</para><para>en-us:Logical service key bound to the listener.</para></summary>
    public string ServiceKey { get; set; } = string.Empty;

    /// <summary><para>zh-cn:监听地址。为空时绑定 0.0.0.0。</para><para>en-us:Listen address. Empty values bind to 0.0.0.0.</para></summary>
    public string ListenHost { get; set; } = "0.0.0.0";

    /// <summary><para>zh-cn:监听端口。</para><para>en-us:Listen port.</para></summary>
    public int ListenPort { get; set; }

    /// <summary><para>zh-cn:单个监听器允许的最大并发 TCP 连接数。</para><para>en-us:Maximum concurrent TCP connections allowed by one listener.</para></summary>
    public int MaxConnections { get; set; } = 200;

    /// <summary><para>zh-cn:Agent 连接本地 TCP 目标的超时秒数。</para><para>en-us:Timeout in seconds for the Agent to connect to the local TCP target.</para></summary>
    public int ConnectTimeoutSeconds { get; set; } = 5;

    /// <summary><para>zh-cn:TCP 穿透连接空闲超时秒数。</para><para>en-us:Idle timeout in seconds for the TCP tunnel connection.</para></summary>
    public int IdleTimeoutSeconds { get; set; } = 300;

    internal void Normalize()
    {
        Name = string.IsNullOrWhiteSpace(Name) ? $"{ServiceKey}:{ListenPort}" : Name.Trim();
        ServiceKey = ServiceKey?.Trim() ?? string.Empty;
        ListenHost = string.IsNullOrWhiteSpace(ListenHost) ? "0.0.0.0" : ListenHost.Trim();
        ListenPort = Math.Clamp(ListenPort, 0, 65_535);
        MaxConnections = Math.Clamp(MaxConnections, 1, 100_000);
        ConnectTimeoutSeconds = Math.Clamp(ConnectTimeoutSeconds, 1, 300);
        IdleTimeoutSeconds = Math.Clamp(IdleTimeoutSeconds, 5, 86_400);
    }
}

/// <summary>
/// <para>zh-cn:Agent 侧本地 TCP 服务配置。真实目标地址只在 Agent 本地声明，不允许外部请求动态传入。</para>
/// <para>en-us:Agent-side local TCP service options. The real target address is declared only on the Agent and cannot be supplied dynamically by external requests.</para>
/// </summary>
public sealed class ReverseTunnelTcpLocalServiceOptions
{
    /// <summary><para>zh-cn:服务展示名称，仅用于管理页面、日志和运维识别。</para><para>en-us:Service display name used only by management pages, logs, and operations.</para></summary>
    public string ServiceName { get; set; } = string.Empty;

    /// <summary><para>zh-cn:逻辑服务键。</para><para>en-us:Logical service key.</para></summary>
    public string ServiceKey { get; set; } = string.Empty;

    /// <summary><para>zh-cn:Agent 所在网络可访问的本地 TCP 主机。</para><para>en-us:Local TCP host reachable from the Agent network.</para></summary>
    public string Host { get; set; } = "127.0.0.1";

    /// <summary><para>zh-cn:Agent 所在网络可访问的本地 TCP 端口。</para><para>en-us:Local TCP port reachable from the Agent network.</para></summary>
    public int Port { get; set; }

    /// <summary>
    /// <para>zh-cn:Server 对外监听的绑定地址。为空时使用 0.0.0.0。</para>
    /// <para>en-us:Server-side bind address. Empty values use 0.0.0.0.</para>
    /// </summary>
    public string ServerListenHost { get; set; } = "0.0.0.0";

    /// <summary>
    /// <para>zh-cn:Server 对外监听端口。该值与 Agent 本地目标端口独立；为 0 时不派生 Server listener。</para>
    /// <para>en-us:Server-side exposed port. It is independent from the Agent target port; zero disables Server-listener derivation.</para>
    /// </summary>
    public int ServerListenPort { get; set; }

    internal void Normalize()
    {
        ServiceName = ServiceName?.Trim() ?? string.Empty;
        ServiceKey = ServiceKey?.Trim() ?? string.Empty;
        Host = string.IsNullOrWhiteSpace(Host) ? "127.0.0.1" : Host.Trim();
        Port = Math.Clamp(Port, 0, 65_535);
        ServerListenHost = string.IsNullOrWhiteSpace(ServerListenHost) ? "0.0.0.0" : ServerListenHost.Trim();
        ServerListenPort = Math.Clamp(ServerListenPort, 0, 65_535);
    }
}
