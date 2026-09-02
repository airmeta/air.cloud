using Air.Cloud.DynamicApp.Contracts.Identifiers;
using Air.Cloud.DynamicApp.Contracts.Lifecycle;
using Air.Cloud.DynamicApp.Contracts.Manifest;
using Air.Cloud.DynamicApp.Contracts.Runtime;
using Air.Cloud.DynamicApp.Host;
using Air.Cloud.DynamicApp.Manager;

using Microsoft.Extensions.DependencyInjection;

namespace Air.Cloud.DynamicApp.UnitTest.Host;

/// <summary>
/// <para>zh-cn:验证 Process 目标在启动瞬时探活失败后能够从存活进程恢复为健康状态。</para>
/// <para>en-us:Verifies that Process targets recover to healthy state after a transient startup probe failure when their process remains alive.</para>
/// </summary>
public sealed class DynamicAppHealthRecoveryTests
{
    /// <summary>
    /// <para>zh-cn:验证健康监控拿到实例锁前人工停止目标时，不会继续探测或把停止状态恢复为活动状态。</para>
    /// <para>en-us:Verifies that health monitoring skips probing when a target is manually stopped before its instance lock is released.</para>
    /// </summary>
    [Fact]
    public async Task Health_monitor_should_skip_stale_candidate_stopped_while_waiting_for_lock()
    {
        var key = new DynamicModVersionKey(new DynamicAppId("app"), new DynamicModId("workflow"), new DynamicAppVersion("1.0.0"));
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

        var coordinator = new GateRuntimeOperationCoordinator();
        var probe = new CountingProbe();
        var monitor = new DynamicAppHealthMonitorHostedService(
            stateStore,
            probe,
            manifestStore,
            new LiveRunner(),
            new DynamicAppHealthMonitorOptions
            {
                ProbeInterval = TimeSpan.FromMilliseconds(1),
                SuccessThreshold = 1,
                FailureThreshold = 1
            },
            new ServiceCollection().BuildServiceProvider(),
            coordinator);

        await monitor.StartAsync(CancellationToken.None);
        await coordinator.Requested.Task.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.True(stateStore.UpdateState(runtimeKey, DynamicModLifecycleState.Stopped, DynamicModHealthStatus.Stopped).Succeeded);
        coordinator.Release();
        await Task.Delay(50);
        await monitor.StopAsync(CancellationToken.None);

        var target = Assert.Single(stateStore.ListTargets());
        Assert.Equal(DynamicModLifecycleState.Stopped, target.State);
        Assert.Equal(DynamicModHealthStatus.Stopped, target.HealthStatus);
        Assert.Equal(0, probe.CallCount);
    }

    [Fact]
    public async Task Health_monitor_should_recover_failed_target_when_runner_process_is_alive()
    {
        var key = new DynamicModVersionKey(new DynamicAppId("app"), new DynamicModId("auth"), new DynamicAppVersion("1.0.0"));
        var stateStore = new InMemoryDynamicAppStateStore();
        stateStore.UpsertTarget(new DynamicAppRouteTarget
        {
            Key = key,
            DeploymentId = "replica-1",
            BaseAddress = new Uri("http://127.0.0.1:5119"),
            HealthPath = "/Health",
            State = DynamicModLifecycleState.Failed,
            HealthStatus = DynamicModHealthStatus.Unhealthy,
            InstanceId = "159"
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
                    Artifact = new DynamicModArtifactManifest { File = "auth.dll", DeploymentId = "replica-1" },
                    Health = new DynamicModHealthManifest { Path = "/Health", StartupTimeoutSeconds = 1 }
                }
            ]
        });

        var services = new ServiceCollection().BuildServiceProvider();
        var monitor = new DynamicAppHealthMonitorHostedService(
            stateStore,
            new HealthyProbe(),
            manifestStore,
            new LiveRunner(),
            new DynamicAppHealthMonitorOptions
            {
                ProbeInterval = TimeSpan.FromSeconds(1),
                SuccessThreshold = 1,
                FailureThreshold = 1
            },
            services,
            new DynamicAppRuntimeOperationCoordinator());

        await monitor.StartAsync(CancellationToken.None);
        await Task.Delay(TimeSpan.FromMilliseconds(1300));
        await monitor.StopAsync(CancellationToken.None);

        var target = Assert.Single(stateStore.ListTargets());
        Assert.Equal(DynamicModLifecycleState.Active, target.State);
        Assert.Equal(DynamicModHealthStatus.Healthy, target.HealthStatus);
    }

    /// <summary>
    /// <para>zh-cn:验证 Container 目标恢复时不调用只负责 Process 的 Runner。</para>
    /// <para>en-us:Verifies that recovering a Container target does not call the Runner, which only owns Process targets.</para>
    /// </summary>
    [Fact]
    public async Task Health_monitor_should_recover_container_target_without_process_runner()
    {
        var key = new DynamicModVersionKey(new DynamicAppId("app"), new DynamicModId("container"), new DynamicAppVersion("1.0.0"));
        var stateStore = new InMemoryDynamicAppStateStore();
        stateStore.UpsertTarget(new DynamicAppRouteTarget
        {
            Key = key,
            BaseAddress = new Uri("http://127.0.0.1:5129"),
            HealthPath = "/Health",
            State = DynamicModLifecycleState.Failed,
            HealthStatus = DynamicModHealthStatus.Unhealthy
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
                    ExecutionMode = DynamicModExecutionMode.Container,
                    Artifact = new DynamicModArtifactManifest { Image = "test/image" },
                    Health = new DynamicModHealthManifest { Path = "/Health", StartupTimeoutSeconds = 1 }
                }
            ]
        });

        var monitor = new DynamicAppHealthMonitorHostedService(
            stateStore,
            new HealthyProbe(),
            manifestStore,
            new RunnerThatMustNotBeCalled(),
            new DynamicAppHealthMonitorOptions
            {
                ProbeInterval = TimeSpan.FromSeconds(1),
                SuccessThreshold = 1,
                FailureThreshold = 1
            },
            new ServiceCollection().BuildServiceProvider(),
            new DynamicAppRuntimeOperationCoordinator());

        await monitor.StartAsync(CancellationToken.None);
        await Task.Delay(TimeSpan.FromMilliseconds(1300));
        await monitor.StopAsync(CancellationToken.None);

        var target = Assert.Single(stateStore.ListTargets());
        Assert.Equal(DynamicModLifecycleState.Active, target.State);
        Assert.Equal(DynamicModHealthStatus.Healthy, target.HealthStatus);
    }

    private sealed class HealthyProbe : IDynamicAppHealthProbe
    {
        public Task<DynamicAppHealthProbeResult> ProbeAsync(DynamicAppRouteTarget target, DynamicModHealthManifest health, CancellationToken cancellationToken = default)
            => Task.FromResult(DynamicAppHealthProbeResult.Healthy("healthy"));
    }

    private sealed class CountingProbe : IDynamicAppHealthProbe
    {
        public int CallCount { get; private set; }

        public Task<DynamicAppHealthProbeResult> ProbeAsync(DynamicAppRouteTarget target, DynamicModHealthManifest health, CancellationToken cancellationToken = default)
        {
            CallCount++;
            return Task.FromResult(DynamicAppHealthProbeResult.Healthy("healthy"));
        }
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

    private sealed class LiveRunner : IDynamicAppModRunner
    {
        public Task<DynamicAppOperationResult> StartAsync(DynamicAppManifest manifest, DynamicModManifest mod, CancellationToken cancellationToken = default)
            => Task.FromResult(DynamicAppOperationResult.Success("already running"));

        public Task<DynamicAppOperationResult> InspectAsync(DynamicModVersionKey key, CancellationToken cancellationToken = default)
            => Task.FromResult(DynamicAppOperationResult.Success("alive"));

        public Task<DynamicAppOperationResult> InspectAsync(DynamicAppRuntimeKey key, CancellationToken cancellationToken = default)
            => Task.FromResult(DynamicAppOperationResult.Success("alive"));

        public Task<DynamicAppOperationResult> StopAsync(DynamicModVersionKey key, CancellationToken cancellationToken = default)
            => Task.FromResult(DynamicAppOperationResult.Success("stopped"));
    }

    private sealed class RunnerThatMustNotBeCalled : IDynamicAppModRunner
    {
        public Task<DynamicAppOperationResult> StartAsync(DynamicAppManifest manifest, DynamicModManifest mod, CancellationToken cancellationToken = default)
            => Task.FromResult(DynamicAppOperationResult.Failure("not used"));

        public Task<DynamicAppOperationResult> InspectAsync(DynamicModVersionKey key, CancellationToken cancellationToken = default)
            => throw new InvalidOperationException("Container recovery must not inspect through Process Runner.");

        public Task<DynamicAppOperationResult> StopAsync(DynamicModVersionKey key, CancellationToken cancellationToken = default)
            => Task.FromResult(DynamicAppOperationResult.Success("not used"));
    }
}
