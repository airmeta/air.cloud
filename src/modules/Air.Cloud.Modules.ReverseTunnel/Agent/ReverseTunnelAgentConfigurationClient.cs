using Air.Cloud.Modules.ReverseTunnel.Common.Options;
using Air.Cloud.Modules.ReverseTunnel.Common.Protocol;
using Air.Cloud.Modules.ReverseTunnel.Common.Runtime;
using Air.Cloud.Modules.ReverseTunnel.Server.Grpc;

using Grpc.Net.Client;

using Microsoft.Extensions.Options;

using System.Net.Security;
using System.Security.Cryptography.X509Certificates;

namespace Air.Cloud.Modules.ReverseTunnel.Agent;

/// <summary>
/// <para>zh-cn:基于 Agent 当前 mTLS 证书主动查询 Server 目标配置的 gRPC 客户端。</para>
/// <para>en-us:gRPC client that actively queries the Server's desired configuration with the Agent's current mTLS certificate.</para>
/// </summary>
public sealed class ReverseTunnelAgentConfigurationClient : IReverseTunnelAgentConfigurationClient
{
    private readonly IReverseTunnelRuntimeOptionsAccessor options;
    private readonly IReverseTunnelAgentConfigurationStore configurationStore;
    private readonly IReverseTunnelAgentCertificateMaterialStore materialStore;

    /// <summary>
    /// <para>zh-cn:创建主动配置查询客户端。</para>
    /// <para>en-us:Creates an active configuration-fetch client.</para>
    /// </summary>
    public ReverseTunnelAgentConfigurationClient(
        IReverseTunnelRuntimeOptionsAccessor options,
        IReverseTunnelAgentConfigurationStore configurationStore,
        IReverseTunnelAgentCertificateMaterialStore materialStore)
    {
        this.options = options;
        this.configurationStore = configurationStore;
        this.materialStore = materialStore;
    }

    /// <inheritdoc />
    public async Task<ReverseTunnelAgentConfigurationFetchResult> FetchAsync(CancellationToken cancellationToken = default)
    {
        var settings = options.Current;
        using var clientCertificate = materialStore.LoadClientCertificate();
        using var channel = CreateChannel(settings, clientCertificate);
        var client = new ReverseTunnelConfiguration.ReverseTunnelConfigurationClient(channel);
        var response = await client.GetDesiredConfigurationAsync(new TunnelConfigurationRequest
        {
            ProtocolVersion = ReverseTunnelProtocol.CurrentVersion,
            AgentId = settings.AgentId,
            ConfigurationVersion = configurationStore.CurrentVersion
        }, cancellationToken: cancellationToken);

        if (!response.Available || response.ConfigurationSnapshot.IsEmpty)
        {
            return new ReverseTunnelAgentConfigurationFetchResult(null, response.ConfigurationVersion);
        }

        var snapshot = ReverseTunnelAgentConfigurationSnapshotSerializer.Deserialize(response.ConfigurationSnapshot.Span);
        if (snapshot.Version != response.ConfigurationVersion ||
            !string.Equals(snapshot.AgentId, settings.AgentId, StringComparison.OrdinalIgnoreCase) ||
            !ReverseTunnelAgentConfigurationSnapshotSerializer.VerifyHash(snapshot))
        {
            return new ReverseTunnelAgentConfigurationFetchResult(
                null,
                response.ConfigurationVersion,
                "Server returned an invalid configuration snapshot.");
        }

        return new ReverseTunnelAgentConfigurationFetchResult(snapshot, snapshot.Version);
    }

    private static GrpcChannel CreateChannel(ReverseTunnelOptions settings, X509Certificate2 clientCertificate)
    {
        if (string.IsNullOrWhiteSpace(settings.ServerAddress))
        {
            throw new InvalidOperationException("ReverseTunnelSettings:ServerAddress is required for configuration synchronization.");
        }

        var handler = new HttpClientHandler();
        handler.ClientCertificates.Add(clientCertificate);
        if (!string.IsNullOrWhiteSpace(settings.AgentCertificate.TrustedIssuerPath) &&
            File.Exists(settings.AgentCertificate.TrustedIssuerPath))
        {
            var trustedIssuerPath = settings.AgentCertificate.TrustedIssuerPath;
            handler.ServerCertificateCustomValidationCallback = (_, certificate, _, errors) =>
            {
                if ((errors & ~SslPolicyErrors.RemoteCertificateChainErrors) != SslPolicyErrors.None || certificate is null)
                {
                    return false;
                }

                using var trustedIssuer = X509CertificateLoader.LoadCertificateFromFile(trustedIssuerPath);
                using var serverCertificate = certificate is X509Certificate2 certificate2
                    ? new X509Certificate2(certificate2)
                    : new X509Certificate2(certificate);
                using var chain = new X509Chain();
                chain.ChainPolicy.TrustMode = X509ChainTrustMode.CustomRootTrust;
                chain.ChainPolicy.CustomTrustStore.Add(trustedIssuer);
                chain.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;
                return chain.Build(serverCertificate);
            };
        }

        return GrpcChannel.ForAddress(settings.ServerAddress, new GrpcChannelOptions
        {
            HttpHandler = handler
        });
    }
}
