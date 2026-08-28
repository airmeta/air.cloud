namespace Air.Cloud.Modules.ReverseTunnel.Common.Options;

/// <summary>
/// <para>zh-cn:Agent 可代理的本地 HTTP 服务声明。Server 在后续阶段仅接受与 Agent 授权范围一致的 ServiceKey。</para>
/// <para>en-us:Local HTTP service declaration an Agent can proxy. In a later phase, the Server will accept only service keys within the Agent authorization scope.</para>
/// </summary>
public sealed class ReverseTunnelLocalServiceOptions
{
    /// <summary>
    /// <para>zh-cn:服务展示名称，仅用于管理页面、日志和运维识别，不参与隧道协议寻址。</para>
    /// <para>en-us:Service display name used only by management pages, logs, and operations; it does not participate in tunnel protocol addressing.</para>
    /// </summary>
    public string ServiceName { get; set; } = string.Empty;

    /// <summary>
    /// <para>zh-cn:Gateway 对外暴露的 HTTP 路径前缀，例如 `/orders`。保存 Agent 配置时该路径会规范化，并用于生成内部 ServiceKey。</para>
    /// <para>en-us:HTTP path prefix exposed by the Gateway, for example `/orders`. Saving Agent configuration normalizes this path and uses it to generate the internal ServiceKey.</para>
    /// </summary>
    public string PublicPath { get; set; } = string.Empty;

    /// <summary>
    /// <para>zh-cn:可选的目标路径重写模板，例如 `/tests` 或 `/tests/{path}`。未配置时保留公开路径剥离后的相对路径。</para>
    /// <para>en-us:Optional target-path rewrite template, such as `/tests` or `/tests/{path}`. When omitted, the relative path left after removing the public route is preserved.</para>
    /// </summary>
    public string RewritePath { get; set; } = string.Empty;

    /// <summary>
    /// <para>zh-cn:逻辑服务键。</para>
    /// <para>en-us:Logical service key.</para>
    /// </summary>
    public string ServiceKey { get; set; } = string.Empty;

    /// <summary>
    /// <para>zh-cn:Agent 所在环境可访问的本地 HTTP 根地址。生产环境通常应使用 127.0.0.1 地址。</para>
    /// <para>en-us:Local HTTP base address reachable from the Agent environment. Production deployments normally use a 127.0.0.1 address.</para>
    /// </summary>
    public string LocalAddress { get; set; } = string.Empty;

    /// <summary>
    /// <para>zh-cn:Agent 本地目标主机。与 Port、Protocol 共同组成目标地址。</para>
    /// <para>en-us:Agent-local target host. Together with Port and Protocol, this forms the target address.</para>
    /// </summary>
    public string Host { get; set; } = "127.0.0.1";

    /// <summary>
    /// <para>zh-cn:Agent 本地目标端口，必须为 1-65535。</para>
    /// <para>en-us:Agent-local target port, which must be between 1 and 65535.</para>
    /// </summary>
    public int Port { get; set; }

    /// <summary>
    /// <para>zh-cn:Agent 本地目标协议，仅支持 http 或 https。</para>
    /// <para>en-us:Agent-local target protocol. Only http and https are supported.</para>
    /// </summary>
    public string Protocol { get; set; } = "http";

    internal void Normalize()
    {
        ServiceName = ServiceName?.Trim() ?? string.Empty;
        PublicPath = PublicPath?.Trim() ?? string.Empty;
        RewritePath = RewritePath?.Trim().TrimEnd('/') ?? string.Empty;
        ServiceKey = ServiceKey?.Trim() ?? string.Empty;
        LocalAddress = LocalAddress?.Trim().TrimEnd('/') ?? string.Empty;
        Host = string.IsNullOrWhiteSpace(Host) ? "127.0.0.1" : Host.Trim();
        Protocol = string.IsNullOrWhiteSpace(Protocol) ? "http" : Protocol.Trim().ToLowerInvariant();
        Port = Math.Clamp(Port, 0, 65_535);

        // Keep old in-memory callers readable while the persisted management shape uses Host/Port/Protocol.
        if (!string.IsNullOrWhiteSpace(LocalAddress) && Port <= 0 && Uri.TryCreate(LocalAddress, UriKind.Absolute, out var address) &&
            (address.Scheme is "http" or "https") && address.Port > 0)
        {
            Protocol = address.Scheme.ToLowerInvariant();
            Host = address.Host;
            Port = address.Port;
        }
    }
}
