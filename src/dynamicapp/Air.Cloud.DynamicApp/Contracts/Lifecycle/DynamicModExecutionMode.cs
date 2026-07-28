namespace Air.Cloud.DynamicApp.Contracts.Lifecycle;

/// <summary>
/// <para>zh-cn:Mod 执行方式。</para>
/// <para>en-us:Mod execution mode.</para>
/// </summary>
public enum DynamicModExecutionMode
{
    /// <summary>
    /// <para>zh-cn:子进程模式。</para>
    /// <para>en-us:Child-process mode.</para>
    /// </summary>
    Process = 0,

    /// <summary>
    /// <para>zh-cn:受控容器模式。</para>
    /// <para>en-us:Trusted container mode.</para>
    /// </summary>
    Container = 1,

    /// <summary>
    /// <para>zh-cn:已有容器接管模式，用于启动、停止和转发已由 Compose、1Panel 或运维平台创建的容器。</para>
    /// <para>en-us:Existing-container takeover mode used to start, stop, and forward to containers already created by Compose, 1Panel, or an operations platform.</para>
    /// </summary>
    ExistingContainer = 2,

    /// <summary>
    /// <para>zh-cn:本地开发联调模式，用于转发到开发者在另一个终端或 IDE 中自行启动的服务，DynamicApp 只负责注册、健康探测和流量转发。</para>
    /// <para>en-us:Local development integration mode used to forward to a service started manually from another terminal or IDE; DynamicApp only registers, health-probes, and forwards traffic.</para>
    /// </summary>
    LocalDevelopment = 3
}
