namespace Air.Cloud.Modules.ReverseTunnel.Common.Options;

/// <summary>
/// <para>zh-cn:AirTunnel 诊断 HTTP API 配置。该能力默认关闭；显式映射诊断 endpoint 后，开启该配置会暴露当前进程内的指标和会话快照，不要求当前进程一定包含 Server 角色。</para>
/// <para>en-us:AirTunnel diagnostic HTTP API options. The capability is disabled by default; after diagnostic endpoints are explicitly mapped, enabling this option exposes in-process metric and session snapshots without requiring the current process to use Server mode.</para>
/// </summary>
public sealed class ReverseTunnelDiagnosticsOptions
{
    /// <summary>
    /// <para>zh-cn:是否开放诊断 HTTP API。默认值为 false，避免公网 Gateway 默认暴露 Agent、ServiceKey 和流量状态。</para>
    /// <para>en-us:Whether to expose the diagnostic HTTP API. The default is false to avoid exposing Agent, ServiceKey, and traffic state on public Gateways by default.</para>
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// <para>zh-cn:诊断 HTTP API 的路径前缀。默认值为 /airtunnel/diagnostics，末尾斜杠会被标准化移除。</para>
    /// <para>en-us:Path prefix for diagnostic HTTP APIs. The default is /airtunnel/diagnostics, and a trailing slash is normalized away.</para>
    /// </summary>
    public string PathPrefix { get; set; } = "/airtunnel/diagnostics";

    /// <summary>
    /// <para>zh-cn:诊断快照采集周期秒数。默认值为 15 秒；后台采集器按该周期刷新缓存快照，诊断 API 返回最近一次缓存结果。</para>
    /// <para>en-us:Diagnostic snapshot collection interval in seconds. The default is 15 seconds; the background collector refreshes the cached snapshot at this interval, and diagnostic APIs return the latest cached result.</para>
    /// </summary>
    public int SnapshotCollectionIntervalSeconds { get; set; } = 15;

    /// <summary>
    /// <para>zh-cn:标准化诊断配置。空路径会回退到默认路径，非斜杠开头的路径会自动补齐前导斜杠。</para>
    /// <para>en-us:Normalizes diagnostic options. An empty path falls back to the default, and paths without a leading slash receive one automatically.</para>
    /// </summary>
    public void Normalize()
    {
        PathPrefix = string.IsNullOrWhiteSpace(PathPrefix) ? "/airtunnel/diagnostics" : PathPrefix.Trim();
        if (!PathPrefix.StartsWith('/'))
        {
            PathPrefix = "/" + PathPrefix;
        }

        PathPrefix = PathPrefix.TrimEnd('/');
        if (string.IsNullOrWhiteSpace(PathPrefix))
        {
            PathPrefix = "/airtunnel/diagnostics";
        }

        SnapshotCollectionIntervalSeconds = Math.Clamp(SnapshotCollectionIntervalSeconds, 1, 3600);
    }
}
