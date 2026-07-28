namespace Air.Cloud.DynamicApp.Contracts.Runtime;

/// <summary>
/// <para>zh-cn:DynamicApp Mod 健康探测结果。</para>
/// <para>en-us:DynamicApp Mod health probe result.</para>
/// </summary>
public sealed class DynamicAppHealthProbeResult
{
    /// <summary>
    /// <para>zh-cn:健康探测是否成功。</para>
    /// <para>en-us:Whether the health probe succeeded.</para>
    /// </summary>
    public bool IsHealthy { get; init; }

    /// <summary>
    /// <para>zh-cn:探测消息，用于记录失败原因或成功状态。</para>
    /// <para>en-us:Probe message used to record failure reasons or success state.</para>
    /// </summary>
    public string Message { get; init; } = string.Empty;

    /// <summary>
    /// <para>zh-cn:创建健康结果。</para>
    /// <para>en-us:Creates a healthy result.</para>
    /// </summary>
    /// <param name="message">
    /// <para>zh-cn:健康消息。</para>
    /// <para>en-us:Healthy message.</para>
    /// </param>
    /// <returns>
    /// <para>zh-cn:健康探测结果。</para>
    /// <para>en-us:A healthy probe result.</para>
    /// </returns>
    public static DynamicAppHealthProbeResult Healthy(string message) => new()
    {
        IsHealthy = true,
        Message = message
    };

    /// <summary>
    /// <para>zh-cn:创建不健康结果。</para>
    /// <para>en-us:Creates an unhealthy result.</para>
    /// </summary>
    /// <param name="message">
    /// <para>zh-cn:不健康消息。</para>
    /// <para>en-us:Unhealthy message.</para>
    /// </param>
    /// <returns>
    /// <para>zh-cn:不健康探测结果。</para>
    /// <para>en-us:An unhealthy probe result.</para>
    /// </returns>
    public static DynamicAppHealthProbeResult Unhealthy(string message) => new()
    {
        IsHealthy = false,
        Message = message
    };
}
