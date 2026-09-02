namespace Air.Cloud.Modules.ReverseTunnel.Management;

/// <summary>
/// <para>zh-cn:生成不包含私钥的 Agent 部署配置包。</para>
/// <para>en-us:Creates an Agent deployment configuration package without private keys.</para>
/// </summary>
public interface IReverseTunnelAgentPackageExporter
{
    /// <summary>
    /// <para>zh-cn:根据 Server 已保存的 Agent 配置生成使用默认容器参数的 ZIP。</para>
    /// <para>en-us:Creates a ZIP from the Server's desired Agent configuration using default container options.</para>
    /// </summary>
    /// <param name="agentId"><para>zh-cn:目标 AgentId。</para><para>en-us:Target AgentId.</para></param>
    /// <param name="cancellationToken"><para>zh-cn:取消令牌。</para><para>en-us:Cancellation token.</para></param>
    /// <returns><para>zh-cn:ZIP 字节和下载文件名。</para><para>en-us:ZIP bytes and download file name.</para></returns>
    Task<ReverseTunnelAgentPackage> CreateAsync(string agentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// <para>zh-cn:根据 Server 已保存的 Agent 配置和指定的容器运行参数生成 ZIP。</para>
    /// <para>en-us:Creates a ZIP from the Server's desired Agent configuration and the supplied container runtime options.</para>
    /// </summary>
    /// <param name="agentId"><para>zh-cn:目标 AgentId。</para><para>en-us:Target AgentId.</para></param>
    /// <param name="request"><para>zh-cn:导出包中的镜像、容器和运行环境参数。</para><para>en-us:Image, container, and runtime environment options for the package.</para></param>
    /// <param name="cancellationToken"><para>zh-cn:取消令牌。</para><para>en-us:Cancellation token.</para></param>
    Task<ReverseTunnelAgentPackage> CreateAsync(
        string agentId,
        ReverseTunnelAgentPackageExportRequest? request,
        CancellationToken cancellationToken = default);
}

/// <summary><para>zh-cn:Agent 配置包内容。 en-us:Generated Agent package.</para></summary>
public sealed record ReverseTunnelAgentPackage(byte[] Content, string FileName);
