using Air.Cloud.Modules.ReverseTunnel.Common.Options;
using Air.Cloud.Modules.ReverseTunnel.Common.Protocol;
using Air.Cloud.Modules.ReverseTunnel.Common.Enrollment;
using Air.Cloud.Modules.ReverseTunnel.Common.Runtime;
using Air.Cloud.Modules.ReverseTunnel.Server.Grpc;

using Grpc.Net.Client;

using Microsoft.Extensions.Options;
using Microsoft.Extensions.DependencyInjection;

using System.Net.Http;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;

namespace Air.Cloud.Modules.ReverseTunnel.Agent;

/// <summary>
/// <para>zh-cn:基于 gRPC 的 Agent 证书生命周期客户端。首次 Enrollment 使用 CA 公钥加密随机 GUID，续签使用当前本地客户端证书建立 mTLS 调用。</para>
/// <para>en-us:gRPC-based Agent certificate lifecycle client. Initial enrollment encrypts a random GUID with the CA public key, while renewal uses the current local client certificate for mTLS.</para>
/// </summary>
public sealed class ReverseTunnelAgentCertificateClient : IReverseTunnelAgentCertificateClient
{
    private readonly IReverseTunnelRuntimeOptionsAccessor options;
    private readonly IReverseTunnelAgentCertificateMaterialStore materialStore;
    private readonly IReverseTunnelAgentConfigurationStore configurationStore;

    /// <summary>
    /// <para>zh-cn:创建 Agent 证书生命周期客户端。</para>
    /// <para>en-us:Creates an Agent certificate lifecycle client.</para>
    /// </summary>
    /// <param name="options">
    /// <para>zh-cn:包含 AgentId、ServerAddress 和本地证书挂载路径的反向隧道配置。</para>
    /// <para>en-us:Reverse tunnel options containing AgentId, ServerAddress, and local certificate mount paths.</para>
    /// </param>
    /// <param name="materialStore">
    /// <para>zh-cn:Agent 侧证书材料存储，用于生成 CSR、加载 CA 公钥、保存证书和加载 mTLS 证书。</para>
    /// <para>en-us:Agent-side certificate material store used to create CSRs, load the CA public key, save certificates, and load mTLS certificates.</para>
    /// </param>
    /// <param name="configurationStore">
    /// <para>zh-cn:用于保存 Enrollment 首次下发的 Agent 配置快照。</para>
    /// <para>en-us:Store used to persist the initial Agent configuration snapshot returned by enrollment.</para>
    /// </param>
    [ActivatorUtilitiesConstructor]
    public ReverseTunnelAgentCertificateClient(
        IReverseTunnelRuntimeOptionsAccessor options,
        IReverseTunnelAgentCertificateMaterialStore materialStore,
        IReverseTunnelAgentConfigurationStore configurationStore)
    {
        this.options = options;
        this.materialStore = materialStore;
        this.configurationStore = configurationStore;
    }

    /// <summary>
    /// <para>zh-cn:兼容未启用配置快照持久化的调用方。</para>
    /// <para>en-us:Compatibility constructor for callers that do not use configuration-snapshot persistence.</para>
    /// </summary>
    internal ReverseTunnelAgentCertificateClient(
        IOptions<ReverseTunnelOptions> options,
        IReverseTunnelAgentCertificateMaterialStore materialStore)
        : this(new ReverseTunnelRuntimeOptionsAccessor(options), materialStore, new InMemoryReverseTunnelAgentConfigurationStore())
    {
    }

    /// <inheritdoc />
    public async Task<ReverseTunnelAgentCertificateClientResult> EnrollAsync(CancellationToken cancellationToken = default)
    {
        var settings = options.Current;
        if (string.IsNullOrWhiteSpace(settings.AgentId))
        {
            throw new InvalidOperationException("ReverseTunnelSettings:AgentId is required for Agent enrollment.");
        }

        using var trustedIssuer = materialStore.LoadTrustedIssuerCertificate();
        var encryptedGuid = ReverseTunnelEnrollmentCipher.EncryptGuid(trustedIssuer, out _);
        var csr = materialStore.CreateCertificateSigningRequest(settings.AgentId);

        using var channel = CreateChannel(clientCertificate: null, trustedIssuer);
        var client = new ReverseTunnelEnrollment.ReverseTunnelEnrollmentClient(channel);
        var response = await client.EnrollAsync(new TunnelEnrollmentRequest
        {
            ProtocolVersion = ReverseTunnelProtocol.CurrentVersion,
            AgentId = settings.AgentId,
            CertificateSigningRequest = Google.Protobuf.ByteString.CopyFrom(csr),
            EncryptedGuid = Google.Protobuf.ByteString.CopyFrom(encryptedGuid)
        }, cancellationToken: cancellationToken);

        var result = PersistAcceptedCertificate(response.Accepted, response.FailureReason, response.Certificate.ToByteArray(), response.IssuerCertificate.ToByteArray(), response.ExpiresAtUnixTimeSeconds);
        if (result.Succeeded && !response.InitialConfigurationSnapshot.IsEmpty)
        {
            var snapshot = ReverseTunnelAgentConfigurationSnapshotSerializer.Deserialize(response.InitialConfigurationSnapshot.Span);
            if (snapshot.Version == response.InitialConfigurationVersion &&
                string.Equals(snapshot.AgentId, settings.AgentId, StringComparison.OrdinalIgnoreCase) &&
                ReverseTunnelAgentConfigurationSnapshotSerializer.VerifyHash(snapshot))
            {
                snapshot.Configuration = ReverseTunnelAgentConfigurationMerger.Merge(settings, snapshot.Configuration);
                ReverseTunnelAgentConfigurationSnapshotSerializer.RefreshHash(snapshot);
                await configurationStore.SaveAsync(snapshot, cancellationToken);
                options.Update(snapshot.Configuration);
            }
        }

        return result;
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

    private GrpcChannel CreateChannel(X509Certificate2? clientCertificate, X509Certificate2? trustedIssuerCertificate = null)
    {
        var settings = options.Current;
        if (string.IsNullOrWhiteSpace(settings.ServerAddress))
        {
            throw new InvalidOperationException("ReverseTunnelSettings:ServerAddress is required for Agent certificate calls.");
        }

        var handler = new HttpClientHandler();
        if (clientCertificate is not null)
        {
            handler.ClientCertificates.Add(clientCertificate);
        }

        ConfigureTrustedIssuer(handler, trustedIssuerCertificate, settings.AgentCertificate.TrustedIssuerPath);
        return GrpcChannel.ForAddress(settings.ServerAddress, new GrpcChannelOptions
        {
            HttpHandler = handler
        });
    }

    private static void ConfigureTrustedIssuer(
        HttpClientHandler handler,
        X509Certificate2? trustedIssuerCertificate,
        string trustedIssuerPath)
    {
        if (trustedIssuerCertificate is null &&
            (string.IsNullOrWhiteSpace(trustedIssuerPath) || !File.Exists(trustedIssuerPath)))
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

            using var trustedIssuer = trustedIssuerCertificate is null
                ? X509CertificateLoader.LoadCertificateFromFile(trustedIssuerPath)
                : new X509Certificate2(trustedIssuerCertificate);
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

    private sealed class InMemoryReverseTunnelAgentConfigurationStore : IReverseTunnelAgentConfigurationStore
    {
        public long CurrentVersion { get; private set; }

        public Task<ReverseTunnelAgentConfigurationSnapshot?> LoadAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<ReverseTunnelAgentConfigurationSnapshot?>(null);

        public Task SaveAsync(ReverseTunnelAgentConfigurationSnapshot snapshot, CancellationToken cancellationToken = default)
        {
            CurrentVersion = snapshot.Version;
            return Task.CompletedTask;
        }
    }
}
