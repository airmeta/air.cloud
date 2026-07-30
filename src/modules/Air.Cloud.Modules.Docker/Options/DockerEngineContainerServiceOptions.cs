namespace Air.Cloud.Modules.Docker.Options;

/// <summary>
/// <para>zh-cn:Docker Engine 容器访问服务选项。默认关闭 Socket 访问，调用方必须显式启用后服务才会访问 Docker Engine。</para>
/// <para>en-us:Options for the Docker Engine container access service. Socket access is disabled by default, and callers must explicitly enable it before the service accesses Docker Engine.</para>
/// </summary>
public sealed class DockerEngineContainerServiceOptions
{
    /// <summary>
    /// <para>zh-cn:是否启用 Docker Engine Socket 访问；默认关闭，避免未授权环境意外控制宿主机容器。</para>
    /// <para>en-us:Whether Docker Engine socket access is enabled; disabled by default to avoid accidental host-container control in unauthorized environments.</para>
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// <para>zh-cn:Docker Engine Unix Socket 路径；容器化部署时通常挂载 /var/run/docker.sock。</para>
    /// <para>en-us:Docker Engine Unix socket path; containerized deployments usually mount /var/run/docker.sock.</para>
    /// </summary>
    public string DockerSocketPath { get; set; } = "/var/run/docker.sock";

    /// <summary>
    /// <para>zh-cn:普通查询请求超时时间；小于等于零时使用 30 秒。</para>
    /// <para>en-us:Timeout for normal query requests; values less than or equal to zero use 30 seconds.</para>
    /// </summary>
    public TimeSpan RequestTimeout { get; set; } = TimeSpan.FromSeconds(30);

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
