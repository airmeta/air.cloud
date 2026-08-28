using Air.Cloud.Modules.ReverseTunnel.Common.Protocol;
using Air.Cloud.Modules.ReverseTunnel.Server.Grpc;

using Grpc.Core;

namespace Air.Cloud.Modules.ReverseTunnel.Server;

internal sealed class ReverseTunnelEnrollmentService : ReverseTunnelEnrollment.ReverseTunnelEnrollmentBase
{
    private readonly IReverseTunnelAgentEnrollmentService enrollmentService;
    private readonly IReverseTunnelDesiredAgentConfigurationStore desiredConfigurations;

    public ReverseTunnelEnrollmentService(
        IReverseTunnelAgentEnrollmentService enrollmentService,
        IReverseTunnelDesiredAgentConfigurationStore desiredConfigurations)
    {
        this.enrollmentService = enrollmentService;
        this.desiredConfigurations = desiredConfigurations;
    }

    public override async Task<TunnelEnrollmentResponse> Enroll(TunnelEnrollmentRequest request, ServerCallContext context)
    {
        if (request.ProtocolVersion != ReverseTunnelProtocol.CurrentVersion)
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Unsupported reverse tunnel enrollment protocol version."));
        }

        var result = enrollmentService.Enroll(
            request.AgentId,
            request.EncryptedGuid.ToByteArray(),
            request.CertificateSigningRequest.ToByteArray(),
            DateTimeOffset.UtcNow);

        var response = new TunnelEnrollmentResponse
        {
            Accepted = result.Succeeded,
            FailureReason = result.FailureReason ?? string.Empty,
            Certificate = Google.Protobuf.ByteString.CopyFrom(result.Certificate),
            IssuerCertificate = Google.Protobuf.ByteString.CopyFrom(result.IssuerCertificate),
            ExpiresAtUnixTimeSeconds = result.ExpiresAtUtc?.ToUnixTimeSeconds() ?? 0
        };

        if (result.Succeeded)
        {
            var snapshot = await desiredConfigurations.GetAsync(request.AgentId, context.CancellationToken);
            if (snapshot is not null)
            {
                response.InitialConfigurationVersion = snapshot.Version;
                response.InitialConfigurationSnapshot = Google.Protobuf.ByteString.CopyFromUtf8(
                    Air.Cloud.Modules.ReverseTunnel.Common.Runtime.ReverseTunnelAgentConfigurationSnapshotSerializer.Serialize(snapshot));
            }
        }

        return response;
    }
}
