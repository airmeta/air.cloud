using System.Collections.Concurrent;
using System.IO;
using System.Net.Http;

using Air.Cloud.DynamicApp.Contracts.Lifecycle;
using Air.Cloud.DynamicApp.Contracts.Manifest;
using Air.Cloud.DynamicApp.Contracts.Runtime;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Air.Cloud.DynamicApp.Host;

/// <summary>
/// <para>zh-cn:DynamicApp 后台健康监控服务，持续探测可运行目标并更新状态存储。</para>
/// <para>en-us:DynamicApp background health monitor that continuously probes runnable targets and updates the state store.</para>
/// </summary>
public sealed class DynamicAppHealthMonitorHostedService : BackgroundService
{
    private readonly ConcurrentDictionary<string, int> _failureCounts = new(StringComparer.OrdinalIgnoreCase);
    private readonly IDynamicAppHealthProbe _healthProbe;
    private readonly IDynamicAppManifestStore _manifestStore;
    private readonly IDynamicAppModRunner _modRunner;
    private readonly DynamicAppHealthMonitorOptions _options;
    private readonly IServiceProvider _serviceProvider;
    private readonly ConcurrentDictionary<string, int> _successCounts = new(StringComparer.OrdinalIgnoreCase);
    private readonly IDynamicAppStateStore _stateStore;
    private readonly IDynamicAppRuntimeOperationCoordinator _operationCoordinator;

    /// <summary>
    /// <para>zh-cn:创建后台健康监控服务。</para>
    /// <para>en-us:Creates the background health monitor service.</para>
    /// </summary>
    /// <param name="stateStore">
    /// <para>zh-cn:运行状态存储。</para>
    /// <para>en-us:The runtime state store.</para>
    /// </param>
    /// <param name="healthProbe">
    /// <para>zh-cn:健康探测器。</para>
    /// <para>en-us:The health probe.</para>
    /// </param>
    /// <param name="manifestStore">
    /// <para>zh-cn:Manifest 历史存储，用于识别目标执行模式并决定是否跳过 HTTP 探活。</para>
    /// <para>en-us:The manifest history store used to identify target execution mode and decide whether HTTP probing should be skipped.</para>
    /// </param>
    /// <param name="modRunner">
    /// <para>zh-cn:Process 模式运行器，用于在健康探测成功后确认目标仍由当前 Host 持有。</para>
    /// <para>en-us:The Process-mode runner used to confirm that a target is still owned by the current Host after a successful health probe.</para>
    /// </param>
    /// <param name="options">
    /// <para>zh-cn:后台健康监控选项。</para>
    /// <para>en-us:The background health monitor options.</para>
    /// </param>
    /// <param name="serviceProvider">
    /// <para>zh-cn:服务提供器，用于延迟解析可选的已有容器状态目录。</para>
    /// <para>en-us:The service provider used to lazily resolve the optional existing-container status catalog.</para>
    /// </param>
    /// <param name="operationCoordinator">
    /// <para>zh-cn:运行实例操作协调器，用于避免健康检查与生命周期操作交叉执行。</para>
    /// <para>en-us:Runtime operation coordinator used to prevent health checks from interleaving with lifecycle operations.</para>
    /// </param>
    public DynamicAppHealthMonitorHostedService(
        IDynamicAppStateStore stateStore,
        IDynamicAppHealthProbe healthProbe,
        IDynamicAppManifestStore manifestStore,
        IDynamicAppModRunner modRunner,
        DynamicAppHealthMonitorOptions options,
        IServiceProvider serviceProvider,
        IDynamicAppRuntimeOperationCoordinator operationCoordinator)
    {
        _stateStore = stateStore ?? throw new ArgumentNullException(nameof(stateStore));
        _healthProbe = healthProbe ?? throw new ArgumentNullException(nameof(healthProbe));
        _manifestStore = manifestStore ?? throw new ArgumentNullException(nameof(manifestStore));
        _modRunner = modRunner ?? throw new ArgumentNullException(nameof(modRunner));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _operationCoordinator = operationCoordinator ?? throw new ArgumentNullException(nameof(operationCoordinator));
    }

    /// <summary>
    /// <para>zh-cn:按照配置间隔持续探测 Active、Starting 和 Unhealthy 目标；服务停止时退出循环。</para>
    /// <para>en-us:Continuously probes Active, Starting, and Unhealthy targets on the configured interval; exits when the service stops.</para>
    /// </summary>
    /// <param name="stoppingToken">
    /// <para>zh-cn:主机停止令牌。</para>
    /// <para>en-us:The host stopping token.</para>
    /// </param>
    /// <returns>
    /// <para>zh-cn:后台执行任务。</para>
    /// <para>en-us:The background execution task.</para>
    /// </returns>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Enabled)
        {
            return;
        }

        using var timer = new PeriodicTimer(NormalizeInterval(_options.ProbeInterval));
        while (await timer.WaitForNextTickAsync(stoppingToken).ConfigureAwait(false))
        {
            await ProbeTargetsAsync(stoppingToken).ConfigureAwait(false);
        }
    }

    private async Task ProbeTargetsAsync(CancellationToken stoppingToken)
    {
        foreach (var candidate in _stateStore.ListTargets().Where(ShouldProbe))
        {
            if (stoppingToken.IsCancellationRequested)
            {
                return;
            }

            var runtimeKey = ToRuntimeKey(candidate);
            await using var operation = await _operationCoordinator
                .AcquireAsync(runtimeKey, stoppingToken)
                .ConfigureAwait(false);
            // The initial list is only a candidate snapshot. A manual stop may have
            // completed while this operation waited for the instance lock.
            var currentTarget = _stateStore.ListTargets()
                .FirstOrDefault(item => ToRuntimeKey(item) == runtimeKey);
            if (currentTarget == null
                || currentTarget.State is DynamicModLifecycleState.Stopped or DynamicModLifecycleState.Draining)
            {
                continue;
            }

            var key = runtimeKey.ToString();
            var manifestRecord = await _manifestStore.FindAsync(currentTarget.Key, stoppingToken).ConfigureAwait(false);
            var mod = manifestRecord?.Manifest.Mods.FirstOrDefault(item => item.Id == currentTarget.Key.ModId && item.Version == currentTarget.Key.Version);
            if (mod?.ExecutionMode == DynamicModExecutionMode.ExistingContainer)
            {
                await SyncExistingContainerStatusAsync(currentTarget, stoppingToken).ConfigureAwait(false);
                continue;
            }

            var health = new DynamicModHealthManifest
            {
                Path = string.IsNullOrWhiteSpace(currentTarget.HealthPath) ? "/health" : currentTarget.HealthPath,
                StartupTimeoutSeconds = currentTarget.HealthProbeTimeoutSeconds <= 0 ? 60 : currentTarget.HealthProbeTimeoutSeconds
            };

            var result = await _healthProbe.ProbeAsync(currentTarget, health, stoppingToken).ConfigureAwait(false);
            if (result.IsHealthy)
            {
                await RecordSuccessAsync(currentTarget, mod, key, stoppingToken).ConfigureAwait(false);
            }
            else
            {
                RecordFailure(currentTarget, key);
            }
        }
    }

    private async Task RecordSuccessAsync(
        DynamicAppRouteTarget target,
        DynamicModManifest? mod,
        string key,
        CancellationToken cancellationToken)
    {
        _failureCounts[key] = 0;
        var successes = _successCounts.AddOrUpdate(key, 1, (_, current) => current + 1);
        if (successes < Math.Max(1, _options.SuccessThreshold))
        {
            return;
        }

        if (target.State is DynamicModLifecycleState.Stopped or DynamicModLifecycleState.Draining)
        {
            return;
        }

        if (target.State == DynamicModLifecycleState.Failed || target.HealthStatus == DynamicModHealthStatus.Unhealthy)
        {
            // Only Process targets are owned by this Runner. Container and local-development
            // targets are validated by their orchestrator or HTTP probe and must not be
            // rejected because the Process Runner does not track them.
            if (mod?.ExecutionMode == DynamicModExecutionMode.Process)
            {
                var runtime = await _modRunner.InspectAsync(ToRuntimeKey(target), cancellationToken).ConfigureAwait(false);
                if (!runtime.Succeeded)
                {
                    return;
                }
            }

            _stateStore.UpdateState(ToRuntimeKey(target), DynamicModLifecycleState.Active, DynamicModHealthStatus.Healthy,
                $"Target `{ToRuntimeKey(target)}` recovered after a successful health probe.");
            return;
        }
        else if (target.State is not (DynamicModLifecycleState.Active or DynamicModLifecycleState.Starting))
        {
            return;
        }

        if (target.HealthStatus != DynamicModHealthStatus.Healthy || target.State == DynamicModLifecycleState.Failed)
        {
            _stateStore.UpdateState(ToRuntimeKey(target), DynamicModLifecycleState.Active, DynamicModHealthStatus.Healthy,
                $"Target `{ToRuntimeKey(target)}` recovered after a successful health probe.");
        }
    }

    private void RecordFailure(DynamicAppRouteTarget target, string key)
    {
        _successCounts[key] = 0;
        var failures = _failureCounts.AddOrUpdate(key, 1, (_, current) => current + 1);
        if (failures < Math.Max(1, _options.FailureThreshold))
        {
            return;
        }

        var nextState = target.State == DynamicModLifecycleState.Starting
            ? DynamicModLifecycleState.Failed
            : target.State;
        _stateStore.UpdateState(ToRuntimeKey(target), nextState, DynamicModHealthStatus.Unhealthy);
    }

    /// <summary>
    /// <para>zh-cn:同步 ExistingContainer 目标的容器引擎状态；该方法只读取容器引擎标准，不访问业务 HTTP 健康检查端点。</para>
    /// <para>en-us:Synchronizes container engine status for an ExistingContainer target; this method only reads the container engine standard and does not call the business HTTP health endpoint.</para>
    /// </summary>
    /// <param name="target">
    /// <para>zh-cn:需要同步的运行目标快照。</para>
    /// <para>en-us:The runtime target snapshot to synchronize.</para>
    /// </param>
    /// <param name="cancellationToken">
    /// <para>zh-cn:取消令牌。</para>
    /// <para>en-us:Cancellation token.</para>
    /// </param>
    /// <returns>
    /// <para>zh-cn:状态同步任务。</para>
    /// <para>en-us:The status synchronization task.</para>
    /// </returns>
    private async Task SyncExistingContainerStatusAsync(DynamicAppRouteTarget target, CancellationToken cancellationToken)
    {
        var catalog = _serviceProvider.GetService<IDynamicAppExistingContainerCatalog>();
        if (catalog == null)
        {
            _stateStore.UpdateState(ToRuntimeKey(target), DynamicModLifecycleState.Failed, DynamicModHealthStatus.Unhealthy, "IDynamicAppExistingContainerCatalog is not registered.");
            return;
        }

        DynamicAppExistingContainerView view;
        try
        {
            view = await catalog.InspectAsync(ToRuntimeKey(target), cancellationToken).ConfigureAwait(false);
        }
        catch (Exception exception) when (exception is InvalidOperationException or HttpRequestException or IOException)
        {
            _stateStore.UpdateState(ToRuntimeKey(target), DynamicModLifecycleState.Failed, DynamicModHealthStatus.Unhealthy, $"Docker container status inspection failed for `{ToRuntimeKey(target)}`. {exception.Message}");
            return;
        }

        if (view.IsAvailable)
        {
            var nextState = target.State == DynamicModLifecycleState.Failed
                ? DynamicModLifecycleState.Active
                : target.State;
            _stateStore.UpdateState(ToRuntimeKey(target), nextState, DynamicModHealthStatus.Healthy, $"Docker container `{view.Name}` is running for `{ToRuntimeKey(target)}`. {view.Status}");
            return;
        }

        _stateStore.UpdateState(ToRuntimeKey(target), DynamicModLifecycleState.Failed, DynamicModHealthStatus.Unhealthy, $"Docker container `{view.Name}` is not available for `{ToRuntimeKey(target)}`. {view.Status}");
    }

    private static bool ShouldProbe(DynamicAppRouteTarget target)
    {
        return target.State is DynamicModLifecycleState.Active or DynamicModLifecycleState.Starting
            || target.HealthStatus == DynamicModHealthStatus.Unhealthy;
    }

    private static TimeSpan NormalizeInterval(TimeSpan interval)
    {
        return interval <= TimeSpan.Zero ? TimeSpan.FromSeconds(15) : interval;
    }

    private static DynamicAppRuntimeKey ToRuntimeKey(DynamicAppRouteTarget target)
        => new(target.Key, string.IsNullOrWhiteSpace(target.DeploymentId) ? "default" : target.DeploymentId.Trim());
}
