using Air.Cloud.DynamicApp.Contracts.Identifiers;
using Air.Cloud.DynamicApp.Contracts.Lifecycle;
using Air.Cloud.DynamicApp.Contracts.Manifest;
using Air.Cloud.DynamicApp.Contracts.Runtime;
using Air.Cloud.DynamicApp.Host;
using Air.Cloud.DynamicApp.Manager;

namespace Air.Cloud.DynamicApp.IntegrationTest.Host;

/// <summary>
/// <para>zh-cn:验证自愈服务在实例锁等待期间读取到人工停止状态后不会重新启动目标。</para>
/// <para>en-us:Verifies that self-healing does not restart a target when it becomes manually stopped while waiting for the instance lock.</para>
/// </summary>
public sealed class DynamicAppSelfHealingCoordinationIntegrationTests
{
    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Module", "DynamicApp")]
    public async Task Self_healing_should_skip_stale_candidate_stopped_while_waiting_for_lock()
    {
        var root = Path.Combine(Path.GetTempPath(), $"air-cloud-self-healing-{Guid.NewGuid():N}");
        Directory.CreateDirectory(root);
        var key = new DynamicModVersionKey(new DynamicAppId("integration-app"), new DynamicModId("workflow"), new DynamicAppVersion("1.0.0"));
        var runtimeKey = new DynamicAppRuntimeKey(key, "default");
        var stateStore = new InMemoryDynamicAppStateStore();
        stateStore.UpsertTarget(new DynamicAppRouteTarget
        {
            Key = key,
            BaseAddress = new Uri("http://127.0.0.1:5112"),
            State = DynamicModLifecycleState.Active,
            HealthStatus = DynamicModHealthStatus.Healthy
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
                    ExecutionMode = DynamicModExecutionMode.Process,
                    Artifact = new DynamicModArtifactManifest { File = "workflow.dll" }
                }
            ]
        });

        var runner = new CountingRunner();
        var coordinator = new GateRuntimeOperationCoordinator();
        var service = new DynamicAppProcessSelfHealingHostedService(
            stateStore,
            manifestStore,
            runner,
            new HealthyProbe(),
            new NoopManager(),
            new FileDynamicAppSelfHealingStateStore(new FileDynamicAppSelfHealingStateStoreOptions
            {
                FilePath = Path.Combine(root, "self-healing.json")
            }),
            new FileDynamicAppTargetArchiveStore(new FileDynamicAppTargetArchiveStoreOptions
            {
                FilePath = Path.Combine(root, "archives.json")
            }),
            new DynamicAppSelfHealingOptions
            {
                ScanInterval = TimeSpan.FromMilliseconds(1),
                RestartBackoff = TimeSpan.Zero
            },
            coordinator);

        try
        {
            await service.StartAsync(CancellationToken.None);
            await coordinator.Requested.Task.WaitAsync(TimeSpan.FromSeconds(5));
            Assert.True(stateStore.UpdateState(runtimeKey, DynamicModLifecycleState.Stopped, DynamicModHealthStatus.Stopped).Succeeded);
            coordinator.Release();
            await Task.Delay(50);
            await service.StopAsync(CancellationToken.None);

            var target = Assert.Single(stateStore.ListTargets());
            Assert.Equal(DynamicModLifecycleState.Stopped, target.State);
            Assert.Equal(DynamicModHealthStatus.Stopped, target.HealthStatus);
            Assert.Equal(0, runner.InspectCount);
            Assert.Equal(0, runner.StartCount);
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }

    private sealed class HealthyProbe : IDynamicAppHealthProbe
    {
        public Task<DynamicAppHealthProbeResult> ProbeAsync(DynamicAppRouteTarget target, DynamicModHealthManifest health, CancellationToken cancellationToken = default)
            => Task.FromResult(DynamicAppHealthProbeResult.Healthy("healthy"));
    }

    private sealed class CountingRunner : IDynamicAppModRunner
    {
        public int InspectCount { get; private set; }
        public int StartCount { get; private set; }

        public Task<DynamicAppOperationResult> StartAsync(DynamicAppManifest manifest, DynamicModManifest mod, CancellationToken cancellationToken = default)
        {
            StartCount++;
            return Task.FromResult(DynamicAppOperationResult.Success("started"));
        }

        public Task<DynamicAppOperationResult> InspectAsync(DynamicModVersionKey key, CancellationToken cancellationToken = default)
        {
            InspectCount++;
            return Task.FromResult(DynamicAppOperationResult.Failure("not running"));
        }

        public Task<DynamicAppOperationResult> InspectAsync(DynamicAppRuntimeKey key, CancellationToken cancellationToken = default)
        {
            InspectCount++;
            return Task.FromResult(DynamicAppOperationResult.Failure("not running"));
        }

        public Task<DynamicAppOperationResult> StopAsync(DynamicModVersionKey key, CancellationToken cancellationToken = default)
            => Task.FromResult(DynamicAppOperationResult.Success("stopped"));

        public Task<DynamicAppOperationResult> StopAsync(DynamicAppRuntimeKey key, CancellationToken cancellationToken = default)
            => Task.FromResult(DynamicAppOperationResult.Success("stopped"));
    }

    private sealed class NoopManager : IDynamicAppManager
    {
        public Task<DynamicAppOperationResult> PublishAsync(DynamicAppManifest manifest, CancellationToken cancellationToken = default)
            => Task.FromResult(DynamicAppOperationResult.Success("published"));

        public Task<DynamicAppOperationResult> ActivateAsync(DynamicModVersionKey key, CancellationToken cancellationToken = default)
            => Task.FromResult(DynamicAppOperationResult.Success("activated"));

        public Task<DynamicAppOperationResult> RollbackAsync(DynamicAppId dynamicAppId, DynamicModId modId, CancellationToken cancellationToken = default)
            => Task.FromResult(DynamicAppOperationResult.Success("rolled back"));
    }

    private sealed class GateRuntimeOperationCoordinator : IDynamicAppRuntimeOperationCoordinator
    {
        private readonly TaskCompletionSource<bool> _release = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public TaskCompletionSource<bool> Requested { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public async Task<IAsyncDisposable> AcquireAsync(DynamicAppRuntimeKey key, CancellationToken cancellationToken = default)
        {
            Requested.TrySetResult(true);
            await _release.Task.WaitAsync(cancellationToken);
            return NoopHandle.Instance;
        }

        public void Release() => _release.TrySetResult(true);

        public Task<IAsyncDisposable> AcquirePackageAsync(DynamicModVersionKey key, CancellationToken cancellationToken = default)
            => Task.FromResult<IAsyncDisposable>(NoopHandle.Instance);

        private sealed class NoopHandle : IAsyncDisposable
        {
            public static NoopHandle Instance { get; } = new();

            public ValueTask DisposeAsync() => ValueTask.CompletedTask;
        }
    }
}
