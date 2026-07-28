namespace Air.Cloud.DynamicApp.Manager;

/// <summary>
/// <para>zh-cn:DynamicApp Mod 包清单视图，按应用和 Mod 聚合所有已上传版本。</para>
/// <para>en-us:DynamicApp Mod package inventory view that groups all uploaded versions by application and Mod.</para>
/// </summary>
public sealed class DynamicAppModPackageModView
{
    /// <summary>
    /// <para>zh-cn:DynamicApp 应用标识。</para>
    /// <para>en-us:DynamicApp application identifier.</para>
    /// </summary>
    public string DynamicAppId { get; set; } = string.Empty;

    /// <summary>
    /// <para>zh-cn:Mod 标识。</para>
    /// <para>en-us:Mod identifier.</para>
    /// </summary>
    public string ModId { get; set; } = string.Empty;

    /// <summary>
    /// <para>zh-cn:该 Mod 下已经上传到包存储中的所有版本。</para>
    /// <para>en-us:All versions uploaded into the package store for this Mod.</para>
    /// </summary>
    public IReadOnlyCollection<DynamicAppModPackageVersionView> Versions { get; set; } = Array.Empty<DynamicAppModPackageVersionView>();
}
