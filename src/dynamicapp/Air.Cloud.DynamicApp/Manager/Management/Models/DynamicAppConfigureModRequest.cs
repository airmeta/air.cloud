using Air.Cloud.DynamicApp.Contracts.Identifiers;
using Air.Cloud.DynamicApp.Contracts.Lifecycle;
using Air.Cloud.DynamicApp.Contracts.Manifest;

namespace Air.Cloud.DynamicApp.Manager;

/// <summary>
/// <para>zh-cn:DynamicApp 单 Mod 动态配置请求，用于外部控制面按配置发布或部署一个 Mod。</para>
/// <para>en-us:DynamicApp single-Mod dynamic configuration request used by external control planes to publish or deploy one Mod by configuration.</para>
/// </summary>
public sealed class DynamicAppConfigureModRequest
{
    /// <summary>
    /// <para>zh-cn:DynamicApp 应用标识。</para>
    /// <para>en-us:DynamicApp application identifier.</para>
    /// </summary>
    public string DynamicAppId { get; set; } = string.Empty;

    /// <summary>
    /// <para>zh-cn:DynamicApp 发布版本；为空时使用 Mod 版本。</para>
    /// <para>en-us:DynamicApp release version; the Mod version is used when empty.</para>
    /// </summary>
    public string AppVersion { get; set; } = string.Empty;

    /// <summary>
    /// <para>zh-cn:网关端口元数据，默认 8080。</para>
    /// <para>en-us:Gateway port metadata, 8080 by default.</para>
    /// </summary>
    public int GatewayPort { get; set; } = 8080;

    /// <summary>
    /// <para>zh-cn:管理端口元数据，默认 8081。</para>
    /// <para>en-us:Manager port metadata, 8081 by default.</para>
    /// </summary>
    public int ManagerPort { get; set; } = 8081;

    /// <summary>
    /// <para>zh-cn:管理端点是否仅限内部访问，默认 true。</para>
    /// <para>en-us:Whether the manager endpoint is internal-only, true by default.</para>
    /// </summary>
    public bool ManagerInternalOnly { get; set; } = true;

    /// <summary>
    /// <para>zh-cn:是否在发布后立即部署并激活，默认 false。</para>
    /// <para>en-us:Whether to deploy and activate immediately after publishing, false by default.</para>
    /// </summary>
    public bool Deploy { get; set; }

    /// <summary>
    /// <para>zh-cn:需要动态配置的 Mod Manifest。</para>
    /// <para>en-us:The Mod manifest to configure dynamically.</para>
    /// </summary>
    public DynamicModManifest Mod { get; set; } = new();

    /// <summary>
    /// <para>zh-cn:转换为只包含目标 Mod 的 DynamicApp Manifest；该方法会规范化应用标识并补齐默认发布版本。</para>
    /// <para>en-us:Converts the request into a DynamicApp manifest containing only the target Mod; this method normalizes the application identifier and fills the default release version.</para>
    /// </summary>
    /// <returns>
    /// <para>zh-cn:DynamicApp Manifest。</para>
    /// <para>en-us:The DynamicApp manifest.</para>
    /// </returns>
    public DynamicAppManifest ToManifest()
    {
        var modVersion = Mod.Version.Value;
        return new DynamicAppManifest
        {
            Id = new DynamicAppId(DynamicAppId),
            Version = new DynamicAppVersion(string.IsNullOrWhiteSpace(AppVersion) ? modVersion : AppVersion),
            Runtime = new DynamicAppRuntimeManifest
            {
                Profile = DynamicAppRuntimeProfile.Compact
            },
            Endpoints = new DynamicAppEndpointManifest
            {
                GatewayPort = GatewayPort,
                ManagerPort = ManagerPort,
                ManagerInternalOnly = ManagerInternalOnly
            },
            Mods = new List<DynamicModManifest>
            {
                Mod
            }
        };
    }
}
