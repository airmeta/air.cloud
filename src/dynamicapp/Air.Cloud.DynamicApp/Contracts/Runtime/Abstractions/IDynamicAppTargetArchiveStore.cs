namespace Air.Cloud.DynamicApp.Contracts.Runtime;

/// <summary>
/// <para>zh-cn:DynamicApp 目标归档存储接口，用于把坏包或错�?Manifest 对应的版本从默认管理视图和启动链路中隔离。</para>
/// <para>en-us:DynamicApp target archive store interface used to isolate bad-package or invalid-manifest versions from default management views and startup paths.</para>
/// </summary>
public interface IDynamicAppTargetArchiveStore
{
    /// <summary>
    /// <para>zh-cn:归档指定目标版本，并记录治理原因。</para>
    /// <para>en-us:Archives the specified target version and records the governance reason.</para>
    /// </summary>
    /// <param name="key">
    /// <para>zh-cn:需要归档的 Mod 版本键。</para>
    /// <para>en-us:The Mod version key to archive.</para>
    /// </param>
    /// <param name="reason">
    /// <para>zh-cn:归档原因；为空时由实现写入默认原因。</para>
    /// <para>en-us:The archive reason; implementations write a default reason when it is empty.</para>
    /// </param>
    /// <returns>
    /// <para>zh-cn:归档后的记录副本。</para>
    /// <para>en-us:A copy of the archived record.</para>
    /// </returns>
    DynamicAppArchivedTarget Archive(DynamicModVersionKey key, string? reason);

    /// <summary>
    /// <para>zh-cn:取消归档指定目标版本，使其重新出现在默认管理视图并允许再次启动。</para>
    /// <para>en-us:Unarchives the specified target version so it can reappear in default management views and be started again.</para>
    /// </summary>
    /// <param name="key">
    /// <para>zh-cn:需要取消归档的 Mod 版本键。</para>
    /// <para>en-us:The Mod version key to unarchive.</para>
    /// </param>
    /// <returns>
    /// <para>zh-cn:是否实际取消了归档。</para>
    /// <para>en-us:Whether an archive entry was actually removed.</para>
    /// </returns>
    bool Unarchive(DynamicModVersionKey key);

    /// <summary>
    /// <para>zh-cn:判断指定目标版本是否已归档。</para>
    /// <para>en-us:Determines whether the specified target version is archived.</para>
    /// </summary>
    /// <param name="key">
    /// <para>zh-cn:目标 Mod 版本键。</para>
    /// <para>en-us:The target Mod version key.</para>
    /// </param>
    /// <returns>
    /// <para>zh-cn:目标版本是否已归档。</para>
    /// <para>en-us:Whether the target version is archived.</para>
    /// </returns>
    bool IsArchived(DynamicModVersionKey key);

    /// <summary>
    /// <para>zh-cn:列出全部归档记录副本。</para>
    /// <para>en-us:Lists copies of all archived target records.</para>
    /// </summary>
    /// <returns>
    /// <para>zh-cn:归档记录集合。</para>
    /// <para>en-us:The archived target record collection.</para>
    /// </returns>
    IReadOnlyCollection<DynamicAppArchivedTarget> List();
}
