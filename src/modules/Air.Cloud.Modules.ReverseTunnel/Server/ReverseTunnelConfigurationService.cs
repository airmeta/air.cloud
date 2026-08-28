using Air.Cloud.Modules.ReverseTunnel.Common.Protocol;
using Air.Cloud.Modules.ReverseTunnel.Common.Runtime;
using Air.Cloud.Modules.ReverseTunnel.Server.Grpc;

using Grpc.Core;

namespace Air.Cloud.Modules.ReverseTunnel.Server;

/// <summary>
/// <para>zh-cn:提供 Agent 通过已建立的 mTLS 身份主动查询 Server 目标配置的 gRPC 服务。响应只包含已保存的运行期配置快照，不包含证书私钥、CA 密码或 Enrollment 凭据。</para>
/// <para>en-us: gRPC service that lets an Agent actively query the Server's desired configuration through its authenticated mTLS identity. Responses contain only the stored runtime snapshot and never certificate private keys, CA passwords, or enrollment credentials.</para>
/// </summary>
internal sealed class ReverseTunnelConfigurationService : ReverseTunnelConfiguration.ReverseTunnelConfigurationBase
{
    private readonly IReverseTunnelAgentConnectionAuthenticator connectionAuthenticator;
    private readonly IReverseTunnelDesiredAgentConfigurationStore desiredConfigurations;

    public ReverseTunnelConfigurationService(
        IReverseTunnelAgentConnectionAuthenticator connectionAuthenticator,
        IReverseTunnelDesiredAgentConfigurationStore desiredConfigurations)
    {
        this.connectionAuthenticator = connectionAuthenticator;
        this.desiredConfigurations = desiredConfigurations;
    }

    /// <summary>
    /// <para>zh-cn:按 AgentId 和本地版本返回更新后的目标配置；没有更新时返回 available=false。</para>
    /// <para>en-us:Returns a newer desired configuration for the authenticated AgentId, or available=false when the local version is current.</para>
    /// </summary>
    public override async Task<TunnelConfigurationResponse> GetDesiredConfiguration(
        TunnelConfigurationRequest request,
        ServerCallContext context)
    {
        if (request.ProtocolVersion != ReverseTunnelProtocol.CurrentVersion)
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Unsupported reverse tunnel configuration protocol version."));
        }

        var authentication = connectionAuthenticator.Authenticate(
            context.GetHttpContext().Connection.ClientCertificate,
            request.AgentId,
            DateTimeOffset.UtcNow);
        if (!authentication.Succeeded || string.IsNullOrWhiteSpace(authentication.AgentId))
        {
            throw new RpcException(new Status(
                StatusCode.Unauthenticated,
                authentication.FailureReason ?? "Agent certificate authentication failed."));
        }

        var snapshot = await desiredConfigurations.GetAsync(authentication.AgentId, context.CancellationToken);
        if (snapshot is null || snapshot.Version <= request.ConfigurationVersion)
        {
            return new TunnelConfigurationResponse
            {
                Available = false,
                ConfigurationVersion = snapshot?.Version ?? request.ConfigurationVersion
            };
        }

        return new TunnelConfigurationResponse
        {
            Available = true,
            ConfigurationVersion = snapshot.Version,
            ConfigurationSnapshot = Google.Protobuf.ByteString.CopyFromUtf8(
                ReverseTunnelAgentConfigurationSnapshotSerializer.Serialize(snapshot))
        };
    }
}
