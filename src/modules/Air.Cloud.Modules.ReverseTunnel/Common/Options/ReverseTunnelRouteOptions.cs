namespace Air.Cloud.Modules.ReverseTunnel.Common.Options;

/// <summary>
/// <para>zh-cn:一条由 Ocelot 虚拟下游主机标识的反向隧道路由。后续 Gateway 处理器通过 VirtualHost 找到 ServiceKey，而不是把 B 注册为可直连的 Consul 地址。</para>
/// <para>en-us:A reverse tunnel route identified by an Ocelot virtual downstream host. The later Gateway handler resolves ServiceKey from VirtualHost instead of registering B as a directly reachable Consul address.</para>
/// </summary>
public sealed class ReverseTunnelRouteOptions
{
    /// <summary>
    /// <para>zh-cn:Ocelot 下游配置中的虚拟主机名，例如 tunnel-order-service。</para>
    /// <para>en-us:Virtual host name in Ocelot downstream configuration, for example tunnel-order-service.</para>
    /// </summary>
    public string VirtualHost { get; set; } = string.Empty;

    /// <summary>
    /// <para>zh-cn:由已连接 Agent 提供的逻辑服务键。</para>
    /// <para>en-us:Logical service key provided by a connected Agent.</para>
    /// </summary>
    public string ServiceKey { get; set; } = string.Empty;

    /// <summary>
    /// <para>zh-cn:单次调用超时秒数。未配置时使用 30 秒。</para>
    /// <para>en-us:Per-call timeout in seconds. The default is 30 seconds when unspecified.</para>
    /// </summary>
    public int TimeoutSeconds { get; set; } = 30;

    internal void Normalize()
    {
        VirtualHost = VirtualHost?.Trim().TrimEnd('.').ToLowerInvariant() ?? string.Empty;
        ServiceKey = ServiceKey?.Trim() ?? string.Empty;
        TimeoutSeconds = Math.Clamp(TimeoutSeconds, 1, 300);
    }
}
