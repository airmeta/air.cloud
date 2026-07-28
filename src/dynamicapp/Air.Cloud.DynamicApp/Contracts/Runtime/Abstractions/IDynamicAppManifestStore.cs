using Air.Cloud.DynamicApp.Contracts.Identifiers;
using Air.Cloud.DynamicApp.Contracts.Manifest;

namespace Air.Cloud.DynamicApp.Contracts.Runtime;

/// <summary>
/// <para>zh-cn:DynamicApp Manifest 历史存储接口，用于审计、回滚和人工查验发布记录。</para>
/// <para>en-us:DynamicApp manifest history store used for audit, rollback, and operator inspection of publish records.</para>
/// </summary>
public interface IDynamicAppManifestStore
{
    /// <summary>
    /// <para>zh-cn:保存一次 Manifest 发布快照。</para>
    /// <para>en-us:Saves a manifest publish snapshot.</para>
    /// </summary>
    /// <param name="manifest">
    /// <para>zh-cn:要保存的 Manifest。</para>
    /// <para>en-us:Manifest to save.</para>
    /// </param>
    /// <param name="cancellationToken">
    /// <para>zh-cn:取消令牌。</para>
    /// <para>en-us:Cancellation token.</para>
    /// </param>
    /// <returns>
    /// <para>zh-cn:保存后的历史记录。</para>
    /// <para>en-us:The saved history record.</para>
    /// </returns>
    Task<DynamicAppManifestRecord> SaveAsync(DynamicAppManifest manifest, CancellationToken cancellationToken = default);

    /// <summary>
    /// <para>zh-cn:按保存时间倒序列出全部 DynamicApp Manifest 历史记录；控制面板可用该方法构建应用和 Mod 总览。</para>
    /// <para>en-us:Lists all DynamicApp manifest history records in descending save order; control panels may use this method to build application and Mod overviews.</para>
    /// </summary>
    /// <param name="cancellationToken">
    /// <para>zh-cn:取消令牌。</para>
    /// <para>en-us:Cancellation token.</para>
    /// </param>
    /// <returns>
    /// <para>zh-cn:全部 Manifest 历史记录集合。</para>
    /// <para>en-us:All manifest history records.</para>
    /// </returns>
    Task<IReadOnlyCollection<DynamicAppManifestRecord>> ListAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// <para>zh-cn:按 DynamicApp 标识倒序列出 Manifest 历史。</para>
    /// <para>en-us:Lists manifest history for a DynamicApp in descending save order.</para>
    /// </summary>
    /// <param name="dynamicAppId">
    /// <para>zh-cn:DynamicApp 标识。</para>
    /// <para>en-us:DynamicApp identifier.</para>
    /// </param>
    /// <param name="cancellationToken">
    /// <para>zh-cn:取消令牌。</para>
    /// <para>en-us:Cancellation token.</para>
    /// </param>
    /// <returns>
    /// <para>zh-cn:Manifest 历史记录集合。</para>
    /// <para>en-us:Manifest history records.</para>
    /// </returns>
    Task<IReadOnlyCollection<DynamicAppManifestRecord>> ListAsync(DynamicAppId dynamicAppId, CancellationToken cancellationToken = default);

    /// <summary>
    /// <para>zh-cn:按 DynamicApp/Mod/Version 精确查找最近一次包含目标 Mod 版本的 Manifest 历史记录；未找到时返回 null。</para>
    /// <para>en-us:Finds the latest manifest history record containing the specified DynamicApp/Mod/Version target; returns null when no record exists.</para>
    /// </summary>
    /// <param name="key">
    /// <para>zh-cn:目标 Mod 版本键。</para>
    /// <para>en-us:The target Mod version key.</para>
    /// </param>
    /// <param name="cancellationToken">
    /// <para>zh-cn:取消令牌。</para>
    /// <para>en-us:Cancellation token.</para>
    /// </param>
    /// <returns>
    /// <para>zh-cn:匹配的最近 Manifest 历史记录；不存在时为 null。</para>
    /// <para>en-us:The latest matching manifest history record; null when absent.</para>
    /// </returns>
    Task<DynamicAppManifestRecord?> FindAsync(DynamicModVersionKey key, CancellationToken cancellationToken = default);
}
