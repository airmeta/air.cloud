namespace Air.Cloud.DynamicApp.Contracts.Runtime;

/// <summary>
/// <para>zh-cn:独立的 DynamicApp Gateway 动态路由规则；该规则描述入口模板、目标运行对象和下游模板。</para>
/// <para>en-us:Independent DynamicApp Gateway route rule describing the upstream template, runtime target, and downstream template.</para>
/// </summary>
public sealed class DynamicAppGatewayRoute
{
    /// <summary>
    /// <para>zh-cn:稳定的路由规则标识。</para>
    /// <para>en-us:Stable route-rule identifier.</para>
    /// </summary>
    public string RouteId { get; set; } = string.Empty;

    /// <summary>
    /// <para>zh-cn:管理页面展示名称。</para>
    /// <para>en-us:Display name used by the management page.</para>
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// <para>zh-cn:AirForge 接收请求时使用的 Ocelot 风格路径模板。</para>
    /// <para>en-us:Ocelot-style path template received by AirForge.</para>
    /// </summary>
    public string UpstreamPathTemplate { get; set; } = string.Empty;

    /// <summary>
    /// <para>zh-cn:允许匹配的 HTTP 方法；为空表示允许所有方法。</para>
    /// <para>en-us:HTTP methods allowed for matching; an empty collection allows every method.</para>
    /// </summary>
    public string[] UpstreamHttpMethods { get; set; } = Array.Empty<string>();

    /// <summary>
    /// <para>zh-cn:目标 DynamicApp 标识。</para>
    /// <para>en-us:Target DynamicApp identifier.</para>
    /// </summary>
    public string TargetDynamicAppId { get; set; } = string.Empty;

    /// <summary>
    /// <para>zh-cn:目标 Mod 标识。</para>
    /// <para>en-us:Target Mod identifier.</para>
    /// </summary>
    public string TargetModId { get; set; } = string.Empty;

    /// <summary>
    /// <para>zh-cn:可选目标版本；保存时可以指向当前不可用版本，但转发时必须重新检查目标状态。</para>
    /// <para>en-us:Optional target version; saving may reference a currently unavailable version, but forwarding must recheck target state.</para>
    /// </summary>
    public string? TargetVersion { get; set; }

    /// <summary>
    /// <para>zh-cn:可选 DeploymentId；为空时由运行时在可用实例中按路由轮询。</para>
    /// <para>en-us:Optional DeploymentId; when empty, runtime round-robins among available instances for the route.</para>
    /// </summary>
    public string? TargetDeploymentId { get; set; }

    /// <summary>
    /// <para>zh-cn:发给目标实例的 Ocelot 风格下游路径模板。</para>
    /// <para>en-us:Ocelot-style downstream path template sent to the target instance.</para>
    /// </summary>
    public string DownstreamPathTemplate { get; set; } = string.Empty;

    /// <summary>
    /// <para>zh-cn:路由匹配顺序，数值越小越先匹配；子路由必须使用小于父路由的值。</para>
    /// <para>en-us:Route matching order; lower values match first, and child routes must use a lower value than their parent routes.</para>
    /// </summary>
    public int Order { get; set; }

    /// <summary>
    /// <para>zh-cn:是否参与请求匹配。</para>
    /// <para>en-us:Whether the rule participates in request matching.</para>
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// <para>zh-cn:路由创建时间。</para>
    /// <para>en-us:Route creation time.</para>
    /// </summary>
    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// <para>zh-cn:路由最后更新时间。</para>
    /// <para>en-us:Route last-update time.</para>
    /// </summary>
    public DateTimeOffset UpdatedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// <para>zh-cn:最近一次管理校验或运行状态摘要；不用于记录每次请求。</para>
    /// <para>en-us:Summary of the latest management validation or runtime state; it is not updated for every request.</para>
    /// </summary>
    public string LastStatusMessage { get; set; } = string.Empty;
}
