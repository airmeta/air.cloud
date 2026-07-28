using Air.Cloud.DynamicApp.Contracts.Identifiers;

namespace Air.Cloud.DynamicApp.Contracts.Runtime;

/// <summary>
/// <para>zh-cn:DynamicApp Process 自愈状态快照，用于记录某个 Mod 版本在当前统计窗口内的重启次数、退避时间和最近失败原因。</para>
/// <para>en-us:DynamicApp Process self-healing state snapshot that records restart attempts, backoff timing, and the latest failure reason for one Mod version within the current counting window.</para>
/// </summary>
public sealed class DynamicAppSelfHealingState
{
    /// <summary>
    /// <para>zh-cn:被自愈管理的 Mod 版本键。</para>
    /// <para>en-us:The Mod version key managed by self-healing.</para>
    /// </summary>
    public DynamicModVersionKey Key { get; set; }

    /// <summary>
    /// <para>zh-cn:当前统计窗口内已经保留或执行的自愈重启次数。</para>
    /// <para>en-us:The number of reserved or executed self-healing restart attempts in the current counting window.</para>
    /// </summary>
    public int AttemptCount { get; set; }

    /// <summary>
    /// <para>zh-cn:当前策略允许的最大重启次数；用于控制面板展示当时生效的限制。</para>
    /// <para>en-us:The maximum restart attempts allowed by the active policy; shown by the control panel as the effective limit at that time.</para>
    /// </summary>
    public int MaxAttempts { get; set; }

    /// <summary>
    /// <para>zh-cn:当前统计窗口第一次自愈尝试的 UTC 时间。</para>
    /// <para>en-us:The UTC time of the first self-healing attempt in the current counting window.</para>
    /// </summary>
    public DateTimeOffset FirstAttemptUtc { get; set; }

    /// <summary>
    /// <para>zh-cn:最近一次保留或执行自愈尝试�?UTC 时间。</para>
    /// <para>en-us:The UTC time when the latest self-healing attempt was reserved or executed.</para>
    /// </summary>
    public DateTimeOffset LastAttemptUtc { get; set; }

    /// <summary>
    /// <para>zh-cn:下一次允许重试的 UTC 时间；退避期内用于说明为什么暂不重启。</para>
    /// <para>en-us:The UTC time when the next retry is allowed; used during backoff to explain why restart is currently skipped.</para>
    /// </summary>
    public DateTimeOffset? NextRetryAtUtc { get; set; }

    /// <summary>
    /// <para>zh-cn:当前统计窗口重置�?UTC 时间；达到最大次数后需要等到该时间或人工重置。</para>
    /// <para>en-us:The UTC time when the current counting window resets; after the attempt limit is reached, retry waits until this time or a manual reset.</para>
    /// </summary>
    public DateTimeOffset WindowResetAtUtc { get; set; }

    /// <summary>
    /// <para>zh-cn:是否因为达到最大重试次数而暂停自动自愈。</para>
    /// <para>en-us:Whether automatic self-healing is paused because the maximum retry attempts were reached.</para>
    /// </summary>
    public bool IsPaused { get; set; }

    /// <summary>
    /// <para>zh-cn:最近一次失败阶段，例如 restart、health probe �?activation。</para>
    /// <para>en-us:The latest failed stage, such as restart, health probe, or activation.</para>
    /// </summary>
    public string LastFailureStage { get; set; } = string.Empty;

    /// <summary>
    /// <para>zh-cn:最近一次失败或跳过重试的可读消息。</para>
    /// <para>en-us:The latest readable message for a failure or skipped retry.</para>
    /// </summary>
    public string LastFailureMessage { get; set; } = string.Empty;

    /// <summary>
    /// <para>zh-cn:该自愈状态最后更新的 UTC 时间。</para>
    /// <para>en-us:The UTC time when this self-healing state was last updated.</para>
    /// </summary>
    public DateTimeOffset UpdatedAtUtc { get; set; }
}
