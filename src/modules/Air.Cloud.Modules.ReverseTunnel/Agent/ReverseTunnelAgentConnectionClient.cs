using Air.Cloud.Modules.ReverseTunnel.Common.Options;
using Air.Cloud.Modules.ReverseTunnel.Common.Protocol;
using Air.Cloud.Modules.ReverseTunnel.Server.Grpc;

using Google.Protobuf;

using Grpc.Core;
using Grpc.Net.Client;

using Microsoft.Extensions.Options;

using System.Net.Http;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;

namespace Air.Cloud.Modules.ReverseTunnel.Agent;

/// <summary>
/// <para>zh-cn:基于 gRPC 双向流的 Agent 出站连接客户端。当前实现发送 Hello、周期性 Ping，并处理 Server 下发的 HTTP 请求帧。</para>
/// <para>en-us:gRPC bidirectional-stream Agent outbound connection client. The current implementation sends Hello, sends periodic Ping frames, and processes HTTP request frames sent by the Server.</para>
/// </summary>
internal sealed class ReverseTunnelAgentConnectionClient : IReverseTunnelAgentConnectionClient
{
    private readonly IOptions<ReverseTunnelOptions> options;
    private readonly IReverseTunnelAgentCertificateMaterialStore materialStore;
    private readonly IReverseTunnelAgentFrameProcessor frameProcessor;

    /// <summary>
    /// <para>zh-cn:创建 Agent 出站连接客户端。</para>
    /// <para>en-us:Creates an Agent outbound connection client.</para>
    /// </summary>
    /// <param name="options">
    /// <para>zh-cn:包含 AgentId、ServerAddress、LocalServices 和心跳间隔的反向隧道配置。</para>
    /// <para>en-us:Reverse tunnel options containing AgentId, ServerAddress, LocalServices, and heartbeat interval.</para>
    /// </param>
    /// <param name="materialStore">
    /// <para>zh-cn:用于加载当前 mTLS 客户端证书和可信签发者证书的 Agent 证书材料存储。</para>
    /// <para>en-us:Agent certificate material store used to load the current mTLS client certificate and trusted issuer certificate.</para>
    /// </param>
    /// <param name="frameProcessor">
    /// <para>zh-cn:处理 Server 下发请求帧的 Agent 帧处理器。</para>
    /// <para>en-us:Agent frame processor that handles request frames sent by the Server.</para>
    /// </param>
    public ReverseTunnelAgentConnectionClient(
        IOptions<ReverseTunnelOptions> options,
        IReverseTunnelAgentCertificateMaterialStore materialStore,
        IReverseTunnelAgentFrameProcessor frameProcessor)
    {
        this.options = options;
        this.materialStore = materialStore;
        this.frameProcessor = frameProcessor;
    }

    /// <inheritdoc />
    public async Task RunOnceAsync(CancellationToken cancellationToken = default)
    {
        using var clientCertificate = materialStore.LoadClientCertificate();
        using var channel = CreateChannel(clientCertificate);
        var client = new ReverseTunnelTransport.ReverseTunnelTransportClient(channel);
        using var call = client.Connect(cancellationToken: cancellationToken);
        using var writeLock = new SemaphoreSlim(1, 1);
        using var heartbeatCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var sessionId = Guid.NewGuid().ToString("N");

        await WriteFrameAsync(call.RequestStream, writeLock, new TunnelFrameMessage
        {
            ProtocolVersion = ReverseTunnelProtocol.CurrentVersion,
            SessionId = sessionId,
            Kind = TunnelFrameKindMessage.Hello,
            AgentId = options.Value.AgentId,
            ServiceKeys = { options.Value.LocalServices.Select(item => item.ServiceKey).Where(key => !string.IsNullOrWhiteSpace(key)) }
        }, cancellationToken);

        var heartbeatTask = RunHeartbeatAsync(call.RequestStream, writeLock, sessionId, heartbeatCancellation.Token);
        try
        {
            while (await call.ResponseStream.MoveNext(cancellationToken))
            {
                await frameProcessor.ProcessAsync(
                    call.ResponseStream.Current,
                    (outboundFrame, token) => WriteFrameAsync(call.RequestStream, writeLock, outboundFrame, token),
                    cancellationToken);
            }
        }
        finally
        {
            await heartbeatCancellation.CancelAsync();
            await call.RequestStream.CompleteAsync();
            await AwaitHeartbeatStopAsync(heartbeatTask);
        }
    }

    private async Task RunHeartbeatAsync(IClientStreamWriter<TunnelFrameMessage> requestStream, SemaphoreSlim writeLock, string sessionId, CancellationToken cancellationToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(options.Value.HeartbeatIntervalSeconds));
        while (await timer.WaitForNextTickAsync(cancellationToken))
        {
            await WriteFrameAsync(requestStream, writeLock, new TunnelFrameMessage
            {
                ProtocolVersion = ReverseTunnelProtocol.CurrentVersion,
                SessionId = sessionId,
                Kind = TunnelFrameKindMessage.Ping,
                Payload = ByteString.Empty
            }, cancellationToken);
        }
    }

    private static async Task AwaitHeartbeatStopAsync(Task heartbeatTask)
    {
        try
        {
            await heartbeatTask;
        }
        catch (OperationCanceledException)
        {
        }
        catch (RpcException)
        {
        }
    }

    private static async Task WriteFrameAsync(IClientStreamWriter<TunnelFrameMessage> requestStream, SemaphoreSlim writeLock, TunnelFrameMessage frame, CancellationToken cancellationToken)
    {
        await writeLock.WaitAsync(cancellationToken);
        try
        {
            await requestStream.WriteAsync(frame, cancellationToken);
        }
        finally
        {
            writeLock.Release();
        }
    }

    private GrpcChannel CreateChannel(X509Certificate2 clientCertificate)
    {
        var settings = options.Value;
        if (string.IsNullOrWhiteSpace(settings.ServerAddress))
        {
            throw new InvalidOperationException("ReverseTunnelSettings:ServerAddress is required for Agent connections.");
        }

        var handler = new HttpClientHandler();
        handler.ClientCertificates.Add(clientCertificate);
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

        handler.ServerCertificateCustomValidationCallback = (_, certificate, _, errors) =>
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
