using Air.Cloud.DynamicApp.Contracts.Runtime;

namespace Air.Cloud.DynamicApp.Manager;

/// <summary>
/// <para>zh-cn:DynamicApp Mod 包存储接口，负责保存上传压缩包并解析出可启动工件位置。</para>
/// <para>en-us:DynamicApp Mod package store interface responsible for saving uploaded archives and resolving startable artifact locations.</para>
/// </summary>
public interface IDynamicAppModPackageStore
{
    /// <summary>
    /// <para>zh-cn:保存并解压一个 Mod 压缩包；实现方必须校验路径边界、防止压缩包逃逸写入，并在入口文件缺省时推断入口 .dll。</para>
    /// <para>en-us:Saves and extracts a Mod archive; implementations must validate path boundaries, prevent archive extraction escape, and infer the entry .dll when the entry file is omitted.</para>
    /// </summary>
    /// <param name="request">
    /// <para>zh-cn:包保存请求，包含目标键、可选入口文件和覆盖策略。</para>
    /// <para>en-us:The package save request containing the target key, optional entry file, and overwrite policy.</para>
    /// </param>
    /// <param name="packageStream">
    /// <para>zh-cn:上传压缩包数据流，调用方负责打开和关闭该流。</para>
    /// <para>en-us:The uploaded archive stream; callers own stream opening and disposal.</para>
    /// </param>
    /// <param name="cancellationToken">
    /// <para>zh-cn:取消令牌。</para>
    /// <para>en-us:Cancellation token.</para>
    /// </param>
    /// <returns>
    /// <para>zh-cn:包保存记录，包含解压目录和入口文件绝对路径。</para>
    /// <para>en-us:The package save record containing the extraction directory and absolute entry file path.</para>
    /// </returns>
    Task<DynamicAppModPackageRecord> SaveAsync(DynamicAppModPackageSaveRequest request, Stream packageStream, CancellationToken cancellationToken = default);

    /// <summary>
    /// <para>zh-cn:从不可变包创建独立部署目录；部署目录不会与包库存目录共享可变配置或运行文件。</para>
    /// <para>en-us:Creates an isolated deployment directory from an immutable package; the deployment directory shares no mutable configuration or runtime files with the package inventory directory.</para>
    /// </summary>
    /// <param name="key">
    /// <para>zh-cn:源包版本键。</para>
    /// <para>en-us:The source package version key.</para>
    /// </param>
    /// <param name="deploymentId">
    /// <para>zh-cn:新的独立部署标识。</para>
    /// <para>en-us:The new isolated deployment identifier.</para>
    /// </param>
    /// <param name="cancellationToken">
    /// <para>zh-cn:取消令牌。</para>
    /// <para>en-us:Cancellation token.</para>
    /// </param>
    /// <returns>
    /// <para>zh-cn:独立部署目录记录。</para>
    /// <para>en-us:The isolated deployment-directory record.</para>
    /// </returns>
    /// <param name="entryFile">
    /// <para>zh-cn:Optional source-package entry file; empty values use DLL inference.</para>
    /// <para>en-us:Optional source-package entry file; empty values use DLL inference.</para>
    /// </param>
    Task<DynamicAppModPackageDeploymentRecord> CreateDeploymentAsync(
        DynamicModVersionKey key,
        string deploymentId,
        CancellationToken cancellationToken = default,
        string? entryFile = null);

    /// <summary>
    /// <para>zh-cn:列出当前包存储根目录下已经保存的所有 Mod 包版本，供控制面板包管理器查验和上传后刷新使用。</para>
    /// <para>en-us:Lists all Mod package versions currently saved under the package-store root for package-manager inspection and post-upload refresh.</para>
    /// </summary>
    /// <returns>
    /// <para>zh-cn:按 DynamicApp 和 Mod 聚合后的包清单；包根目录不存在时返回空集合。</para>
    /// <para>en-us:Package inventory grouped by DynamicApp and Mod; an empty collection is returned when the package root does not exist.</para>
    /// </returns>
    IReadOnlyCollection<DynamicAppModPackageModView> ListPackages();

    /// <summary>
    /// <para>zh-cn:删除一个未被运行实例或归档占用的 Mod 包版本目录。</para>
    /// <para>en-us:Deletes one Mod package-version directory that is not referenced by a runtime instance or archive.</para>
    /// </summary>
    bool RemovePackageVersion(DynamicModVersionKey key);

    /// <summary>
    /// <para>zh-cn:创建一个包管理应用目录，使操作员可以先选择应用再继续上传该应用下的 Mod 包。</para>
    /// <para>en-us:Creates a package-management application directory so operators can select the application before uploading Mod packages under it.</para>
    /// </summary>
    /// <param name="dynamicAppId">
    /// <para>zh-cn:DynamicApp 应用标识；实现必须把它当作路径片段验证，避免写出包存储根目录。</para>
    /// <para>en-us:DynamicApp application identifier; implementations must validate it as a path segment to prevent writes outside the package-store root.</para>
    /// </param>
    /// <returns>
    /// <para>zh-cn:创建或已存在的应用视图。</para>
    /// <para>en-us:The created or already existing application view.</para>
    /// </returns>
    DynamicAppPackageApplicationView CreateApplication(string dynamicAppId);

    /// <summary>
    /// <para>zh-cn:在指定 DynamicApp 应用下创建 Mod 配置目录；该操作只创建包空间，不发布 Manifest、不创建运行实例也不启动服务。</para>
    /// <para>en-us:Creates a Mod configuration directory under the specified DynamicApp application; this only creates package space and does not publish a manifest, create a runtime instance, or start a service.</para>
    /// </summary>
    /// <param name="dynamicAppId">
    /// <para>zh-cn:DynamicApp 应用标识，必须是安全的单级路径片段。</para>
    /// <para>en-us:DynamicApp application identifier, which must be one safe path segment.</para>
    /// </param>
    /// <param name="modId">
    /// <para>zh-cn:Mod 标识，必须是安全的单级路径片段。</para>
    /// <para>en-us:Mod identifier, which must be one safe path segment.</para>
    /// </param>
    /// <returns>
    /// <para>zh-cn:创建后的包管理应用视图。</para>
    /// <para>en-us:The package-management application view after creation.</para>
    /// </returns>
    DynamicAppPackageApplicationView CreateMod(string dynamicAppId, string modId);

    /// <summary>
    /// <para>zh-cn:删除一个没有包版本的 Mod 服务目录；实现方必须拒绝仍有包、运行目录或归档文件的服务。</para>
    /// <para>en-us:Deletes a Mod service directory only when it has no package versions, runtime directories, or archived files.</para>
    /// </summary>
    bool RemoveMod(string dynamicAppId, string modId);

    /// <summary>
    /// <para>zh-cn:列出包存储中已创建的应用目录，包含暂未上传 Mod 包的空应用。</para>
    /// <para>en-us:Lists application directories already created in the package store, including empty applications without uploaded Mod packages.</para>
    /// </summary>
    /// <returns>
    /// <para>zh-cn:包存储应用集合；目录不存在时返回空集合。</para>
    /// <para>en-us:Package-store application collection; an empty collection is returned when the directory does not exist.</para>
    /// </returns>
    IReadOnlyCollection<DynamicAppPackageApplicationView> ListApplications();

    /// <summary>
    /// <para>zh-cn:扫描所有已创建的隔离部署目录，供管理面板识别孤儿目录和清理范围。</para>
    /// <para>en-us:Scans all isolated deployment directories so the control plane can identify orphaned directories and cleanup scope.</para>
    /// </summary>
    /// <returns>
    /// <para>zh-cn:磁盘上的部署目录记录。</para>
    /// <para>en-us:Deployment-directory records found on disk.</para>
    /// </returns>
    IReadOnlyCollection<DynamicAppModPackageDeploymentDirectoryRecord> ListDeploymentDirectories();

    /// <summary>
    /// <para>zh-cn:列出指定运行实例副本中的文件；结果只来自 deployments 运行空间，不回退到 mods 包空间。</para>
    /// <para>en-us:Lists files from one isolated runtime deployment only; the result never falls back to the mods package inventory.</para>
    /// </summary>
    /// <param name="key">
    /// <para>zh-cn:运行实例键。</para>
    /// <para>en-us:The runtime-instance key.</para>
    /// </param>
    /// <returns>
    /// <para>zh-cn:运行副本文件清单；副本不存在时返回空集合。</para>
    /// <para>en-us:The deployment file inventory; an empty collection is returned when the isolated copy does not exist.</para>
    /// </returns>
    IReadOnlyCollection<DynamicAppModPackageFileView> ListDeploymentFiles(DynamicAppRuntimeKey key);

    /// <summary>
    /// <para>zh-cn:列出运行状态指定目录中的文件，供默认挂载实例浏览其实际运行目录；调用方必须传入状态存储登记的绝对目录。</para>
    /// <para>en-us:Lists files from the directory registered by runtime state so default mounted instances can expose their actual runtime files; callers must provide the registered absolute directory.</para>
    /// </summary>
    /// <param name="key">
    /// <para>zh-cn:运行实例键。</para>
    /// <para>en-us:The runtime-instance key.</para>
    /// </param>
    /// <param name="runtimeDirectory">
    /// <para>zh-cn:状态存储登记的运行目录绝对路径。</para>
    /// <para>en-us:The absolute runtime directory registered by runtime state.</para>
    /// </param>
    /// <returns>
    /// <para>zh-cn:运行目录下的文件清单。</para>
    /// <para>en-us:The file inventory under the runtime directory.</para>
    /// </returns>
    IReadOnlyCollection<DynamicAppModPackageFileView> ListDeploymentFiles(DynamicAppRuntimeKey key, string runtimeDirectory)
        => ListDeploymentFiles(key);

    /// <summary>
    /// <para>zh-cn:删除指定部署实例的隔离目录，不修改运行状态或自愈状态。</para>
    /// <para>en-us:Deletes one isolated deployment directory without changing runtime or self-healing state.</para>
    /// </summary>
    /// <param name="key">
    /// <para>zh-cn:要删除的运行实例键。</para>
    /// <para>en-us:The runtime-instance key to delete.</para>
    /// </param>
    /// <param name="cancellationToken">
    /// <para>zh-cn:删除过程中的取消令牌。</para>
    /// <para>en-us:The cancellation token used during deletion.</para>
    /// </param>
    /// <returns>
    /// <para>zh-cn:目录存在并成功删除时返回 true；目录不存在时返回 false。</para>
    /// <para>en-us:True when the directory existed and was deleted; false when it did not exist.</para>
    /// </returns>
    Task<bool> RemoveDeploymentAsync(DynamicAppRuntimeKey key, CancellationToken cancellationToken = default);

    /// <summary>
    /// <para>zh-cn:把停止实例的运行目录移动到归档空间。</para>
    /// <para>en-us:Moves a stopped instance's runtime directory into archive space.</para>
    /// </summary>
    Task<DynamicAppModPackageDeploymentArchiveRecord> ArchiveDeploymentAsync(
        DynamicAppRuntimeKey key,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// <para>zh-cn:把实例归档目录恢复到运行空间。</para>
    /// <para>en-us:Restores an archived instance directory into runtime space.</para>
    /// </summary>
    Task<DynamicAppModPackageDeploymentArchiveRecord> RestoreArchivedDeploymentAsync(
        DynamicAppRuntimeKey key,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// <para>zh-cn:删除实例归档目录。</para>
    /// <para>en-us:Deletes an archived instance directory.</para>
    /// </summary>
    Task<bool> RemoveArchivedDeploymentAsync(
        DynamicAppRuntimeKey key,
        CancellationToken cancellationToken = default);
}
