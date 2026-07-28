using Air.Cloud.DynamicApp.Contracts.Lifecycle;
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
    private readonly IDynamicAppModRunner _runner;
    private readonly DynamicAppDrainOptions _options;
    private readonly IDynamicAppStateStore _stateStore;

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
    public DynamicAppDrainCleanupHostedService(IDynamicAppStateStore stateStore, IDynamicAppModRunner runner, IDynamicAppOrchestrator orchestrator, DynamicAppDrainOptions options)
    {
        _stateStore = stateStore ?? throw new ArgumentNullException(nameof(stateStore));
        _runner = runner ?? throw new ArgumentNullException(nameof(runner));
        _orchestrator = orchestrator ?? throw new ArgumentNullException(nameof(orchestrator));
        _options = options ?? throw new ArgumentNullException(nameof(options));
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

            var stopResult = await _runner.StopAsync(target.Key, stoppingToken).ConfigureAwait(false);
            if (!stopResult.Succeeded)
            {
                stopResult = await _orchestrator.StopAsync(target.Key, stoppingToken).ConfigureAwait(false);
            }

            if (stopResult.Succeeded || _options.RemoveTargetWhenStopFails)
            {
                _stateStore.RemoveTarget(target.Key);
            }
        }
    }

    private static TimeSpan NormalizeInterval(TimeSpan interval) => interval <= TimeSpan.Zero ? TimeSpan.FromSeconds(15) : interval;

    private static TimeSpan NormalizeTimeout(TimeSpan timeout) => timeout <= TimeSpan.Zero ? TimeSpan.FromSeconds(60) : timeout;
}
