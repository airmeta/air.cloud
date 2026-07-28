namespace Air.Cloud.DynamicApp.Gateway;

/// <summary>
/// <para>zh-cn:DynamicApp Gateway 转发选项。</para>
/// <para>en-us:Forwarding options for DynamicApp Gateway.</para>
/// </summary>
public sealed class DynamicAppGatewayOptions
{
    /// <summary>
    /// <para>zh-cn:当前 Gateway 所属 DynamicApp 标识，宿主必须显式设置。</para>
    /// <para>en-us:Identifier of the DynamicApp owned by the current Gateway; the host must set it explicitly.</para>
    /// </summary>
    public string DynamicAppId { get; set; } = string.Empty;

    /// <summary>
    /// <para>zh-cn:Mod 路由入口前缀，默认 /mods。</para>
    /// <para>en-us:Route entry prefix for Mods, /mods by default.</para>
    /// </summary>
    public string RoutePrefix { get; set; } = "/mods";

    /// <summary>
    /// <para>zh-cn:是否把原始 Host 请求头转发给上游，默认 false。</para>
    /// <para>en-us:Whether to forward the original Host header upstream, false by default.</para>
    /// </summary>
    public bool ForwardHostHeader { get; set; }

    /// <summary>
    /// <para>zh-cn:上游转发超时时间，默认 100 秒；小于等于零时使用默认值。</para>
    /// <para>en-us:Upstream forwarding timeout, 100 seconds by default; non-positive values use the default.</para>
    /// </summary>
    public TimeSpan ForwardTimeout { get; set; } = TimeSpan.FromSeconds(100);

    /// <summary>
    /// <para>zh-cn:最大请求体字节数，默认 100 MB；小于等于零表示不在 Gateway 层限制。</para>
    /// <para>en-us:Maximum request body size in bytes, 100 MB by default; non-positive values disable gateway-level limiting.</para>
    /// </summary>
    public long MaxRequestBodyBytes { get; set; } = 100 * 1024 * 1024;

    /// <summary>
    /// <para>zh-cn:是否拒绝 WebSocket/Upgrade 请求，默认 true，因为当前转发器只覆盖 HTTP 请求/响应。</para>
    /// <para>en-us:Whether WebSocket/Upgrade requests are rejected, true by default because the current forwarder only covers HTTP request/response traffic.</para>
    /// </summary>
    public bool RejectUpgradeRequests { get; set; } = true;
}
