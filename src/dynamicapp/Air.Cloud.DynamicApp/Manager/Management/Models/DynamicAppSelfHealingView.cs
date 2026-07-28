namespace Air.Cloud.DynamicApp.Manager;

/// <summary>
/// <para>zh-cn:DynamicApp 控制面板自愈视图，用于展示某个 Mod 版本的重试窗口、暂停状态和最近失败原因。</para>
/// <para>en-us:DynamicApp control-panel self-healing view used to display retry window, pause state, and latest failure reason for one Mod version.</para>
/// </summary>
public sealed class DynamicAppSelfHealingView
{
    /// <summary>
    /// <para>zh-cn:当前统计窗口内的自愈重试次数。</para>
    /// <para>en-us:The self-healing retry attempt count in the current counting window.</para>
    /// </summary>
    public int AttemptCount { get; set; }

    /// <summary>
    /// <para>zh-cn:当前策略允许的最大重试次数。</para>
    /// <para>en-us:The maximum retry attempts allowed by the current policy.</para>
    /// </summary>
    public int MaxAttempts { get; set; }

    /// <summary>
    /// <para>zh-cn:当前统计窗口第一次尝试的 UTC 时间。</para>
    /// <para>en-us:The UTC time of the first attempt in the current counting window.</para>
    /// </summary>
    public DateTimeOffset FirstAttemptUtc { get; set; }

    /// <summary>
    /// <para>zh-cn:最近一次尝试的 UTC 时间。</para>
    /// <para>en-us:The UTC time of the latest attempt.</para>
    /// </summary>
    public DateTimeOffset LastAttemptUtc { get; set; }

    /// <summary>
    /// <para>zh-cn:下一次允许重试的 UTC 时间。</para>
    /// <para>en-us:The UTC time when the next retry is allowed.</para>
    /// </summary>
    public DateTimeOffset? NextRetryAtUtc { get; set; }

    /// <summary>
    /// <para>zh-cn:当前统计窗口重置的 UTC 时间。</para>
    /// <para>en-us:The UTC time when the current counting window resets.</para>
    /// </summary>
    public DateTimeOffset WindowResetAtUtc { get; set; }

    /// <summary>
    /// <para>zh-cn:是否因为达到最大重试次数而暂停自愈。</para>
    /// <para>en-us:Whether self-healing is paused because the maximum retry attempts were reached.</para>
    /// </summary>
    public bool IsPaused { get; set; }

    /// <summary>
    /// <para>zh-cn:最近失败阶段。</para>
    /// <para>en-us:The latest failed stage.</para>
    /// </summary>
    public string LastFailureStage { get; set; } = string.Empty;

    /// <summary>
    /// <para>zh-cn:最近失败或跳过重试的消息。</para>
    /// <para>en-us:The latest failure or skipped-retry message.</para>
    /// </summary>
    public string LastFailureMessage { get; set; } = string.Empty;
}
