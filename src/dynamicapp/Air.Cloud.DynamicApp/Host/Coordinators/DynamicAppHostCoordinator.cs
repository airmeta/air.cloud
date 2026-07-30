using System.IO;
using System.Net.Http;

using Air.Cloud.DynamicApp.Contracts.Lifecycle;
using Air.Cloud.DynamicApp.Contracts.Manifest;
using Air.Cloud.DynamicApp.Contracts.Runtime;

using Microsoft.Extensions.DependencyInjection;

namespace Air.Cloud.DynamicApp.Host;

/// <summary>
/// <para>zh-cn:默认 DynamicApp Host 协调器，串联清单发布、Mod 启动、健康探测和版本激活。</para>
/// <para>en-us:Default DynamicApp Host coordinator that connects manifest publication, Mod startup, health probing, and version activation.</para>
/// </summary>
public sealed class DynamicAppHostCoordinator : IDynamicAppHostCoordinator
{
    private readonly IDynamicAppHealthProbe _healthProbe;
    private readonly IDynamicAppManager _manager;
    private readonly IDynamicAppModRunner _modRunner;
    private readonly IDynamicAppOrchestrator _orchestrator;
    private readonly IDynamicAppProcessLogSink _processLogSink;
    private readonly IServiceProvider _serviceProvider;
    private readonly IDynamicAppStateStore _stateStore;

    /// <summary>
    /// <para>zh-cn:创建默认 Host 协调器。</para>
    /// <para>en-us:Creates the default Host coordinator.</para>
    /// </summary>
    /// <param name="manager">
    /// <para>zh-cn:Manager 控制面。</para>
    /// <para>en-us:The Manager control plane.</para>
    /// </param>
    /// <param name="modRunner">
    /// <para>zh-cn:Process 模式 Mod 运行器。</para>
    /// <para>en-us:The Process-mode Mod runner.</para>
    /// </param>
    /// <param name="orchestrator">
    /// <para>zh-cn:Container 模式编排器。</para>
    /// <para>en-us:The orchestrator for Container and ExistingContainer modes.</para>
    /// </param>
    /// <param name="healthProbe">
    /// <para>zh-cn:健康探测器。</para>
    /// <para>en-us:The health probe.</para>
    /// </param>
    /// <param name="processLogSink">
    /// <para>zh-cn:Process Mod 日志存储，用于在启动失败、运行时巡检失败或健康检查失败时把最近的 stderr/exit 日志追加到返回消息中。</para>
    /// <para>en-us:The Process Mod log sink used to append recent stderr/exit logs to returned messages when startup, runtime inspection, or health probing fails.</para>
    /// </param>
    /// <param name="stateStore">
    /// <para>zh-cn:运行状态存储。</para>
    /// <para>en-us:The runtime state store.</para>
    /// </param>
    /// <param name="serviceProvider">
    /// <para>zh-cn:服务提供器，用于按执行模式延迟解析可选的已有容器状态目录。</para>
    /// <para>en-us:The service provider used to lazily resolve the optional existing-container status catalog by execution mode.</para>
    /// </param>
    public DynamicAppHostCoordinator(
        IDynamicAppManager manager,
        IDynamicAppModRunner modRunner,
        IDynamicAppOrchestrator orchestrator,
        IDynamicAppHealthProbe healthProbe,
        IDynamicAppProcessLogSink processLogSink,
        IDynamicAppStateStore stateStore,
        IServiceProvider serviceProvider)
    {
        _manager = manager ?? throw new ArgumentNullException(nameof(manager));
        _modRunner = modRunner ?? throw new ArgumentNullException(nameof(modRunner));
        _orchestrator = orchestrator ?? throw new ArgumentNullException(nameof(orchestrator));
        _healthProbe = healthProbe ?? throw new ArgumentNullException(nameof(healthProbe));
        _processLogSink = processLogSink ?? throw new ArgumentNullException(nameof(processLogSink));
        _stateStore = stateStore ?? throw new ArgumentNullException(nameof(stateStore));
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    }

    /// <summary>
    /// <para>zh-cn:部署清单中的所有 Mod；任一 Mod 启动或健康探测失败时会标记失败并返回失败结果。</para>
    /// <para>en-us:Deploys all Mods in a manifest; if any Mod fails startup or health probing, it is marked failed and a failure result is returned.</para>
    /// </summary>
    /// <param name="manifest">
    /// <para>zh-cn:要部署的 DynamicApp 清单。</para>
    /// <para>en-us:The DynamicApp manifest to deploy.</para>
    /// </param>
    /// <param name="cancellationToken">
    /// <para>zh-cn:取消令牌。</para>
    /// <para>en-us:Cancellation token.</para>
    /// </param>
    /// <returns>
    /// <para>zh-cn:部署协调结果。</para>
    /// <para>en-us:The deployment coordination result.</para>
    /// </returns>
    public async Task<DynamicAppOperationResult> DeployAsync(DynamicAppManifest manifest, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(manifest);
        var publish = await _manager.PublishAsync(manifest, cancellationToken).ConfigureAwait(false);
        if (!publish.Succeeded)
        {
            return publish;
        }

        foreach (var mod in manifest.Mods)
        {
            var key = new DynamicModVersionKey(manifest.Id, mod.Id, mod.Version);
            var start = mod.ExecutionMode switch
            {
                DynamicModExecutionMode.Process => await _modRunner.StartAsync(manifest, mod, cancellationToken).ConfigureAwait(false),
                DynamicModExecutionMode.LocalDevelopment => ActivateLocalDevelopmentTarget(key),
                _ => await _orchestrator.DeployAsync(manifest, mod, cancellationToken).ConfigureAwait(false)
            };

            if (!start.Succeeded)
            {
                var message = await AppendProcessDiagnosticsAsync(key, start.Message, cancellationToken).ConfigureAwait(false);
                _stateStore.UpdateState(key, DynamicModLifecycleState.Failed, DynamicModHealthStatus.Unhealthy, message);
                return DynamicAppOperationResult.Failure(message);
            }

            var target = _stateStore.ListTargets().FirstOrDefault(item => item.Key == key);
            if (target == null)
            {
                _stateStore.UpdateState(key, DynamicModLifecycleState.Failed, DynamicModHealthStatus.Unhealthy, $"Runtime target `{key}` was not published after startup.");
                return DynamicAppOperationResult.Failure($"Runtime target `{key}` was not published after startup.");
            }

            var runtime = await InspectProcessRuntimeIfNeededAsync(target, mod, cancellationToken).ConfigureAwait(false);
            if (!runtime.Succeeded)
            {
                var message = await AppendProcessDiagnosticsAsync(key, $"Runtime target `{key}` startup inspection failed. {runtime.Message}", cancellationToken).ConfigureAwait(false);
                _stateStore.UpdateState(key, DynamicModLifecycleState.Failed, DynamicModHealthStatus.Unhealthy, message);
                return DynamicAppOperationResult.Failure(message);
            }

            if (mod.ExecutionMode == DynamicModExecutionMode.ExistingContainer)
            {
                var existingContainerActivation = await ActivateExistingContainerTargetAsync(key, cancellationToken).ConfigureAwait(false);
                if (!existingContainerActivation.Succeeded)
                {
                    return existingContainerActivation;
                }

                continue;
            }

            var health = await _healthProbe.ProbeAsync(target, mod.Health, cancellationToken).ConfigureAwait(false);
            if (!health.IsHealthy)
            {
                var message = await AppendProcessDiagnosticsAsync(key, $"Runtime target `{key}` health probe failed. {health.Message}", cancellationToken).ConfigureAwait(false);
                _stateStore.UpdateState(key, DynamicModLifecycleState.Failed, DynamicModHealthStatus.Unhealthy, message);
                return DynamicAppOperationResult.Failure(message);
            }

            var activation = await _manager.ActivateAsync(key, cancellationToken).ConfigureAwait(false);
            if (!activation.Succeeded)
            {
                _stateStore.UpdateState(key, DynamicModLifecycleState.Failed, DynamicModHealthStatus.Unhealthy, activation.Message);
                return activation;
            }
        }

        return DynamicAppOperationResult.Success($"DynamicApp `{manifest.Id}` deployed and activated with {manifest.Mods.Count} Mods.");
    }

    private DynamicAppOperationResult ActivateLocalDevelopmentTarget(DynamicModVersionKey key)
    {
        var result = _stateStore.UpdateState(
            key,
            DynamicModLifecycleState.Starting,
            DynamicModHealthStatus.Starting,
            $"Local development target `{key}` is registered; DynamicApp will probe the externally started service before activation.");
        return result.Succeeded
            ? DynamicAppOperationResult.Success($"Local development target `{key}` registered for external debugging.")
            : result;
    }

    /// <summary>
    /// <para>zh-cn:按执行模式检查启动后的运行态；Process 模式必须由当前 Runner 持有活进程，其他执行模式不需要该检查。</para>
    /// <para>en-us:Inspects post-start runtime by execution mode; Process mode must be owned by the current runner as a live process, while other modes do not need this check.</para>
    /// </summary>
    /// <param name="target">
    /// <para>zh-cn:启动后写入状态存储的运行目标。</para>
    /// <para>en-us:The runtime target written to state storage after startup.</para>
    /// </param>
    /// <param name="mod">
    /// <para>zh-cn:对应的 Mod Manifest，用于判断执行模式。</para>
    /// <para>en-us:The related Mod manifest used to determine execution mode.</para>
    /// </param>
    /// <param name="cancellationToken">
    /// <para>zh-cn:取消令牌；取消时返回失败结果。</para>
    /// <para>en-us:Cancellation token; cancellation returns a failed result.</para>
    /// </param>
    /// <returns>
    /// <para>zh-cn:运行态检查结果；非 Process 模式返回成功。</para>
    /// <para>en-us:The runtime inspection result; non-Process modes return success.</para>
    /// </returns>
    private async Task<DynamicAppOperationResult> InspectProcessRuntimeIfNeededAsync(DynamicAppRouteTarget target, DynamicModManifest mod, CancellationToken cancellationToken)
    {
        return mod.ExecutionMode == DynamicModExecutionMode.Process
            ? await _modRunner.InspectAsync(target.Key, cancellationToken).ConfigureAwait(false)
            : DynamicAppOperationResult.Success($"Target `{target.Key}` does not require Process runtime inspection.");
    }

    /// <summary>
    /// <para>zh-cn:按容器引擎状态激活 ExistingContainer 目标；该模式不调用业务 HTTP 健康检查端点。</para>
    /// <para>en-us:Activates an ExistingContainer target from container engine status; this mode does not call the business HTTP health endpoint.</para>
    /// </summary>
    /// <param name="key">
    /// <para>zh-cn:目标 Mod 版本键。</para>
    /// <para>en-us:The target Mod version key.</para>
    /// </param>
    /// <param name="cancellationToken">
    /// <para>zh-cn:取消令牌。</para>
    /// <para>en-us:Cancellation token.</para>
    /// </param>
    /// <returns>
    /// <para>zh-cn:容器引擎状态检查和激活结果。</para>
    /// <para>en-us:The container engine status inspection and activation result.</para>
    /// </returns>
    private async Task<DynamicAppOperationResult> ActivateExistingContainerTargetAsync(DynamicModVersionKey key, CancellationToken cancellationToken)
    {
        var catalog = _serviceProvider.GetService<IDynamicAppExistingContainerCatalog>();
        if (catalog == null)
        {
            var message = "IDynamicAppExistingContainerCatalog is not registered.";
            _stateStore.UpdateState(key, DynamicModLifecycleState.Failed, DynamicModHealthStatus.Unhealthy, message);
            return DynamicAppOperationResult.Failure(message);
        }

        DynamicAppExistingContainerView view;
        try
        {
            view = await catalog.InspectAsync(key, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception exception) when (exception is InvalidOperationException or HttpRequestException or IOException)
        {
            var message = $"Docker container status inspection failed for `{key}`. {exception.Message}";
            _stateStore.UpdateState(key, DynamicModLifecycleState.Failed, DynamicModHealthStatus.Unhealthy, message);
            return DynamicAppOperationResult.Failure(message);
        }

        if (!view.IsAvailable)
        {
            var message = $"Docker container `{view.Name}` is not available for `{key}`. {view.Status}";
            _stateStore.UpdateState(key, DynamicModLifecycleState.Failed, DynamicModHealthStatus.Unhealthy, message);
            return DynamicAppOperationResult.Failure(message);
        }

        var activation = await _manager.ActivateAsync(key, cancellationToken).ConfigureAwait(false);
        return activation.Succeeded
            ? DynamicAppOperationResult.Success($"ExistingContainer target `{key}` is active from Docker container `{view.Name}`. {view.Status}")
            : activation;
    }

    private async Task<string> AppendProcessDiagnosticsAsync(DynamicModVersionKey key, string message, CancellationToken cancellationToken)
    {
        await Task.Delay(TimeSpan.FromMilliseconds(150), cancellationToken).ConfigureAwait(false);
        var logs = await _processLogSink.QueryAsync(key, take: 80, cancellationToken).ConfigureAwait(false);
        var candidates = logs
            .Where(item => item.Stream.Equals("stderr", StringComparison.OrdinalIgnoreCase) || item.Stream.Equals("exit", StringComparison.OrdinalIgnoreCase))
            .Select(item => $"[{item.Stream}] {item.Message}")
            .Where(item => !string.IsNullOrWhiteSpace(item))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var diagnostics = candidates
            .Where(IsImportantProcessDiagnostic)
            .TakeLast(8)
            .DefaultIfEmpty()
            .ToArray();
        if (diagnostics.Length == 1 && diagnostics[0] == null)
        {
            diagnostics = candidates.TakeLast(6).ToArray();
        }

        return diagnostics.Length == 0
            ? message
            : message + " Recent process diagnostics: " + string.Join(" | ", diagnostics);
    }

    private static bool IsImportantProcessDiagnostic(string message)
    {
        return message.Contains("exception", StringComparison.OrdinalIgnoreCase)
            || message.Contains("failed to bind", StringComparison.OrdinalIgnoreCase)
            || message.Contains("address already in use", StringComparison.OrdinalIgnoreCase)
            || message.Contains("unhandled", StringComparison.OrdinalIgnoreCase)
            || message.Contains("exit code", StringComparison.OrdinalIgnoreCase);
    }
}
