using Air.Cloud.DynamicApp.Contracts.Identifiers;

namespace Air.Cloud.DynamicApp.Contracts.Runtime;

/// <summary>
/// <para>zh-cn:DynamicApp 运行实例唯一键，由包版本键和独立部署标识组成；同一包版本可以拥有多个运行实例。</para>
/// <para>en-us:Unique DynamicApp runtime-instance key composed of a package version key and an isolated deployment identifier; one package version may have multiple runtime instances.</para>
/// </summary>
/// <param name="PackageKey">
/// <para>zh-cn:不可变包的 DynamicApp/Mod/Version 键。</para>
/// <para>en-us:The DynamicApp/Mod/Version key of the immutable package.</para>
/// </param>
/// <param name="DeploymentId">
/// <para>zh-cn:独立部署实例标识，用于区分灰度、蓝绿或集群副本。</para>
/// <para>en-us:Isolated deployment identifier used to distinguish gray, blue-green, or clustered replicas.</para>
/// </param>
public readonly record struct DynamicAppRuntimeKey(DynamicModVersionKey PackageKey, string DeploymentId)
{
    /// <summary>
    /// <para>zh-cn:返回用于状态、日志和诊断的复合键。</para>
    /// <para>en-us:Returns the composite key used by state, log, and diagnostic stores.</para>
    /// </summary>
    /// <returns>
    /// <para>zh-cn:DynamicApp/Mod/Version/DeploymentId 格式的运行实例键。</para>
    /// <para>en-us:A runtime-instance key in DynamicApp/Mod/Version/DeploymentId format.</para>
    /// </returns>
    public override string ToString() => $"{PackageKey}/{DeploymentId}";

    /// <summary>
    /// <para>zh-cn:创建默认部署实例键，用于兼容未启用集群化的旧目标。</para>
    /// <para>en-us:Creates the default deployment key for legacy targets that do not use clustering.</para>
    /// </summary>
    /// <param name="packageKey">
    /// <para>zh-cn:包版本键。</para>
    /// <para>en-us:The package version key.</para>
    /// </param>
    /// <returns>
    /// <para>zh-cn:DeploymentId 为 default 的运行实例键。</para>
    /// <para>en-us:A runtime-instance key with the default deployment identifier.</para>
    /// </returns>
    public static DynamicAppRuntimeKey Default(DynamicModVersionKey packageKey) => new(packageKey, "default");
}
