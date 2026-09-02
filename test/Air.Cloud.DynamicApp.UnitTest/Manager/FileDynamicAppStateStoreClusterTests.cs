using Air.Cloud.DynamicApp.Contracts.Identifiers;
using Air.Cloud.DynamicApp.Contracts.Lifecycle;
using Air.Cloud.DynamicApp.Contracts.Runtime;

using Air.Cloud.DynamicApp.Manager;

namespace Air.Cloud.DynamicApp.UnitTest.Manager;

/// <summary>
/// <para>zh-cn:文件状态存储的集群部署回归测试，验证副本持久化、恢复和故障隔离。</para>
/// <para>en-us:Regression tests for clustered deployments in the file state store, covering persistence, reload, and failure isolation.</para>
/// </summary>
public sealed class FileDynamicAppStateStoreClusterTests
{
    /// <summary>
    /// <para>zh-cn:验证同一包版本的多个部署实例可以写入 JSON，重启状态存储后仍可独立读取，并且一个副本失败不会影响另一个副本。</para>
    /// <para>en-us:Verifies that multiple deployments of one package version persist to JSON, reload independently after a state-store restart, and isolate one failed replica from another.</para>
    /// </summary>
    [Fact]
    public void Store_should_persist_and_isolate_cluster_deployments()
    {
        using var workspace = DynamicAppTestWorkspace.Create();
        var filePath = Path.Combine(workspace.RootPath, "dynamicapp-state.json");
        var packageKey = new DynamicModVersionKey(
            new DynamicAppId("security-platform"),
            new DynamicModId("auth"),
            new DynamicAppVersion("1.0.0"));

        var store = CreateStore(filePath);
        store.UpsertTarget(CreateTarget(packageKey, "blue", 5111));
        store.UpsertTarget(CreateTarget(packageKey, "gray", 5112));
        Assert.True(store.Activate(packageKey).Succeeded);

        var reloaded = CreateStore(filePath);
        Assert.Equal(2, reloaded.ListActiveTargets(packageKey.DynamicAppId, packageKey.ModId).Count);

        var grayKey = new DynamicAppRuntimeKey(packageKey, "gray");
        Assert.True(reloaded.UpdateState(
            grayKey,
            DynamicModLifecycleState.Failed,
            DynamicModHealthStatus.Unhealthy,
            "gray deployment failed.").Succeeded);

        var active = reloaded.ListActiveTargets(packageKey.DynamicAppId, packageKey.ModId);
        var blue = Assert.Single(active);
        Assert.Equal("blue", blue.DeploymentId);
        Assert.Equal("http://127.0.0.1:5111/", blue.BaseAddress.ToString());
    }

    /// <summary>
    /// <para>zh-cn:验证两个 Host 先后写入同一状态文件时，后一次写入会重载最新快照并保留前一个 Host 的目标。</para>
    /// <para>en-us:Verifies that a later write from another Host reloads the latest snapshot and preserves the first Host's target.</para>
    /// </summary>
    [Fact]
    public void Store_should_merge_latest_snapshot_before_cross_host_write()
    {
        using var workspace = DynamicAppTestWorkspace.Create();
        var filePath = Path.Combine(workspace.RootPath, "dynamicapp-state.json");
        var key = new DynamicModVersionKey(new DynamicAppId("security-platform"), new DynamicModId("auth"), new DynamicAppVersion("1.0.0"));
        var firstHost = CreateStore(filePath);
        var secondHost = CreateStore(filePath);

        firstHost.UpsertTarget(CreateTarget(key, "blue", 5111));
        secondHost.UpsertTarget(CreateTarget(key, "gray", 5112));

        Assert.Equal(new[] { "blue", "gray" }, firstHost.ListTargets().OrderBy(item => item.DeploymentId).Select(item => item.DeploymentId));
    }

    /// <summary>
    /// <para>zh-cn:验证实例级激活不会把同包版本的兄弟实例一起激活。</para>
    /// <para>en-us:Verifies that instance activation does not activate sibling deployments of the same package version.</para>
    /// </summary>
    [Fact]
    public void Store_should_activate_only_requested_deployment()
    {
        using var workspace = DynamicAppTestWorkspace.Create();
        var filePath = Path.Combine(workspace.RootPath, "dynamicapp-state.json");
        var key = new DynamicModVersionKey(new DynamicAppId("security-platform"), new DynamicModId("auth"), new DynamicAppVersion("1.0.0"));
        var store = CreateStore(filePath);
        store.UpsertTarget(CreateTarget(key, "blue", 5111));
        store.UpsertTarget(CreateTarget(key, "gray", 5112));

        Assert.True(store.ActivateInstance(new DynamicAppRuntimeKey(key, "blue")).Succeeded);

        var targets = store.ListTargets().ToDictionary(item => item.DeploymentId);
        Assert.Equal(DynamicModLifecycleState.Active, targets["blue"].State);
        Assert.Equal(DynamicModLifecycleState.Pending, targets["gray"].State);
    }

    /// <summary>
    /// <para>zh-cn:验证损坏的 JSON 状态文件不会阻止状态存储创建，并且后续写入会生成完整状态文件。</para>
    /// <para>en-us:Verifies that a corrupt JSON state file does not prevent store creation and that a later write creates a complete state file.</para>
    /// </summary>
    [Fact]
    public void Store_should_tolerate_corrupt_json_and_rewrite_atomically()
    {
        using var workspace = DynamicAppTestWorkspace.Create();
        var filePath = Path.Combine(workspace.RootPath, "dynamicapp-state.json");
        File.WriteAllText(filePath, "{ invalid json");

        var store = CreateStore(filePath);
        Assert.Empty(store.ListTargets());
        store.UpsertTarget(CreateTarget(
            new DynamicModVersionKey(new DynamicAppId("security-platform"), new DynamicModId("auth"), new DynamicAppVersion("1.0.0")),
            "blue",
            5111));

        Assert.Contains("\"targets\"", File.ReadAllText(filePath), StringComparison.OrdinalIgnoreCase);
        Assert.Empty(Directory.EnumerateFiles(workspace.RootPath, "*.tmp"));
    }

    private static FileDynamicAppStateStore CreateStore(string filePath)
        => new(new FileDynamicAppStateStoreOptions
        {
            FilePath = filePath,
            SaveOnChange = true
        });

    private static DynamicAppRouteTarget CreateTarget(DynamicModVersionKey key, string deploymentId, int port)
        => new()
        {
            Key = key,
            DeploymentId = deploymentId,
            BaseAddress = new Uri($"http://127.0.0.1:{port}"),
            HealthPath = "/Health",
            State = DynamicModLifecycleState.Pending,
            HealthStatus = DynamicModHealthStatus.Unknown
        };
}
