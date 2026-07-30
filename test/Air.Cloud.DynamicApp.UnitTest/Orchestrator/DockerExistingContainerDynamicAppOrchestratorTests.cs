using Air.Cloud.Core.Standard.Container;
using Air.Cloud.Core.Standard.Container.Model;
using Air.Cloud.DynamicApp.Contracts.Identifiers;
using Air.Cloud.DynamicApp.Contracts.Lifecycle;
using Air.Cloud.DynamicApp.Contracts.Manifest;
using Air.Cloud.DynamicApp.Contracts.Runtime;
using Air.Cloud.DynamicApp.Orchestrator;

namespace Air.Cloud.DynamicApp.UnitTest.Orchestrator;

/// <summary>
/// <para>zh-cn:已有容器 DynamicApp 编排器单元测试，验证编排器只依赖 Core 容器引擎标准，不直接依赖 Docker Socket 配置。</para>
/// <para>en-us:Unit tests for the existing-container DynamicApp orchestrator, verifying that it only depends on the Core container engine standard and not on Docker socket configuration directly.</para>
/// </summary>
public sealed class DockerExistingContainerDynamicAppOrchestratorTests
{
    /// <summary>
    /// <para>zh-cn:验证部署 ExistingContainer 时通过 Core 容器引擎标准启动实例，并把目标地址登记到运行状态存储。</para>
    /// <para>en-us:Verifies that deploying ExistingContainer starts the instance through the Core container engine standard and registers the target address into runtime state.</para>
    /// </summary>
    [Fact]
    public async Task DeployAsync_should_start_container_through_container_engine_standard()
    {
        var stateStore = new FakeStateStore();
        var engine = new FakeContainerEngine();
        var orchestrator = CreateOrchestrator(stateStore, engine);
        var manifest = CreateManifest(endpoint: "http://air.system:8080", containerName: "air.system");
        var mod = manifest.Mods.Single();

        var result = await orchestrator.DeployAsync(manifest, mod);

        Assert.True(result.Succeeded);
        Assert.Equal("air.system", engine.StartedName);
        var target = Assert.Single(stateStore.Targets);
        Assert.Equal("air.system", target.InstanceId);
        Assert.Equal("http://air.system:8080/", target.BaseAddress.ToString());
    }

    /// <summary>
    /// <para>zh-cn:验证容器列表查询会把 Core 容器引擎实例映射为 DynamicApp 既有容器视图，保留健康状态字段兼容面板显示。</para>
    /// <para>en-us:Verifies that listing containers maps Core container engine instances to DynamicApp existing-container views and preserves the health field for panel compatibility.</para>
    /// </summary>
    [Fact]
    public async Task ListAsync_should_map_container_engine_instances_to_dynamicapp_views()
    {
        var engine = new FakeContainerEngine
        {
            Instances =
            [
                new ContainerEngineInstance
                {
                    Id = "123456789012",
                    Name = "air.account",
                    Image = "air-account:latest",
                    Runtime = "Docker",
                    State = "running",
                    Status = "running / docker-health:healthy",
                    HealthStatus = "healthy",
                    IsRunning = true,
                    IsAvailable = true
                }
            ]
        };
        var orchestrator = CreateOrchestrator(new FakeStateStore(), engine);

        var containers = await orchestrator.ListAsync();

        var container = Assert.Single(containers);
        Assert.Equal("air.account", container.Name);
        Assert.Equal("healthy", container.DockerHealthStatus);
        Assert.True(container.IsAvailable);
    }

    /// <summary>
    /// <para>zh-cn:验证停止时优先使用运行状态中的实例 ID，并通过 Core 容器引擎标准执行停止。</para>
    /// <para>en-us:Verifies that stop prefers the runtime state's instance id and executes stop through the Core container engine standard.</para>
    /// </summary>
    [Fact]
    public async Task StopAsync_should_stop_runtime_instance_through_container_engine_standard()
    {
        var stateStore = new FakeStateStore();
        var engine = new FakeContainerEngine();
        var orchestrator = CreateOrchestrator(stateStore, engine);
        var manifest = CreateManifest(endpoint: "http://air.system:8080", containerName: "air.system");
        var key = new DynamicModVersionKey(manifest.Id, manifest.Mods.Single().Id, manifest.Mods.Single().Version);
        stateStore.UpsertTarget(new DynamicAppRouteTarget
        {
            Key = key,
            BaseAddress = new Uri("http://air.system:8080"),
            InstanceId = "air.system",
            State = DynamicModLifecycleState.Active,
            HealthStatus = DynamicModHealthStatus.Healthy
        });

        var result = await orchestrator.StopAsync(key);

        Assert.True(result.Succeeded);
        Assert.Equal("air.system", engine.StoppedName);
        Assert.Equal(DynamicModLifecycleState.Stopped, stateStore.Targets.Single().State);
    }

    private static DockerExistingContainerDynamicAppOrchestrator CreateOrchestrator(FakeStateStore stateStore, FakeContainerEngine engine)
    {
        return new DockerExistingContainerDynamicAppOrchestrator(
            stateStore,
            new FakeManifestStore(),
            engine,
            new DockerExistingContainerDynamicAppOrchestratorOptions
            {
                Enabled = true,
                ContainerNameTemplate = "air.{modId}",
                TargetBaseAddressTemplate = "http://{containerName}:8080"
            });
    }

    private static DynamicAppManifest CreateManifest(string endpoint, string containerName) => new()
    {
        Id = new DynamicAppId("security-platform"),
        Version = new DynamicAppVersion("101-current"),
        Mods =
        [
            new DynamicModManifest
            {
                Id = new DynamicModId("system"),
                Version = new DynamicAppVersion("101-current"),
                ExecutionMode = DynamicModExecutionMode.ExistingContainer,
                Artifact = new DynamicModArtifactManifest
                {
                    Endpoint = endpoint,
                    ContainerName = containerName
                },
                Route = new DynamicModRouteManifest { Prefix = "/mods/system" },
                Health = new DynamicModHealthManifest { Path = "/Health", StartupTimeoutSeconds = 10 }
            }
        ]
    };

    private sealed class FakeContainerEngine : IContainerEngineStandard
    {
        public IReadOnlyCollection<ContainerEngineInstance> Instances { get; set; } = Array.Empty<ContainerEngineInstance>();

        public string? StartedName { get; private set; }

        public string? StoppedName { get; private set; }

        public Task<IReadOnlyCollection<ContainerEngineInstance>> ListAsync(bool includeStopped = true, CancellationToken cancellationToken = default)
            => Task.FromResult(Instances);

        public Task<ContainerEngineInstance> InspectAsync(string containerNameOrId, CancellationToken cancellationToken = default)
            => Task.FromResult(Instances.First(item => item.Name == containerNameOrId || item.Id == containerNameOrId));

        public Task<ContainerEngineOperationResult> StartAsync(string containerNameOrId, CancellationToken cancellationToken = default)
        {
            StartedName = containerNameOrId;
            return Task.FromResult(ContainerEngineOperationResult.Success("started"));
        }

        public Task<ContainerEngineOperationResult> StopAsync(string containerNameOrId, CancellationToken cancellationToken = default)
        {
            StoppedName = containerNameOrId;
            return Task.FromResult(ContainerEngineOperationResult.Success("stopped"));
        }
    }

    private sealed class FakeStateStore : IDynamicAppStateStore
    {
        private readonly List<DynamicAppRouteTarget> targets = new();

        public IReadOnlyCollection<DynamicAppRouteTarget> Targets => targets;

        public void UpsertTarget(DynamicAppRouteTarget target)
        {
            targets.RemoveAll(item => item.Key == target.Key);
            targets.Add(target);
        }

        public DynamicAppOperationResult Activate(DynamicModVersionKey key) => DynamicAppOperationResult.Success("active");

        public DynamicAppOperationResult UpdateState(DynamicModVersionKey key, DynamicModLifecycleState state, DynamicModHealthStatus healthStatus, string? message = null)
        {
            var target = targets.Single(item => item.Key == key);
            target.State = state;
            target.HealthStatus = healthStatus;
            target.LastStatusMessage = message ?? string.Empty;
            return DynamicAppOperationResult.Success("updated");
        }

        public DynamicAppOperationResult RemoveTarget(DynamicModVersionKey key)
        {
            targets.RemoveAll(item => item.Key == key);
            return DynamicAppOperationResult.Success("removed");
        }

        public bool TryGetActiveTarget(DynamicAppId dynamicAppId, DynamicModId modId, out DynamicAppRouteTarget? target)
        {
            target = targets.FirstOrDefault(item => item.Key.DynamicAppId == dynamicAppId && item.Key.ModId == modId);
            return target != null;
        }

        public IReadOnlyCollection<DynamicAppRouteTarget> ListTargets() => targets.ToArray();
    }

    private sealed class FakeManifestStore : IDynamicAppManifestStore
    {
        public Task<DynamicAppManifestRecord> SaveAsync(DynamicAppManifest manifest, CancellationToken cancellationToken = default)
            => Task.FromResult(new DynamicAppManifestRecord { Manifest = manifest, SavedAtUtc = DateTimeOffset.UtcNow });

        public Task<IReadOnlyCollection<DynamicAppManifestRecord>> ListAllAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyCollection<DynamicAppManifestRecord>>(Array.Empty<DynamicAppManifestRecord>());

        public Task<IReadOnlyCollection<DynamicAppManifestRecord>> ListAsync(DynamicAppId dynamicAppId, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyCollection<DynamicAppManifestRecord>>(Array.Empty<DynamicAppManifestRecord>());

        public Task<DynamicAppManifestRecord?> FindAsync(DynamicModVersionKey key, CancellationToken cancellationToken = default)
            => Task.FromResult<DynamicAppManifestRecord?>(null);
    }
}
