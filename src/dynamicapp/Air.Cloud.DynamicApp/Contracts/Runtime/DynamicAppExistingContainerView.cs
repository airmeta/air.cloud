namespace Air.Cloud.DynamicApp.Contracts.Runtime;

/// <summary>
/// <para>zh-cn:已有容器视图，用于控制面板展示可登记到 DynamicApp 的容器或托管实例及其容器引擎即时状态。</para>
/// <para>en-us:Existing-container view used by the control panel to display containers or hosted instances that can be registered into DynamicApp with their live container-engine status.</para>
/// </summary>
public sealed class DynamicAppExistingContainerView
{
    /// <summary>
    /// <para>zh-cn:容器或托管实例短标识。</para>
    /// <para>en-us:Container or hosted instance short identifier.</para>
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// <para>zh-cn:容器或托管实例名称。</para>
    /// <para>en-us:Container or hosted instance name.</para>
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// <para>zh-cn:容器镜像、站点路径或其他来源描述。</para>
    /// <para>en-us:Container image, site path, or other source description.</para>
    /// </summary>
    public string Image { get; set; } = string.Empty;

    /// <summary>
    /// <para>zh-cn:容器引擎原始状态，例如 running、exited、Started 或 Stopped。</para>
    /// <para>en-us:Container engine raw state, such as running, exited, Started, or Stopped.</para>
    /// </summary>
    public string State { get; set; } = string.Empty;

    /// <summary>
    /// <para>zh-cn:容器引擎状态描述，用于展示即时状态。</para>
    /// <para>en-us:Container engine status description used to display live status.</para>
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// <para>zh-cn:兼容字段，承载容器引擎健康子状态；Docker 实现中对应 Docker Healthcheck 状态。</para>
    /// <para>en-us:Compatibility field carrying the container engine health sub-state; in the Docker implementation it maps to Docker Healthcheck status.</para>
    /// </summary>
    public string DockerHealthStatus { get; set; } = string.Empty;

    /// <summary>
    /// <para>zh-cn:容器或托管实例当前是否处于运行状态。</para>
    /// <para>en-us:Whether the container or hosted instance is currently running.</para>
    /// </summary>
    public bool IsRunning { get; set; }

    /// <summary>
    /// <para>zh-cn:容器或托管实例是否可被 DynamicApp 视为可接管；判断依据仅来自容器引擎状态。</para>
    /// <para>en-us:Whether DynamicApp can treat the container or hosted instance as adoptable; the decision is based only on container engine status.</para>
    /// </summary>
    public bool IsAvailable { get; set; }
}
