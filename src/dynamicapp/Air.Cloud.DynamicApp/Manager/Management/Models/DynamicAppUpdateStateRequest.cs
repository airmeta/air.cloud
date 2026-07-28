using Air.Cloud.DynamicApp.Contracts.Lifecycle;

namespace Air.Cloud.DynamicApp.Manager;

/// <summary>
/// <para>zh-cn:DynamicApp 目标状态更新请求模型。</para>
/// <para>en-us:Request model for updating DynamicApp target state.</para>
/// </summary>
public sealed class DynamicAppUpdateStateRequest : DynamicAppTargetKeyRequest
{
    /// <summary>
    /// <para>zh-cn:要写入的生命周期状态。</para>
    /// <para>en-us:Lifecycle state to write.</para>
    /// </summary>
    public DynamicModLifecycleState State { get; set; }

    /// <summary>
    /// <para>zh-cn:要写入的健康状态。</para>
    /// <para>en-us:Health status to write.</para>
    /// </summary>
    public DynamicModHealthStatus HealthStatus { get; set; }

    /// <summary>
    /// <para>zh-cn:可选状态消息，用于覆盖默认状态描述。</para>
    /// <para>en-us:Optional status message used to override the default state description.</para>
    /// </summary>
    public string? Message { get; set; }
}
