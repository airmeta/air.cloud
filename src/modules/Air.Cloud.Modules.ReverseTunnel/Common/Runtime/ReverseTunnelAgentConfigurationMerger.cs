using Air.Cloud.Modules.ReverseTunnel.Common.Options;

namespace Air.Cloud.Modules.ReverseTunnel.Common.Runtime;

/// <summary>
/// <para>zh-cn:合并 Server 下发配置与 Agent 本地安全配置。远端配置不能覆盖 AgentId、ServerAddress、证书、Enrollment 和管理访问等本地安全字段。</para>
/// <para>en-us:Merges a Server-provided configuration with Agent-local security settings. Remote configuration cannot overwrite AgentId, ServerAddress, certificate, enrollment, or management-access fields.</para>
/// </summary>
public static class ReverseTunnelAgentConfigurationMerger
{
    /// <summary>
    /// <para>zh-cn:返回合并并标准化后的 Agent 配置。</para>
    /// <para>en-us:Returns the merged and normalized Agent configuration.</para>
    /// </summary>
    public static ReverseTunnelOptions Merge(ReverseTunnelOptions current, ReverseTunnelOptions incoming)
    {
        ArgumentNullException.ThrowIfNull(current);
        ArgumentNullException.ThrowIfNull(incoming);

        var normalized = ReverseTunnelOptionsCloner.CloneAndNormalize(incoming);
        var currentSecurity = ReverseTunnelOptionsCloner.CloneAndNormalize(current);
        normalized.AgentId = currentSecurity.AgentId;
        normalized.ServerAddress = currentSecurity.ServerAddress;
        normalized.TunnelMode = currentSecurity.TunnelMode;
        normalized.EnableTunnelProxy = currentSecurity.EnableTunnelProxy;
        normalized.CertificateAuthority = currentSecurity.CertificateAuthority;
        normalized.Enrollment = currentSecurity.Enrollment;
        normalized.AgentCertificate = currentSecurity.AgentCertificate;
        normalized.Management = currentSecurity.Management;
        normalized.Gateway = currentSecurity.Gateway;
        return ReverseTunnelOptionsCloner.CloneAndNormalize(normalized);
    }
}
