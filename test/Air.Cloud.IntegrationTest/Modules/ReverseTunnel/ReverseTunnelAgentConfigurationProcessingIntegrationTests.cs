using Air.Cloud.Modules.ReverseTunnel.Agent;
using Air.Cloud.Modules.ReverseTunnel.Agent.Tcp;
using Air.Cloud.Modules.ReverseTunnel.Agent.Udp;
using Air.Cloud.Modules.ReverseTunnel.Common.Options;
using Air.Cloud.Modules.ReverseTunnel.Common.Protocol;
using Air.Cloud.Modules.ReverseTunnel.Common.Runtime;
using Air.Cloud.Modules.ReverseTunnel.Server.Grpc;

using Google.Protobuf;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

using System.Threading.Channels;

namespace Air.Cloud.IntegrationTest.Modules.ReverseTunnel;

/// <summary>
/// <para>zh-cn:验证 Agent 运行期配置下发、持久化和失败回滚。</para>
/// <para>en-us:Verifies Agent runtime configuration application, persistence, and rollback on failure.</para>
/// </summary>
public sealed class ReverseTunnelAgentConfigurationProcessingIntegrationTests
{
    /// <summary>
    /// <para>zh-cn:验证有效配置会更新运行期配置并保存快照。</para>
    /// <para>en-us:Verifies that a valid offer updates runtime options and persists a snapshot.</para>
    /// </summary>
    [Fact]
    public async Task Agent_should_apply_and_persist_valid_configuration()
    {
        var options = CreateOptions();
        var startupOptions = Options.Create(options);
        var runtimeOptions = new ReverseTunnelRuntimeOptionsAccessor(startupOptions);
        var snapshotPath = CreateSnapshotPath();
        try
        {
            var store = CreateStore(snapshotPath);
            var processor = CreateProcessor(startupOptions, runtimeOptions, store);
            var pushedConfiguration = ReverseTunnelOptionsCloner.CloneAndNormalize(options);
            pushedConfiguration.MaxConcurrentStreams = 256;
            pushedConfiguration.LocalServices[0].LocalAddress = "http://127.0.0.1:6101";
            var snapshot = ReverseTunnelAgentConfigurationSnapshotSerializer.Create("agent-alpha", pushedConfiguration, 2);

            var responses = await processor.ProcessAsync(CreateOffer(snapshot));

            var response = Assert.Single(responses);
            Assert.Equal(TunnelFrameKindMessage.ConfigurationAck, response.Kind);
            Assert.Equal(2, store.CurrentVersion);
            Assert.Equal(256, runtimeOptions.Current.MaxConcurrentStreams);
            Assert.Equal("http://127.0.0.1:6101", runtimeOptions.Current.LocalServices[0].LocalAddress);
            Assert.True(File.Exists(snapshotPath));
        }
        finally
        {
            DeleteSnapshotDirectory(snapshotPath);
        }
    }

    /// <summary>
    /// <para>zh-cn:验证哈希不匹配时配置被拒绝且旧配置保持不变。</para>
    /// <para>en-us:Verifies that a hash mismatch is rejected and the previous runtime options remain active.</para>
    /// </summary>
    [Fact]
    public async Task Agent_should_reject_tampered_configuration_and_keep_previous_options()
    {
        var options = CreateOptions();
        var startupOptions = Options.Create(options);
        var runtimeOptions = new ReverseTunnelRuntimeOptionsAccessor(startupOptions);
        var snapshotPath = CreateSnapshotPath();
        try
        {
            var store = CreateStore(snapshotPath);
            var processor = CreateProcessor(startupOptions, runtimeOptions, store);
            var tampered = ReverseTunnelAgentConfigurationSnapshotSerializer.Create("agent-alpha", options, 2);
            tampered.Configuration.MaxConcurrentStreams = 1024;

            var responses = await processor.ProcessAsync(CreateOffer(tampered));

            var response = Assert.Single(responses);
            Assert.Equal(TunnelFrameKindMessage.ConfigurationReject, response.Kind);
            Assert.Equal(128, runtimeOptions.Current.MaxConcurrentStreams);
            Assert.Equal(0, store.CurrentVersion);
            Assert.False(File.Exists(snapshotPath));
        }
        finally
        {
            DeleteSnapshotDirectory(snapshotPath);
        }
    }

    private static ReverseTunnelAgentFrameProcessor CreateProcessor(
        IOptions<ReverseTunnelOptions> startupOptions,
        IReverseTunnelRuntimeOptionsAccessor runtimeOptions,
        IReverseTunnelAgentConfigurationStore store)
        => new(
            new NoopLocalHttpDispatcher(),
            new NoopTcpDispatcher(),
            new NoopUdpDispatcher(),
            startupOptions,
            runtimeOptions,
            store);

    private static MountedReverseTunnelAgentConfigurationStore CreateStore(string snapshotPath)
    {
        var directory = Path.GetDirectoryName(snapshotPath)!;
        return new MountedReverseTunnelAgentConfigurationStore(Options.Create(new ReverseTunnelOptions
        {
            AgentId = "agent-alpha",
            AgentCertificate = new ReverseTunnelAgentCertificateOptions
            {
                CertificatePath = Path.Combine(directory, "agent.cer"),
                ConfigurationSnapshotPath = snapshotPath
            }
        }));
    }

    private static ReverseTunnelOptions CreateOptions()
    {
        var options = new ReverseTunnelOptions
        {
            Enabled = true,
            TunnelMode = ReverseTunnelMode.Agent,
            AgentId = "agent-alpha",
            LocalServices =
            [
                new ReverseTunnelLocalServiceOptions
                {
                    ServiceKey = "order-service",
                    LocalAddress = "http://127.0.0.1:5101"
                }
            ]
        };
        options.PostConfigure(options, new ConfigurationBuilder().Build());
        return options;
    }

    private static TunnelFrameMessage CreateOffer(ReverseTunnelAgentConfigurationSnapshot snapshot)
        => new()
        {
            ProtocolVersion = ReverseTunnelProtocol.CurrentVersion,
            SessionId = "session-alpha",
            StreamId = 0,
            Kind = TunnelFrameKindMessage.ConfigurationOffer,
            Payload = ByteString.CopyFromUtf8(ReverseTunnelAgentConfigurationSnapshotSerializer.Serialize(snapshot))
        };

    private static string CreateSnapshotPath()
        => Path.Combine(Path.GetTempPath(), "air-cloud-reverse-tunnel", Guid.NewGuid().ToString("N"), "agent.snapshot.json");

    private static void DeleteSnapshotDirectory(string snapshotPath)
    {
        var directory = Path.GetDirectoryName(snapshotPath);
        if (!string.IsNullOrWhiteSpace(directory) && Directory.Exists(directory))
        {
            Directory.Delete(directory, true);
        }
    }

    private sealed class NoopLocalHttpDispatcher : IReverseTunnelAgentLocalHttpDispatcher
    {
        public Task DispatchAsync(
            ReverseTunnelHttpRequestPayload request,
            ChannelReader<byte[]> requestBodyReader,
            Func<ReverseTunnelHttpResponsePayload, CancellationToken, Task> onResponseHeaders,
            Func<ReadOnlyMemory<byte>, CancellationToken, Task> onResponseBody,
            CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task<ReverseTunnelLocalHttpResponse> DispatchAsync(
            ReverseTunnelHttpRequestPayload request,
            byte[] body,
            CancellationToken cancellationToken = default)
            => Task.FromResult(new ReverseTunnelLocalHttpResponse
            {
                Headers = new ReverseTunnelHttpResponsePayload { StatusCode = 200 }
            });
    }

    private sealed class NoopTcpDispatcher : IReverseTunnelAgentTcpDispatcher
    {
        public Task DispatchAsync(
            ReverseTunnelTcpOpenPayload payload,
            ChannelReader<byte[]> inboundFrames,
            Func<ReadOnlyMemory<byte>, CancellationToken, Task> sendDataAsync,
            Func<CancellationToken, Task> sendCloseAsync,
            Func<string, string, CancellationToken, Task> sendErrorAsync,
            CancellationToken cancellationToken)
            => Task.CompletedTask;
    }

    private sealed class NoopUdpDispatcher : IReverseTunnelAgentUdpDispatcher
    {
        public Task SendAsync(
            long streamId,
            ReverseTunnelUdpDatagramPayload payload,
            Func<ReverseTunnelUdpDatagramPayload, CancellationToken, Task> sendDatagramAsync,
            Func<string, string, CancellationToken, Task> sendErrorAsync,
            CancellationToken cancellationToken)
            => Task.CompletedTask;

        public void Close(long streamId)
        {
        }
    }
}


