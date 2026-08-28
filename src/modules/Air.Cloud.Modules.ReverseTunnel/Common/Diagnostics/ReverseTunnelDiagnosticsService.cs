using Air.Cloud.Modules.ReverseTunnel.Common.Options;
using Air.Cloud.Modules.ReverseTunnel.Common.Runtime;
using Air.Cloud.Modules.ReverseTunnel.Server;

namespace Air.Cloud.Modules.ReverseTunnel.Common.Diagnostics;

/// <summary>
/// <para>zh-cn:默认 AirTunnel 诊断快照读取服务，合并当前进程内指标、会话注册表和运行配置摘要。</para>
/// <para>en-us:Default AirTunnel diagnostic snapshot reader that combines current-process metrics, the session registry, and runtime configuration summary.</para>
/// </summary>
public sealed class ReverseTunnelDiagnosticsService : IReverseTunnelDiagnosticsService
{
    private readonly IReverseTunnelSessionRegistry sessionRegistry;
    private readonly IReverseTunnelRuntimeOptionsAccessor options;
    private readonly object snapshotLock = new();
    private ReverseTunnelDiagnosticsSnapshot? cachedSnapshot;

    /// <summary>
    /// <para>zh-cn:创建诊断快照读取服务。构造函数只保存依赖，不读取会话或指标，避免在宿主启动阶段产生额外副作用。</para>
    /// <para>en-us:Creates the diagnostic snapshot reader. The constructor only stores dependencies and does not read sessions or metrics, avoiding extra startup side effects.</para>
    /// </summary>
    /// <param name="sessionRegistry">
    /// <para>zh-cn:当前进程内的 AirTunnel Server 会话注册表。</para>
    /// <para>en-us:The AirTunnel Server session registry for the current process.</para>
    /// </param>
    /// <param name="options">
    /// <para>zh-cn:AirTunnel 运行配置。</para>
    /// <para>en-us:AirTunnel runtime options.</para>
    /// </param>
    public ReverseTunnelDiagnosticsService(IReverseTunnelSessionRegistry sessionRegistry, IReverseTunnelRuntimeOptionsAccessor options)
    {
        this.sessionRegistry = sessionRegistry;
        this.options = options;
    }

    /// <inheritdoc />
    public ReverseTunnelDiagnosticsSnapshot GetSnapshot()
    {
        var snapshot = cachedSnapshot;
        if (snapshot is not null)
        {
            return snapshot;
        }

        return RefreshSnapshot();
    }

    /// <summary>
    /// <para>zh-cn:立即刷新当前进程内诊断快照缓存。后台采集服务按配置周期调用该方法；首次 API 查询时如果缓存为空也会同步刷新。</para>
    /// <para>en-us:Immediately refreshes the current-process diagnostic snapshot cache. The background collector calls this method by the configured interval, and the first API read also refreshes synchronously when the cache is empty.</para>
    /// </summary>
    /// <returns>
    /// <para>zh-cn:刷新后的诊断快照。</para>
    /// <para>en-us:The refreshed diagnostic snapshot.</para>
    /// </returns>
    public ReverseTunnelDiagnosticsSnapshot RefreshSnapshot()
    {
        var generatedAtUtc = DateTimeOffset.UtcNow;
        var settings = options.Current;
        var diagnostics = ReverseTunnelDiagnosticsConfigurationReader.Read(settings);
        var snapshot = new ReverseTunnelDiagnosticsSnapshot
        {
            GeneratedAtUtc = generatedAtUtc,
            Enabled = settings.Enabled,
            TunnelMode = settings.TunnelMode.ToString(),
            EnableTunnelProxy = settings.EnableTunnelProxy,
            SessionSelectionStrategy = settings.SessionSelectionStrategy.ToString(),
            MaxConcurrentStreams = settings.MaxConcurrentStreams,
            AgentConnectionCount = settings.AgentConnectionCount,
            DiagnosticsEnabled = diagnostics.Enabled,
            SnapshotCollectionIntervalSeconds = diagnostics.SnapshotCollectionIntervalSeconds,
            Metrics = ReverseTunnelMetrics.GetSnapshot(),
            Sessions = sessionRegistry.GetSnapshot(generatedAtUtc)
        };

        lock (snapshotLock)
        {
            cachedSnapshot = snapshot;
        }

        return snapshot;
    }

    /// <inheritdoc />
    public ReverseTunnelMetricsSnapshot GetMetrics()
    {
        return GetSnapshot().Metrics;
    }

    /// <inheritdoc />
    public IReadOnlyList<ReverseTunnelSessionDiagnosticsSnapshot> GetSessions()
    {
        return GetSnapshot().Sessions;
    }
}
