using Air.Cloud.DynamicApp.Contracts.Identifiers;
using Air.Cloud.DynamicApp.Contracts.Runtime;
using Air.Cloud.DynamicApp.Manager;

namespace Air.Cloud.DynamicApp.UnitTest.Manager;

/// <summary>
/// <para>zh-cn:文件型 DynamicApp 自愈状态存储测试，验证集群副本隔离和重启后的持久化。</para>
/// <para>en-us:Tests for the file-based DynamicApp self-healing state store, covering replica isolation and persistence across restarts.</para>
/// </summary>
public sealed class FileDynamicAppSelfHealingStateStoreTests
{
    /// <summary>
    /// <para>zh-cn:验证同一包版本的 blue 和 gray 副本各自保存独立的重试计数，并能从 JSON 文件重新加载。</para>
    /// <para>en-us:Verifies that blue and gray replicas of one package version keep independent retry counters and reload from the JSON file.</para>
    /// </summary>
    [Fact]
    public void Store_should_persist_self_healing_state_per_deployment()
    {
        using var workspace = DynamicAppTestWorkspace.Create();
        var filePath = Path.Combine(workspace.RootPath, "self-healing.json");
        var packageKey = new DynamicModVersionKey(
            new DynamicAppId("security-platform"),
            new DynamicModId("auth"),
            new DynamicAppVersion("1.0.0"));

        var store = new FileDynamicAppSelfHealingStateStore(new FileDynamicAppSelfHealingStateStoreOptions
        {
            FilePath = filePath,
            SaveOnChange = true
        });
        store.Upsert(CreateState(packageKey, "blue", 2));
        store.Upsert(CreateState(packageKey, "gray", 5));

        var reloaded = new FileDynamicAppSelfHealingStateStore(new FileDynamicAppSelfHealingStateStoreOptions
        {
            FilePath = filePath,
            SaveOnChange = true
        });

        Assert.Equal(2, reloaded.Get(new DynamicAppRuntimeKey(packageKey, "blue"))!.AttemptCount);
        Assert.Equal(5, reloaded.Get(new DynamicAppRuntimeKey(packageKey, "gray"))!.AttemptCount);
        Assert.True(File.Exists(filePath));
    }

    /// <summary>
    /// <para>zh-cn:验证损坏的自愈 JSON 文件会被容错读取，避免 Host 因历史文件损坏而无法启动。</para>
    /// <para>en-us:Verifies that a corrupt self-healing JSON file is tolerated so a Host does not fail to start because of a damaged historical file.</para>
    /// </summary>
    [Fact]
    public void Store_should_tolerate_corrupt_json()
    {
        using var workspace = DynamicAppTestWorkspace.Create();
        var filePath = Path.Combine(workspace.RootPath, "self-healing.json");
        File.WriteAllText(filePath, "{ invalid json");

        var store = new FileDynamicAppSelfHealingStateStore(new FileDynamicAppSelfHealingStateStoreOptions
        {
            FilePath = filePath,
            SaveOnChange = true
        });

        Assert.Empty(store.List());
    }

    private static DynamicAppSelfHealingState CreateState(DynamicModVersionKey key, string deploymentId, int attempts) => new()
    {
        Key = key,
        DeploymentId = deploymentId,
        AttemptCount = attempts,
        MaxAttempts = 5,
        FirstAttemptUtc = DateTimeOffset.UtcNow,
        LastAttemptUtc = DateTimeOffset.UtcNow,
        WindowResetAtUtc = DateTimeOffset.UtcNow.AddMinutes(5),
        UpdatedAtUtc = DateTimeOffset.UtcNow,
        LastFailureStage = "restart",
        LastFailureMessage = deploymentId
    };
}
