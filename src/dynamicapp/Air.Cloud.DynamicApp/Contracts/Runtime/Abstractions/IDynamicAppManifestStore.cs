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

    /// <summary>
    /// <para>zh-cn:从 Manifest 历史中删除指定服务，并保留同一应用下的其他服务记录。</para>
    /// <para>en-us:Removes a service from manifest history while preserving other services under the same application.</para>
    /// </summary>
    /// <param name="dynamicAppId">
    /// <para>zh-cn:DynamicApp 应用标识。</para>
    /// <para>en-us:The DynamicApp application identifier.</para>
    /// </param>
    /// <param name="modId">
    /// <para>zh-cn:服务 Mod 标识。</para>
    /// <para>en-us:The service Mod identifier.</para>
    /// </param>
    /// <param name="cancellationToken">
    /// <para>zh-cn:取消令牌。</para>
    /// <para>en-us:Cancellation token.</para>
    /// </param>
    /// <returns>
    /// <para>zh-cn:删除的 Manifest 记录数量。</para>
    /// <para>en-us:The number of manifest records changed or removed.</para>
    /// </returns>
    Task<int> RemoveModAsync(DynamicAppId dynamicAppId, DynamicModId modId, CancellationToken cancellationToken = default)
        => Task.FromResult(0);

    /// <summary>
    /// <para>zh-cn:从 Manifest 历史中删除指定 Mod 版本，避免删除包后仍以 Manifest-only 形式重新出现在包管理器中。</para>
    /// <para>en-us:Removes one Mod version from manifest history so a deleted package does not reappear as a manifest-only inventory entry.</para>
    /// </summary>
    Task<int> RemoveVersionAsync(DynamicModVersionKey key, CancellationToken cancellationToken = default)
        => Task.FromResult(0);

    /// <summary>
    /// <para>zh-cn:删除一次精确的 Manifest 发布记录，用于发布事务在取消或异常时回滚刚写入的快照。</para>
    /// <para>en-us:Removes one exact manifest publish record so a publish transaction can roll back the snapshot it just wrote after cancellation or failure.</para>
    /// </summary>
    /// <param name="recordId">
    /// <para>zh-cn:要删除的 Manifest 历史记录标识。</para>
    /// <para>en-us:The manifest history record identifier to remove.</para>
    /// </param>
    /// <param name="cancellationToken">
    /// <para>zh-cn:取消令牌。</para>
    /// <para>en-us:The cancellation token.</para>
    /// </param>
    /// <returns>
    /// <para>zh-cn:是否删除了记录。</para>
    /// <para>en-us:Whether a record was removed.</para>
    /// </returns>
    Task<bool> RemoveRecordAsync(string recordId, CancellationToken cancellationToken = default)
        => Task.FromResult(false);
}
