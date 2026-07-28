using Air.Cloud.DynamicApp.Contracts.Lifecycle;

namespace Air.Cloud.DynamicApp.Contracts.Runtime;

/// <summary>
/// <para>zh-cn:网关可转发的 Mod 运行目标，包含生命周期、健康状态和排空时间线。</para>
/// <para>en-us:Forwardable Mod runtime target that carries lifecycle, health, and draining timeline state.</para>
/// </summary>
public sealed class DynamicAppRouteTarget
{
    /// <summary>
    /// <para>zh-cn:目标版本键，用于唯一标识 DynamicApp/Mod/Version。</para>
    /// <para>en-us:Target version key that uniquely identifies DynamicApp/Mod/Version.</para>
    /// </summary>
    public DynamicModVersionKey Key { get; set; }

    /// <summary>
    /// <para>zh-cn:上游实例基础地址，网关会基于该地址拼接转发路径。</para>
    /// <para>en-us:Base address of the upstream instance; the gateway appends the forwarded path to it.</para>
    /// </summary>
    public Uri BaseAddress { get; set; } = new("http://127.0.0.1");

    /// <summary>
    /// <para>zh-cn:Mod 在网关中的路由前缀，必须使用绝对路径格式。</para>
    /// <para>en-us:Mod route prefix in the gateway; it must use an absolute path format.</para>
    /// </summary>
    public string RoutePrefix { get; set; } = string.Empty;

    /// <summary>
    /// <para>zh-cn:健康检查路径，后台健康监控会使用该路径探测目标。</para>
    /// <para>en-us:Health check path used by the background health monitor to probe the target.</para>
    /// </summary>
    public string HealthPath { get; set; } = "/health";

    /// <summary>
    /// <para>zh-cn:单次健康探测超时时间，单位为秒。</para>
    /// <para>en-us:Single health probe timeout in seconds.</para>
    /// </summary>
    public int HealthProbeTimeoutSeconds { get; set; } = 60;

    /// <summary>
    /// <para>zh-cn:生命周期状态，由 Manager、Host、健康监控和排空任务共同维护。</para>
    /// <para>en-us:Lifecycle state maintained by Manager, Host, health monitoring, and draining cleanup.</para>
    /// </summary>
    public DynamicModLifecycleState State { get; set; }

    /// <summary>
    /// <para>zh-cn:健康状态，用于决定网关是否允许转发新流量。</para>
    /// <para>en-us:Health status used to decide whether the gateway may forward new traffic.</para>
    /// </summary>
    public DynamicModHealthStatus HealthStatus { get; set; }

    /// <summary>
    /// <para>zh-cn:运行实例标识，进程模式通常写入进程 Id，容器模式可写入容器或 Pod 标识。</para>
    /// <para>en-us:Runtime instance identifier; process mode usually stores the process id, while container mode may store a container or pod id.</para>
    /// </summary>
    public string InstanceId { get; set; } = Guid.NewGuid().ToString("N");

    /// <summary>
    /// <para>zh-cn:最后一次状态消息，用于 Manager API 和人工排障展示。</para>
    /// <para>en-us:Last status message used by Manager API responses and operator diagnostics.</para>
    /// </summary>
    public string LastStatusMessage { get; set; } = string.Empty;

    /// <summary>
    /// <para>zh-cn:目标首次写入状态存储的 UTC 时间。</para>
    /// <para>en-us:UTC timestamp when the target was first written into the state store.</para>
    /// </summary>
    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// <para>zh-cn:目标最后一次被状态存储更新的 UTC 时间。</para>
    /// <para>en-us:UTC timestamp when the target was last updated by the state store.</para>
    /// </summary>
    public DateTimeOffset UpdatedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// <para>zh-cn:目标进入排空状态的 UTC 时间；非排空状态下为空。</para>
    /// <para>en-us:UTC timestamp when the target entered draining state; null when the target is not draining.</para>
    /// </summary>
    public DateTimeOffset? DrainingSinceUtc { get; set; }
}
