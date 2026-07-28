using Air.Cloud.DynamicApp.Contracts.Lifecycle;
using Air.Cloud.DynamicApp.Contracts.Manifest;
using Air.Cloud.DynamicApp.Contracts.Runtime;

using Microsoft.Extensions.Hosting;

namespace Air.Cloud.DynamicApp.Host;

/// <summary>
/// <para>zh-cn:DynamicApp Process 模式自愈后台服务，负责在子进程异常退出、活进程缺失或目标进入 Failed/Unhealthy 后按 Manifest 自动重启并重新激活。</para>
/// <para>en-us:DynamicApp Process-mode self-healing background service that restarts and reactivates targets from manifest history after unexpected child-process exits, missing live processes, or Failed/Unhealthy states.</para>
/// </summary>
public sealed class DynamicAppProcessSelfHealingHostedService : BackgroundService
{
    private readonly IDynamicAppHealthProbe _healthProbe;
    private readonly IDynamicAppManager _manager;
    private readonly IDynamicAppManifestStore _manifestStore;
    private readonly IDynamicAppModRunner _modRunner;
    private readonly DynamicAppSelfHealingOptions _options;
    private readonly object _attemptSync = new();
    private readonly IDynamicAppSelfHealingStateStore _selfHealingStateStore;
    private readonly IDynamicAppStateStore _stateStore;
    private readonly IDynamicAppTargetArchiveStore _targetArchiveStore;

    /// <summary>
    /// <para>zh-cn:创建 Process 模式自愈后台服务。</para>
    /// <para>en-us:Creates the Process-mode self-healing background service.</para>
    /// </summary>
    /// <param name="stateStore">
    /// <para>zh-cn:运行状态存储，用于读取候选目标并写回自愈结果。</para>
    /// <para>en-us:Runtime state store used to read candidate targets and write self-healing results.</para>
    /// </param>
    /// <param name="manifestStore">
    /// <para>zh-cn:Manifest 历史存储，用于按目标版本查找可重启的 Process Mod 定义。</para>
    /// <para>en-us:Manifest history store used to find the restartable Process Mod definition by target version.</para>
    /// </param>
    /// <param name="modRunner">
    /// <para>zh-cn:Process 模式 Mod 运行器，用于检查和重启子进程。</para>
    /// <para>en-us:Process-mode Mod runner used to inspect and restart child processes.</para>
    /// </param>
    /// <param name="healthProbe">
    /// <para>zh-cn:健康探测器，用于确认重启后的目标是否可以接收流量。</para>
    /// <para>en-us:Health probe used to confirm the restarted target can receive traffic.</para>
    /// </param>
    /// <param name="manager">
    /// <para>zh-cn:Manager 控制面，用于自愈成功后重新激活目标版本。</para>
    /// <para>en-us:Manager control plane used to reactivate the target version after successful self-healing.</para>
    /// </param>
    /// <param name="selfHealingStateStore">
    /// <para>zh-cn:自愈状态存储，用于在 Host 重启后保留重试计数、退避时间和失败阶段。</para>
    /// <para>en-us:Self-healing state store used to keep retry counters, backoff timestamps, and failure stages after Host restarts.</para>
    /// </param>
    /// <param name="targetArchiveStore">
    /// <para>zh-cn:目标归档存储，用于跳过已经被人工隔离的坏包或错误 Manifest 版本。</para>
    /// <para>en-us:Target archive store used to skip bad-package or invalid-manifest versions that have been manually isolated.</para>
    /// </param>
    /// <param name="options">
    /// <para>zh-cn:自愈选项，用于控制扫描间隔、退避和最大重试次数。</para>
    /// <para>en-us:Self-healing options controlling scan interval, backoff, and maximum retry attempts.</para>
    /// </param>
    public DynamicAppProcessSelfHealingHostedService(
        IDynamicAppStateStore stateStore,
        IDynamicAppManifestStore manifestStore,
        IDynamicAppModRunner modRunner,
        IDynamicAppHealthProbe healthProbe,
        IDynamicAppManager manager,
        IDynamicAppSelfHealingStateStore selfHealingStateStore,
        IDynamicAppTargetArchiveStore targetArchiveStore,
        DynamicAppSelfHealingOptions options)
    {
        _stateStore = stateStore ?? throw new ArgumentNullException(nameof(stateStore));
        _manifestStore = manifestStore ?? throw new ArgumentNullException(nameof(manifestStore));
        _modRunner = modRunner ?? throw new ArgumentNullException(nameof(modRunner));
        _healthProbe = healthProbe ?? throw new ArgumentNullException(nameof(healthProbe));
        _manager = manager ?? throw new ArgumentNullException(nameof(manager));
        _selfHealingStateStore = selfHealingStateStore ?? throw new ArgumentNullException(nameof(selfHealingStateStore));
        _targetArchiveStore = targetArchiveStore ?? throw new ArgumentNullException(nameof(targetArchiveStore));
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    /// <summary>
    /// <para>zh-cn:按配置间隔扫描需要自愈的 Process 目标；人工 Stopped、Draining 和已归档目标不会被自动重启。</para>
    /// <para>en-us:Scans Process targets requiring self-healing on the configured interval; manually Stopped, Draining, and archived targets are not restarted automatically.</para>
    /// </summary>
    /// <param name="stoppingToken">
    /// <para>zh-cn:主机停止令牌。</para>
    /// <para>en-us:Host stopping token.</para>
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
            await HealTargetsAsync(stoppingToken).ConfigureAwait(false);
        }
    }

    private async Task HealTargetsAsync(CancellationToken stoppingToken)
    {
        foreach (var target in _stateStore.ListTargets().Where(ShouldConsider))
        {
            if (stoppingToken.IsCancellationRequested)
            {
                return;
            }

            if (_targetArchiveStore.IsArchived(target.Key))
            {
                continue;
            }

            var record = await _manifestStore.FindAsync(target.Key, stoppingToken).ConfigureAwait(false);
            var mod = record?.Manifest.Mods.FirstOrDefault(item => item.Id == target.Key.ModId && item.Version == target.Key.Version);
            if (record == null || mod == null || mod.ExecutionMode != DynamicModExecutionMode.Process)
            {
                continue;
            }

            await HealTargetAsync(record.Manifest, mod, target, stoppingToken).ConfigureAwait(false);
        }
    }

    private async Task HealTargetAsync(DynamicAppManifest manifest, DynamicModManifest mod, DynamicAppRouteTarget target, CancellationToken stoppingToken)
    {
        var runtime = await _modRunner.InspectAsync(target.Key, stoppingToken).ConfigureAwait(false);
        if (runtime.Succeeded)
        {
            ClearAttemptIfHealthy(target);
            return;
        }

        if (!ShouldRestart(target))
        {
            return;
        }

        if (!TryReserveAttempt(target, runtime.Message, out var skipMessage, out var attemptState))
        {
            if (!string.IsNullOrWhiteSpace(skipMessage))
            {
                _stateStore.UpdateState(target.Key, DynamicModLifecycleState.Failed, DynamicModHealthStatus.Unhealthy, skipMessage);
            }

            return;
        }

        var start = await _modRunner.StartAsync(manifest, mod, stoppingToken).ConfigureAwait(false);
        if (!start.Succeeded)
        {
            var failureMessage = BuildFailureMessage(target.Key, "restart", start.Message, attemptState.AttemptCount, attemptState.MaxAttempts);
            MarkAttemptFailure(attemptState, "restart", failureMessage);
            _stateStore.UpdateState(target.Key, DynamicModLifecycleState.Failed, DynamicModHealthStatus.Unhealthy, failureMessage);
            return;
        }

        var restartedTarget = _stateStore.ListTargets().FirstOrDefault(item => item.Key == target.Key) ?? target;
        var health = await _healthProbe.ProbeAsync(restartedTarget, mod.Health, stoppingToken).ConfigureAwait(false);
        if (!health.IsHealthy)
        {
            var failureMessage = BuildFailureMessage(target.Key, "health probe", health.Message, attemptState.AttemptCount, attemptState.MaxAttempts);
            MarkAttemptFailure(attemptState, "health probe", failureMessage);
            _stateStore.UpdateState(target.Key, DynamicModLifecycleState.Failed, DynamicModHealthStatus.Unhealthy, failureMessage);
            return;
        }

        var activation = await _manager.ActivateAsync(target.Key, stoppingToken).ConfigureAwait(false);
        if (activation.Succeeded)
        {
            _selfHealingStateStore.Remove(target.Key);
            return;
        }

        var activationFailure = BuildFailureMessage(target.Key, "activation", activation.Message, attemptState.AttemptCount, attemptState.MaxAttempts);
        MarkAttemptFailure(attemptState, "activation", activationFailure);
        _stateStore.UpdateState(target.Key, DynamicModLifecycleState.Failed, DynamicModHealthStatus.Unhealthy, activationFailure);
    }

    private void ClearAttemptIfHealthy(DynamicAppRouteTarget target)
    {
        if (_options.ResetAttemptsAfterHealthy
            && target.State == DynamicModLifecycleState.Active
            && target.HealthStatus == DynamicModHealthStatus.Healthy)
        {
            _selfHealingStateStore.Remove(target.Key);
        }
    }

    private bool TryReserveAttempt(DynamicAppRouteTarget target, string runtimeMessage, out string? skipMessage, out DynamicAppSelfHealingState attemptState)
    {
        skipMessage = null;
        var now = DateTimeOffset.UtcNow;
        var maxAttempts = Math.Max(1, _options.MaxRestartAttempts);
        var window = NormalizeWindow(_options.AttemptWindow);
        var backoff = NormalizeBackoff(_options.RestartBackoff);

        lock (_attemptSync)
        {
            var state = _selfHealingStateStore.Get(target.Key);
            if (state == null || now >= state.WindowResetAtUtc)
            {
                attemptState = CreateAttemptState(target.Key, runtimeMessage, now, maxAttempts, window, backoff);
                _selfHealingStateStore.Upsert(attemptState);
                return true;
            }

            state.MaxAttempts = maxAttempts;
            state.WindowResetAtUtc = state.FirstAttemptUtc + window;
            if (state.IsPaused && now < state.WindowResetAtUtc)
            {
                skipMessage = $"Self-healing restart skipped for `{target.Key}` because the restart attempt limit `{maxAttempts}` was reached. The counting window resets at `{state.WindowResetAtUtc:O}`.";
                state.LastFailureStage = "retry limit";
                state.LastFailureMessage = skipMessage;
                state.UpdatedAtUtc = now;
                attemptState = state;
                _selfHealingStateStore.Upsert(state);
                return false;
            }

            if (state.NextRetryAtUtc.HasValue && now < state.NextRetryAtUtc.Value)
            {
                skipMessage = $"Self-healing restart skipped for `{target.Key}` because the restart backoff is active until `{state.NextRetryAtUtc.Value:O}`.";
                state.LastFailureStage = "backoff";
                state.LastFailureMessage = skipMessage;
                state.UpdatedAtUtc = now;
                attemptState = state;
                _selfHealingStateStore.Upsert(state);
                return false;
            }

            if (state.AttemptCount >= maxAttempts)
            {
                skipMessage = $"Self-healing restart skipped for `{target.Key}` because the restart attempt limit `{maxAttempts}` was reached in `{window}`.";
                state.IsPaused = true;
                state.LastFailureStage = "retry limit";
                state.LastFailureMessage = skipMessage;
                state.UpdatedAtUtc = now;
                attemptState = state;
                _selfHealingStateStore.Upsert(state);
                return false;
            }

            state.AttemptCount++;
            state.LastAttemptUtc = now;
            state.NextRetryAtUtc = now + backoff;
            state.IsPaused = false;
            state.LastFailureStage = "runtime inspection";
            state.LastFailureMessage = runtimeMessage;
            state.UpdatedAtUtc = now;
            attemptState = state;
            _selfHealingStateStore.Upsert(state);
            return true;
        }
    }

    private static DynamicAppSelfHealingState CreateAttemptState(DynamicModVersionKey key, string runtimeMessage, DateTimeOffset now, int maxAttempts, TimeSpan window, TimeSpan backoff) => new()
    {
        Key = key,
        AttemptCount = 1,
        MaxAttempts = maxAttempts,
        FirstAttemptUtc = now,
        LastAttemptUtc = now,
        NextRetryAtUtc = now + backoff,
        WindowResetAtUtc = now + window,
        IsPaused = false,
        LastFailureStage = "runtime inspection",
        LastFailureMessage = runtimeMessage,
        UpdatedAtUtc = now
    };

    private void MarkAttemptFailure(DynamicAppSelfHealingState state, string stage, string message)
    {
        state.LastFailureStage = stage;
        state.LastFailureMessage = message;
        state.IsPaused = state.AttemptCount >= state.MaxAttempts;
        state.UpdatedAtUtc = DateTimeOffset.UtcNow;
        _selfHealingStateStore.Upsert(state);
    }

    private static string BuildFailureMessage(DynamicModVersionKey key, string stage, string message, int attemptCount, int maxAttempts)
    {
        var limitMessage = attemptCount >= maxAttempts ? $" Restart attempt limit `{maxAttempts}` reached; automatic self-healing is paused until the attempt window resets or the target becomes healthy." : string.Empty;
        return $"Self-healing {stage} failed for `{key}` on attempt `{attemptCount}/{maxAttempts}`. {message}{limitMessage}";
    }

    private static bool ShouldConsider(DynamicAppRouteTarget target)
    {
        return target.State is not DynamicModLifecycleState.Stopped and not DynamicModLifecycleState.Draining;
    }

    private static bool ShouldRestart(DynamicAppRouteTarget target)
    {
        return target.State is DynamicModLifecycleState.Active or DynamicModLifecycleState.Starting or DynamicModLifecycleState.Failed
            || target.HealthStatus is DynamicModHealthStatus.Unhealthy or DynamicModHealthStatus.Starting;
    }

    private static TimeSpan NormalizeInterval(TimeSpan interval) => interval <= TimeSpan.Zero ? TimeSpan.FromSeconds(5) : interval;

    private static TimeSpan NormalizeBackoff(TimeSpan backoff) => backoff < TimeSpan.Zero ? TimeSpan.Zero : backoff;

    private static TimeSpan NormalizeWindow(TimeSpan window) => window <= TimeSpan.Zero ? TimeSpan.FromMinutes(5) : window;
}
