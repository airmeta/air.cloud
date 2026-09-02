namespace Air.Cloud.DynamicApp.Orchestrator;

/// <summary>
/// <para>zh-cn:已有容器编排器选项，用于把已经创建好的容器或托管实例纳入 DynamicApp 生命周期控制。具体容器平台访问由 Core 容器引擎标准实现提供。</para>
/// <para>en-us:Options for the existing-container orchestrator, used to bring already-created containers or hosted instances under DynamicApp lifecycle control. Concrete platform access is provided by the Core container engine standard implementation.</para>
/// </summary>
public sealed class DockerExistingContainerDynamicAppOrchestratorOptions
{
    /// <summary>
    /// <para>zh-cn:是否启用已有容器接管；默认关闭，避免未显式授权时控制宿主容器或托管实例。</para>
    /// <para>en-us:Whether existing-container adoption is enabled; disabled by default to avoid controlling host containers or hosted instances without explicit authorization.</para>
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// <para>zh-cn:容器名称模板，支持 {dynamicAppId}、{modId}、{version} 和 {deploymentId}。如果 Manifest 的 Artifact.ContainerName 已设置，会优先使用 Manifest 中的容器名称。</para>
    /// <para>en-us:Container name template supporting {dynamicAppId}, {modId}, {version}, and {deploymentId}. If Manifest Artifact.ContainerName is set, the manifest container name takes precedence.</para>
    /// </summary>
    public string ContainerNameTemplate { get; set; } = "{dynamicAppId}-{modId}-{version}";

    /// <summary>
    /// <para>zh-cn:目标基础地址模板，支持 {dynamicAppId}、{modId}、{version} 和 {containerName}。为空时优先使用 Manifest 的 Artifact.Endpoint。</para>
    /// <para>en-us:Target base address template supporting {dynamicAppId}, {modId}, {version}, and {containerName}. When empty, Manifest Artifact.Endpoint is used first.</para>
    /// </summary>
    public string? TargetBaseAddressTemplate { get; set; }
}
