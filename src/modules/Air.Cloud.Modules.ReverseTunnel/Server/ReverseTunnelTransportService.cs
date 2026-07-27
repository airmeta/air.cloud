using Air.Cloud.Modules.ReverseTunnel.Common.Protocol;
using Air.Cloud.Modules.ReverseTunnel.Common.Options;
using Air.Cloud.Modules.ReverseTunnel.Server.Grpc;

using Grpc.Core;
using Microsoft.Extensions.Options;

namespace Air.Cloud.Modules.ReverseTunnel.Server;

internal sealed class ReverseTunnelTransportService : ReverseTunnelTransport.ReverseTunnelTransportBase
{
    private readonly IReverseTunnelAgentConnectionAuthenticator connectionAuthenticator;
    private readonly IReverseTunnelAgentServiceAuthorizer serviceAuthorizer;
    private readonly IReverseTunnelSessionRegistry sessionRegistry;
    private readonly IOptions<ReverseTunnelOptions> options;

    public ReverseTunnelTransportService(
        IReverseTunnelAgentConnectionAuthenticator connectionAuthenticator,
        IReverseTunnelAgentServiceAuthorizer serviceAuthorizer,
        IReverseTunnelSessionRegistry sessionRegistry,
        IOptions<ReverseTunnelOptions> options)
    {
        this.connectionAuthenticator = connectionAuthenticator;
        this.serviceAuthorizer = serviceAuthorizer;
        this.sessionRegistry = sessionRegistry;
        this.options = options;
    }

    public override async Task Connect(IAsyncStreamReader<TunnelFrameMessage> requestStream, IServerStreamWriter<TunnelFrameMessage> responseStream, ServerCallContext context)
    {
        if (!await requestStream.MoveNext(context.CancellationToken))
        {
            return;
        }

        var hello = requestStream.Current;
        if (hello.Kind != TunnelFrameKindMessage.Hello)
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Reverse tunnel stream must start with a Hello frame."));
        }

        var httpContext = context.GetHttpContext();
        var authentication = connectionAuthenticator.Authenticate(httpContext.Connection.ClientCertificate, hello.AgentId, DateTimeOffset.UtcNow);
        if (!authentication.Succeeded || string.IsNullOrWhiteSpace(authentication.AgentId))
        {
            throw new RpcException(new Status(StatusCode.Unauthenticated, authentication.FailureReason ?? "Agent certificate authentication failed."));
        }

        var authorization = serviceAuthorizer.Authorize(authentication.AgentId, hello.ServiceKeys);
        if (!authorization.Succeeded)
        {
            throw new RpcException(new Status(StatusCode.PermissionDenied, authorization.FailureReason ?? "Agent is not authorized to declare requested reverse tunnel services."));
        }

        var sessionId = string.IsNullOrWhiteSpace(hello.SessionId) ? Guid.NewGuid().ToString("N") : hello.SessionId.Trim();
        var settings = options.Value;
        var session = new ReverseTunnelSession(
            sessionId,
            authentication.AgentId,
            hello.ServiceKeys,
            DateTimeOffset.UtcNow,
            settings.MaxConcurrentStreams,
            settings.OutboundFrameQueueCapacity,
            settings.MaxFramePayloadBytes,
            settings.MaxResponseBodyBytes,
            settings.MaxRequestBodyBytes);
        sessionRegistry.Register(session);
        var outboundWriterTask = WriteOutboundFramesAsync(session, responseStream, context.CancellationToken);

        await session.EnqueueAsync(new TunnelFrameMessage
        {
            ProtocolVersion = ReverseTunnelProtocol.CurrentVersion,
            SessionId = sessionId,
            Kind = TunnelFrameKindMessage.Welcome,
            AgentId = session.AgentId
        }, context.CancellationToken);

        try
        {
            while (await requestStream.MoveNext(context.CancellationToken))
            {
                var frame = requestStream.Current;
                if (frame.Kind == TunnelFrameKindMessage.Ping)
                {
                    session.Touch(DateTimeOffset.UtcNow);
                    await session.EnqueueAsync(new TunnelFrameMessage
                    {
                        ProtocolVersion = ReverseTunnelProtocol.CurrentVersion,
                        SessionId = sessionId,
                        Kind = TunnelFrameKindMessage.Pong,
                        AgentId = session.AgentId
                    }, context.CancellationToken);
                    continue;
                }

                await session.HandleInboundFrameAsync(frame, context.CancellationToken);
            }
        }
        finally
        {
            sessionRegistry.Remove(sessionId);
            session.CompleteOutbound();
            await AwaitOutboundWriterAsync(outboundWriterTask);
        }
    }

    private static async Task WriteOutboundFramesAsync(ReverseTunnelSession session, IServerStreamWriter<TunnelFrameMessage> responseStream, CancellationToken cancellationToken)
    {
        await foreach (var frame in session.OutboundFrames.ReadAllAsync(cancellationToken))
        {
            await responseStream.WriteAsync(frame, cancellationToken);
        }
    }

    private static async Task AwaitOutboundWriterAsync(Task outboundWriterTask)
    {
        try
        {
            await outboundWriterTask;
        }
        catch (OperationCanceledException)
        {
        }
        catch (InvalidOperationException)
        {
        }
    }
}
