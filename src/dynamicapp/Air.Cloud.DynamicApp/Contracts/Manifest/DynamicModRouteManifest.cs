namespace Air.Cloud.DynamicApp.Contracts.Manifest;

/// <summary>
/// <para>zh-cn:Mod 路由声明。</para>
/// <para>en-us:Mod route declaration.</para>
/// </summary>
public sealed class DynamicModRouteManifest
{
    /// <summary>
    /// <para>zh-cn:网关匹配前缀，必须是绝对路径。</para>
    /// <para>en-us:Gateway match prefix, which must be an absolute path.</para>
    /// </summary>
    public string Prefix { get; set; } = string.Empty;
}
