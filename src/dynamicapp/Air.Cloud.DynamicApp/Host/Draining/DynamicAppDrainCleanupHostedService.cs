using Air.Cloud.DynamicApp.Contracts.Lifecycle;
using Air.Cloud.DynamicApp.Contracts.Manifest;
using Air.Cloud.DynamicApp.Contracts.Runtime;

using Microsoft.Extensions.Hosting;

namespace Air.Cloud.DynamicApp.Host;

/// <summary>
/// <para>zh-cn:DynamicApp 排空清理后台服务，负责在排空超时后停止并移除旧目标。</para>
/// <para>en-us:DynamicApp draining cleanup background service that stops and removes old targets after the draining timeout expires.</para>
/// </summary>
public sealed class DynamicAppDrainCleanupHostedService : BackgroundService
{
    private readonly IDynamicAppOrchestrator _orchestrator;
    private readonly IDynamicAppManifestStore? _manifestStore;
    private readonly IDynamicAppModRunner _runner;
    private readonly DynamicAppDrainOptions _options;
    private readonly IDynamicAppStateStore _stateStore;
    private readonly IDynamicAppRuntimeOperationCoordinator _operationCoordinator;

    /// <summary>
    /// <para>zh-cn:创建排空清理后台服务。</para>
    /// <para>en-us:Creates the draining cleanup background service.</para>
    /// </summary>
    /// <param name="stateStore">
    /// <para>zh-cn:运行状态存储。</para>
    /// <para>en-us:The runtime state store.</para>
    /// </param>
    /// <param name="runner">
    /// <para>zh-cn:Mod 运行器，用于停止旧实例。</para>
    /// <para>en-us:Mod runner used to stop old instances.</para>
    /// </param>
    /// <param name="orchestrator">
    /// <para>zh-cn:容器编排器，用于停止非当前进程运行器持有的容器目标。</para>
    /// <para>en-us:Container orchestrator used to stop container targets not owned by the current process runner.</para>
    /// </param>
    /// <param name="options">
    /// <para>zh-cn:排空清理选项。</para>
    /// <para>en-us:Draining cleanup options.</para>
    /// </param>
    /// <param name="operationCoordinator">
    /// <para>zh-cn:运行实例操作协调器，用于避免排空清理与其他生命周期操作交叉执行。</para>
    /// <para>en-us:Runtime operation coordinator used to prevent draining cleanup from interleaving with other lifecycle operations.</para>
    /// </param>
    /// <param name="manifestStore">
    /// <para>zh-cn:可选 Manifest 存储，用于判断 ExistingContainer 目标并选择正确的停止边界。</para>
    /// <para>en-us:Optional manifest store used to identify ExistingContainer targets and choose the correct stop boundary.</para>
    /// </param>
    public DynamicAppDrainCleanupHostedService(
        IDynamicAppStateStore stateStore,
        IDynamicAppModRunner runner,
        IDynamicAppOrchestrator orchestrator,
        DynamicAppDrainOptions options,
        IDynamicAppRuntimeOperationCoordinator operationCoordinator,
        IDynamicAppManifestStore? manifestStore = null)
    {
        _stateStore = stateStore ?? throw new ArgumentNullException(nameof(stateStore));
        _runner = runner ?? throw new ArgumentNullException(nameof(runner));
        _orchestrator = orchestrator ?? throw new ArgumentNullException(nameof(orchestrator));
        _manifestStore = manifestStore;
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _operationCoordinator = operationCoordinator ?? throw new ArgumentNullException(nameof(operationCoordinator));
    }

    /// <summary>
    /// <para>zh-cn:按配置间隔扫描 Draining 目标，达到排空超时后执行停止和状态移除。</para>
    /// <para>en-us:Scans Draining targets on the configured interval and stops/removes them after the draining timeout is reached.</para>
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

        using var timer = new PeriodicTimer(NormalizeInterval(_options.ScanInterval));
        while (await timer.WaitForNextTickAsync(stoppingToken).ConfigureAwait(false))
        {
            await CleanupAsync(stoppingToken).ConfigureAwait(false);
        }
    }

    private async Task CleanupAsync(CancellationToken stoppingToken)
    {
        var now = DateTimeOffset.UtcNow;
        foreach (var target in _stateStore.ListTargets().Where(target => target.State == DynamicModLifecycleState.Draining))
        {
            if (stoppingToken.IsCancellationRequested)
            {
                return;
            }

            var drainingSince = target.DrainingSinceUtc ?? target.UpdatedAtUtc;
            if (now - drainingSince < NormalizeTimeout(_options.DrainTimeout))
            {
                continue;
            }

            var runtimeKey = ToRuntimeKey(target);
            await using var operation = await _operationCoordinator
                .AcquireAsync(runtimeKey, stoppingToken)
                .ConfigureAwait(false);
            // The drain list is only a candidate snapshot. A manual restart or
            // activation may have changed the target while this operation waited.
            var currentTarget = _stateStore.ListTargets()
                .FirstOrDefault(item => ToRuntimeKey(item) == runtimeKey);
            if (currentTarget == null || currentTarget.State != DynamicModLifecycleState.Draining)
            {
                continue;
            }

            var currentDrainingSince = currentTarget.DrainingSinceUtc ?? currentTarget.UpdatedAtUtc;
            if (DateTimeOffset.UtcNow - currentDrainingSince < NormalizeTimeout(_options.DrainTimeout))
            {
                continue;
            }

            var mod = _manifestStore == null
                ? null
                : (await _manifestStore.FindAsync(currentTarget.Key, stoppingToken).ConfigureAwait(false))?.Manifest.Mods
                    .FirstOrDefault(item => item.Id == currentTarget.Key.ModId && item.Version == currentTarget.Key.Version);
            var stopResult = mod?.ExecutionMode == DynamicModExecutionMode.ExistingContainer
                ? await _orchestrator.StopAsync(runtimeKey, stoppingToken).ConfigureAwait(false)
                : await _runner.StopAsync(runtimeKey, stoppingToken).ConfigureAwait(false);
            if (!stopResult.Succeeded && mod?.ExecutionMode != DynamicModExecutionMode.ExistingContainer)
            {
                stopResult = await _orchestrator.StopAsync(runtimeKey, stoppingToken).ConfigureAwait(false);
            }

            if (stopResult.Succeeded)
            {
                var removed = _stateStore.RemoveTarget(runtimeKey);
                if (!removed.Succeeded)
                {
                    _stateStore.UpdateState(
                        runtimeKey,
                        DynamicModLifecycleState.Draining,
                        DynamicModHealthStatus.Unhealthy,
                        $"Draining target `{runtimeKey}` stopped but state cleanup failed: {removed.Message}");
                }
            }
            else
            {
                _stateStore.UpdateState(
                    runtimeKey,
                    DynamicModLifecycleState.Draining,
                    DynamicModHealthStatus.Unhealthy,
                    $"Draining target `{runtimeKey}` could not be removed because stop failed: {stopResult.Message}");
            }
        }
    }

    private static TimeSpan NormalizeInterval(TimeSpan interval) => interval <= TimeSpan.Zero ? TimeSpan.FromSeconds(15) : interval;

    private static TimeSpan NormalizeTimeout(TimeSpan timeout) => timeout <= TimeSpan.Zero ? TimeSpan.FromSeconds(60) : timeout;

    private static DynamicAppRuntimeKey ToRuntimeKey(DynamicAppRouteTarget target)
        => new(target.Key, string.IsNullOrWhiteSpace(target.DeploymentId) ? "default" : target.DeploymentId.Trim());
}
