using System.Text.Json;

using Air.Cloud.DynamicApp.Contracts.Identifiers;
using Air.Cloud.DynamicApp.Contracts.Manifest;
using Air.Cloud.DynamicApp.Manager;

namespace Air.Cloud.DynamicApp.UnitTest.Manager;

/// <summary>
/// <para>zh-cn:文件型 Manifest 历史存储单元测试，覆盖损坏快照容错、持久化恢复和原子写入行为。</para>
/// <para>en-us:Unit tests for the file-based Manifest history store, covering corrupt-snapshot tolerance, persistence recovery, and atomic writes.</para>
/// </summary>
public sealed class FileDynamicAppManifestStoreTests
{
    /// <summary>
    /// <para>zh-cn:验证损坏的 Manifest JSON 不会阻止存储实例创建，并且后续保存可以重建有效快照。</para>
    /// <para>en-us:Verifies that a corrupt Manifest JSON file does not prevent store construction and that a later save rebuilds a valid snapshot.</para>
    /// </summary>
    [Fact]
    public async Task Corrupt_snapshot_should_not_block_startup_or_future_save()
    {
        using var workspace = DynamicAppTestWorkspace.Create();
        var filePath = Path.Combine(workspace.RootPath, "manifests.json");
        await File.WriteAllTextAsync(filePath, "{ invalid manifest history");

        var store = new FileDynamicAppManifestStore(new FileDynamicAppManifestStoreOptions
        {
            FilePath = filePath
        });

        Assert.Empty(await store.ListAllAsync());
        await store.SaveAsync(CreateManifest("security-platform", "auth", "1.0.0"));

        using var document = JsonDocument.Parse(await File.ReadAllTextAsync(filePath));
        Assert.Single(document.RootElement.GetProperty("records").EnumerateArray());
        Assert.Empty(Directory.EnumerateFiles(workspace.RootPath, "*.tmp", SearchOption.TopDirectoryOnly));
    }

    /// <summary>
    /// <para>zh-cn:验证 Manifest 历史记录会在新的存储实例中恢复，并按应用数量限制裁剪旧记录。</para>
    /// <para>en-us:Verifies that Manifest history is restored by a new store instance and old records are trimmed according to the per-application limit.</para>
    /// </summary>
    [Fact]
    public async Task Saved_manifest_history_should_survive_store_recreation_and_trim_old_records()
    {
        using var workspace = DynamicAppTestWorkspace.Create();
        var filePath = Path.Combine(workspace.RootPath, "manifests.json");
        var options = new FileDynamicAppManifestStoreOptions
        {
            FilePath = filePath,
            MaxRecordsPerApp = 2
        };
        var first = new FileDynamicAppManifestStore(options);

        await first.SaveAsync(CreateManifest("security-platform", "auth", "1.0.0"));
        await first.SaveAsync(CreateManifest("security-platform", "auth", "1.0.1"));
        await first.SaveAsync(CreateManifest("security-platform", "auth", "1.0.2"));

        var second = new FileDynamicAppManifestStore(options);
        var records = await second.ListAsync(new DynamicAppId("security-platform"));

        Assert.Equal(2, records.Count);
        Assert.DoesNotContain(records, record => record.Manifest.Mods.Any(mod => mod.Version.Value == "1.0.0"));
        Assert.Contains(records, record => record.Manifest.Mods.Any(mod => mod.Version.Value == "1.0.2"));
    }

    private static DynamicAppManifest CreateManifest(string dynamicAppId, string modId, string version) => new()
    {
        Id = new DynamicAppId(dynamicAppId),
        Version = new DynamicAppVersion(version),
        Mods =
        [
            new DynamicModManifest
            {
                Id = new DynamicModId(modId),
                Version = new DynamicAppVersion(version)
            }
        ]
    };
}
