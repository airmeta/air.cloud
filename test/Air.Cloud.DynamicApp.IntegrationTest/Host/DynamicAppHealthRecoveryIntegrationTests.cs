using Air.Cloud.DynamicApp.Contracts.Identifiers;
using Air.Cloud.DynamicApp.Contracts.Lifecycle;
using Air.Cloud.DynamicApp.Contracts.Manifest;
using Air.Cloud.DynamicApp.Contracts.Runtime;
using Air.Cloud.DynamicApp.Host;
using Air.Cloud.DynamicApp.Manager;

using Microsoft.Extensions.DependencyInjection;

namespace Air.Cloud.DynamicApp.IntegrationTest.Host;

/// <summary>
/// <para>zh-cn:验证 Process 健康恢复逻辑在集成服务注册下不会把存活实例继续留在失败状态。</para>
/// <para>en-us:Integration coverage for Process health recovery, ensuring a live instance is not left failed after service registration.</para>
/// </summary>
public sealed class DynamicAppHealthRecoveryIntegrationTests
{
    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Module", "DynamicApp")]
    public async Task Registered_health_monitor_should_reconcile_live_failed_target()
    {
        var key = new DynamicModVersionKey(new DynamicAppId("integration-app"), new DynamicModId("auth"), new DynamicAppVersion("1.0.0"));
        var stateStore = new InMemoryDynamicAppStateStore();
        stateStore.UpsertTarget(new DynamicAppRouteTarget
        {
            Key = key,
            DeploymentId = "replica-1",
            BaseAddress = new Uri("http://127.0.0.1:5119"),
            HealthPath = "/Health",
            State = DynamicModLifecycleState.Failed,
            HealthStatus = DynamicModHealthStatus.Unhealthy,
            InstanceId = "integration-process"
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

        var services = new ServiceCollection()
            .AddSingleton<IDynamicAppStateStore>(stateStore)
            .AddSingleton<IDynamicAppManifestStore>(manifestStore)
            .AddSingleton<IDynamicAppHealthProbe, HealthyProbe>()
            .AddSingleton<IDynamicAppModRunner, LiveRunner>()
            .BuildServiceProvider();
        var monitor = new DynamicAppHealthMonitorHostedService(
            stateStore,
            services.GetRequiredService<IDynamicAppHealthProbe>(),
            manifestStore,
            services.GetRequiredService<IDynamicAppModRunner>(),
            new DynamicAppHealthMonitorOptions { ProbeInterval = TimeSpan.FromSeconds(1), SuccessThreshold = 1, FailureThreshold = 1 },
            services,
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
}
