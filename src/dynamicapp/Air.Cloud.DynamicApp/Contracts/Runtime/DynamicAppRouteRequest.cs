using Air.Cloud.DynamicApp.Contracts.Identifiers;

namespace Air.Cloud.DynamicApp.Contracts.Runtime;

/// <summary>
/// <para>zh-cn:网关路由解析请求。</para>
/// <para>en-us:Gateway route resolution request.</para>
/// </summary>
public sealed class DynamicAppRouteRequest
{
    /// <summary>
    /// <para>zh-cn:DynamicApp 标识。</para>
    /// <para>en-us:DynamicApp identifier.</para>
    /// </summary>
    public DynamicAppId DynamicAppId { get; set; }

    /// <summary>
    /// <para>zh-cn:Mod 标识。</para>
    /// <para>en-us:Mod identifier.</para>
    /// </summary>
    public DynamicModId ModId { get; set; }

    /// <summary>
    /// <para>zh-cn:请求路径，默认 /。</para>
    /// <para>en-us:Request path, / by default.</para>
    /// </summary>
    public string Path { get; set; } = "/";
}
