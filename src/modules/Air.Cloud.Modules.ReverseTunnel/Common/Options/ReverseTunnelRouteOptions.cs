namespace Air.Cloud.Modules.ReverseTunnel.Common.Options;

using Air.Cloud.Modules.ReverseTunnel.Common.Routing;

/// <summary>
/// <para>zh-cn:一条由 Agent 公开路径派生的反向隧道路由。Gateway 按 PublicPath 匹配，并使用内部 ServiceKey 选择 Agent 会话。</para>
/// <para>en-us:A reverse tunnel route derived from an Agent public path. The Gateway matches PublicPath and uses the internal ServiceKey to select an Agent session.</para>
/// </summary>
public sealed class ReverseTunnelRouteOptions
{
    /// <summary>
    /// <para>zh-cn:Gateway 对外公开的路径前缀，例如 `/orders`。Gateway 自有路由中间件按路径段匹配该值，并在转发前剥离此前缀。</para>
    /// <para>en-us:Public path prefix exposed by the Gateway, for example `/orders`. The Gateway-native routing middleware matches this value by path segment and removes the prefix before forwarding.</para>
    /// </summary>
    public string PublicPath { get; set; } = string.Empty;

    /// <summary>
    /// <para>zh-cn:匹配后发送给 Agent 的目标路径重写模板，例如 `/tests` 或 `/tests/{path}`。</para>
    /// <para>en-us:Target-path rewrite template sent to the Agent after matching, such as `/tests` or `/tests/{path}`.</para>
    /// </summary>
    public string RewritePath { get; set; } = string.Empty;

    /// <summary>
    /// <para>zh-cn:已废弃的 Ocelot 虚拟主机字段。新路由不读取该字段。</para>
    /// <para>en-us:Deprecated Ocelot virtual-host field. New routing does not read this value.</para>
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
        PublicPath = ReverseTunnelRouteTemplate.NormalizePublicPath(PublicPath);
        RewritePath = ReverseTunnelRouteTemplate.NormalizeRewritePath(RewritePath);
        VirtualHost = VirtualHost?.Trim().TrimEnd('.').ToLowerInvariant() ?? string.Empty;
        ServiceKey = ServiceKey?.Trim() ?? string.Empty;
        TimeoutSeconds = Math.Clamp(TimeoutSeconds, 1, 300);
    }
}
