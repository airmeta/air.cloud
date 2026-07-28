using Air.Cloud.DynamicApp.Contracts.Identifiers;
using Air.Cloud.DynamicApp.Contracts.Lifecycle;

namespace Air.Cloud.DynamicApp.Manager;

/// <summary>
/// <para>zh-cn:DynamicApp 控制面板 Mod 视图，表示一个可人工管控的挂载 Mod 版本及其运行状态。</para>
/// <para>en-us:DynamicApp control-panel Mod view representing one manually manageable mounted Mod version and its runtime state.</para>
/// </summary>
public sealed class DynamicAppControlPanelModView
{
    /// <summary>
    /// <para>zh-cn:DynamicApp 应用标识。</para>
    /// <para>en-us:DynamicApp application identifier.</para>
    /// </summary>
    public DynamicAppId DynamicAppId { get; set; }

    /// <summary>
    /// <para>zh-cn:Mod 标识。</para>
    /// <para>en-us:Mod identifier.</para>
    /// </summary>
    public DynamicModId ModId { get; set; }

    /// <summary>
    /// <para>zh-cn:Mod 版本。</para>
    /// <para>en-us:Mod version.</para>
    /// </summary>
    public DynamicAppVersion Version { get; set; }

    /// <summary>
    /// <para>zh-cn:Mod 执行模式，例如 Process、Container、ExistingContainer 或 LocalDevelopment。</para>
    /// <para>en-us:Mod execution mode, such as Process, Container, ExistingContainer, or LocalDevelopment.</para>
    /// </summary>
    public string ExecutionMode { get; set; } = string.Empty;

    /// <summary>
    /// <para>zh-cn:网关匹配该 Mod 的路由前缀。</para>
    /// <para>en-us:Gateway route prefix matching this Mod.</para>
    /// </summary>
    public string RoutePrefix { get; set; } = string.Empty;

    /// <summary>
    /// <para>zh-cn:健康检查路径，用于控制面板发起一次经过 DynamicApp Gateway 的链路测试。</para>
    /// <para>en-us:Health check path used by the control panel to issue one route test through DynamicApp Gateway.</para>
    /// </summary>
    public string HealthPath { get; set; } = string.Empty;

    /// <summary>
    /// <para>zh-cn:当前运行目标基础地址；没有运行目标时为空字符串。</para>
    /// <para>en-us:Current runtime target base address; empty when no runtime target exists.</para>
    /// </summary>
    public string BaseAddress { get; set; } = string.Empty;

    /// <summary>
    /// <para>zh-cn:当前生命周期状态；没有运行目标时为 null。</para>
    /// <para>en-us:Current lifecycle state; null when no runtime target exists.</para>
    /// </summary>
    public DynamicModLifecycleState? State { get; set; }

    /// <summary>
    /// <para>zh-cn:当前健康状态；没有运行目标时为 null。</para>
    /// <para>en-us:Current health status; null when no runtime target exists.</para>
    /// </summary>
    public DynamicModHealthStatus? HealthStatus { get; set; }

    /// <summary>
    /// <para>zh-cn:运行实例标识，Process 模式通常为进程 Id，容器模式通常为容器名或实例名。</para>
    /// <para>en-us:Runtime instance identifier, usually a process id in Process mode or a container/instance name in container modes.</para>
    /// </summary>
    public string InstanceId { get; set; } = string.Empty;

    /// <summary>
    /// <para>zh-cn:最后一次状态消息，用于控制面板展示和人工排障。</para>
    /// <para>en-us:Last status message for control-panel display and operator diagnostics.</para>
    /// </summary>
    public string LastStatusMessage { get; set; } = string.Empty;

    /// <summary>
    /// <para>zh-cn:运行目标最后更新时间；没有运行目标时为 null。</para>
    /// <para>en-us:Last runtime target update time; null when no runtime target exists.</para>
    /// </summary>
    public DateTimeOffset? UpdatedAtUtc { get; set; }

    /// <summary>
    /// <para>zh-cn:是否存在可用于启动或重启该 Mod 版本的 Manifest 历史。</para>
    /// <para>en-us:Whether manifest history exists for starting or restarting this Mod version.</para>
    /// </summary>
    public bool HasManifest { get; set; }

    /// <summary>
    /// <para>zh-cn:匹配 Manifest 历史记录标识；不存在 Manifest 历史时为空字符串。</para>
    /// <para>en-us:Matching manifest history record identifier; empty when no manifest history exists.</para>
    /// </summary>
    public string ManifestRecordId { get; set; } = string.Empty;

    /// <summary>
    /// <para>zh-cn:匹配 Manifest 的保存时间；不存在 Manifest 历史时为 null。</para>
    /// <para>en-us:Matching manifest save time; null when no manifest history exists.</para>
    /// </summary>
    public DateTimeOffset? ManifestSavedAtUtc { get; set; }

    /// <summary>
    /// <para>zh-cn:当前 Mod 版本的自愈状态；没有失败重试记录时为 null。</para>
    /// <para>en-us:The self-healing state for the current Mod version; null when there is no failed retry record.</para>
    /// </summary>
    public DynamicAppSelfHealingView? SelfHealing { get; set; }
}
