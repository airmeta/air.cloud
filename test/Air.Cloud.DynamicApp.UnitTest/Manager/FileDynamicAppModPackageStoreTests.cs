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

    private static MemoryStream CreatePackageStream(string entryFile)
    {
        var stream = new MemoryStream();
        using (var archive = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true))
        {
            var entry = archive.CreateEntry(entryFile);
            using var writer = new StreamWriter(entry.Open());
            writer.Write("test package payload");
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
