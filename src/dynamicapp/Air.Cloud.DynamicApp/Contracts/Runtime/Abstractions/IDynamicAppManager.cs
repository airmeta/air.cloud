using Air.Cloud.DynamicApp.Contracts.Identifiers;
using Air.Cloud.DynamicApp.Contracts.Manifest;

namespace Air.Cloud.DynamicApp.Contracts.Runtime;

/// <summary>
/// <para>zh-cn:DynamicApp 管理控制面接口，负责发布 Manifest、激活版本和回滚活动版本。</para>
/// <para>en-us:DynamicApp management control-plane interface responsible for publishing manifests, activating versions, and rolling back active versions.</para>
/// </summary>
public interface IDynamicAppManager
{
    /// <summary>
    /// <para>zh-cn:发布 DynamicApp Manifest，并登记可启动的 Mod 目标；实现方可同步保存 Manifest 历史。</para>
    /// <para>en-us:Publishes a DynamicApp manifest and registers startable Mod targets; implementations may also persist manifest history.</para>
    /// </summary>
    /// <param name="manifest">
    /// <para>zh-cn:待发布的 Manifest。</para>
    /// <para>en-us:The manifest to publish.</para>
    /// </param>
    /// <param name="cancellationToken">
    /// <para>zh-cn:取消令牌。</para>
    /// <para>en-us:Cancellation token.</para>
    /// </param>
    /// <returns>
    /// <para>zh-cn:发布结果。</para>
    /// <para>en-us:The publish result.</para>
    /// </returns>
    Task<DynamicAppOperationResult> PublishAsync(DynamicAppManifest manifest, CancellationToken cancellationToken = default);

    /// <summary>
    /// <para>zh-cn:激活指定 Mod 版本，并将同一 DynamicApp/Mod 的其他版本转入排空。</para>
    /// <para>en-us:Activates the specified Mod version and moves other versions under the same DynamicApp/Mod into draining.</para>
    /// </summary>
    /// <param name="key">
    /// <para>zh-cn:目标版本键。</para>
    /// <para>en-us:Target version key.</para>
    /// </param>
    /// <param name="cancellationToken">
    /// <para>zh-cn:取消令牌。</para>
    /// <para>en-us:Cancellation token.</para>
    /// </param>
    /// <returns>
    /// <para>zh-cn:激活结果。</para>
    /// <para>en-us:The activation result.</para>
    /// </returns>
    Task<DynamicAppOperationResult> ActivateAsync(DynamicModVersionKey key, CancellationToken cancellationToken = default);

    /// <summary>
    /// <para>zh-cn:只激活指定部署实例；同包版本其他实例不会因本次激活被覆盖。</para>
    /// <para>en-us:Activates only the specified deployment; other deployments of the same package version are not overwritten by this activation.</para>
    /// </summary>
    /// <param name="key">
    /// <para>zh-cn:运行实例键。</para>
    /// <para>en-us:The runtime-instance key.</para>
    /// </param>
    /// <param name="cancellationToken">
    /// <para>zh-cn:取消令牌。</para>
    /// <para>en-us:The cancellation token.</para>
    /// </param>
    /// <returns>
    /// <para>zh-cn:实例激活结果。</para>
    /// <para>en-us:The instance activation result.</para>
    /// </returns>
    Task<DynamicAppOperationResult> ActivateInstanceAsync(DynamicAppRuntimeKey key, CancellationToken cancellationToken = default)
        => ActivateAsync(key.PackageKey, cancellationToken);

    /// <summary>
    /// <para>zh-cn:回滚指定 DynamicApp/Mod 到最近一个健康的非当前版本。</para>
    /// <para>en-us:Rolls the specified DynamicApp/Mod back to the latest healthy version that is not currently active.</para>
    /// </summary>
    /// <param name="dynamicAppId">
    /// <para>zh-cn:DynamicApp 标识。</para>
    /// <para>en-us:DynamicApp identifier.</para>
    /// </param>
    /// <param name="modId">
    /// <para>zh-cn:Mod 标识。</para>
    /// <para>en-us:Mod identifier.</para>
    /// </param>
    /// <param name="cancellationToken">
    /// <para>zh-cn:取消令牌。</para>
    /// <para>en-us:Cancellation token.</para>
    /// </param>
    /// <returns>
    /// <para>zh-cn:回滚结果。</para>
    /// <para>en-us:The rollback result.</para>
    /// </returns>
    Task<DynamicAppOperationResult> RollbackAsync(DynamicAppId dynamicAppId, DynamicModId modId, CancellationToken cancellationToken = default);
}
