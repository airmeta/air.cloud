using Air.Cloud.DynamicApp.Contracts.Identifiers;
using Air.Cloud.DynamicApp.Contracts.Runtime;

namespace Air.Cloud.DynamicApp.Manager;

/// <summary>
/// <para>zh-cn:DynamicApp 目标版本键请求模型。</para>
/// <para>en-us:Request model for a DynamicApp target version key.</para>
/// </summary>
public class DynamicAppTargetKeyRequest
{
    /// <summary>
    /// <para>zh-cn:DynamicApp 标识。</para>
    /// <para>en-us:DynamicApp identifier.</para>
    /// </summary>
    public string DynamicAppId { get; set; } = string.Empty;

    /// <summary>
    /// <para>zh-cn:Mod 标识。</para>
    /// <para>en-us:Mod identifier.</para>
    /// </summary>
    public string ModId { get; set; } = string.Empty;

    /// <summary>
    /// <para>zh-cn:目标版本。</para>
    /// <para>en-us:Target version.</para>
    /// </summary>
    public string Version { get; set; } = string.Empty;

    /// <summary>
    /// <para>zh-cn:可选的独立部署实例标识；为空时使用 default 实例兼容旧调用。</para>
    /// <para>en-us:Optional isolated deployment identifier; empty uses the default instance for legacy callers.</para>
    /// </summary>
    public string? DeploymentId { get; set; }

    /// <summary>
    /// <para>zh-cn:转换为运行时目标版本键；字段为空或格式非法时抛出参数异常。</para>
    /// <para>en-us:Converts to a runtime target version key; throws an argument exception when fields are empty or invalid.</para>
    /// </summary>
    /// <returns>
    /// <para>zh-cn:运行时目标版本键。</para>
    /// <para>en-us:The runtime target version key.</para>
    /// </returns>
    public DynamicModVersionKey ToKey() => new(new DynamicAppId(DynamicAppId), new DynamicModId(ModId), new DynamicAppVersion(Version));

    /// <summary>
    /// <para>zh-cn:将请求转换为包含部署实例的运行时键。</para>
    /// <para>en-us:Converts the request into a runtime key including its deployment instance.</para>
    /// </summary>
    /// <returns>
    /// <para>zh-cn:运行实例键。</para>
    /// <para>en-us:The runtime-instance key.</para>
    /// </returns>
    public DynamicAppRuntimeKey ToRuntimeKey()
        => new(ToKey(), string.IsNullOrWhiteSpace(DeploymentId) ? "default" : DeploymentId.Trim());
}
