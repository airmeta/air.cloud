namespace Air.Cloud.Modules.ReverseTunnel.Common.Options;

/// <summary>
/// <para>zh-cn:AirTunnel Gateway 侧调用配置。该配置用于让 Ocelot Gateway 在不承载 AirTunnel Server 的情况下，把命中的隧道路由转发给独立 AirTunnel Server 进程。</para>
/// <para>en-us:AirTunnel Gateway-side invocation options. These settings let an Ocelot Gateway forward matched tunnel routes to a standalone AirTunnel Server process without using Server mode in the Gateway process.</para>
/// </summary>
public sealed class ReverseTunnelGatewayOptions
{
    /// <summary>
    /// <para>zh-cn:独立 AirTunnel Server 的 HTTP/HTTPS 地址。Gateway 进程未启用 Server 角色时，如果该值为空，则只能返回隧道不可用。</para>
    /// <para>en-us:HTTP/HTTPS address of the standalone AirTunnel Server. When the Gateway process does not use Server mode, an empty value means tunnel invocations can only return unavailable.</para>
    /// </summary>
    public string RemoteServerAddress { get; set; } = string.Empty;

    /// <summary>
    /// <para>zh-cn:Gateway 到 AirTunnel Server 的转发入口前缀。默认 `/_airtunnel/gateway`；Server 侧映射和 Gateway 侧调用必须保持一致。</para>
    /// <para>en-us:Forwarding path prefix from Gateway to AirTunnel Server. The default is `/_airtunnel/gateway`; Server-side mapping and Gateway-side invocation must use the same value.</para>
    /// </summary>
    public string InvokePathPrefix { get; set; } = "/_airtunnel/gateway";

    /// <summary>
    /// <para>zh-cn:可选访问令牌的环境变量名称。配置后，Gateway 会从该环境变量读取令牌并写入请求头；Server 会用同一环境变量校验请求。</para>
    /// <para>en-us:Optional environment-variable name for the access token. When configured, Gateway reads the token from the variable and sends it as a request header; Server validates requests against the same variable.</para>
    /// </summary>
    public string AccessTokenEnvironmentVariable { get; set; } = string.Empty;

    /// <summary>
    /// <para>zh-cn:标准化 Gateway 调用配置。该方法只整理字符串、路径前缀和尾部斜杠，不会启用远程调用，也不会读取令牌值。</para>
    /// <para>en-us:Normalizes Gateway invocation options. This method only trims strings, path prefixes, and trailing slashes; it does not enable remote invocation or read token values.</para>
    /// </summary>
    public void Normalize()
    {
        RemoteServerAddress = RemoteServerAddress?.Trim().TrimEnd('/') ?? string.Empty;
        InvokePathPrefix = string.IsNullOrWhiteSpace(InvokePathPrefix)
            ? "/_airtunnel/gateway"
            : "/" + InvokePathPrefix.Trim().Trim('/');
        AccessTokenEnvironmentVariable = AccessTokenEnvironmentVariable?.Trim() ?? string.Empty;
    }
}
