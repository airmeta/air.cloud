using Air.Cloud.DynamicApp.Contracts.Identifiers;

namespace Air.Cloud.DynamicApp.Contracts.Manifest;

/// <summary>
/// <para>zh-cn:DynamicApp 发布清单根对象，描述应用标识、版本、运行形态、端点和 Mod 集合。</para>
/// <para>en-us:Root DynamicApp release manifest describing the application identity, version, runtime profile, endpoints, and Mods.</para>
/// </summary>
public sealed class DynamicAppManifest
{
    /// <summary>
    /// <para>zh-cn:DynamicApp 标识。</para>
    /// <para>en-us:DynamicApp identifier.</para>
    /// </summary>
    public DynamicAppId Id { get; set; }

    /// <summary>
    /// <para>zh-cn:DynamicApp 发布版本。</para>
    /// <para>en-us:DynamicApp release version.</para>
    /// </summary>
    public DynamicAppVersion Version { get; set; }

    /// <summary>
    /// <para>zh-cn:运行时形态配置，默认使用 Compact 低资源模式。</para>
    /// <para>en-us:Runtime profile configuration, using the low-resource Compact profile by default.</para>
    /// </summary>
    public DynamicAppRuntimeManifest Runtime { get; set; } = new();

    /// <summary>
    /// <para>zh-cn:网关和管理端点配置。</para>
    /// <para>en-us:Gateway and management endpoint configuration.</para>
    /// </summary>
    public DynamicAppEndpointManifest Endpoints { get; set; } = new();

    /// <summary>
    /// <para>zh-cn:清单声明的 Mod 集合。</para>
    /// <para>en-us:Mods declared by this manifest.</para>
    /// </summary>
    public List<DynamicModManifest> Mods { get; set; } = new();
}
