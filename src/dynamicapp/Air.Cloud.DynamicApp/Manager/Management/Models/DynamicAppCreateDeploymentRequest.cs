namespace Air.Cloud.DynamicApp.Manager;

/// <summary>
/// <para>zh-cn:基于现有包创建独立 DynamicApp 部署实例的请求。</para>
/// <para>en-us:Request for creating an isolated DynamicApp deployment from an existing package.</para>
/// </summary>
public sealed class DynamicAppCreateDeploymentRequest
{
    /// <summary>
    /// <para>zh-cn:DynamicApp 应用标识。</para>
    /// <para>en-us:DynamicApp application identifier.</para>
    /// </summary>
    public string DynamicAppId { get; set; } = string.Empty;

    /// <summary>
    /// <para>zh-cn:Mod 标识。</para>
    /// <para>en-us:Mod identifier.</para>
    /// </summary>
    public string ModId { get; set; } = string.Empty;

    /// <summary>
    /// <para>zh-cn:包版本。</para>
    /// <para>en-us:Package version.</para>
    /// </summary>
    public string Version { get; set; } = string.Empty;

    /// <summary>
    /// <para>zh-cn:独立部署实例标识，例如 blue、gray 或 replica-02。</para>
    /// <para>en-us:Isolated deployment identifier such as blue, gray, or replica-02.</para>
    /// </summary>
    public string DeploymentId { get; set; } = string.Empty;

    /// <summary>
    /// <para>zh-cn:实例监听端口；必须大于 0。</para>
    /// <para>en-us:Instance listening port; must be greater than 0.</para>
    /// </summary>
    public int Port { get; set; }

    /// <summary>
    /// <para>zh-cn:实例基础地址；为空时根据 Port 使用 http://127.0.0.1:{Port}。</para>
    /// <para>en-us:Instance base address; when empty, http://127.0.0.1:{Port} is used from Port.</para>
    /// </summary>
    public string? Endpoint { get; set; }

    /// <summary>
    /// <para>zh-cn:进程启动参数；运行时会追加实例端点参数。</para>
    /// <para>en-us:Process startup arguments; the instance endpoint argument is appended at runtime.</para>
    /// </summary>
    public string? Arguments { get; set; }

    /// <summary>
    /// <para>zh-cn:健康检查路径；为空时使用 /Health。</para>
    /// <para>en-us:Health-check path; /Health is used when empty.</para>
    /// </summary>
    public string? HealthPath { get; set; }

    /// <summary>
    /// <para>zh-cn:健康检查启动超时时间，单位为秒；为空或非正数时使用 60。</para>
    /// <para>en-us:Health-check startup timeout in seconds; 60 is used when empty or non-positive.</para>
    /// </summary>
    public int StartupTimeoutSeconds { get; set; } = 60;

    /// <summary>
    /// <para>zh-cn:包内入口文件相对路径；为空时由包内容推断。</para>
    /// <para>en-us:Relative entry-file path inside the package; inferred from package content when empty.</para>
    /// </summary>
    public string? EntryFile { get; set; }

    /// <summary>
    /// <para>zh-cn:创建后是否立即启动实例；默认 false，只登记部署目录和目标。</para>
    /// <para>en-us:Whether to start the instance immediately; false by default, creating only the deployment directory and target registration.</para>
    /// </summary>
    public bool Start { get; set; }
}
