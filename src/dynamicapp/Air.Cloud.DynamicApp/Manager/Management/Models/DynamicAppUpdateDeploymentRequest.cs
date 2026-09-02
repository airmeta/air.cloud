namespace Air.Cloud.DynamicApp.Manager;

/// <summary>
/// <para>zh-cn:从当前运行实例切换到新包实例的更新请求。</para>
/// <para>en-us:Request for switching a current runtime instance to a new package deployment.</para>
/// </summary>
public sealed class DynamicAppUpdateDeploymentRequest
{
    /// <summary>
    /// <para>zh-cn:当前运行实例的 DynamicApp 标识。</para>
    /// <para>en-us:Current runtime DynamicApp identifier.</para>
    /// </summary>
    public string CurrentDynamicAppId { get; set; } = string.Empty;

    /// <summary>
    /// <para>zh-cn:当前运行实例的 Mod 标识。</para>
    /// <para>en-us:Current runtime Mod identifier.</para>
    /// </summary>
    public string CurrentModId { get; set; } = string.Empty;

    /// <summary>
    /// <para>zh-cn:当前运行实例的版本。</para>
    /// <para>en-us:Current runtime version.</para>
    /// </summary>
    public string CurrentVersion { get; set; } = string.Empty;

    /// <summary>
    /// <para>zh-cn:当前运行实例的部署标识。</para>
    /// <para>en-us:Current runtime deployment identifier.</para>
    /// </summary>
    public string CurrentDeploymentId { get; set; } = "default";

    /// <summary>
    /// <para>zh-cn:目标包所在的 DynamicApp 标识。</para>
    /// <para>en-us:Target package DynamicApp identifier.</para>
    /// </summary>
    public string DynamicAppId { get; set; } = string.Empty;

    /// <summary>
    /// <para>zh-cn:目标包的 Mod 标识。</para>
    /// <para>en-us:Target package Mod identifier.</para>
    /// </summary>
    public string ModId { get; set; } = string.Empty;

    /// <summary>
    /// <para>zh-cn:目标包版本。</para>
    /// <para>en-us:Target package version.</para>
    /// </summary>
    public string Version { get; set; } = string.Empty;

    /// <summary>
    /// <para>zh-cn:新实例部署标识，必须与当前实例不同。</para>
    /// <para>en-us:New deployment identifier, which must differ from the current instance.</para>
    /// </summary>
    public string DeploymentId { get; set; } = string.Empty;

    /// <summary>
    /// <para>zh-cn:新实例监听端口。</para>
    /// <para>en-us:New instance listening port.</para>
    /// </summary>
    public int Port { get; set; }

    /// <summary>
    /// <para>zh-cn:新实例基础地址；为空时根据端口使用 http://127.0.0.1:{Port}。</para>
    /// <para>en-us:New instance base address; http://127.0.0.1:{Port} is used from Port when empty.</para>
    /// </summary>
    public string? Endpoint { get; set; }

    /// <summary>
    /// <para>zh-cn:新实例进程启动参数。</para>
    /// <para>en-us:New instance process startup arguments.</para>
    /// </summary>
    public string? Arguments { get; set; }

    /// <summary>
    /// <para>zh-cn:新实例健康检查路径。</para>
    /// <para>en-us:New instance health-check path.</para>
    /// </summary>
    public string? HealthPath { get; set; }

    /// <summary>
    /// <para>zh-cn:新实例健康检查超时时间，单位为秒。</para>
    /// <para>en-us:New instance health-check timeout in seconds.</para>
    /// </summary>
    public int StartupTimeoutSeconds { get; set; } = 60;

    /// <summary>
    /// <para>zh-cn:包内入口文件相对路径；为空时自动推断。</para>
    /// <para>en-us:Relative package entry-file path; inferred when empty.</para>
    /// </summary>
    public string? EntryFile { get; set; }
}
