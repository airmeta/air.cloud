namespace Air.Cloud.Modules.ReverseTunnel.Common.Options;

/// <summary>
/// <para>zh-cn:Agent 周期查询 Server 目标配置的选项。启用后，Agent 会按周期查询最新版本；查询失败不会中断已经建立的隧道连接。</para>
/// <para>en-us:Options for periodic Agent queries of the Server's desired configuration. When enabled, the Agent polls for newer versions and keeps existing tunnel connections alive when a query fails.</para>
/// </summary>
public sealed class ReverseTunnelConfigurationSyncOptions
{
    /// <summary>
    /// <para>zh-cn:是否启用周期主动查询。默认启用。</para>
    /// <para>en-us:Whether periodic active queries are enabled. The default is true.</para>
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// <para>zh-cn:主动查询周期，单位为秒。默认 30 秒，运行期会限制在 5 到 3600 秒之间。</para>
    /// <para>en-us:Active-query interval in seconds. The default is 30 seconds and runtime normalization clamps it between 5 and 3600 seconds.</para>
    /// </summary>
    public int IntervalSeconds { get; set; } = 30;

    internal void Normalize()
    {
        IntervalSeconds = Math.Clamp(IntervalSeconds, 5, 3600);
    }
}
