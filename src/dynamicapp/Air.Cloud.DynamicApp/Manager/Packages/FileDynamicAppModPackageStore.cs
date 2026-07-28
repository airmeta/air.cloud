using System.IO.Compression;

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
        if (existed)
        {
            Directory.Delete(packageDirectory, recursive: true);
        }

        Directory.CreateDirectory(packageDirectory);
        var archivePath = Path.Combine(packageDirectory, PackageFileName);
        var bytes = await CopyPackageAsync(packageStream, archivePath, cancellationToken).ConfigureAwait(false);
        var extractedDirectory = Path.Combine(packageDirectory, "content");
        Directory.CreateDirectory(extractedDirectory);
        ExtractZipSafely(archivePath, extractedDirectory);

        var entryFile = string.IsNullOrWhiteSpace(request.EntryFile)
            ? InferEntryFile(extractedDirectory)
            : request.EntryFile;
        var entryFilePath = ResolveEntryFilePath(extractedDirectory, entryFile);
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

                if (versions.Count == 0)
                {
                    continue;
                }

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
