using Air.Cloud.Modules.ReverseTunnel.Agent;
using Air.Cloud.Modules.ReverseTunnel.Common.Options;
using Air.Cloud.Modules.ReverseTunnel.Common.Runtime;

namespace Air.Cloud.UnitTest.Modules.ReverseTunnel;

/// <summary>
/// <para>zh-cn:验证 Server 下发配置与 Agent 本地安全字段合并时的边界。</para>
/// <para>en-us:Verifies the boundary between Server-provided settings and Agent-local security settings.</para>
/// </summary>
public sealed class ReverseTunnelAgentConfigurationMergerTests
{
    /// <summary>
    /// <para>zh-cn:验证远程配置不能覆盖 Agent 身份、Server 地址、证书、Enrollment、管理和 Gateway 字段。</para>
    /// <para>en-us:Verifies that remote configuration cannot overwrite Agent identity, Server address, certificate, enrollment, management, or Gateway fields.</para>
    /// </summary>
    [Fact]
    public void Merge_should_preserve_local_security_and_connection_fields()
    {
        var current = new ReverseTunnelOptions
        {
            Enabled = true,
            TunnelMode = ReverseTunnelMode.Agent,
            AgentId = "local-agent",
            ServerAddress = "https://server.local:18443",
            AgentCertificate = new ReverseTunnelAgentCertificateOptions
            {
                CertificatePath = "/local/agent.crt",
                PrivateKeyPath = "/local/agent.key",
                TrustedIssuerPath = "/local/ca.crt",
                ConfigurationSnapshotPath = "/local/snapshot.json"
            },
            Enrollment = new ReverseTunnelEnrollmentOptions
            {
                Enabled = true,
                CredentialDirectory = "/local/enrollment"
            },
            Management = new ReverseTunnelManagementOptions
            {
                Enabled = true,
                ConfigDirectory = "/local/config"
            },
            Gateway = new ReverseTunnelGatewayOptions
            {
                InvokePathPrefix = "/local-gateway",
                AccessTokenEnvironmentVariable = "LOCAL_TOKEN"
            }
        };
        current.PostConfigure(current, new Microsoft.Extensions.Configuration.ConfigurationBuilder().Build());

        var incoming = ReverseTunnelOptionsCloner.CloneAndNormalize(current);
        incoming.AgentId = "remote-agent";
        incoming.ServerAddress = "https://remote.example:18443";
        incoming.MaxConcurrentStreams = 256;
        incoming.AgentCertificate.CertificatePath = "/remote/agent.crt";
        incoming.Enrollment.CredentialDirectory = "/remote/enrollment";
        incoming.Management.ConfigDirectory = "/remote/config";
        incoming.Gateway.InvokePathPrefix = "/remote-gateway";

        var merged = ReverseTunnelAgentConfigurationMerger.Merge(current, incoming);

        Assert.Equal("local-agent", merged.AgentId);
        Assert.Equal("https://server.local:18443", merged.ServerAddress);
        Assert.Equal(ReverseTunnelMode.Agent, merged.TunnelMode);
        Assert.Equal("/local/agent.crt", merged.AgentCertificate.CertificatePath);
        Assert.Equal("/local/agent.key", merged.AgentCertificate.PrivateKeyPath);
        Assert.Equal("/local/enrollment", merged.Enrollment.CredentialDirectory);
        Assert.Equal("/local/config", merged.Management.ConfigDirectory);
        Assert.Equal("/local-gateway", merged.Gateway.InvokePathPrefix);
        Assert.Equal(256, merged.MaxConcurrentStreams);
    }
}


