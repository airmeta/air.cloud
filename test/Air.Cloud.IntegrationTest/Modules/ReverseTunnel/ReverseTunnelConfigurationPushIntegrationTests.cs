using Air.Cloud.Modules.ReverseTunnel.Common.Options;
using Air.Cloud.Modules.ReverseTunnel.Common.Protocol;
using Air.Cloud.Modules.ReverseTunnel.Common.Runtime;
using Air.Cloud.Modules.ReverseTunnel.Server;
using Air.Cloud.Modules.ReverseTunnel.Server.Grpc;

using Google.Protobuf;

namespace Air.Cloud.IntegrationTest.Modules.ReverseTunnel;

/// <summary>
/// <para>zh-cn:验证 Server Session 通过现有双向 gRPC 流下发配置并接收 ACK。</para>
/// <para>en-us:Verifies configuration push and ACK handling over the existing bidirectional gRPC stream.</para>
/// </summary>
public sealed class ReverseTunnelConfigurationPushIntegrationTests
{
    /// <summary>
    /// <para>zh-cn:验证配置下发使用控制帧，不影响业务流标识。</para>
    /// <para>en-us:Verifies that configuration push uses control frames without consuming a business stream identifier.</para>
    /// </summary>
    [Fact]
    public async Task Session_should_push_configuration_and_complete_on_ack()
    {
        var session = new ReverseTunnelSession(
            "session-1",
            "agent-alpha",
            ["order-service"],
            DateTimeOffset.UtcNow);
        var snapshot = ReverseTunnelAgentConfigurationSnapshotSerializer.Create(
            "agent-alpha",
            new ReverseTunnelOptions
            {
                Enabled = true,
                TunnelMode = ReverseTunnelMode.Agent,
                AgentId = "agent-alpha"
            },
            9);

        var pushTask = session.PushConfigurationAsync(snapshot, TimeSpan.FromSeconds(2), CancellationToken.None);
        var outbound = await session.OutboundFrames.ReadAsync();

        Assert.Equal(TunnelFrameKindMessage.ConfigurationOffer, outbound.Kind);
        Assert.Equal(0, outbound.StreamId);

        var ack = new ReverseTunnelAgentConfigurationResult
        {
            Version = 9,
            AgentId = "agent-alpha",
            Accepted = true
        };
        await session.HandleInboundFrameAsync(new TunnelFrameMessage
        {
            ProtocolVersion = ReverseTunnelProtocol.CurrentVersion,
            SessionId = "session-1",
            StreamId = 0,
            Kind = TunnelFrameKindMessage.ConfigurationAck,
            Payload = ByteString.CopyFromUtf8(System.Text.Json.JsonSerializer.Serialize(ack))
        }, CancellationToken.None);

        var result = await pushTask;
        Assert.True(result.Accepted);
        Assert.Equal(9, result.Version);
        Assert.Equal(9, session.ConfigurationVersion);
        Assert.Equal(9, session.DesiredConfigurationVersion);
        Assert.Equal("acknowledged", session.ConfigurationDeliveryStatus);
        Assert.Null(session.ConfigurationDeliveryFailureReason);
        Assert.NotNull(session.ConfigurationDeliveryAtUtc);
    }
}


