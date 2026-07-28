namespace Air.Cloud.DynamicApp.Orchestrator;

/// <summary>
/// <para>zh-cn:已有 Docker 容器编排器选项；用于把宿主机上已经创建好的容器纳入 DynamicApp 生命周期控制。</para>
/// <para>en-us:Options for the existing Docker container orchestrator, used to bring already-created host containers under DynamicApp lifecycle control.</para>
/// </summary>
public sealed class DockerExistingContainerDynamicAppOrchestratorOptions
{
    /// <summary>
    /// <para>zh-cn:是否启用 Docker 容器控制；默认关闭，避免未显式授权时访问 Docker Socket。</para>
    /// <para>en-us:Whether Docker container control is enabled; disabled by default to avoid Docker socket access without explicit authorization.</para>
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// <para>zh-cn:Docker Engine Unix Socket 路径；容器化部署时通常挂载 /var/run/docker.sock。</para>
    /// <para>en-us:Docker Engine Unix socket path; containerized deployments usually mount /var/run/docker.sock.</para>
    /// </summary>
    public string DockerSocketPath { get; set; } = "/var/run/docker.sock";

    /// <summary>
    /// <para>zh-cn:容器名称模板，支持 {dynamicAppId}、{modId} 和 {version}；当前 101 演示环境使用 air.{modId}。</para>
    /// <para>en-us:Container name template supporting {dynamicAppId}, {modId}, and {version}; the current 101 demo environment uses air.{modId}.</para>
    /// </summary>
    public string ContainerNameTemplate { get; set; } = "{dynamicAppId}-{modId}-{version}";

    /// <summary>
    /// <para>zh-cn:目标基础地址模板，支持 {dynamicAppId}、{modId}、{version} 和 {containerName}；为空时使用 Manifest Artifact.Endpoint。</para>
    /// <para>en-us:Target base address template supporting {dynamicAppId}, {modId}, {version}, and {containerName}; when empty, Manifest Artifact.Endpoint is used.</para>
    /// </summary>
    public string? TargetBaseAddressTemplate { get; set; }

    /// <summary>
    /// <para>zh-cn:启动容器请求超时时间；小于等于零时使用 60 秒。</para>
    /// <para>en-us:Timeout for container start requests; values less than or equal to zero use 60 seconds.</para>
    /// </summary>
    public TimeSpan StartTimeout { get; set; } = TimeSpan.FromSeconds(60);

    /// <summary>
    /// <para>zh-cn:停止容器请求超时时间；小于等于零时使用 60 秒。</para>
    /// <para>en-us:Timeout for container stop requests; values less than or equal to zero use 60 seconds.</para>
    /// </summary>
    public TimeSpan StopTimeout { get; set; } = TimeSpan.FromSeconds(60);

    /// <summary>
    /// <para>zh-cn:Docker 停止容器时传递的优雅退出秒数；小于零时按零处理。</para>
    /// <para>en-us:Grace period in seconds passed to Docker when stopping a container; values below zero are treated as zero.</para>
    /// </summary>
    public int StopGracePeriodSeconds { get; set; } = 15;
}
