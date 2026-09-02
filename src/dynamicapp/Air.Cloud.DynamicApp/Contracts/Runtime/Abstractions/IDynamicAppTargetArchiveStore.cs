using Air.Cloud.DynamicApp.Contracts.Identifiers;

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
    /// <para>zh-cn:保存一个运行实例销毁归档，并保留实例配置、自愈状态和归档目录元数据。</para>
    /// <para>en-us:Stores a destroyed runtime-instance archive with its configuration, self-healing snapshot, and archived-directory metadata.</para>
    /// </summary>
    /// <param name="key">
    /// <para>zh-cn:实例所属的包版本键。</para>
    /// <para>en-us:The package-version key owned by the instance.</para>
    /// </param>
    /// <param name="deploymentId">
    /// <para>zh-cn:被销毁的实例标识。</para>
    /// <para>en-us:The destroyed deployment identifier.</para>
    /// </param>
    /// <param name="target">
    /// <para>zh-cn:实例运行配置和状态快照。</para>
    /// <para>en-us:The runtime configuration and state snapshot.</para>
    /// </param>
    /// <param name="selfHealing">
    /// <para>zh-cn:实例自愈状态快照，可为空。</para>
    /// <para>en-us:The optional self-healing state snapshot.</para>
    /// </param>
    /// <param name="archiveDirectory">
    /// <para>zh-cn:运行文件归档目录，可为空。</para>
    /// <para>en-us:The archived runtime-file directory, which may be empty.</para>
    /// </param>
    /// <param name="reason">
    /// <para>zh-cn:归档原因。</para>
    /// <para>en-us:The archive reason.</para>
    /// </param>
    /// <returns>
    /// <para>zh-cn:实例归档记录副本。</para>
    /// <para>en-us:A copy of the runtime-instance archive record.</para>
    /// </returns>
    DynamicAppArchivedTarget ArchiveDeployment(
        DynamicModVersionKey key,
        string deploymentId,
        DynamicAppRouteTarget target,
        DynamicAppSelfHealingState? selfHealing,
        string? archiveDirectory,
        string? reason);

    /// <summary>
    /// <para>zh-cn:查找指定运行实例的归档记录。</para>
    /// <para>en-us:Finds the archive record for the specified runtime instance.</para>
    /// </summary>
    DynamicAppArchivedTarget? FindDeployment(DynamicAppRuntimeKey key);

    /// <summary>
    /// <para>zh-cn:列出实例级归档记录。</para>
    /// <para>en-us:Lists runtime-instance archive records.</para>
    /// </summary>
    IReadOnlyCollection<DynamicAppArchivedTarget> ListDeploymentArchives();

    /// <summary>
    /// <para>zh-cn:删除指定实例的归档记录。</para>
    /// <para>en-us:Deletes the archive record for the specified runtime instance.</para>
    /// </summary>
    bool RemoveDeployment(DynamicAppRuntimeKey key);

    /// <summary>
    /// <para>zh-cn:判断指定服务是否存在任何归档记录。</para>
    /// <para>en-us:Determines whether any archive record exists for the specified service.</para>
    /// </summary>
    bool HasArchives(DynamicAppId dynamicAppId, DynamicModId modId);

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
    /// <para>zh-cn:按运行实例判断目标是否被版本级或实例级归档；实例级归档只影响指定 DeploymentId。</para>
    /// <para>en-us:Determines whether a runtime target is blocked by a version-level or instance-level archive; an instance archive affects only its specified DeploymentId.</para>
    /// </summary>
    /// <param name="key">
    /// <para>zh-cn:运行实例键。</para>
    /// <para>en-us:The runtime-instance key.</para>
    /// </param>
    /// <returns>
    /// <para>zh-cn:目标是否处于归档阻断状态。</para>
    /// <para>en-us:Whether the target is blocked by an archive.</para>
    /// </returns>
    bool IsArchived(DynamicAppRuntimeKey key);

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
