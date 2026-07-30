namespace Air.Cloud.Core.Standard.Container.Model;

/// <summary>
/// <para>zh-cn:容器引擎实例视图。该模型用于在 Core 层表达 Docker 容器、IIS 站点或其他托管实例的通用运行状态。</para>
/// <para>en-us:Container engine instance view. This model expresses common runtime status for Docker containers, IIS sites, or other hosted instances at the Core layer.</para>
/// </summary>
public sealed class ContainerEngineInstance
{
    /// <summary>
    /// <para>zh-cn:实例短标识或平台返回的 ID。</para>
    /// <para>en-us:The short instance identifier or platform-returned id.</para>
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// <para>zh-cn:实例名称。</para>
    /// <para>en-us:The instance name.</para>
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// <para>zh-cn:实例镜像、站点路径或其他来源描述。</para>
    /// <para>en-us:The instance image, site path, or other source description.</para>
    /// </summary>
    public string Image { get; set; } = string.Empty;

    /// <summary>
    /// <para>zh-cn:运行时类型，例如 Docker、IIS、Podman 或 Kubernetes。</para>
    /// <para>en-us:The runtime type, such as Docker, IIS, Podman, or Kubernetes.</para>
    /// </summary>
    public string Runtime { get; set; } = string.Empty;

    /// <summary>
    /// <para>zh-cn:平台原始状态，例如 running、exited、Started 或 Stopped。</para>
    /// <para>en-us:The platform raw state, such as running, exited, Started, or Stopped.</para>
    /// </summary>
    public string State { get; set; } = string.Empty;

    /// <summary>
    /// <para>zh-cn:平台状态描述，用于展示即时状态。</para>
    /// <para>en-us:The platform status description used to display live status.</para>
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// <para>zh-cn:平台健康状态。平台未提供健康检查时为空。</para>
    /// <para>en-us:The platform health status. Empty when the platform does not provide health checks.</para>
    /// </summary>
    public string HealthStatus { get; set; } = string.Empty;

    /// <summary>
    /// <para>zh-cn:实例是否处于运行状态。</para>
    /// <para>en-us:Whether the instance is running.</para>
    /// </summary>
    public bool IsRunning { get; set; }

    /// <summary>
    /// <para>zh-cn:实例是否可被上层能力视为可用。实现方应基于引擎状态判断，不应隐式访问业务 HTTP 健康检查。</para>
    /// <para>en-us:Whether upper-layer capabilities can treat the instance as available. Implementations should decide from engine status and should not implicitly call business HTTP health checks.</para>
    /// </summary>
    public bool IsAvailable { get; set; }
}
