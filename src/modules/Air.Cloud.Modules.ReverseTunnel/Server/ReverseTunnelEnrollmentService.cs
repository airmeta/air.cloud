using Air.Cloud.Modules.ReverseTunnel.Common.Protocol;
using Air.Cloud.Modules.ReverseTunnel.Server.Grpc;

using Grpc.Core;

namespace Air.Cloud.Modules.ReverseTunnel.Server;

internal sealed class ReverseTunnelEnrollmentService : ReverseTunnelEnrollment.ReverseTunnelEnrollmentBase
{
    private readonly IReverseTunnelAgentEnrollmentService enrollmentService;

    public ReverseTunnelEnrollmentService(IReverseTunnelAgentEnrollmentService enrollmentService)
    {
        this.enrollmentService = enrollmentService;
    }

    public override Task<TunnelEnrollmentResponse> Enroll(TunnelEnrollmentRequest request, ServerCallContext context)
    {
        if (request.ProtocolVersion != ReverseTunnelProtocol.CurrentVersion)
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Unsupported reverse tunnel enrollment protocol version."));
        }

        var result = enrollmentService.Enroll(
            request.AgentId,
            request.EnrollmentCredential,
            request.CertificateSigningRequest.ToByteArray(),
            DateTimeOffset.UtcNow.AddMinutes(-1));

        return Task.FromResult(new TunnelEnrollmentResponse
        {
            Accepted = result.Succeeded,
            FailureReason = result.FailureReason ?? string.Empty,
            Certificate = Google.Protobuf.ByteString.CopyFrom(result.Certificate),
            IssuerCertificate = Google.Protobuf.ByteString.CopyFrom(result.IssuerCertificate),
            ExpiresAtUnixTimeSeconds = result.ExpiresAtUtc?.ToUnixTimeSeconds() ?? 0
        });
    }
}
