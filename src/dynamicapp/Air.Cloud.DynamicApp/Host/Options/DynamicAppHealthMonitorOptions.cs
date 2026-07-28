namespace Air.Cloud.DynamicApp.Host;

/// <summary>
/// <para>zh-cn:DynamicApp 后台健康监控选项。</para>
/// <para>en-us:Options for the DynamicApp background health monitor.</para>
/// </summary>
public sealed class DynamicAppHealthMonitorOptions
{
    /// <summary>
    /// <para>zh-cn:是否启用后台健康监控，默认 true。</para>
    /// <para>en-us:Whether the background health monitor is enabled, true by default.</para>
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// <para>zh-cn:健康探测间隔，默认 15 秒。</para>
    /// <para>en-us:Health probe interval, 15 seconds by default.</para>
    /// </summary>
    public TimeSpan ProbeInterval { get; set; } = TimeSpan.FromSeconds(15);

    /// <summary>
    /// <para>zh-cn:连续失败多少次后将目标标记为 Unhealthy，默认 2 次。</para>
    /// <para>en-us:Number of consecutive failures required before marking a target Unhealthy, 2 by default.</para>
    /// </summary>
    public int FailureThreshold { get; set; } = 2;

    /// <summary>
    /// <para>zh-cn:连续成功多少次后将目标恢复为 Healthy，默认 1 次。</para>
    /// <para>en-us:Number of consecutive successes required before restoring a target to Healthy, 1 by default.</para>
    /// </summary>
    public int SuccessThreshold { get; set; } = 1;
}
