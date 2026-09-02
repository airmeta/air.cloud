namespace Air.Cloud.DynamicApp.Host;

/// <summary>
/// <para>zh-cn:DynamicApp 排空清理选项。</para>
/// <para>en-us:Options for DynamicApp draining cleanup.</para>
/// </summary>
public sealed class DynamicAppDrainOptions
{
    /// <summary>
    /// <para>zh-cn:是否启用后台排空清理，默认 true。</para>
    /// <para>en-us:Whether background draining cleanup is enabled, true by default.</para>
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// <para>zh-cn:后台扫描排空目标的间隔，默认 15 秒。</para>
    /// <para>en-us:Interval used to scan draining targets, 15 seconds by default.</para>
    /// </summary>
    public TimeSpan ScanInterval { get; set; } = TimeSpan.FromSeconds(15);

    /// <summary>
    /// <para>zh-cn:目标进入 Draining 后保留旧实例的时长，默认 60 秒。</para>
    /// <para>en-us:How long an old instance is kept after entering Draining, 60 seconds by default.</para>
    /// </summary>
    public TimeSpan DrainTimeout { get; set; } = TimeSpan.FromSeconds(60);

    /// <summary>
    /// <para>zh-cn:停止旧实例失败时是否仍移除状态目标，默认 false；必须保留目标以便继续重试和追踪幽灵进程。</para>
    /// <para>en-us:Whether to remove state targets when stopping old instances fails, false by default so failed stops remain visible and retryable.</para>
    /// </summary>
    public bool RemoveTargetWhenStopFails { get; set; }
}
