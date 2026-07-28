using Air.Cloud.DynamicApp.Contracts.Lifecycle;

namespace Air.Cloud.DynamicApp.Contracts.Manifest;

/// <summary>
/// <para>zh-cn:DynamicApp 运行形态配置。</para>
/// <para>en-us:DynamicApp runtime profile configuration.</para>
/// </summary>
public sealed class DynamicAppRuntimeManifest
{
    /// <summary>
    /// <para>zh-cn:运行形态，默认 Compact，用于单应用低资源部署。</para>
    /// <para>en-us:Runtime profile, Compact by default for low-resource single-application deployment.</para>
    /// </summary>
    public DynamicAppRuntimeProfile Profile { get; set; } = DynamicAppRuntimeProfile.Compact;
}
