namespace Air.Cloud.DynamicApp.Contracts.Lifecycle;

/// <summary>
/// <para>zh-cn:Mod 健康状态。</para>
/// <para>en-us:Mod health status.</para>
/// </summary>
public enum DynamicModHealthStatus
{
    /// <summary>
    /// <para>zh-cn:健康状态未知。</para>
    /// <para>en-us:Health status is unknown.</para>
    /// </summary>
    Unknown = 0,

    /// <summary>
    /// <para>zh-cn:正在启动或等待首次健康检查。</para>
    /// <para>en-us:Starting or waiting for the first health check.</para>
    /// </summary>
    Starting = 1,

    /// <summary>
    /// <para>zh-cn:健康。</para>
    /// <para>en-us:Healthy.</para>
    /// </summary>
    Healthy = 2,

    /// <summary>
    /// <para>zh-cn:不健康。</para>
    /// <para>en-us:Unhealthy.</para>
    /// </summary>
    Unhealthy = 3,

    /// <summary>
    /// <para>zh-cn:正在排空。</para>
    /// <para>en-us:Draining.</para>
    /// </summary>
    Draining = 4,

    /// <summary>
    /// <para>zh-cn:已停止。</para>
    /// <para>en-us:Stopped.</para>
    /// </summary>
    Stopped = 5
}
