using Air.Cloud.DynamicApp.Contracts.Manifest;
using Air.Cloud.DynamicApp.Contracts.Runtime;

namespace Air.Cloud.DynamicApp.Manager;

/// <summary>
/// <para>zh-cn:DynamicApp 单 Mod 配置结果，包含生成的 Manifest、包保存记录和发布或部署操作结果。</para>
/// <para>en-us:DynamicApp single-Mod configuration result containing the generated manifest, package save record, and publish or deploy operation result.</para>
/// </summary>
public sealed class DynamicAppConfigureModResult
{
    /// <summary>
    /// <para>zh-cn:本次配置生成或发布的 Manifest。</para>
    /// <para>en-us:The manifest generated or published by this configuration operation.</para>
    /// </summary>
    public DynamicAppManifest Manifest { get; set; } = new();

    /// <summary>
    /// <para>zh-cn:可选包保存记录；纯 JSON 配置不上传文件时为 null。</para>
    /// <para>en-us:Optional package save record; null for JSON-only configuration without file upload.</para>
    /// </summary>
    public DynamicAppModPackageRecord? Package { get; set; }

    /// <summary>
    /// <para>zh-cn:发布或部署操作结果。</para>
    /// <para>en-us:The publish or deploy operation result.</para>
    /// </summary>
    public DynamicAppOperationResult Operation { get; set; } = DynamicAppOperationResult.Success("DynamicApp Mod configured.");
}
