using System.IO.Compression;

using Air.Cloud.DynamicApp.Contracts.Identifiers;
using Air.Cloud.DynamicApp.Contracts.Runtime;

namespace Air.Cloud.DynamicApp.Manager;

/// <summary>
/// <para>zh-cn:文件型 DynamicApp Mod 包存储，将上传 zip 保存到受控目录并安全解压。</para>
/// <para>en-us:File-based DynamicApp Mod package store that saves uploaded zip archives into a controlled directory and extracts them safely.</para>
/// </summary>
public sealed class FileDynamicAppModPackageStore : IDynamicAppModPackageStore
{
    private const string PackageFileName = "package.zip";
    private readonly DynamicAppPackageStoreOptions _options;
    private readonly string _rootPath;

    /// <summary>
    /// <para>zh-cn:创建文件型 Mod 包存储；构造阶段只规范化根路径，不写入文件。</para>
    /// <para>en-us:Creates the file-based Mod package store; construction only normalizes the root path and does not write files.</para>
    /// </summary>
    /// <param name="options">
    /// <para>zh-cn:包存储选项，为 null 时使用默认根目录和大小限制。</para>
    /// <para>en-us:Package storage options; default root path and size limits are used when null.</para>
    /// </param>
    public FileDynamicAppModPackageStore(DynamicAppPackageStoreOptions? options = null)
    {
        _options = options ?? new DynamicAppPackageStoreOptions();
        _rootPath = EnsureTrailingSeparator(ResolveRootPath(_options.RootPath));
    }

    /// <summary>
    /// <para>zh-cn:保存并解压 zip 包，校验压缩包大小、目标目录、每个解压项路径和入口文件存在性；入口文件为空时从解压目录推断第一个 .dll。</para>
    /// <para>en-us:Saves and extracts a zip package while validating package size, target directory, each extracted entry path, and entry-file existence; when the entry file is empty, the first .dll is inferred from the extracted directory.</para>
    /// </summary>
    /// <param name="request">
    /// <para>zh-cn:包保存请求，包含目标键、压缩包文件名、可选入口文件和覆盖策略。</para>
    /// <para>en-us:The package save request containing target key, archive file name, optional entry file, and overwrite policy.</para>
    /// </param>
    /// <param name="packageStream">
    /// <para>zh-cn:上传 zip 数据流。</para>
    /// <para>en-us:The uploaded zip data stream.</para>
    /// </param>
    /// <param name="cancellationToken">
    /// <para>zh-cn:取消令牌。</para>
    /// <para>en-us:Cancellation token.</para>
    /// </param>
    /// <returns>
    /// <para>zh-cn:包保存记录。</para>
    /// <para>en-us:The package save record.</para>
    /// </returns>
    public async Task<DynamicAppModPackageRecord> SaveAsync(DynamicAppModPackageSaveRequest request, Stream packageStream, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(packageStream);
        ValidateRequest(request);

        var packageDirectory = BuildPackageDirectory(request);
        var existed = Directory.Exists(packageDirectory);
        if (existed && (!_options.AllowOverwrite || !request.Overwrite))
        {
            throw new InvalidOperationException($"DynamicApp package target `{request.Key}` already exists and overwrite is not allowed.");
        }

        EnsureInsideRoot(packageDirectory);
        var parentDirectory = Path.GetDirectoryName(packageDirectory)
            ?? throw new InvalidOperationException($"Package directory `{packageDirectory}` has no parent directory.");
        Directory.CreateDirectory(parentDirectory);

        var stagingDirectory = packageDirectory + ".staging-" + Guid.NewGuid().ToString("N");
        var backupDirectory = packageDirectory + ".backup-" + Guid.NewGuid().ToString("N");
        EnsureInsideRoot(stagingDirectory);
        EnsureInsideRoot(backupDirectory);

        try
        {
            Directory.CreateDirectory(stagingDirectory);
            var stagingArchivePath = Path.Combine(stagingDirectory, PackageFileName);
            var bytes = await CopyPackageAsync(packageStream, stagingArchivePath, cancellationToken).ConfigureAwait(false);
            var stagingExtractedDirectory = Path.Combine(stagingDirectory, "content");
            Directory.CreateDirectory(stagingExtractedDirectory);
            ExtractZipSafely(stagingArchivePath, stagingExtractedDirectory);

            var entryFile = string.IsNullOrWhiteSpace(request.EntryFile)
                ? InferEntryFile(stagingExtractedDirectory)
                : request.EntryFile;
            var stagingEntryFilePath = ResolveEntryFilePath(stagingExtractedDirectory, entryFile);

            if (existed)
            {
                Directory.Move(packageDirectory, backupDirectory);
            }

            try
            {
                Directory.Move(stagingDirectory, packageDirectory);
            }
            catch
            {
                if (existed && Directory.Exists(backupDirectory) && !Directory.Exists(packageDirectory))
                {
                    Directory.Move(backupDirectory, packageDirectory);
                }

                throw;
            }

            if (Directory.Exists(backupDirectory))
            {
                Directory.Delete(backupDirectory, recursive: true);
            }

            var archivePath = Path.Combine(packageDirectory, PackageFileName);
            var extractedDirectory = Path.Combine(packageDirectory, "content");
            var entryFilePath = Path.Combine(extractedDirectory, Path.GetRelativePath(stagingExtractedDirectory, stagingEntryFilePath));
            return new DynamicAppModPackageRecord
            {
                Key = request.Key,
                OriginalFileName = Path.GetFileName(request.OriginalFileName),
                PackageFilePath = archivePath,
                ExtractedDirectory = extractedDirectory,
                EntryFilePath = entryFilePath,
                PackageBytes = bytes,
                SavedAtUtc = DateTimeOffset.UtcNow,
                Overwritten = existed
            };
        }
        finally
        {
            if (Directory.Exists(stagingDirectory))
            {
                Directory.Delete(stagingDirectory, recursive: true);
            }

            if (Directory.Exists(backupDirectory))
            {
                Directory.Delete(backupDirectory, recursive: true);
            }
        }
    }

    /// <summary>
    /// <para>zh-cn:从包库存创建独立部署目录，并复制完整内容与压缩包快照；后续运行时不会读取包库存中的可变文件。</para>
    /// <para>en-us:Creates an isolated deployment directory and copies the complete content and archive snapshot; the runtime will not read mutable files from the package inventory afterward.</para>
    /// </summary>
    /// <param name="key">
    /// <para>zh-cn:源包版本键。</para>
    /// <para>en-us:The source package version key.</para>
    /// </param>
    /// <param name="deploymentId">
    /// <para>zh-cn:独立部署标识。</para>
    /// <para>en-us:The isolated deployment identifier.</para>
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
    /// <para>zh-cn:可选的源包入口文件；为空时从部署副本中推导 DLL。</para>
    /// <para>en-us:Optional source-package entry file; when empty, a DLL is inferred from the deployment copy.</para>
    /// </param>
    public Task<DynamicAppModPackageDeploymentRecord> CreateDeploymentAsync(
        DynamicModVersionKey key,
        string deploymentId,
        CancellationToken cancellationToken = default,
        string? entryFile = null)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ValidatePathSegment(deploymentId, nameof(deploymentId));

        var sourceDirectory = BuildPackageDirectory(new DynamicAppModPackageSaveRequest
        {
            Key = key,
            OriginalFileName = "package.zip"
        });
        var sourceArchive = Path.Combine(sourceDirectory, PackageFileName);
        var sourceContent = Path.Combine(sourceDirectory, "content");
        if (!File.Exists(sourceArchive) || !Directory.Exists(sourceContent))
        {
            throw new FileNotFoundException($"DynamicApp package `{key}` was not found.", sourceDirectory);
        }

        var deploymentDirectory = Path.GetFullPath(Path.Combine(
            _rootPath,
            "deployments",
            key.DynamicAppId.Value,
            key.ModId.Value,
            key.Version.Value,
            deploymentId));
        EnsureInsideRoot(deploymentDirectory);
        if (Directory.Exists(deploymentDirectory))
        {
            throw new InvalidOperationException($"DynamicApp deployment `{key}/{deploymentId}` already exists.");
        }

        try
        {
            Directory.CreateDirectory(deploymentDirectory);
            File.Copy(sourceArchive, Path.Combine(deploymentDirectory, PackageFileName));
            CopyDirectory(sourceContent, Path.Combine(deploymentDirectory, "content"), cancellationToken);
            var deploymentContent = Path.Combine(deploymentDirectory, "content");
            var relativeEntryFile = ResolveRelativeEntryFile(sourceContent, entryFile);
            var resolvedEntryFile = string.IsNullOrWhiteSpace(relativeEntryFile)
                ? InferEntryFile(deploymentContent)
                : relativeEntryFile;
            return Task.FromResult(new DynamicAppModPackageDeploymentRecord
            {
                Key = key,
                DeploymentId = deploymentId,
                DeploymentDirectory = deploymentDirectory,
                EntryFilePath = ResolveEntryFilePath(deploymentContent, resolvedEntryFile),
                CreatedAtUtc = DateTimeOffset.UtcNow
            });
        }
        catch
        {
            if (Directory.Exists(deploymentDirectory))
            {
                Directory.Delete(deploymentDirectory, recursive: true);
            }

            throw;
        }
    }

    /// <summary>
    /// <para>zh-cn:扫描包存储根目录下的 mods/{dynamicAppId}/{modId}/{version} 结构，并生成包管理器展示所需的只读清单。</para>
    /// <para>en-us:Scans the mods/{dynamicAppId}/{modId}/{version} structure under the package-store root and builds the read-only inventory required by the package manager.</para>
    /// </summary>
    /// <returns>
    /// <para>zh-cn:按应用和 Mod 排序后的包清单；目录不存在或没有包文件时返回空集合。</para>
    /// <para>en-us:Package inventory sorted by application and Mod; an empty collection is returned when directories or package files do not exist.</para>
    /// </returns>
    public IReadOnlyCollection<DynamicAppModPackageModView> ListPackages()
    {
        var modsRoot = Path.GetFullPath(Path.Combine(_rootPath, "mods"));
        EnsureInsideRoot(modsRoot);
        if (!Directory.Exists(modsRoot))
        {
            return Array.Empty<DynamicAppModPackageModView>();
        }

        var result = new List<DynamicAppModPackageModView>();
        foreach (var appDirectory in OrderedDirectories(modsRoot))
        {
            foreach (var modDirectory in OrderedDirectories(appDirectory.FullName))
            {
                var versions = OrderedDirectories(modDirectory.FullName)
                    .Select(versionDirectory => BuildVersionView(appDirectory.Name, modDirectory.Name, versionDirectory))
                    .Where(version => version.Files.Count > 0)
                    .OrderBy(version => version.Version, StringComparer.OrdinalIgnoreCase)
                    .ToList();

                result.Add(new DynamicAppModPackageModView
                {
                    DynamicAppId = appDirectory.Name,
                    ModId = modDirectory.Name,
                    Versions = versions
                });
            }
        }

        return result
            .OrderBy(item => item.DynamicAppId, StringComparer.OrdinalIgnoreCase)
            .ThenBy(item => item.ModId, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    /// <summary>
    /// <para>zh-cn:删除指定 Mod 包版本目录，并在目录为空时清理上层 Mod 和应用目录。</para>
    /// <para>en-us:Deletes the specified Mod package-version directory and removes empty parent Mod and application directories.</para>
    /// </summary>
    public bool RemovePackageVersion(DynamicModVersionKey key)
    {
        var versionDirectory = Path.GetFullPath(Path.Combine(_rootPath, "mods", key.DynamicAppId.Value, key.ModId.Value, key.Version.Value));
        EnsureInsideRoot(versionDirectory);
        if (!Directory.Exists(versionDirectory))
        {
            return false;
        }

        Directory.Delete(versionDirectory, recursive: true);
        var modDirectory = Path.GetDirectoryName(versionDirectory);
        if (!string.IsNullOrWhiteSpace(modDirectory) && Directory.Exists(modDirectory) && !Directory.EnumerateFileSystemEntries(modDirectory).Any())
        {
            Directory.Delete(modDirectory);
        }

        var appDirectory = !string.IsNullOrWhiteSpace(modDirectory) ? Path.GetDirectoryName(modDirectory) : null;
        if (!string.IsNullOrWhiteSpace(appDirectory) && Directory.Exists(appDirectory) && !Directory.EnumerateFileSystemEntries(appDirectory).Any())
        {
            Directory.Delete(appDirectory);
        }

        return true;
    }

    /// <summary>
    /// <para>zh-cn:创建 DynamicApp 应用目录；该操作只创建包管理命名空间，不发布 Manifest，也不启动任何 Mod。</para>
    /// <para>en-us:Creates a DynamicApp application directory; this only creates the package-management namespace and does not publish manifests or start Mods.</para>
    /// </summary>
    /// <param name="dynamicAppId">
    /// <para>zh-cn:DynamicApp 应用标识，必须是单个安全路径片段。</para>
    /// <para>en-us:DynamicApp application identifier, which must be one safe path segment.</para>
    /// </param>
    /// <returns>
    /// <para>zh-cn:创建或已存在的应用视图。</para>
    /// <para>en-us:The created or already existing application view.</para>
    /// </returns>
    public DynamicAppPackageApplicationView CreateApplication(string dynamicAppId)
    {
        ValidatePathSegment(dynamicAppId, nameof(dynamicAppId));
        var directory = Path.GetFullPath(Path.Combine(_rootPath, "mods", dynamicAppId));
        EnsureInsideRoot(directory);
        Directory.CreateDirectory(directory);
        return BuildApplicationView(new DirectoryInfo(directory));
    }

    /// <summary>
    /// <para>zh-cn:创建应用下的 Mod 配置目录；目录存在时保持幂等，不写入版本包、Manifest 或运行时状态。</para>
    /// <para>en-us:Creates an application Mod configuration directory idempotently without writing a package version, manifest, or runtime state.</para>
    /// </summary>
    /// <param name="dynamicAppId">
    /// <para>zh-cn:DynamicApp 应用标识。</para>
    /// <para>en-us:DynamicApp application identifier.</para>
    /// </param>
    /// <param name="modId">
    /// <para>zh-cn:Mod 标识。</para>
    /// <para>en-us:Mod identifier.</para>
    /// </param>
    /// <returns>
    /// <para>zh-cn:包含已创建 Mod 数量的包管理应用视图。</para>
    /// <para>en-us:The package-management application view containing the created Mod count.</para>
    /// </returns>
    public DynamicAppPackageApplicationView CreateMod(string dynamicAppId, string modId)
    {
        ValidatePathSegment(dynamicAppId, nameof(dynamicAppId));
        ValidatePathSegment(modId, nameof(modId));
        var directory = Path.GetFullPath(Path.Combine(_rootPath, "mods", dynamicAppId, modId));
        EnsureInsideRoot(directory);
        if (Directory.Exists(directory))
        {
            throw new InvalidOperationException($"DynamicApp Mod `{dynamicAppId}/{modId}` already exists.");
        }

        Directory.CreateDirectory(directory);
        return BuildApplicationView(new DirectoryInfo(Path.GetDirectoryName(directory)!));
    }

    /// <summary>
    /// <para>zh-cn:删除没有包版本、运行目录或归档目录的 Mod 服务。</para>
    /// <para>en-us:Deletes a Mod service only when no package versions, runtime directories, or archived directories remain.</para>
    /// </summary>
    public bool RemoveMod(string dynamicAppId, string modId)
    {
        ValidatePathSegment(dynamicAppId, nameof(dynamicAppId));
        ValidatePathSegment(modId, nameof(modId));
        var modDirectory = Path.GetFullPath(Path.Combine(_rootPath, "mods", dynamicAppId, modId));
        var deploymentsDirectory = Path.GetFullPath(Path.Combine(_rootPath, "deployments", dynamicAppId, modId));
        var archivesDirectory = Path.GetFullPath(Path.Combine(_rootPath, "archives", "deployments", dynamicAppId, modId));
        EnsureInsideRoot(modDirectory);
        EnsureInsideRoot(deploymentsDirectory);
        EnsureInsideRoot(archivesDirectory);
        if (!Directory.Exists(modDirectory))
        {
            return false;
        }

        if (Directory.EnumerateFiles(modDirectory, "*", SearchOption.AllDirectories).Any()
            || Directory.Exists(deploymentsDirectory) && Directory.EnumerateFiles(deploymentsDirectory, "*", SearchOption.AllDirectories).Any()
            || Directory.Exists(archivesDirectory) && Directory.EnumerateFiles(archivesDirectory, "*", SearchOption.AllDirectories).Any())
        {
            throw new InvalidOperationException($"DynamicApp Mod `{dynamicAppId}/{modId}` cannot be removed while packages, deployments, or archived files exist.");
        }

        Directory.Delete(modDirectory, recursive: true);
        var appDirectory = Path.GetDirectoryName(modDirectory);
        if (!string.IsNullOrWhiteSpace(appDirectory)
            && Directory.Exists(appDirectory)
            && !Directory.EnumerateFileSystemEntries(appDirectory).Any())
        {
            Directory.Delete(appDirectory);
        }

        return true;
    }

    /// <summary>
    /// <para>zh-cn:列出 DynamicApp 应用目录；即使应用下还没有 Mod 版本，也会返回该应用。</para>
    /// <para>en-us:Lists DynamicApp application directories; applications are returned even when no Mod version exists under them.</para>
    /// </summary>
    /// <returns>
    /// <para>zh-cn:按应用标识排序的应用视图集合。</para>
    /// <para>en-us:Application views sorted by application identifier.</para>
    /// </returns>
    public IReadOnlyCollection<DynamicAppPackageApplicationView> ListApplications()
    {
        var modsRoot = Path.GetFullPath(Path.Combine(_rootPath, "mods"));
        EnsureInsideRoot(modsRoot);
        if (!Directory.Exists(modsRoot))
        {
            return Array.Empty<DynamicAppPackageApplicationView>();
        }

        return OrderedDirectories(modsRoot)
            .Select(BuildApplicationView)
            .OrderBy(item => item.DynamicAppId, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    /// <summary>
    /// <para>zh-cn:扫描 deployments/{dynamicAppId}/{modId}/{version}/{deploymentId} 目录结构，返回磁盘上存在的全部隔离副本目录。</para>
    /// <para>en-us:Scans the deployments/{dynamicAppId}/{modId}/{version}/{deploymentId} structure and returns every isolated replica directory present on disk.</para>
    /// </summary>
    /// <returns>
    /// <para>zh-cn:按应用、Mod、版本和部署实例排序的目录记录。</para>
    /// <para>en-us:Directory records sorted by application, Mod, version, and deployment instance.</para>
    /// </returns>
    public IReadOnlyCollection<DynamicAppModPackageDeploymentDirectoryRecord> ListDeploymentDirectories()
    {
        var deploymentsRoot = Path.GetFullPath(Path.Combine(_rootPath, "deployments"));
        EnsureInsideRoot(deploymentsRoot);
        if (!Directory.Exists(deploymentsRoot))
        {
            return Array.Empty<DynamicAppModPackageDeploymentDirectoryRecord>();
        }

        var records = new List<DynamicAppModPackageDeploymentDirectoryRecord>();
        foreach (var appDirectory in OrderedDirectories(deploymentsRoot))
        {
            foreach (var modDirectory in OrderedDirectories(appDirectory.FullName))
            {
                foreach (var versionDirectory in OrderedDirectories(modDirectory.FullName))
                {
                    foreach (var deploymentDirectory in OrderedDirectories(versionDirectory.FullName))
                    {
                        try
                        {
                            var key = new DynamicModVersionKey(
                                new DynamicAppId(appDirectory.Name),
                                new DynamicModId(modDirectory.Name),
                                new DynamicAppVersion(versionDirectory.Name));
                            EnsureInsideRoot(deploymentDirectory.FullName);
                            records.Add(new DynamicAppModPackageDeploymentDirectoryRecord
                            {
                                Key = key,
                                DeploymentId = deploymentDirectory.Name,
                                DeploymentDirectory = deploymentDirectory.FullName,
                                LastModifiedUtc = deploymentDirectory.LastWriteTimeUtc
                            });
                        }
                        catch (ArgumentException)
                        {
                            // Ignore manually created directories that do not match the DynamicApp identifier contract.
                        }
                    }
                }
            }
        }

        return records
            .OrderBy(item => item.Key.DynamicAppId.Value, StringComparer.OrdinalIgnoreCase)
            .ThenBy(item => item.Key.ModId.Value, StringComparer.OrdinalIgnoreCase)
            .ThenBy(item => item.Key.Version.Value, StringComparer.OrdinalIgnoreCase)
            .ThenBy(item => item.DeploymentId, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    /// <summary>
    /// <para>zh-cn:扫描指定运行副本目录中的文件，并将路径限制在 deployments 运行空间内。</para>
    /// <para>en-us:Scans one isolated deployment directory while constraining every returned path to the deployments runtime space.</para>
    /// </summary>
    /// <param name="key">
    /// <para>zh-cn:运行实例键。</para>
    /// <para>en-us:The runtime-instance key.</para>
    /// </param>
    /// <returns>
    /// <para>zh-cn:运行副本文件清单；目录不存在或没有文件时返回空集合。</para>
    /// <para>en-us:The deployment file inventory; an empty collection is returned when the directory does not exist or has no files.</para>
    /// </returns>
    public IReadOnlyCollection<DynamicAppModPackageFileView> ListDeploymentFiles(DynamicAppRuntimeKey key)
    {
        ValidatePathSegment(key.PackageKey.DynamicAppId.Value, nameof(key));
        ValidatePathSegment(key.PackageKey.ModId.Value, nameof(key));
        ValidatePathSegment(key.PackageKey.Version.Value, nameof(key));
        ValidatePathSegment(key.DeploymentId, nameof(key));

        var deploymentDirectory = Path.GetFullPath(Path.Combine(
            _rootPath,
            "deployments",
            key.PackageKey.DynamicAppId.Value,
            key.PackageKey.ModId.Value,
            key.PackageKey.Version.Value,
            key.DeploymentId));
        EnsureInsideRoot(deploymentDirectory);
        return ListDeploymentFiles(key, deploymentDirectory);
    }

    /// <summary>
    /// <para>zh-cn:列出指定运行目录中的文件；目录由运行状态存储提供，结果使用该目录作为相对路径根。</para>
    /// <para>en-us:Lists files under a specified runtime directory; the directory comes from runtime state and is used as the relative-path root.</para>
    /// </summary>
    /// <param name="key">
    /// <para>zh-cn:运行实例键。</para>
    /// <para>en-us:The runtime-instance key.</para>
    /// </param>
    /// <param name="runtimeDirectory">
    /// <para>zh-cn:运行目录绝对路径。</para>
    /// <para>en-us:The absolute runtime directory.</para>
    /// </param>
    /// <returns>
    /// <para>zh-cn:运行目录文件清单。</para>
    /// <para>en-us:The runtime-directory file inventory.</para>
    /// </returns>
    public IReadOnlyCollection<DynamicAppModPackageFileView> ListDeploymentFiles(DynamicAppRuntimeKey key, string runtimeDirectory)
    {
        ValidatePathSegment(key.PackageKey.DynamicAppId.Value, nameof(key));
        ValidatePathSegment(key.PackageKey.ModId.Value, nameof(key));
        ValidatePathSegment(key.PackageKey.Version.Value, nameof(key));
        ValidatePathSegment(key.DeploymentId, nameof(key));
        if (string.IsNullOrWhiteSpace(runtimeDirectory) || !Path.IsPathFullyQualified(runtimeDirectory))
        {
            throw new ArgumentException("Runtime directory must be an absolute path.", nameof(runtimeDirectory));
        }

        var deploymentDirectory = Path.GetFullPath(runtimeDirectory);
        if (!Directory.Exists(deploymentDirectory))
        {
            return Array.Empty<DynamicAppModPackageFileView>();
        }

        var packagePath = Path.Combine(deploymentDirectory, PackageFileName);
        var contentDirectory = Path.Combine(deploymentDirectory, "content");
        return Directory.EnumerateFiles(deploymentDirectory, "*", SearchOption.AllDirectories)
            .Select(path => BuildDeploymentFileView(path, deploymentDirectory, packagePath, contentDirectory))
            .OrderBy(file => file.RelativePath, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    /// <summary>
    /// <para>zh-cn:删除指定运行实例对应的隔离目录；删除前检查路径仍位于包存储根目录内。</para>
    /// <para>en-us:Deletes the isolated directory for one runtime instance after verifying that the path remains inside the package-store root.</para>
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
    public Task<bool> RemoveDeploymentAsync(DynamicAppRuntimeKey key, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var deploymentDirectory = BuildDeploymentDirectory(key);
        if (!Directory.Exists(deploymentDirectory))
        {
            return Task.FromResult(false);
        }

        Directory.Delete(deploymentDirectory, recursive: true);
        return Task.FromResult(true);
    }

    /// <summary>
    /// <para>zh-cn:将指定运行实例目录从 deployments 迁移到独立 archives 空间；目标归档目录存在时拒绝覆盖。</para>
    /// <para>en-us:Moves one runtime-instance directory from deployments into isolated archive space and refuses to overwrite an existing archive directory.</para>
    /// </summary>
    public Task<DynamicAppModPackageDeploymentArchiveRecord> ArchiveDeploymentAsync(
        DynamicAppRuntimeKey key,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var deploymentDirectory = BuildDeploymentDirectory(key);
        var archiveDirectory = BuildArchiveDirectory(key);
        if (!Directory.Exists(deploymentDirectory))
        {
            return Task.FromResult(new DynamicAppModPackageDeploymentArchiveRecord
            {
                Key = key.PackageKey,
                DeploymentId = key.DeploymentId,
                DeploymentDirectory = deploymentDirectory,
                ArchiveDirectory = archiveDirectory,
                Moved = false
            });
        }

        EnsureInsideRoot(archiveDirectory);
        if (Directory.Exists(archiveDirectory))
        {
            throw new InvalidOperationException($"DynamicApp archived deployment `{key}` already exists.");
        }

        Directory.CreateDirectory(Path.GetDirectoryName(archiveDirectory)!);
        Directory.Move(deploymentDirectory, archiveDirectory);
        return Task.FromResult(new DynamicAppModPackageDeploymentArchiveRecord
        {
            Key = key.PackageKey,
            DeploymentId = key.DeploymentId,
            DeploymentDirectory = deploymentDirectory,
            ArchiveDirectory = archiveDirectory,
            Moved = true
        });
    }

    /// <summary>
    /// <para>zh-cn:将归档实例目录恢复到 deployments 运行空间；运行目录已存在时拒绝覆盖。</para>
    /// <para>en-us:Restores an archived instance directory into deployments runtime space and refuses to overwrite an existing runtime directory.</para>
    /// </summary>
    public Task<DynamicAppModPackageDeploymentArchiveRecord> RestoreArchivedDeploymentAsync(
        DynamicAppRuntimeKey key,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var deploymentDirectory = BuildDeploymentDirectory(key);
        var archiveDirectory = BuildArchiveDirectory(key);
        if (!Directory.Exists(archiveDirectory))
        {
            throw new FileNotFoundException($"DynamicApp archived deployment `{key}` was not found.", archiveDirectory);
        }

        EnsureInsideRoot(deploymentDirectory);
        if (Directory.Exists(deploymentDirectory))
        {
            throw new InvalidOperationException($"DynamicApp deployment `{key}` already exists.");
        }

        Directory.CreateDirectory(Path.GetDirectoryName(deploymentDirectory)!);
        Directory.Move(archiveDirectory, deploymentDirectory);
        return Task.FromResult(new DynamicAppModPackageDeploymentArchiveRecord
        {
            Key = key.PackageKey,
            DeploymentId = key.DeploymentId,
            DeploymentDirectory = deploymentDirectory,
            ArchiveDirectory = archiveDirectory,
            Moved = true
        });
    }

    /// <summary>
    /// <para>zh-cn:删除指定实例的归档目录，并保持包空间和其他运行实例不变。</para>
    /// <para>en-us:Deletes one archived instance directory without affecting package space or other runtime instances.</para>
    /// </summary>
    public Task<bool> RemoveArchivedDeploymentAsync(
        DynamicAppRuntimeKey key,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var archiveDirectory = BuildArchiveDirectory(key);
        if (!Directory.Exists(archiveDirectory))
        {
            return Task.FromResult(false);
        }

        Directory.Delete(archiveDirectory, recursive: true);
        return Task.FromResult(true);
    }

    private static void ValidateRequest(DynamicAppModPackageSaveRequest request)
    {
        ValidatePathSegment(request.Key.DynamicAppId.Value, nameof(request));
        ValidatePathSegment(request.Key.ModId.Value, nameof(request));
        ValidatePathSegment(request.Key.Version.Value, nameof(request));

        if (string.IsNullOrWhiteSpace(request.OriginalFileName)
            || !string.Equals(Path.GetExtension(request.OriginalFileName), ".zip", StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("Mod package must be a .zip file.", nameof(request));
        }

        if (!string.IsNullOrWhiteSpace(request.EntryFile) && Path.IsPathFullyQualified(request.EntryFile))
        {
            throw new ArgumentException("Mod package entry file must be a relative path.", nameof(request));
        }
    }

    private static void ValidatePathSegment(string value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value)
            || value.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0
            || value.Contains(Path.DirectorySeparatorChar)
            || value.Contains(Path.AltDirectorySeparatorChar)
            || value == "."
            || value == "..")
        {
            throw new ArgumentException("DynamicApp package path segment is invalid.", parameterName);
        }
    }

    private static string ResolveRootPath(string? rootPath)
    {
        if (string.IsNullOrWhiteSpace(rootPath))
        {
            return Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "dynamicapp-packages"));
        }

        return Path.IsPathFullyQualified(rootPath)
            ? Path.GetFullPath(rootPath)
            : Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, rootPath));
    }

    private DynamicAppPackageApplicationView BuildApplicationView(DirectoryInfo appDirectory)
    {
        EnsureInsideRoot(appDirectory.FullName);
        var modDirectories = Directory.Exists(appDirectory.FullName)
            ? Directory.EnumerateDirectories(appDirectory.FullName).Select(directory => new DirectoryInfo(directory)).ToList()
            : new List<DirectoryInfo>();
        return new DynamicAppPackageApplicationView
        {
            DynamicAppId = appDirectory.Name,
            ModCount = modDirectories.Count,
            VersionCount = modDirectories.Sum(directory => Directory.Exists(directory.FullName) ? Directory.EnumerateDirectories(directory.FullName).Count() : 0)
        };
    }

    private string BuildPackageDirectory(DynamicAppModPackageSaveRequest request)
    {
        return Path.GetFullPath(Path.Combine(
            _rootPath,
            "mods",
            request.Key.DynamicAppId.Value,
            request.Key.ModId.Value,
            request.Key.Version.Value));
    }

    private string BuildDeploymentDirectory(DynamicAppRuntimeKey key)
    {
        ValidatePathSegment(key.PackageKey.DynamicAppId.Value, nameof(key));
        ValidatePathSegment(key.PackageKey.ModId.Value, nameof(key));
        ValidatePathSegment(key.PackageKey.Version.Value, nameof(key));
        ValidatePathSegment(key.DeploymentId, nameof(key));
        var directory = Path.GetFullPath(Path.Combine(
            _rootPath,
            "deployments",
            key.PackageKey.DynamicAppId.Value,
            key.PackageKey.ModId.Value,
            key.PackageKey.Version.Value,
            key.DeploymentId));
        EnsureInsideRoot(directory);
        return directory;
    }

    private string BuildArchiveDirectory(DynamicAppRuntimeKey key)
    {
        ValidatePathSegment(key.PackageKey.DynamicAppId.Value, nameof(key));
        ValidatePathSegment(key.PackageKey.ModId.Value, nameof(key));
        ValidatePathSegment(key.PackageKey.Version.Value, nameof(key));
        ValidatePathSegment(key.DeploymentId, nameof(key));
        var directory = Path.GetFullPath(Path.Combine(
            _rootPath,
            "archives",
            "deployments",
            key.PackageKey.DynamicAppId.Value,
            key.PackageKey.ModId.Value,
            key.PackageKey.Version.Value,
            key.DeploymentId));
        EnsureInsideRoot(directory);
        return directory;
    }

    private static void CopyDirectory(string sourceDirectory, string destinationDirectory, CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(destinationDirectory);
        foreach (var directory in Directory.EnumerateDirectories(sourceDirectory, "*", SearchOption.AllDirectories))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var relative = Path.GetRelativePath(sourceDirectory, directory);
            Directory.CreateDirectory(Path.Combine(destinationDirectory, relative));
        }

        foreach (var file in Directory.EnumerateFiles(sourceDirectory, "*", SearchOption.AllDirectories))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var relative = Path.GetRelativePath(sourceDirectory, file);
            var destination = Path.Combine(destinationDirectory, relative);
            Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
            File.Copy(file, destination, overwrite: false);
        }
    }

    private async Task<long> CopyPackageAsync(Stream packageStream, string archivePath, CancellationToken cancellationToken)
    {
        var maxBytes = _options.MaxPackageBytes <= 0 ? 200 * 1024 * 1024 : _options.MaxPackageBytes;
        var buffer = new byte[81920];
        long total = 0;
        await using var output = File.Create(archivePath);
        while (true)
        {
            var read = await packageStream.ReadAsync(buffer, cancellationToken).ConfigureAwait(false);
            if (read == 0)
            {
                break;
            }

            total += read;
            if (total > maxBytes)
            {
                throw new InvalidOperationException($"Mod package exceeds max size `{maxBytes}` bytes.");
            }

            await output.WriteAsync(buffer.AsMemory(0, read), cancellationToken).ConfigureAwait(false);
        }

        return total;
    }

    private void ExtractZipSafely(string archivePath, string extractedDirectory)
    {
        var extractionRoot = EnsureTrailingSeparator(Path.GetFullPath(extractedDirectory));
        using var archive = ZipFile.OpenRead(archivePath);
        foreach (var entry in archive.Entries)
        {
            if (string.IsNullOrWhiteSpace(entry.FullName))
            {
                continue;
            }

            var destinationPath = Path.GetFullPath(Path.Combine(extractionRoot, entry.FullName));
            EnsureInsidePath(extractionRoot, destinationPath);
            if (entry.FullName.EndsWith("/", StringComparison.Ordinal) || entry.FullName.EndsWith("\\", StringComparison.Ordinal))
            {
                Directory.CreateDirectory(destinationPath);
                continue;
            }

            var directory = Path.GetDirectoryName(destinationPath);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }

            entry.ExtractToFile(destinationPath, overwrite: false);
        }
    }

    private DynamicAppModPackageVersionView BuildVersionView(string dynamicAppId, string modId, DirectoryInfo versionDirectory)
    {
        EnsureInsideRoot(versionDirectory.FullName);
        var packagePath = Path.GetFullPath(Path.Combine(versionDirectory.FullName, PackageFileName));
        var hasPackage = File.Exists(packagePath);
        var packageInfo = hasPackage ? new FileInfo(packagePath) : null;
        var extractedDirectory = Path.GetFullPath(Path.Combine(versionDirectory.FullName, "content"));
        var hasExtractedDirectory = Directory.Exists(extractedDirectory);
        var files = Directory.EnumerateFiles(versionDirectory.FullName, "*", SearchOption.AllDirectories)
            .Select(path => BuildPackageFileView(path, packagePath, extractedDirectory))
            .OrderBy(file => file.RelativePath, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return new DynamicAppModPackageVersionView
        {
            DynamicAppId = dynamicAppId,
            ModId = modId,
            Version = versionDirectory.Name,
            PackageFilePath = hasPackage ? packagePath : string.Empty,
            ExtractedDirectory = hasExtractedDirectory ? extractedDirectory : string.Empty,
            PackageBytes = packageInfo?.Length ?? 0,
            SavedAtUtc = packageInfo?.LastWriteTimeUtc ?? versionDirectory.LastWriteTimeUtc,
            Files = files
        };
    }

    private DynamicAppModPackageFileView BuildPackageFileView(string path, string packagePath, string extractedDirectory)
    {
        var fullPath = Path.GetFullPath(path);
        EnsureInsideRoot(fullPath);
        var file = new FileInfo(fullPath);
        var kind = string.Equals(fullPath, packagePath, StringComparison.OrdinalIgnoreCase)
            ? "Package"
            : (fullPath.StartsWith(EnsureTrailingSeparator(extractedDirectory), StringComparison.OrdinalIgnoreCase) ? "Content" : "Other");

        return new DynamicAppModPackageFileView
        {
            Name = file.Name,
            RelativePath = Path.GetRelativePath(_rootPath, fullPath),
            FullPath = fullPath,
            Length = file.Length,
            LastModifiedUtc = file.LastWriteTimeUtc,
            Kind = kind
        };
    }

    private static DynamicAppModPackageFileView BuildDeploymentFileView(
        string path,
        string deploymentDirectory,
        string packagePath,
        string contentDirectory)
    {
        var fullPath = Path.GetFullPath(path);
        var file = new FileInfo(fullPath);
        var normalizedContentRoot = EnsureTrailingSeparator(Path.GetFullPath(contentDirectory));
        var kind = string.Equals(fullPath, Path.GetFullPath(packagePath), StringComparison.OrdinalIgnoreCase)
            ? "Package"
            : (fullPath.StartsWith(normalizedContentRoot, StringComparison.OrdinalIgnoreCase) ? "Content" : "Other");
        return new DynamicAppModPackageFileView
        {
            Name = file.Name,
            RelativePath = Path.GetRelativePath(deploymentDirectory, fullPath),
            FullPath = fullPath,
            Length = file.Length,
            LastModifiedUtc = file.LastWriteTimeUtc,
            Kind = kind
        };
    }

    private static IEnumerable<DirectoryInfo> OrderedDirectories(string path)
    {
        return Directory.EnumerateDirectories(path)
            .Select(directory => new DirectoryInfo(directory))
            .OrderBy(directory => directory.Name, StringComparer.OrdinalIgnoreCase);
    }

    private static string ResolveEntryFilePath(string extractedDirectory, string entryFile)
    {
        var root = EnsureTrailingSeparator(Path.GetFullPath(extractedDirectory));
        var fullPath = Path.GetFullPath(Path.Combine(root, entryFile));
        EnsureInsidePath(root, fullPath);
        if (!File.Exists(fullPath))
        {
            throw new FileNotFoundException($"Mod package entry file `{entryFile}` was not found after extraction.", fullPath);
        }

        return fullPath;
    }

    private static string InferEntryFile(string extractedDirectory)
    {
        var candidates = Directory.EnumerateFiles(extractedDirectory, "*.dll", SearchOption.AllDirectories)
            .OrderBy(path => Path.GetRelativePath(extractedDirectory, path).Count(character => character == Path.DirectorySeparatorChar || character == Path.AltDirectorySeparatorChar))
            .ThenBy(path => Path.GetFileName(path), StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (candidates.Count == 0)
        {
            throw new FileNotFoundException("Mod package entry file was not specified and no .dll file was found after extraction.", extractedDirectory);
        }

        return Path.GetRelativePath(extractedDirectory, candidates[0]);
    }

    private static string? ResolveRelativeEntryFile(string sourceContentDirectory, string? entryFile)
    {
        if (string.IsNullOrWhiteSpace(entryFile))
        {
            return null;
        }

        if (!entryFile.Trim().EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("DynamicApp deployment entry file must be a DLL.", nameof(entryFile));
        }

        var sourceRoot = EnsureTrailingSeparator(Path.GetFullPath(sourceContentDirectory));
        var candidate = Path.IsPathFullyQualified(entryFile)
            ? Path.GetFullPath(entryFile)
            : Path.GetFullPath(Path.Combine(sourceRoot, entryFile));
        if (!candidate.StartsWith(sourceRoot, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("DynamicApp deployment entry file must remain inside the package content directory.");
        }

        return Path.GetRelativePath(sourceRoot, candidate);
    }

    private void EnsureInsideRoot(string path) => EnsureInsidePath(_rootPath, path);

    private static void EnsureInsidePath(string rootPath, string path)
    {
        var normalizedRoot = EnsureTrailingSeparator(Path.GetFullPath(rootPath));
        var normalizedPath = Path.GetFullPath(path);
        if (!normalizedPath.StartsWith(normalizedRoot, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException($"Path `{normalizedPath}` is outside DynamicApp package root `{normalizedRoot}`.");
        }
    }

    private static string EnsureTrailingSeparator(string path)
    {
        return path.EndsWith(Path.DirectorySeparatorChar) || path.EndsWith(Path.AltDirectorySeparatorChar)
            ? path
            : path + Path.DirectorySeparatorChar;
    }
}
