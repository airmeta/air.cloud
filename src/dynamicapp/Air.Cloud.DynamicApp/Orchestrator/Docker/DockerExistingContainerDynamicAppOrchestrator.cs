using Air.Cloud.Core.Standard.Container;
using Air.Cloud.Core.Standard.Container.Model;
using Air.Cloud.DynamicApp.Contracts.Lifecycle;
using Air.Cloud.DynamicApp.Contracts.Manifest;
using Air.Cloud.DynamicApp.Contracts.Runtime;

namespace Air.Cloud.DynamicApp.Orchestrator;

/// <summary>
/// <para>zh-cn:已有容器编排器。该编排器通过 Core 容器引擎标准控制已经存在的容器或托管实例，不直接访问 Docker Socket，也不创建容器或拉取镜像。</para>
/// <para>en-us:Existing-container orchestrator. It controls already-existing containers or hosted instances through the Core container engine standard, does not access Docker sockets directly, and does not create containers or pull images.</para>
/// </summary>
public sealed class DockerExistingContainerDynamicAppOrchestrator : IDynamicAppOrchestrator, IDynamicAppExistingContainerCatalog
{
    private readonly IContainerEngineStandard containerEngine;
    private readonly IDynamicAppManifestStore manifestStore;
    private readonly DockerExistingContainerDynamicAppOrchestratorOptions options;
    private readonly IDynamicAppStateStore stateStore;

    /// <summary>
    /// <para>zh-cn:创建已有容器编排器。容器生命周期操作由注入的 <see cref="IContainerEngineStandard"/> 完成，因此 DynamicApp 不绑定具体容器平台。</para>
    /// <para>en-us:Creates the existing-container orchestrator. Container lifecycle operations are performed by the injected <see cref="IContainerEngineStandard"/>, so DynamicApp is not bound to a concrete container platform.</para>
    /// </summary>
    /// <param name="stateStore">
    /// <para>zh-cn:运行状态存储，用于在容器启动成功后登记目标和在停止成功后更新状态。</para>
    /// <para>en-us:Runtime state store used to register targets after container start and update state after stop.</para>
    /// </param>
    /// <param name="manifestStore">
    /// <para>zh-cn:Manifest 历史存储，用于运行态缺失时回查容器名称。</para>
    /// <para>en-us:Manifest history store used to look up container names when runtime state is missing.</para>
    /// </param>
    /// <param name="containerEngine">
    /// <para>zh-cn:Core 容器引擎标准实现。</para>
    /// <para>en-us:The Core container engine standard implementation.</para>
    /// </param>
    /// <param name="options">
    /// <para>zh-cn:已有容器编排选项，包含启用开关、容器名称模板和目标地址模板。</para>
    /// <para>en-us:Existing-container orchestration options including enable switch, container name template, and target address template.</para>
    /// </param>
    public DockerExistingContainerDynamicAppOrchestrator(
        IDynamicAppStateStore stateStore,
        IDynamicAppManifestStore manifestStore,
        IContainerEngineStandard containerEngine,
        DockerExistingContainerDynamicAppOrchestratorOptions options)
    {
        this.stateStore = stateStore ?? throw new ArgumentNullException(nameof(stateStore));
        this.manifestStore = manifestStore ?? throw new ArgumentNullException(nameof(manifestStore));
        this.containerEngine = containerEngine ?? throw new ArgumentNullException(nameof(containerEngine));
        this.options = options ?? throw new ArgumentNullException(nameof(options));
    }

    /// <inheritdoc />
    public async Task<DynamicAppOperationResult> DeployAsync(DynamicAppManifest manifest, DynamicModManifest mod, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(manifest);
        ArgumentNullException.ThrowIfNull(mod);

        var validation = ValidateOptions();
        if (!validation.Succeeded)
        {
            return validation;
        }

        if (mod.ExecutionMode != DynamicModExecutionMode.ExistingContainer)
        {
            return DynamicAppOperationResult.Failure($"Mod `{manifest.Id}/{mod.Id}` is not configured for ExistingContainer mode.");
        }

        var key = new DynamicModVersionKey(manifest.Id, mod.Id, mod.Version);
        var containerName = ResolveContainerName(manifest, mod);
        var start = await containerEngine.StartAsync(containerName, cancellationToken).ConfigureAwait(false);
        if (!start.Succeeded)
        {
            return DynamicAppOperationResult.Failure($"Container `{containerName}` could not be started for `{key}`. {start.Message}");
        }

        stateStore.UpsertTarget(new DynamicAppRouteTarget
        {
            Key = key,
            BaseAddress = ResolveTargetBaseAddress(manifest, mod, containerName),
            RoutePrefix = mod.Route.Prefix,
            HealthPath = mod.Health.Path,
            HealthProbeTimeoutSeconds = mod.Health.StartupTimeoutSeconds,
            State = DynamicModLifecycleState.Starting,
            HealthStatus = DynamicModHealthStatus.Starting,
            InstanceId = containerName,
            LastStatusMessage = $"Existing container `{containerName}` started for `{key}`."
        });

        return DynamicAppOperationResult.Success($"Existing container `{containerName}` started for `{key}`.");
    }

    /// <inheritdoc />
    public async Task<IReadOnlyCollection<DynamicAppExistingContainerView>> ListAsync(CancellationToken cancellationToken = default)
    {
        var validation = ValidateOptions();
        if (!validation.Succeeded)
        {
            throw new InvalidOperationException(validation.Message);
        }

        var containers = await containerEngine.ListAsync(includeStopped: true, cancellationToken).ConfigureAwait(false);
        return containers.Select(ToDynamicAppView).ToArray();
    }

    /// <inheritdoc />
    public async Task<DynamicAppExistingContainerView> InspectAsync(DynamicModVersionKey key, CancellationToken cancellationToken = default)
    {
        var validation = ValidateOptions();
        if (!validation.Succeeded)
        {
            throw new InvalidOperationException(validation.Message);
        }

        var containerName = await ResolveContainerNameAsync(key, cancellationToken).ConfigureAwait(false);
        var container = await containerEngine.InspectAsync(containerName, cancellationToken).ConfigureAwait(false);
        return ToDynamicAppView(container);
    }

    /// <inheritdoc />
    public async Task<DynamicAppOperationResult> StopAsync(DynamicModVersionKey key, CancellationToken cancellationToken = default)
    {
        var validation = ValidateOptions();
        if (!validation.Succeeded)
        {
            return validation;
        }

        var containerName = await ResolveContainerNameAsync(key, cancellationToken).ConfigureAwait(false);
        var stop = await containerEngine.StopAsync(containerName, cancellationToken).ConfigureAwait(false);
        if (!stop.Succeeded)
        {
            return DynamicAppOperationResult.Failure($"Container `{containerName}` could not be stopped for `{key}`. {stop.Message}");
        }

        stateStore.UpdateState(key, DynamicModLifecycleState.Stopped, DynamicModHealthStatus.Stopped, $"Existing container `{containerName}` stopped for `{key}`.");
        return DynamicAppOperationResult.Success($"Existing container `{containerName}` stopped for `{key}`.");
    }

    private DynamicAppOperationResult ValidateOptions()
    {
        if (!options.Enabled)
        {
            return DynamicAppOperationResult.Failure("Existing-container DynamicApp orchestrator is not enabled.");
        }

        if (string.IsNullOrWhiteSpace(options.ContainerNameTemplate))
        {
            return DynamicAppOperationResult.Failure("Container name template is not configured.");
        }

        return DynamicAppOperationResult.Success("Existing-container orchestrator options are valid.");
    }

    private Uri ResolveTargetBaseAddress(DynamicAppManifest manifest, DynamicModManifest mod, string containerName)
    {
        var value = !string.IsNullOrWhiteSpace(mod.Artifact.Endpoint)
            ? mod.Artifact.Endpoint
            : string.IsNullOrWhiteSpace(options.TargetBaseAddressTemplate)
                ? null
                : ApplyTemplate(options.TargetBaseAddressTemplate, manifest.Id.Value, mod.Id.Value, mod.Version.Value, containerName);

        return Uri.TryCreate(value, UriKind.Absolute, out var uri)
            ? uri
            : new Uri("http://localhost");
    }

    private string ResolveContainerName(DynamicAppManifest manifest, DynamicModManifest mod)
    {
        return string.IsNullOrWhiteSpace(mod.Artifact.ContainerName)
            ? ApplyTemplate(options.ContainerNameTemplate, manifest.Id.Value, mod.Id.Value, mod.Version.Value, string.Empty)
            : mod.Artifact.ContainerName.Trim();
    }

    private async Task<string> ResolveContainerNameAsync(DynamicModVersionKey key, CancellationToken cancellationToken)
    {
        var target = stateStore.ListTargets().FirstOrDefault(item => item.Key == key);
        if (!string.IsNullOrWhiteSpace(target?.InstanceId))
        {
            return target.InstanceId;
        }

        var manifestRecord = await manifestStore.FindAsync(key, cancellationToken).ConfigureAwait(false);
        var mod = manifestRecord?.Manifest.Mods.FirstOrDefault(item => item.Id == key.ModId && item.Version == key.Version);
        if (!string.IsNullOrWhiteSpace(mod?.Artifact.ContainerName))
        {
            return mod.Artifact.ContainerName.Trim();
        }

        return ApplyTemplate(options.ContainerNameTemplate, key.DynamicAppId.Value, key.ModId.Value, key.Version.Value, string.Empty);
    }

    private static DynamicAppExistingContainerView ToDynamicAppView(ContainerEngineInstance container)
    {
        return new DynamicAppExistingContainerView
        {
            Id = container.Id,
            Name = container.Name,
            Image = container.Image,
            State = container.State,
            Status = container.Status,
            DockerHealthStatus = container.HealthStatus,
            IsRunning = container.IsRunning,
            IsAvailable = container.IsAvailable
        };
    }

    private static string ApplyTemplate(string template, string dynamicAppId, string modId, string version, string containerName)
    {
        return template
            .Replace("{dynamicAppId}", dynamicAppId, StringComparison.OrdinalIgnoreCase)
            .Replace("{modId}", modId, StringComparison.OrdinalIgnoreCase)
            .Replace("{version}", version, StringComparison.OrdinalIgnoreCase)
            .Replace("{containerName}", containerName, StringComparison.OrdinalIgnoreCase);
    }
}
