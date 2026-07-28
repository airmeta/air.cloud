namespace Air.Cloud.DynamicApp.Contracts.Lifecycle;

/// <summary>
/// <para>zh-cn:Mod 运行生命周期状态。</para>
/// <para>en-us:Mod runtime lifecycle state.</para>
/// </summary>
public enum DynamicModLifecycleState
{
    /// <summary>
    /// <para>zh-cn:已声明但尚未启动。</para>
    /// <para>en-us:Declared but not yet started.</para>
    /// </summary>
    Pending = 0,

    /// <summary>
    /// <para>zh-cn:正在启动。</para>
    /// <para>en-us:Starting.</para>
    /// </summary>
    Starting = 1,

    /// <summary>
    /// <para>zh-cn:可接收网关流量。</para>
    /// <para>en-us:Ready to receive gateway traffic.</para>
    /// </summary>
    Active = 2,

    /// <summary>
    /// <para>zh-cn:正在排空，不再接收新流量。</para>
    /// <para>en-us:Draining and no longer receiving new traffic.</para>
    /// </summary>
    Draining = 3,

    /// <summary>
    /// <para>zh-cn:已停止。</para>
    /// <para>en-us:Stopped.</para>
    /// </summary>
    Stopped = 4,

    /// <summary>
    /// <para>zh-cn:运行失败。</para>
    /// <para>en-us:Failed.</para>
    /// </summary>
    Failed = 5
}
