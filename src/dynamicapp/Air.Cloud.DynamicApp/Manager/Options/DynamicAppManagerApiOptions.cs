namespace Air.Cloud.DynamicApp.Manager;

/// <summary>
/// <para>zh-cn:DynamicApp Manager 内部管理 API 选项。</para>
/// <para>en-us:Options for the internal DynamicApp Manager API.</para>
/// </summary>
public sealed class DynamicAppManagerApiOptions
{
    /// <summary>
    /// <para>zh-cn:管理 API 路由前缀，默认 /dynamicapp-manager。</para>
    /// <para>en-us:Manager API route prefix, /dynamicapp-manager by default.</para>
    /// </summary>
    public string RoutePrefix { get; set; } = "/dynamicapp-manager";

    /// <summary>
    /// <para>zh-cn:是否要求控制面板 Ticket，默认 true；生产环境不应关闭。</para>
    /// <para>en-us:Whether a control-panel Ticket is required, true by default; production environments should not disable it.</para>
    /// </summary>
    public bool RequireTicket { get; set; } = true;

    /// <summary>
    /// <para>zh-cn:兼容旧配置的访问令牌开关；新配置应使用 RequireTicket。</para>
    /// <para>en-us:Compatibility switch for legacy access-token configuration; new configuration should use RequireTicket.</para>
    /// </summary>
    [Obsolete("Use RequireTicket instead.")]
    public bool RequireAccessToken
    {
        get => RequireTicket;
        set => RequireTicket = value;
    }

    /// <summary>
    /// <para>zh-cn:控制面板 Ticket 请求头名称，默认 X-DynamicApp-Manager-Ticket。</para>
    /// <para>en-us:Control-panel Ticket request header name, X-DynamicApp-Manager-Ticket by default.</para>
    /// </summary>
    public string HeaderName { get; set; } = "X-DynamicApp-Manager-Ticket";

    /// <summary>
    /// <para>zh-cn:允许访问控制面板的 Ticket 集合；RequireTicket 为 true 时至少需要配置一个 Ticket。</para>
    /// <para>en-us:Tickets allowed to access the control panel; at least one Ticket is required when RequireTicket is true.</para>
    /// </summary>
    public string[] Tickets { get; set; } = Array.Empty<string>();

    /// <summary>
    /// <para>zh-cn:兼容旧配置的访问令牌集合；新配置应使用 Tickets。</para>
    /// <para>en-us:Compatibility collection for legacy access-token configuration; new configuration should use Tickets.</para>
    /// </summary>
    [Obsolete("Use Tickets instead.")]
    public string[] AccessTokens
    {
        get => Tickets;
        set => Tickets = value ?? Array.Empty<string>();
    }
}
