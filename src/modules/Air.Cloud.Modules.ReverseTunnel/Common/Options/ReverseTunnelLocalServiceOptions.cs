namespace Air.Cloud.Modules.ReverseTunnel.Common.Options;

/// <summary>
/// <para>zh-cn:Agent 可代理的本地 HTTP 服务声明。Server 在后续阶段仅接受与 Agent 授权范围一致的 ServiceKey。</para>
/// <para>en-us:Local HTTP service declaration an Agent can proxy. In a later phase, the Server will accept only service keys within the Agent authorization scope.</para>
/// </summary>
public sealed class ReverseTunnelLocalServiceOptions
{
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

    internal void Normalize()
    {
        ServiceKey = ServiceKey?.Trim() ?? string.Empty;
        LocalAddress = LocalAddress?.Trim().TrimEnd('/') ?? string.Empty;
    }
}
