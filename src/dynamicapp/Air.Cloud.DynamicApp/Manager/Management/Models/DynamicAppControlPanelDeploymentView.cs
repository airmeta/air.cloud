using Air.Cloud.DynamicApp.Contracts.Lifecycle;

namespace Air.Cloud.DynamicApp.Manager;

/// <summary>
/// <para>zh-cn:DynamicApp 控制面板部署实例视图，表示同一个包版本下的一个独立运行副本。</para>
/// <para>en-us:DynamicApp control-panel deployment view representing one isolated runtime replica of a package version.</para>
/// </summary>
public sealed class DynamicAppControlPanelDeploymentView
{
    /// <summary>
    /// <para>zh-cn:部署实例标识；未启用集群时使用 default。</para>
    /// <para>en-us:Deployment identifier; default is used when clustering is not enabled.</para>
    /// </summary>
    public string DeploymentId { get; set; } = "default";

    /// <summary>
    /// <para>zh-cn:实例上游基础地址。</para>
    /// <para>en-us:Upstream base address of the runtime instance.</para>
    /// </summary>
    public string BaseAddress { get; set; } = string.Empty;

    /// <summary>
    /// <para>zh-cn:实例健康检查路径。</para>
    /// <para>en-us:Health-check path of the runtime instance.</para>
    /// </summary>
    public string HealthPath { get; set; } = string.Empty;

    /// <summary>
    /// <para>zh-cn:实例启动参数。</para>
    /// <para>en-us:Startup arguments of the runtime instance.</para>
    /// </summary>
    public string Arguments { get; set; } = string.Empty;

    /// <summary>
    /// <para>zh-cn:实例入口文件绝对路径。</para>
    /// <para>en-us:Absolute entry-file path of the runtime instance.</para>
    /// </summary>
    public string EntryFile { get; set; } = string.Empty;

    /// <summary>
    /// <para>zh-cn:实例启动健康检查超时时间，单位为秒。</para>
    /// <para>en-us:Startup health-check timeout in seconds.</para>
    /// </summary>
    public int StartupTimeoutSeconds { get; set; } = 60;

    /// <summary>
    /// <para>zh-cn:实例生命周期状态；未登记运行目标时为空。</para>
    /// <para>en-us:Runtime lifecycle state; null when no runtime target is registered.</para>
    /// </summary>
    public DynamicModLifecycleState? State { get; set; }

    /// <summary>
    /// <para>zh-cn:实例健康状态；未登记运行目标时为空。</para>
    /// <para>en-us:Runtime health status; null when no runtime target is registered.</para>
    /// </summary>
    public DynamicModHealthStatus? HealthStatus { get; set; }

    /// <summary>
    /// <para>zh-cn:实例标识，例如进程 Id 或容器标识。</para>
    /// <para>en-us:Runtime instance identifier, such as a process id or container identifier.</para>
    /// </summary>
    public string InstanceId { get; set; } = string.Empty;

    /// <summary>
    /// <para>zh-cn:实例最后状态消息。</para>
    /// <para>en-us:Last status message for the runtime instance.</para>
    /// </summary>
    public string LastStatusMessage { get; set; } = string.Empty;

    /// <summary>
    /// <para>zh-cn:实例最后更新时间。</para>
    /// <para>en-us:Last update time of the runtime instance.</para>
    /// </summary>
    public DateTimeOffset? UpdatedAtUtc { get; set; }

    /// <summary>
    /// <para>zh-cn:该实例的自愈状态。</para>
    /// <para>en-us:Self-healing state for the runtime instance.</para>
    /// </summary>
    public DynamicAppSelfHealingView? SelfHealing { get; set; }
}
