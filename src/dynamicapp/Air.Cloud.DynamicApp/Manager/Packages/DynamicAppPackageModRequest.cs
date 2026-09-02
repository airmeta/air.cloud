namespace Air.Cloud.DynamicApp.Manager;

/// <summary>
/// <para>zh-cn:DynamicApp Mod 配置创建请求，用于在指定应用下创建一个空的 Mod 包空间。</para>
/// <para>en-us:DynamicApp Mod configuration creation request used to create an empty Mod package namespace under an application.</para>
/// </summary>
public sealed class DynamicAppPackageModRequest
{
    /// <summary>
    /// <para>zh-cn:DynamicApp 应用标识；该值会作为包空间的应用目录名持久化。</para>
    /// <para>en-us:DynamicApp application identifier persisted as the package-store application directory name.</para>
    /// </summary>
    public string DynamicAppId { get; set; } = string.Empty;

    /// <summary>
    /// <para>zh-cn:Mod 标识；该值会作为应用下的 Mod 目录名持久化。</para>
    /// <para>en-us:Mod identifier persisted as the Mod directory name under the application.</para>
    /// </summary>
    public string ModId { get; set; } = string.Empty;
}
