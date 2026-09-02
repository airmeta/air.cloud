namespace Air.Cloud.DynamicApp.Contracts.Manifest;

/// <summary>
/// <para>zh-cn:Mod 工件声明，用于描述进程入口文件或容器镜像。</para>
/// <para>en-us:Mod artifact declaration used to describe the process entry file, container image, or existing target endpoint.</para>
/// </summary>
public sealed class DynamicModArtifactManifest
{
    /// <summary>
    /// <para>zh-cn:Process 模式入口文件路径。</para>
    /// <para>en-us:Entry file path for Process mode.</para>
    /// </summary>
    public string? File { get; set; }

    /// <summary>
    /// <para>zh-cn:Container 模式镜像名称。</para>
    /// <para>en-us:Image name for Container mode.</para>
    /// </summary>
    public string? Image { get; set; }

    /// <summary>
    /// <para>zh-cn:ExistingContainer 模式要接管的已有容器名称；用于明确绑定具体 Docker 容器，避免依赖模板反推。</para>
    /// <para>en-us:Existing container name adopted by ExistingContainer mode; it explicitly binds the Docker container instead of relying on template inference.</para>
    /// </summary>
    public string? ContainerName { get; set; }

    /// <summary>
    /// <para>zh-cn:进程启动参数。</para>
    /// <para>en-us:Process startup arguments.</para>
    /// </summary>
    public string? Arguments { get; set; }

    /// <summary>
    /// <para>zh-cn:Mod 实例对 Host 或 Gateway 暴露的基础地址；Process 模式未设置时 Host 会尝试从启动参数中解析 --urls，ExistingContainer 与 LocalDevelopment 模式必须显式声明。</para>
    /// <para>en-us:Base address exposed by the Mod instance to Host or Gateway; Process mode can resolve it from --urls when omitted, while ExistingContainer and LocalDevelopment modes must declare it explicitly.</para>
    /// </summary>
    public string? Endpoint { get; set; }

    /// <summary>
    /// <para>zh-cn:独立部署实例标识；为空时使用 default，仅用于运行时实例，不改变包身份。</para>
    /// <para>en-us:Isolated deployment identifier; default is used when empty and the value affects only runtime identity, not package identity.</para>
    /// </summary>
    public string? DeploymentId { get; set; }

    /// <summary>
    /// <para>zh-cn:独立部署目录绝对路径；仅由运行时部署副本使用，不改变包库存身份。</para>
    /// <para>en-us:Absolute isolated deployment-directory path used by a runtime deployment copy without changing package identity.</para>
    /// </summary>
    public string? DeploymentDirectory { get; set; }
}
