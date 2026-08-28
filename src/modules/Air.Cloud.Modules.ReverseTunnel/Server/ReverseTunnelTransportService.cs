using Air.Cloud.Modules.ReverseTunnel.Common.Protocol;
using Air.Cloud.Modules.ReverseTunnel.Common.Options;
using Air.Cloud.Modules.ReverseTunnel.Common.Runtime;
using Air.Cloud.Modules.ReverseTunnel.Server.Grpc;

using Grpc.Core;
using Microsoft.Extensions.Options;

namespace Air.Cloud.Modules.ReverseTunnel.Server;

internal sealed class ReverseTunnelTransportService : ReverseTunnelTransport.ReverseTunnelTransportBase
{
    private readonly IReverseTunnelAgentConnectionAuthenticator connectionAuthenticator;
    private readonly IReverseTunnelAgentServiceAuthorizer serviceAuthorizer;
    private readonly IReverseTunnelSessionRegistry sessionRegistry;
    private readonly IReverseTunnelDesiredAgentConfigurationStore desiredConfigurations;
    private readonly IReverseTunnelRuntimeOptionsAccessor options;

    public ReverseTunnelTransportService(
        IReverseTunnelAgentConnectionAuthenticator connectionAuthenticator,
        IReverseTunnelAgentServiceAuthorizer serviceAuthorizer,
        IReverseTunnelSessionRegistry sessionRegistry,
        IReverseTunnelDesiredAgentConfigurationStore desiredConfigurations,
        IReverseTunnelRuntimeOptionsAccessor options)
    {
        this.connectionAuthenticator = connectionAuthenticator;
        this.serviceAuthorizer = serviceAuthorizer;
        this.sessionRegistry = sessionRegistry;
        this.desiredConfigurations = desiredConfigurations;
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
        var settings = options.Current;
        var session = new ReverseTunnelSession(
            sessionId,
            authentication.AgentId,
            hello.ServiceKeys,
            DateTimeOffset.UtcNow,
            settings.MaxConcurrentStreams,
            settings.OutboundFrameQueueCapacity,
            settings.MaxFramePayloadBytes,
            settings.MaxResponseBodyBytes,
            settings.MaxRequestBodyBytes,
            () =>
            {
                var current = options.Current;
                return new ReverseTunnelSessionLimits(
                    current.MaxConcurrentStreams,
                    current.OutboundFrameQueueCapacity,
                    current.MaxFramePayloadBytes,
                    current.MaxRequestBodyBytes,
                    current.MaxResponseBodyBytes);
            },
            authentication.IssuerThumbprint,
            authentication.SerialNumber,
            authentication.Subject,
            hello.ConfigurationVersion);
        sessionRegistry.Register(session);
        var outboundWriterTask = WriteOutboundFramesAsync(session, responseStream, context.CancellationToken);

        await session.EnqueueAsync(new TunnelFrameMessage
        {
            ProtocolVersion = ReverseTunnelProtocol.CurrentVersion,
            SessionId = sessionId,
            Kind = TunnelFrameKindMessage.Welcome,
            AgentId = session.AgentId
        }, context.CancellationToken);

        var desiredSnapshot = await desiredConfigurations.GetAsync(session.AgentId, context.CancellationToken);
        // A standalone Server replays only an explicitly stored desired snapshot.
        // A combined Server+Agent host keeps the existing local synchronization fallback.
        if (desiredSnapshot is null &&
            settings.TunnelMode == ReverseTunnelMode.Agent &&
            string.Equals(settings.AgentId, session.AgentId, StringComparison.OrdinalIgnoreCase))
        {
            desiredSnapshot = ReverseTunnelAgentConfigurationSnapshotSerializer.Create(
                session.AgentId,
                settings,
                Math.Max(1, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()));
        }

        if (desiredSnapshot is not null && desiredSnapshot.Version > hello.ConfigurationVersion)
        {
            session.MarkConfigurationOffer(desiredSnapshot.Version);
            await session.EnqueueAsync(new TunnelFrameMessage
            {
                ProtocolVersion = ReverseTunnelProtocol.CurrentVersion,
                SessionId = sessionId,
                StreamId = 0,
                Kind = TunnelFrameKindMessage.ConfigurationOffer,
                Payload = Google.Protobuf.ByteString.CopyFrom(System.Text.Encoding.UTF8.GetBytes(
                    ReverseTunnelAgentConfigurationSnapshotSerializer.Serialize(desiredSnapshot)))
            }, context.CancellationToken);
        }

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
