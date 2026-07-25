using Air.Cloud.Modules.ReverseTunnel.Common.Options;
using Air.Cloud.Modules.ReverseTunnel.Common.Protocol;
using Air.Cloud.Modules.ReverseTunnel.Server.Grpc;

using Grpc.Net.Client;

using Microsoft.Extensions.Options;

using System.Net.Http;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;

namespace Air.Cloud.Modules.ReverseTunnel.Agent;

/// <summary>
/// <para>zh-cn:基于 gRPC 的 Agent 证书生命周期客户端。Enrollment 使用一次性挂载 token，续签使用当前本地客户端证书建立 mTLS 调用。</para>
/// <para>en-us:gRPC-based Agent certificate lifecycle client. Enrollment uses a one-time mounted token, and renewal uses the current local client certificate for mTLS.</para>
/// </summary>
public sealed class ReverseTunnelAgentCertificateClient : IReverseTunnelAgentCertificateClient
{
    private readonly IOptions<ReverseTunnelOptions> options;
    private readonly IReverseTunnelAgentCertificateMaterialStore materialStore;

    /// <summary>
    /// <para>zh-cn:创建 Agent 证书生命周期客户端。</para>
    /// <para>en-us:Creates an Agent certificate lifecycle client.</para>
    /// </summary>
    /// <param name="options">
    /// <para>zh-cn:包含 AgentId、ServerAddress 和本地证书挂载路径的反向隧道配置。</para>
    /// <para>en-us:Reverse tunnel options containing AgentId, ServerAddress, and local certificate mount paths.</para>
    /// </param>
    /// <param name="materialStore">
    /// <para>zh-cn:Agent 侧证书材料存储，用于生成 CSR、读取 token、保存证书和加载 mTLS 证书。</para>
    /// <para>en-us:Agent-side certificate material store used to create CSRs, read tokens, save certificates, and load mTLS certificates.</para>
    /// </param>
    public ReverseTunnelAgentCertificateClient(IOptions<ReverseTunnelOptions> options, IReverseTunnelAgentCertificateMaterialStore materialStore)
    {
        this.options = options;
        this.materialStore = materialStore;
    }

    /// <inheritdoc />
    public async Task<ReverseTunnelAgentCertificateClientResult> EnrollAsync(CancellationToken cancellationToken = default)
    {
        var settings = options.Value;
        var credential = materialStore.ReadEnrollmentCredential();
        var csr = materialStore.CreateCertificateSigningRequest(settings.AgentId);

        using var channel = CreateChannel(clientCertificate: null);
        var client = new ReverseTunnelEnrollment.ReverseTunnelEnrollmentClient(channel);
        var response = await client.EnrollAsync(new TunnelEnrollmentRequest
        {
            ProtocolVersion = ReverseTunnelProtocol.CurrentVersion,
            AgentId = settings.AgentId,
            EnrollmentCredential = credential,
            CertificateSigningRequest = Google.Protobuf.ByteString.CopyFrom(csr)
        }, cancellationToken: cancellationToken);

        return PersistAcceptedCertificate(response.Accepted, response.FailureReason, response.Certificate.ToByteArray(), response.IssuerCertificate.ToByteArray(), response.ExpiresAtUnixTimeSeconds);
    }

    /// <inheritdoc />
    public async Task<ReverseTunnelAgentCertificateClientResult> RenewAsync(CancellationToken cancellationToken = default)
    {
        var settings = options.Value;
        var csr = materialStore.CreateCertificateSigningRequest(settings.AgentId);
        using var clientCertificate = materialStore.LoadClientCertificate();
        using var channel = CreateChannel(clientCertificate);
        var client = new ReverseTunnelCertificateManagement.ReverseTunnelCertificateManagementClient(channel);
        var response = await client.RenewAsync(new TunnelCertificateRenewalRequest
        {
            ProtocolVersion = ReverseTunnelProtocol.CurrentVersion,
            AgentId = settings.AgentId,
            CertificateSigningRequest = Google.Protobuf.ByteString.CopyFrom(csr)
        }, cancellationToken: cancellationToken);

        return PersistAcceptedCertificate(response.Accepted, response.FailureReason, response.Certificate.ToByteArray(), response.IssuerCertificate.ToByteArray(), response.ExpiresAtUnixTimeSeconds);
    }

    private ReverseTunnelAgentCertificateClientResult PersistAcceptedCertificate(bool accepted, string failureReason, byte[] certificate, byte[] issuerCertificate, long expiresAtUnixTimeSeconds)
    {
        if (!accepted)
        {
            return ReverseTunnelAgentCertificateClientResult.Failure(string.IsNullOrWhiteSpace(failureReason) ? "Server rejected the Agent certificate request." : failureReason);
        }

        materialStore.SaveIssuedCertificate(certificate, issuerCertificate);
        var expiresAtUtc = expiresAtUnixTimeSeconds > 0
            ? DateTimeOffset.FromUnixTimeSeconds(expiresAtUnixTimeSeconds)
            : (DateTimeOffset?)null;
        return ReverseTunnelAgentCertificateClientResult.Success(expiresAtUtc);
    }

    private GrpcChannel CreateChannel(X509Certificate2? clientCertificate)
    {
        var settings = options.Value;
        if (string.IsNullOrWhiteSpace(settings.ServerAddress))
        {
            throw new InvalidOperationException("ReverseTunnelSettings:ServerAddress is required for Agent certificate calls.");
        }

        var handler = new HttpClientHandler();
        if (clientCertificate is not null)
        {
            handler.ClientCertificates.Add(clientCertificate);
        }

        ConfigureTrustedIssuer(handler, settings.AgentCertificate.TrustedIssuerPath);
        return GrpcChannel.ForAddress(settings.ServerAddress, new GrpcChannelOptions
        {
            HttpHandler = handler
        });
    }

    private static void ConfigureTrustedIssuer(HttpClientHandler handler, string trustedIssuerPath)
    {
        if (string.IsNullOrWhiteSpace(trustedIssuerPath) || !File.Exists(trustedIssuerPath))
        {
            return;
        }

        handler.ServerCertificateCustomValidationCallback = (_, certificate, chain, errors) =>
        {
            if ((errors & ~SslPolicyErrors.RemoteCertificateChainErrors) != SslPolicyErrors.None)
            {
                return false;
            }

            if (certificate is null)
            {
                return false;
            }

            using var trustedIssuer = X509CertificateLoader.LoadCertificateFromFile(trustedIssuerPath);
            using var serverCertificate = certificate is X509Certificate2 certificate2
                ? new X509Certificate2(certificate2)
                : new X509Certificate2(certificate);
            using var customChain = new X509Chain();
            customChain.ChainPolicy.TrustMode = X509ChainTrustMode.CustomRootTrust;
            customChain.ChainPolicy.CustomTrustStore.Add(trustedIssuer);
            customChain.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;
            return customChain.Build(serverCertificate);
        };
    }
}
