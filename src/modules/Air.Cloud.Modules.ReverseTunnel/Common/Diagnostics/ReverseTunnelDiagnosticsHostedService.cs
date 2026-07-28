using Air.Cloud.Modules.ReverseTunnel.Common.Options;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Air.Cloud.Modules.ReverseTunnel.Common.Diagnostics;

/// <summary>
/// <para>zh-cn:AirTunnel 诊断快照后台采集服务。该服务通过 `AppCore.Configuration` 读取诊断开关和采集周期，只在诊断开启时按周期刷新当前进程内缓存快照。</para>
/// <para>en-us:AirTunnel diagnostic snapshot background collector. The service reads the diagnostic switch and collection interval from `AppCore.Configuration` and refreshes the current-process cached snapshot only when diagnostics are enabled.</para>
/// </summary>
public sealed class ReverseTunnelDiagnosticsHostedService : BackgroundService
{
    private readonly ReverseTunnelDiagnosticsService diagnosticsService;
    private readonly IOptions<ReverseTunnelOptions> options;

    /// <summary>
    /// <para>zh-cn:创建诊断快照后台采集服务。构造阶段不启动采集，实际周期由运行时配置决定。</para>
    /// <para>en-us:Creates the diagnostic snapshot background collector. Collection is not started in the constructor; the runtime configuration decides the actual interval.</para>
    /// </summary>
    /// <param name="diagnosticsService">
    /// <para>zh-cn:负责生成并缓存诊断快照的服务。</para>
    /// <para>en-us:The service that generates and caches diagnostic snapshots.</para>
    /// </param>
    /// <param name="options">
    /// <para>zh-cn:AirTunnel 选项，作为 `AppCore.Configuration` 不可用时的回退配置。</para>
    /// <para>en-us:AirTunnel options used as fallback configuration when `AppCore.Configuration` is unavailable.</para>
    /// </param>
    public ReverseTunnelDiagnosticsHostedService(ReverseTunnelDiagnosticsService diagnosticsService, IOptions<ReverseTunnelOptions> options)
    {
        this.diagnosticsService = diagnosticsService;
        this.options = options;
    }

    /// <summary>
    /// <para>zh-cn:按 `ReverseTunnelSettings:Diagnostics:SnapshotCollectionIntervalSeconds` 周期刷新诊断快照；诊断关闭时仅低频检查配置变化。</para>
    /// <para>en-us:Refreshes diagnostic snapshots by `ReverseTunnelSettings:Diagnostics:SnapshotCollectionIntervalSeconds`; when diagnostics are disabled, it only checks configuration changes at a low frequency.</para>
    /// </summary>
    /// <param name="stoppingToken">
    /// <para>zh-cn:宿主停止令牌。</para>
    /// <para>en-us:The host shutdown token.</para>
    /// </param>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var diagnostics = ReverseTunnelDiagnosticsConfigurationReader.Read(options.Value);
            if (options.Value.Enabled && diagnostics.Enabled)
            {
                diagnosticsService.RefreshSnapshot();
            }

            var delaySeconds = diagnostics.Enabled ? diagnostics.SnapshotCollectionIntervalSeconds : Math.Max(30, diagnostics.SnapshotCollectionIntervalSeconds);
            try
            {
                await Task.Delay(TimeSpan.FromSeconds(delaySeconds), stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
        }
    }
}
