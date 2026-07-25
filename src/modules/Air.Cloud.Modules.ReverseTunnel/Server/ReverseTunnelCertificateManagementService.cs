using Air.Cloud.Modules.ReverseTunnel.Common.Protocol;
using Air.Cloud.Modules.ReverseTunnel.Server.Grpc;

using Google.Protobuf;

using Grpc.Core;

namespace Air.Cloud.Modules.ReverseTunnel.Server;

internal sealed class ReverseTunnelCertificateManagementService : ReverseTunnelCertificateManagement.ReverseTunnelCertificateManagementBase
{
    private readonly IReverseTunnelAgentCertificateRenewalService renewalService;

    public ReverseTunnelCertificateManagementService(IReverseTunnelAgentCertificateRenewalService renewalService)
    {
        this.renewalService = renewalService;
    }

    public override Task<TunnelCertificateRenewalResponse> Renew(TunnelCertificateRenewalRequest request, ServerCallContext context)
    {
        if (request.ProtocolVersion != ReverseTunnelProtocol.CurrentVersion)
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Unsupported reverse tunnel certificate-renewal protocol version."));
        }

        var httpContext = context.GetHttpContext();
        var result = renewalService.Renew(
            httpContext.Connection.ClientCertificate,
            request.AgentId,
            request.CertificateSigningRequest.ToByteArray(),
            DateTimeOffset.UtcNow);

        return Task.FromResult(new TunnelCertificateRenewalResponse
        {
            Accepted = result.Succeeded,
            FailureReason = result.FailureReason ?? string.Empty,
            Certificate = ByteString.CopyFrom(result.Certificate),
            IssuerCertificate = ByteString.CopyFrom(result.IssuerCertificate),
            ExpiresAtUnixTimeSeconds = result.ExpiresAtUtc?.ToUnixTimeSeconds() ?? 0
        });
    }
}
