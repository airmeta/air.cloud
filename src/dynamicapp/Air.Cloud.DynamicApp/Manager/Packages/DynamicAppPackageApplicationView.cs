namespace Air.Cloud.DynamicApp.Manager;

/// <summary>
/// <para>zh-cn:DynamicApp 包管理应用视图，表示可以在应用管理器中选择并继续创建 Mod 包的应用。</para>
/// <para>en-us:DynamicApp package-management application view representing an application that can be selected before creating Mod packages.</para>
/// </summary>
public sealed class DynamicAppPackageApplicationView
{
    /// <summary>
    /// <para>zh-cn:DynamicApp 应用标识。</para>
    /// <para>en-us:DynamicApp application identifier.</para>
    /// </summary>
    public string DynamicAppId { get; set; } = string.Empty;

    /// <summary>
    /// <para>zh-cn:该应用下已发现的 Mod 数量。</para>
    /// <para>en-us:Number of discovered Mods under the application.</para>
    /// </summary>
    public int ModCount { get; set; }

    /// <summary>
    /// <para>zh-cn:该应用下已发现的包版本数量。</para>
    /// <para>en-us:Number of discovered package versions under the application.</para>
    /// </summary>
    public int VersionCount { get; set; }
}
