using Air.Cloud.DynamicApp.Contracts.Identifiers;
using Air.Cloud.DynamicApp.Contracts.Lifecycle;
using Air.Cloud.DynamicApp.Contracts.Manifest;
using Air.Cloud.DynamicApp.Contracts.Runtime;
using Air.Cloud.DynamicApp.Host;
using Air.Cloud.DynamicApp.Manager;

namespace Air.Cloud.DynamicApp.UnitTest.Host;

/// <summary>
/// <para>zh-cn:验证排空清理只删除已确认停止的指定部署实例。</para>
/// <para>en-us:Verifies that draining cleanup removes only the specified deployment after a confirmed stop.</para>
/// </summary>
public sealed class DynamicAppDrainCleanupHostedServiceTests
{
    /// <summary>
    /// <para>zh-cn:验证 ExistingContainer 排空时直接调用容器编排器，不会因为 Runner 的幂等成功而留下占用端口的容器。</para>
    /// <para>en-us:Verifies that ExistingContainer draining calls the container orchestrator directly instead of trusting a misleading idempotent Runner success that could leave a container holding its port.</para>
    /// </summary>
    [Fact]
    public async Task Cleanup_should_stop_existing_container_through_orchestrator()
    {
        var key = new DynamicModVersionKey(new DynamicAppId("drain-container"), new DynamicModId("api"), new DynamicAppVersion("1.0.0"));
        var stateStore = new InMemoryDynamicAppStateStore();
        stateStore.UpsertTarget(new DynamicAppRouteTarget
        {
            Key = key,
            DeploymentId = "blue",
            State = DynamicModLifecycleState.Draining,
            HealthStatus = DynamicModHealthStatus.Draining,
            DrainingSinceUtc = DateTimeOffset.UtcNow.AddMinutes(-5)
        });
        var manifestStore = new InMemoryDynamicAppManifestStore();
        await manifestStore.SaveAsync(new DynamicAppManifest
        {
            Id = key.DynamicAppId,
            Version = key.Version,
            Mods =
            [
                new DynamicModManifest
                {
                    Id = key.ModId,
                    Version = key.Version,
                    ExecutionMode = DynamicModExecutionMode.ExistingContainer
                }
            ]
        });
        var orchestrator = new TrackingOrchestrator();
        using var service = new DynamicAppDrainCleanupHostedService(
            stateStore,
            new FailingRunner(),
            orchestrator,
            new DynamicAppDrainOptions { ScanInterval = TimeSpan.FromMilliseconds(10), DrainTimeout = TimeSpan.Zero },
            new DynamicAppRuntimeOperationCoordinator(),
            manifestStore);

        await service.StartAsync(CancellationToken.None);
        await Task.Delay(100);
        await service.StopAsync(CancellationToken.None);

        Assert.True(orchestrator.StopCalled);
        Assert.Empty(stateStore.ListTargets());
    }

    [Fact]
    public async Task Cleanup_should_keep_target_when_both_stop_channels_fail()
    {
        var key = new DynamicModVersionKey(new DynamicAppId("drain-test"), new DynamicModId("api"), new DynamicAppVersion("1.0.0"));
        var stateStore = new InMemoryDynamicAppStateStore();
        stateStore.UpsertTarget(new DynamicAppRouteTarget
        {
            Key = key,
            DeploymentId = "blue",
            State = DynamicModLifecycleState.Draining,
            HealthStatus = DynamicModHealthStatus.Draining,
            DrainingSinceUtc = DateTimeOffset.UtcNow.AddMinutes(-5)
        });

        using var service = new DynamicAppDrainCleanupHostedService(
            stateStore,
            new FailingRunner(),
            new FailingOrchestrator(),
            new DynamicAppDrainOptions
            {
                ScanInterval = TimeSpan.FromMilliseconds(10),
                DrainTimeout = TimeSpan.Zero
            },
            new DynamicAppRuntimeOperationCoordinator());

        await service.StartAsync(CancellationToken.None);
        await Task.Delay(100);
        await service.StopAsync(CancellationToken.None);

        var target = Assert.Single(stateStore.ListTargets());
        Assert.Equal("blue", target.DeploymentId);
        Assert.Equal(DynamicModLifecycleState.Draining, target.State);
        Assert.Equal(DynamicModHealthStatus.Unhealthy, target.HealthStatus);
    }

    private sealed class FailingRunner : IDynamicAppModRunner
    {
        public Task<DynamicAppOperationResult> StartAsync(DynamicAppManifest manifest, DynamicModManifest mod, CancellationToken cancellationToken = default)
            => Task.FromResult(DynamicAppOperationResult.Failure("start failed"));

        public Task<DynamicAppOperationResult> InspectAsync(DynamicModVersionKey key, CancellationToken cancellationToken = default)
            => Task.FromResult(DynamicAppOperationResult.Failure("inspect failed"));

        public Task<DynamicAppOperationResult> StopAsync(DynamicModVersionKey key, CancellationToken cancellationToken = default)
            => Task.FromResult(DynamicAppOperationResult.Failure("runner stop failed"));

        public Task<DynamicAppOperationResult> StopAsync(DynamicAppRuntimeKey key, CancellationToken cancellationToken = default)
            => Task.FromResult(DynamicAppOperationResult.Failure("runner stop failed"));
    }

    private sealed class FailingOrchestrator : IDynamicAppOrchestrator
    {
        public Task<DynamicAppOperationResult> DeployAsync(DynamicAppManifest manifest, DynamicModManifest mod, CancellationToken cancellationToken = default)
            => Task.FromResult(DynamicAppOperationResult.Failure("deploy failed"));

        public Task<DynamicAppOperationResult> StopAsync(DynamicModVersionKey key, CancellationToken cancellationToken = default)
            => Task.FromResult(DynamicAppOperationResult.Failure("orchestrator stop failed"));
    }

    private sealed class TrackingOrchestrator : IDynamicAppOrchestrator
    {
        public bool StopCalled { get; private set; }
        public Task<DynamicAppOperationResult> DeployAsync(DynamicAppManifest manifest, DynamicModManifest mod, CancellationToken cancellationToken = default)
            => Task.FromResult(DynamicAppOperationResult.Success("deployed"));

        public Task<DynamicAppOperationResult> StopAsync(DynamicModVersionKey key, CancellationToken cancellationToken = default)
        {
            StopCalled = true;
            return Task.FromResult(DynamicAppOperationResult.Success("container stopped"));
        }
    }
}
