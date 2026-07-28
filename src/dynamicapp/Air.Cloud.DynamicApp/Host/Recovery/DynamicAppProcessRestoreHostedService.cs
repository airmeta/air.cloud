using Air.Cloud.DynamicApp.Contracts.Lifecycle;
using Air.Cloud.DynamicApp.Contracts.Manifest;
using Air.Cloud.DynamicApp.Contracts.Runtime;

using Microsoft.Extensions.Hosting;

namespace Air.Cloud.DynamicApp.Host;

/// <summary>
/// <para>zh-cn:DynamicApp Process 模式启动恢复服务，用于 Host 容器重启后重新拉起状态存储中仍标记为 Active 的本地 Mod 进程。</para>
/// <para>en-us:DynamicApp Process-mode startup recovery service that restarts local Mod processes still marked Active in the state store after the Host container restarts.</para>
/// </summary>
public sealed class DynamicAppProcessRestoreHostedService : BackgroundService
{
    private readonly IDynamicAppHealthProbe _healthProbe;
    private readonly IDynamicAppManifestStore _manifestStore;
    private readonly IDynamicAppManager _manager;
    private readonly IDynamicAppModRunner _modRunner;
    private readonly IDynamicAppStateStore _stateStore;

    /// <summary>
    /// <para>zh-cn:创建 Process 模式启动恢复服务。</para>
    /// <para>en-us:Creates the Process-mode startup recovery service.</para>
    /// </summary>
    /// <param name="stateStore">
    /// <para>zh-cn:运行状态存储，用于读取需要恢复的 Active 目标并写回恢复结果。</para>
    /// <para>en-us:Runtime state store used to read Active targets requiring recovery and write recovery results.</para>
    /// </param>
    /// <param name="manifestStore">
    /// <para>zh-cn:Manifest 历史存储，用于按目标版本反查 Process 模式的启动声明。</para>
    /// <para>en-us:Manifest history store used to resolve the Process-mode startup declaration for each target version.</para>
    /// </param>
    /// <param name="modRunner">
    /// <para>zh-cn:Process 模式 Mod 运行器，用于实际启动本地子进程。</para>
    /// <para>en-us:Process-mode Mod runner used to start local child processes.</para>
    /// </param>
    /// <param name="healthProbe">
    /// <para>zh-cn:健康探测器，用于启动后确认恢复目标是否可以接收流量。</para>
    /// <para>en-us:Health probe used to confirm recovered targets can receive traffic after startup.</para>
    /// </param>
    /// <param name="manager">
    /// <para>zh-cn:Manager 控制面，用于恢复成功后重新激活目标版本。</para>
    /// <para>en-us:Manager control plane used to reactivate a target version after successful recovery.</para>
    /// </param>
    public DynamicAppProcessRestoreHostedService(
        IDynamicAppStateStore stateStore,
        IDynamicAppManifestStore manifestStore,
        IDynamicAppModRunner modRunner,
        IDynamicAppHealthProbe healthProbe,
        IDynamicAppManager manager)
    {
        _stateStore = stateStore ?? throw new ArgumentNullException(nameof(stateStore));
        _manifestStore = manifestStore ?? throw new ArgumentNullException(nameof(manifestStore));
        _modRunner = modRunner ?? throw new ArgumentNullException(nameof(modRunner));
        _healthProbe = healthProbe ?? throw new ArgumentNullException(nameof(healthProbe));
        _manager = manager ?? throw new ArgumentNullException(nameof(manager));
    }

    /// <summary>
    /// <para>zh-cn:Host 启动时恢复本地 Process 目标；无法匹配 Manifest 或健康探测失败的目标会被标记为 Unhealthy，避免网关继续转发到无效进程。</para>
    /// <para>en-us:Restores local Process targets when the Host starts; targets without a matching manifest or with failed health probes are marked Unhealthy so the gateway does not keep forwarding to dead processes.</para>
    /// </summary>
    /// <param name="stoppingToken">
    /// <para>zh-cn:宿主启动取消令牌。</para>
    /// <para>en-us:Host startup cancellation token.</para>
    /// </param>
    /// <returns>
    /// <para>zh-cn:异步启动任务。</para>
    /// <para>en-us:The asynchronous startup task.</para>
    /// </returns>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var targets = _stateStore.ListTargets()
            .Where(target => target.State == DynamicModLifecycleState.Active && IsLocalProcessAddress(target.BaseAddress))
            .ToArray();

        foreach (var target in targets)
        {
            if (stoppingToken.IsCancellationRequested)
            {
                return;
            }

            await RestoreTargetAsync(target, stoppingToken).ConfigureAwait(false);
        }
    }

    private async Task RestoreTargetAsync(DynamicAppRouteTarget target, CancellationToken cancellationToken)
    {
        var manifest = await ResolveManifestAsync(target, cancellationToken).ConfigureAwait(false);
        var mod = manifest?.Mods.FirstOrDefault(item => item.Id == target.Key.ModId && item.Version == target.Key.Version);
        if (manifest == null || mod == null || mod.ExecutionMode != DynamicModExecutionMode.Process)
        {
            _stateStore.UpdateState(target.Key, target.State, DynamicModHealthStatus.Unhealthy, $"Process target `{target.Key}` cannot be restored because its Process manifest was not found.");
            return;
        }

        var start = await _modRunner.StartAsync(manifest, mod, cancellationToken).ConfigureAwait(false);
        if (!start.Succeeded)
        {
            _stateStore.UpdateState(target.Key, DynamicModLifecycleState.Failed, DynamicModHealthStatus.Unhealthy, start.Message);
            return;
        }

        var restoredTarget = _stateStore.ListTargets().FirstOrDefault(item => item.Key == target.Key) ?? target;
        var health = await _healthProbe.ProbeAsync(restoredTarget, mod.Health, cancellationToken).ConfigureAwait(false);
        if (!health.IsHealthy)
        {
            _stateStore.UpdateState(target.Key, DynamicModLifecycleState.Failed, DynamicModHealthStatus.Unhealthy, $"Process target `{target.Key}` startup recovery health probe failed. {health.Message}");
            return;
        }

        await _manager.ActivateAsync(target.Key, cancellationToken).ConfigureAwait(false);
    }

    private async Task<DynamicAppManifest?> ResolveManifestAsync(DynamicAppRouteTarget target, CancellationToken cancellationToken)
    {
        var records = await _manifestStore.ListAsync(target.Key.DynamicAppId, cancellationToken).ConfigureAwait(false);
        return records
            .Select(record => record.Manifest)
            .FirstOrDefault(manifest => manifest.Mods.Any(mod => mod.Id == target.Key.ModId && mod.Version == target.Key.Version));
    }

    private static bool IsLocalProcessAddress(Uri baseAddress)
    {
        return baseAddress.IsLoopback || baseAddress.Host is "0.0.0.0" or "+" or "*";
    }
}
