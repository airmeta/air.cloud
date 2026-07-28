namespace Air.Cloud.DynamicApp.Host;

/// <summary>
/// <para>zh-cn:DynamicApp Process 模式自愈选项，用于控制异常退出或活进程缺失后的自动重启策略。</para>
/// <para>en-us:Options for DynamicApp Process-mode self-healing, controlling automatic restart after unexpected exits or missing live processes.</para>
/// </summary>
public sealed class DynamicAppSelfHealingOptions
{
    /// <summary>
    /// <para>zh-cn:是否启用 Process 模式自愈，默认 true。</para>
    /// <para>en-us:Whether Process-mode self-healing is enabled, true by default.</para>
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// <para>zh-cn:自愈扫描间隔，默认 5 秒；小于等于 0 时会回退到 5 秒。</para>
    /// <para>en-us:Self-healing scan interval, 5 seconds by default; values less than or equal to 0 fall back to 5 seconds.</para>
    /// </summary>
    public TimeSpan ScanInterval { get; set; } = TimeSpan.FromSeconds(5);

    /// <summary>
    /// <para>zh-cn:同一目标两次自愈重启之间的最小退避时间，默认 5 秒；小于 0 时会回退到 0。</para>
    /// <para>en-us:Minimum backoff between two self-healing restart attempts for the same target, 5 seconds by default; values less than 0 fall back to 0.</para>
    /// </summary>
    public TimeSpan RestartBackoff { get; set; } = TimeSpan.FromSeconds(5);

    /// <summary>
    /// <para>zh-cn:同一窗口内单个目标允许的最大自愈重启次数，默认 5 次；小于等于 0 时会回退到 1 次。</para>
    /// <para>en-us:Maximum self-healing restart attempts allowed for one target within a window, 5 by default; values less than or equal to 0 fall back to 1.</para>
    /// </summary>
    public int MaxRestartAttempts { get; set; } = 5;

    /// <summary>
    /// <para>zh-cn:自愈重启次数统计窗口，默认 5 分钟；窗口过期后会重新计数。</para>
    /// <para>en-us:Counting window for self-healing restart attempts, 5 minutes by default; attempts are reset after the window expires.</para>
    /// </summary>
    public TimeSpan AttemptWindow { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>
    /// <para>zh-cn:目标恢复到 Active/Healthy 且活进程存在后是否清理重试计数，默认 true。</para>
    /// <para>en-us:Whether retry counters are cleared after a target returns to Active/Healthy with a live process, true by default.</para>
    /// </summary>
    public bool ResetAttemptsAfterHealthy { get; set; } = true;
}
