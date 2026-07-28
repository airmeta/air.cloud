namespace Air.Cloud.DynamicApp.Manager;

/// <summary>
/// <para>zh-cn:DynamicApp 包管理应用创建请求，用于先创建应用命名空间再继续维护该应用下的 Mod 包。</para>
/// <para>en-us:DynamicApp package-management application creation request used to create an application namespace before maintaining Mod packages under it.</para>
/// </summary>
public sealed class DynamicAppPackageApplicationRequest
{
    /// <summary>
    /// <para>zh-cn:DynamicApp 应用标识；该值会作为包存储目录名持久化。</para>
    /// <para>en-us:DynamicApp application identifier; this value is persisted as the package-store directory name.</para>
    /// </summary>
    public string DynamicAppId { get; set; } = string.Empty;
}
