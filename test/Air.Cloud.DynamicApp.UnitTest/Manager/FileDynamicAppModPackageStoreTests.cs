using System.IO.Compression;

using Air.Cloud.DynamicApp.Contracts.Identifiers;
using Air.Cloud.DynamicApp.Contracts.Runtime;
using Air.Cloud.DynamicApp.Manager;

namespace Air.Cloud.DynamicApp.UnitTest.Manager;

/// <summary>
/// <para>zh-cn:文件型 DynamicApp Mod 包存储单元测试，覆盖应用目录持久化、包版本清单和覆盖策略。</para>
/// <para>en-us:Unit tests for the file-based DynamicApp Mod package store, covering application-directory persistence, package inventory, and overwrite policy.</para>
/// </summary>
public sealed class FileDynamicAppModPackageStoreTests
{
    /// <summary>
    /// <para>zh-cn:验证创建应用目录后，即使尚未上传 Mod 包，也会被应用管理器清单持久化并返回。</para>
    /// <para>en-us:Verifies that a created application directory is persisted and returned by the application manager inventory even before any Mod package is uploaded.</para>
    /// </summary>
    [Fact]
    public void CreateApplication_should_persist_empty_application_namespace()
    {
        using var workspace = DynamicAppTestWorkspace.Create();
        var store = workspace.CreatePackageStore();

        var application = store.CreateApplication("chaos-platform");
        var applications = store.ListApplications();

        Assert.Equal("chaos-platform", application.DynamicAppId);
        var stored = Assert.Single(applications);
        Assert.Equal("chaos-platform", stored.DynamicAppId);
        Assert.Equal(0, stored.ModCount);
        Assert.Equal(0, stored.VersionCount);
    }

    /// <summary>
    /// <para>zh-cn:验证创建服务会持久化应用下的空 Mod 配置目录，并让包清单返回该 Mod 组。</para>
    /// <para>en-us:Verifies that creating a service persists an empty application Mod directory and exposes that Mod group in the package inventory.</para>
    /// </summary>
    [Fact]
    public void CreateMod_should_persist_empty_mod_namespace()
    {
        using var workspace = DynamicAppTestWorkspace.Create();
        var store = workspace.CreatePackageStore();

        var application = store.CreateMod("chaos-platform", "workflow");
        var packages = store.ListPackages();

        Assert.Equal("chaos-platform", application.DynamicAppId);
        Assert.Equal(1, application.ModCount);
        var mod = Assert.Single(packages);
        Assert.Equal("chaos-platform", mod.DynamicAppId);
        Assert.Equal("workflow", mod.ModId);
        Assert.Empty(mod.Versions);
        Assert.True(Directory.Exists(Path.Combine(workspace.RootPath, "mods", "chaos-platform", "workflow")));
    }

    /// <summary>
    /// <para>zh-cn:验证包保存后会写入 mods/{dynamicAppId}/{modId}/{version} 结构并拒绝未开启覆盖的重复版本。</para>
    /// <para>en-us:Verifies that package saving writes the mods/{dynamicAppId}/{modId}/{version} structure and rejects duplicate versions when overwrite is not enabled.</para>
    /// </summary>
    [Fact]
    public async Task SaveAsync_should_list_saved_package_and_reject_duplicate_without_overwrite()
    {
        using var workspace = DynamicAppTestWorkspace.Create();
        var store = workspace.CreatePackageStore(allowOverwrite: false);
        var request = CreateSaveRequest("security-platform", "testmod", "2026.07.29.100000");

        await using var package = CreatePackageStream("air.cloud.dynamicapp.testmod.dll");
        var record = await store.SaveAsync(request, package);
        var packages = store.ListPackages();

        Assert.True(File.Exists(record.PackageFilePath));
        var mod = Assert.Single(packages);
        Assert.Equal("security-platform", mod.DynamicAppId);
        Assert.Equal("testmod", mod.ModId);
        var version = Assert.Single(mod.Versions);
        Assert.Equal("2026.07.29.100000", version.Version);
        Assert.Equal(2, version.Files.Count);

        await using var duplicatePackage = CreatePackageStream("air.cloud.dynamicapp.testmod.dll");
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => store.SaveAsync(request, duplicatePackage));
        Assert.Contains("already exists and overwrite is not allowed", exception.Message, StringComparison.Ordinal);
    }

    /// <summary>
    /// <para>zh-cn:验证覆盖上传在新包解压或入口校验失败时不会破坏原有可用包。</para>
    /// <para>en-us:Verifies that an overwrite upload preserves the existing usable package when extraction or entry validation of the new package fails.</para>
    /// </summary>
    [Fact]
    public async Task SaveAsync_should_preserve_existing_package_when_overwrite_validation_fails()
    {
        using var workspace = DynamicAppTestWorkspace.Create();
        var store = workspace.CreatePackageStore();
        var request = CreateSaveRequest("security-platform", "testmod", "1.0.0");

        await using (var originalPackage = CreatePackageStream("air.cloud.dynamicapp.testmod.dll"))
        {
            await store.SaveAsync(request, originalPackage);
        }

        await using var invalidPackage = new MemoryStream("not-a-zip"u8.ToArray());
        var exception = await Assert.ThrowsAnyAsync<Exception>(() => store.SaveAsync(request, invalidPackage));

        Assert.True(exception is InvalidDataException or IOException or InvalidOperationException);
        var inventory = store.ListPackages();
        var version = Assert.Single(Assert.Single(inventory).Versions);
        Assert.Equal(2, version.Files.Count);
        Assert.Contains(version.Files, file => file.Name == "air.cloud.dynamicapp.testmod.dll");
        Assert.True(File.Exists(Path.Combine(workspace.RootPath, "mods", "security-platform", "testmod", "1.0.0", "package.zip")));
        Assert.Empty(Directory.EnumerateDirectories(
            Path.Combine(workspace.RootPath, "mods", "security-platform", "testmod"),
            "*staging-*",
            SearchOption.TopDirectoryOnly));
    }

    /// <summary>
    /// <para>zh-cn:验证新包首次上传失败时不会留下半成品版本目录。</para>
    /// <para>en-us:Verifies that a failed first upload does not leave a partially created version directory.</para>
    /// </summary>
    [Fact]
    public async Task SaveAsync_should_remove_staging_directory_when_first_upload_fails()
    {
        using var workspace = DynamicAppTestWorkspace.Create();
        var store = workspace.CreatePackageStore();
        var request = CreateSaveRequest("security-platform", "testmod", "2.0.0");

        await using var invalidPackage = new MemoryStream("not-a-zip"u8.ToArray());
        await Assert.ThrowsAnyAsync<Exception>(() => store.SaveAsync(request, invalidPackage));

        var modDirectory = Path.Combine(workspace.RootPath, "mods", "security-platform", "testmod");
        Assert.False(Directory.Exists(Path.Combine(modDirectory, "2.0.0")));
        Assert.Empty(Directory.EnumerateDirectories(modDirectory, "*staging-*", SearchOption.TopDirectoryOnly));
    }

    /// <summary>
    /// <para>zh-cn:验证同一个不可变包可以创建多个互不共享目录的部署副本，满足灰度配置和端口隔离要求。</para>
    /// <para>en-us:Verifies that one immutable package can create multiple deployment copies with isolated directories for gray-release configuration and port separation.</para>
    /// </summary>
    [Fact]
    public async Task CreateDeploymentAsync_should_copy_package_into_isolated_directories()
    {
        using var workspace = DynamicAppTestWorkspace.Create();
        var store = workspace.CreatePackageStore();
        var request = CreateSaveRequest("security-platform", "testmod", "1.0.0");
        await using var package = CreatePackageStream("air.cloud.dynamicapp.testmod.dll");
        await store.SaveAsync(request, package);

        var blue = await store.CreateDeploymentAsync(request.Key, "blue");
        var gray = await store.CreateDeploymentAsync(request.Key, "gray");

        Assert.NotEqual(blue.DeploymentDirectory, gray.DeploymentDirectory);
        Assert.NotEqual(blue.EntryFilePath, gray.EntryFilePath);
        Assert.True(File.Exists(blue.EntryFilePath));
        Assert.True(File.Exists(gray.EntryFilePath));
        Assert.Contains(Path.Combine("deployments", "security-platform", "testmod", "1.0.0", "blue"), blue.DeploymentDirectory, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(Path.Combine("deployments", "security-platform", "testmod", "1.0.0", "gray"), gray.DeploymentDirectory, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// <para>zh-cn:验证部署入口必须是包 content 目录中的 DLL，拒绝非 DLL 和目录穿越路径。</para>
    /// <para>en-us:Verifies that a deployment entry must be a DLL inside the package content directory, rejecting non-DLL and traversal paths.</para>
    /// </summary>
    [Fact]
    public async Task CreateDeploymentAsync_should_validate_entry_file_boundary_and_extension()
    {
        using var workspace = DynamicAppTestWorkspace.Create();
        var store = workspace.CreatePackageStore();
        var request = CreateSaveRequest("security-platform", "testmod", "1.0.0");
        await using var package = CreatePackageStream("air.cloud.dynamicapp.testmod.dll");
        await store.SaveAsync(request, package);

        var extensionException = await Assert.ThrowsAsync<ArgumentException>(() =>
            store.CreateDeploymentAsync(request.Key, "invalid-extension", entryFile: "appsettings.json"));
        Assert.Contains("must be a DLL", extensionException.Message, StringComparison.OrdinalIgnoreCase);

        var traversalException = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            store.CreateDeploymentAsync(request.Key, "traversal", entryFile: "../outside.dll"));
        Assert.Contains("inside the package content directory", traversalException.Message, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// <para>zh-cn:验证指定 DLL 不存在时不会留下半成品部署目录。</para>
    /// <para>en-us:Verifies that a missing selected DLL does not leave a partially created deployment directory.</para>
    /// </summary>
    [Fact]
    public async Task CreateDeploymentAsync_should_remove_directory_when_selected_entry_is_missing()
    {
        using var workspace = DynamicAppTestWorkspace.Create();
        var store = workspace.CreatePackageStore();
        var request = CreateSaveRequest("security-platform", "testmod", "1.0.0");
        await using var package = CreatePackageStream("air.cloud.dynamicapp.testmod.dll");
        await store.SaveAsync(request, package);

        await Assert.ThrowsAsync<FileNotFoundException>(() =>
            store.CreateDeploymentAsync(request.Key, "missing-entry", entryFile: "missing.dll"));

        Assert.False(Directory.Exists(Path.Combine(workspace.RootPath, "deployments", "security-platform", "testmod", "1.0.0", "missing-entry")));
    }

    /// <summary>
    /// <para>zh-cn:验证运行副本文件清单只读取 deployments 目录，并且编辑或新增运行副本文件不会改变包库存文件。</para>
    /// <para>en-us:Verifies that deployment file inventory reads only the deployments directory and that runtime-copy changes do not alter package inventory files.</para>
    /// </summary>
    [Fact]
    public async Task ListDeploymentFiles_should_remain_isolated_from_package_inventory()
    {
        using var workspace = DynamicAppTestWorkspace.Create();
        var store = workspace.CreatePackageStore();
        var request = CreateSaveRequest("security-platform", "testmod", "1.0.0");
        await using var package = CreatePackageStream("air.cloud.dynamicapp.testmod.dll", ("appsettings.json", "{}"));
        await store.SaveAsync(request, package);

        var deployment = await store.CreateDeploymentAsync(request.Key, "blue");
        var runtimeConfig = Path.Combine(deployment.DeploymentDirectory, "content", "appsettings.json");
        await File.WriteAllTextAsync(runtimeConfig, "{\"runtime\":true}");

        var files = store.ListDeploymentFiles(new DynamicAppRuntimeKey(request.Key, "blue"));
        var runtimeFile = Assert.Single(files.Where(file => file.Name == "appsettings.json"));
        Assert.Equal(Path.GetRelativePath(deployment.DeploymentDirectory, runtimeConfig), runtimeFile.RelativePath);
        Assert.StartsWith(deployment.DeploymentDirectory, runtimeFile.FullPath, StringComparison.OrdinalIgnoreCase);
        Assert.Equal("{}", await File.ReadAllTextAsync(Path.Combine(workspace.RootPath, "mods", "security-platform", "testmod", "1.0.0", "content", "appsettings.json")));
        Assert.Empty(store.ListDeploymentFiles(new DynamicAppRuntimeKey(request.Key, "missing")));
    }

    /// <summary>
    /// <para>zh-cn:验证状态存储登记的默认挂载目录也可以复用运行文件清单逻辑，而不要求目录位于 deployments 包空间。</para>
    /// <para>en-us:Verifies that a default mounted directory registered by runtime state can reuse runtime-file inventory logic without being under the deployments package space.</para>
    /// </summary>
    [Fact]
    public async Task ListDeploymentFiles_should_support_runtime_state_directory()
    {
        using var workspace = DynamicAppTestWorkspace.Create();
        var store = workspace.CreatePackageStore();
        var key = new DynamicAppRuntimeKey(CreateSaveRequest("security-platform", "testmod", "1.0.0").Key, "default");
        var runtimeDirectory = Path.Combine(workspace.RootPath, "mounted", "workflow");
        Directory.CreateDirectory(Path.Combine(runtimeDirectory, "config"));
        await File.WriteAllTextAsync(Path.Combine(runtimeDirectory, "config", "appsettings.json"), "{}");

        var files = store.ListDeploymentFiles(key, runtimeDirectory);

        var file = Assert.Single(files);
        Assert.Equal(Path.Combine("config", "appsettings.json"), file.RelativePath);
        Assert.Equal("appsettings.json", file.Name);
    }

    /// <summary>
    /// <para>zh-cn:验证部署目录扫描可以发现磁盘副本，并且删除操作只删除指定副本目录。</para>
    /// <para>en-us:Verifies that deployment-directory scanning finds disk replicas and that removal deletes only the requested replica directory.</para>
    /// </summary>
    [Fact]
    public async Task Deployment_directory_inventory_should_scan_and_remove_one_replica()
    {
        using var workspace = DynamicAppTestWorkspace.Create();
        var store = workspace.CreatePackageStore();
        var request = CreateSaveRequest("security-platform", "testmod", "1.0.0");
        await using var package = CreatePackageStream("air.cloud.dynamicapp.testmod.dll");
        await store.SaveAsync(request, package);

        var blue = await store.CreateDeploymentAsync(request.Key, "blue");
        var gray = await store.CreateDeploymentAsync(request.Key, "gray");
        var inventory = store.ListDeploymentDirectories();

        Assert.Equal(2, inventory.Count);
        Assert.Contains(inventory, item => item.DeploymentId == "blue");
        Assert.Contains(inventory, item => item.DeploymentId == "gray");

        Assert.True(await store.RemoveDeploymentAsync(new DynamicAppRuntimeKey(request.Key, "blue")));
        Assert.False(Directory.Exists(blue.DeploymentDirectory));
        Assert.True(Directory.Exists(gray.DeploymentDirectory));
        Assert.False(await store.RemoveDeploymentAsync(new DynamicAppRuntimeKey(request.Key, "blue")));
    }

    /// <summary>
    /// <para>zh-cn:验证实例销毁会把运行目录迁移到归档空间，删除归档会清理归档文件，并且不会影响包空间。</para>
    /// <para>en-us:Verifies that destroying an instance moves its runtime directory into archive space, deleting the archive removes those files, and package space remains untouched.</para>
    /// </summary>
    [Fact]
    public async Task ArchiveDeployment_should_move_runtime_files_and_remove_archive_without_touching_package()
    {
        using var workspace = DynamicAppTestWorkspace.Create();
        var store = workspace.CreatePackageStore();
        var request = CreateSaveRequest("security-platform", "testmod", "1.0.0");
        await using var package = CreatePackageStream("air.cloud.dynamicapp.testmod.dll", ("appsettings.json", "{}"));
        await store.SaveAsync(request, package);

        var deployment = await store.CreateDeploymentAsync(request.Key, "blue");
        var runtimeConfig = Path.Combine(deployment.DeploymentDirectory, "content", "appsettings.json");
        await File.WriteAllTextAsync(runtimeConfig, "{\"runtime\":true}");

        var archived = await store.ArchiveDeploymentAsync(new DynamicAppRuntimeKey(request.Key, "blue"));
        Assert.True(archived.Moved);
        Assert.False(Directory.Exists(deployment.DeploymentDirectory));
        Assert.True(Directory.Exists(archived.ArchiveDirectory));
        Assert.Equal(
            "{\"runtime\":true}",
            await File.ReadAllTextAsync(Path.Combine(archived.ArchiveDirectory, "content", "appsettings.json")));
        Assert.Equal(
            "{}",
            await File.ReadAllTextAsync(Path.Combine(workspace.RootPath, "mods", "security-platform", "testmod", "1.0.0", "content", "appsettings.json")));

        Assert.True(await store.RemoveArchivedDeploymentAsync(new DynamicAppRuntimeKey(request.Key, "blue")));
        Assert.False(Directory.Exists(archived.ArchiveDirectory));
        Assert.False(await store.RemoveArchivedDeploymentAsync(new DynamicAppRuntimeKey(request.Key, "blue")));
        Assert.True(File.Exists(Path.Combine(workspace.RootPath, "mods", "security-platform", "testmod", "1.0.0", "package.zip")));
    }

    /// <summary>
    /// <para>zh-cn:验证重复创建同名 Mod 服务会失败，避免包管理空间出现不可区分的服务目录。</para>
    /// <para>en-us:Verifies that creating a Mod service with a duplicate name fails so package-management directories remain unambiguous.</para>
    /// </summary>
    [Fact]
    public void CreateMod_should_reject_duplicate_service_name()
    {
        using var workspace = DynamicAppTestWorkspace.Create();
        var store = workspace.CreatePackageStore();

        store.CreateMod("security-platform", "workflow");

        var exception = Assert.Throws<InvalidOperationException>(() => store.CreateMod("security-platform", "workflow"));
        Assert.Contains("already exists", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// <para>zh-cn:验证只有完全为空的 Mod 服务才允许删除，包版本、运行目录或归档目录存在时必须拒绝。</para>
    /// <para>en-us:Verifies that only an entirely empty Mod service can be deleted, while package versions, runtime directories, or archive directories cause rejection.</para>
    /// </summary>
    [Fact]
    public async Task RemoveMod_should_require_package_runtime_and_archive_spaces_to_be_empty()
    {
        using var workspace = DynamicAppTestWorkspace.Create();
        var store = workspace.CreatePackageStore();
        var request = CreateSaveRequest("security-platform", "workflow", "1.0.0");
        store.CreateMod("security-platform", "workflow");
        await using var package = CreatePackageStream("air.cloud.dynamicapp.testmod.dll");
        await store.SaveAsync(request, package);

        var packageException = Assert.Throws<InvalidOperationException>(() => store.RemoveMod("security-platform", "workflow"));
        Assert.Contains("packages, deployments, or archived files", packageException.Message, StringComparison.OrdinalIgnoreCase);

        Assert.True(await store.RemoveDeploymentAsync(new DynamicAppRuntimeKey(request.Key, "blue")) == false);
        var deployment = await store.CreateDeploymentAsync(request.Key, "blue");
        var deploymentException = Assert.Throws<InvalidOperationException>(() => store.RemoveMod("security-platform", "workflow"));
        Assert.Contains("packages, deployments, or archived files", deploymentException.Message, StringComparison.OrdinalIgnoreCase);

        await store.ArchiveDeploymentAsync(new DynamicAppRuntimeKey(request.Key, "blue"));
        Assert.True(await store.RemoveArchivedDeploymentAsync(new DynamicAppRuntimeKey(request.Key, "blue")));
        var archiveException = Assert.Throws<InvalidOperationException>(() => store.RemoveMod("security-platform", "workflow"));
        Assert.Contains("packages, deployments, or archived files", archiveException.Message, StringComparison.OrdinalIgnoreCase);

        var packageRoot = Path.Combine(workspace.RootPath, "mods", "security-platform", "workflow", "1.0.0");
        Directory.Delete(packageRoot, recursive: true);
        Assert.True(store.RemoveMod("security-platform", "workflow"));
        Assert.False(Directory.Exists(Path.Combine(workspace.RootPath, "mods", "security-platform", "workflow")));
        Assert.False(Directory.Exists(deployment.DeploymentDirectory));
    }

    /// <summary>
    /// <para>zh-cn:验证包目标路径片段不能包含目录穿越内容，避免上传包写出受控包根目录。</para>
    /// <para>en-us:Verifies that package target path segments cannot contain traversal content, preventing uploaded packages from escaping the controlled package root.</para>
    /// </summary>
    [Fact]
    public void CreateApplication_should_reject_invalid_path_segments()
    {
        using var workspace = DynamicAppTestWorkspace.Create();
        var store = workspace.CreatePackageStore();

        var exception = Assert.Throws<ArgumentException>(() => store.CreateApplication("../security-platform"));

        Assert.Contains("DynamicApp package path segment is invalid.", exception.Message, StringComparison.Ordinal);
    }

    private static DynamicAppModPackageSaveRequest CreateSaveRequest(string dynamicAppId, string modId, string version) => new()
    {
        Key = new DynamicModVersionKey(new DynamicAppId(dynamicAppId), new DynamicModId(modId), new DynamicAppVersion(version)),
        OriginalFileName = "air-cloud-dynamicapp-testmod.zip",
        EntryFile = "air.cloud.dynamicapp.testmod.dll"
    };

    private static MemoryStream CreatePackageStream(string entryFile, params (string Name, string Content)[] additionalFiles)
    {
        var stream = new MemoryStream();
        using (var archive = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true))
        {
            var entry = archive.CreateEntry(entryFile);
            using (var writer = new StreamWriter(entry.Open()))
            {
                writer.Write("test package payload");
            }
            foreach (var additionalFile in additionalFiles)
            {
                var extra = archive.CreateEntry(additionalFile.Name);
                using var extraWriter = new StreamWriter(extra.Open());
                extraWriter.Write(additionalFile.Content);
            }
        }

        stream.Position = 0;
        return stream;
    }
}

/// <summary>
/// <para>zh-cn:DynamicApp 测试临时工作目录，负责创建隔离包根目录并在测试结束时清理。</para>
/// <para>en-us:Temporary DynamicApp test workspace that creates an isolated package root and cleans it after the test finishes.</para>
/// </summary>
internal sealed class DynamicAppTestWorkspace : IDisposable
{
    private DynamicAppTestWorkspace(string rootPath)
    {
        RootPath = rootPath;
    }

    /// <summary>
    /// <para>zh-cn:测试使用的临时根目录。</para>
    /// <para>en-us:Temporary root directory used by the test.</para>
    /// </summary>
    public string RootPath { get; }

    /// <summary>
    /// <para>zh-cn:当前测试工作区对应的文件型包存储实例。</para>
    /// <para>en-us:The file-based package-store instance backed by the current test workspace.</para>
    /// </summary>
    public FileDynamicAppModPackageStore PackageStore => CreatePackageStore();

    /// <summary>
    /// <para>zh-cn:当前测试工作区使用的内存 Manifest 存储实例。</para>
    /// <para>en-us:The in-memory Manifest store used by the current test workspace.</para>
    /// </summary>
    public InMemoryDynamicAppManifestStore ManifestStore { get; } = new();

    /// <summary>
    /// <para>zh-cn:创建新的测试工作目录，目录名使用随机值避免并发测试冲突。</para>
    /// <para>en-us:Creates a new test workspace using a random directory name to avoid conflicts between parallel tests.</para>
    /// </summary>
    /// <returns>
    /// <para>zh-cn:可释放的测试工作目录。</para>
    /// <para>en-us:A disposable test workspace.</para>
    /// </returns>
    public static DynamicAppTestWorkspace Create()
    {
        var root = Path.Combine(Path.GetTempPath(), "air-cloud-dynamicapp-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        return new DynamicAppTestWorkspace(root);
    }

    /// <summary>
    /// <para>zh-cn:基于当前临时目录创建文件型包存储。</para>
    /// <para>en-us:Creates a file-based package store backed by the current temporary directory.</para>
    /// </summary>
    /// <param name="allowOverwrite">
    /// <para>zh-cn:是否允许覆盖已存在的同版本包目录。</para>
    /// <para>en-us:Whether overwriting an existing package directory with the same version is allowed.</para>
    /// </param>
    /// <returns>
    /// <para>zh-cn:文件型包存储实例。</para>
    /// <para>en-us:A file-based package-store instance.</para>
    /// </returns>
    public FileDynamicAppModPackageStore CreatePackageStore(bool allowOverwrite = true) => new(new DynamicAppPackageStoreOptions
    {
        RootPath = RootPath,
        AllowOverwrite = allowOverwrite
    });

    /// <summary>
    /// <para>zh-cn:删除测试临时目录；若文件已被外部占用则忽略清理异常，避免掩盖测试断言结果。</para>
    /// <para>en-us:Deletes the temporary test directory; cleanup exceptions are ignored when files are externally locked so assertion results are not hidden.</para>
    /// </summary>
    public void Dispose()
    {
        try
        {
            if (Directory.Exists(RootPath))
            {
                Directory.Delete(RootPath, recursive: true);
            }
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }
}
