using Air.Cloud.Modules.ReverseTunnel.Common.Options;
using Air.Cloud.Modules.ReverseTunnel.Common.Http;
using Air.Cloud.Modules.ReverseTunnel.Common.Protocol;
using Air.Cloud.Modules.ReverseTunnel.Server.Grpc;

using Google.Protobuf;

using Microsoft.Extensions.Options;

using System.Collections.Concurrent;
using System.Threading.Channels;

namespace Air.Cloud.Modules.ReverseTunnel.Agent;

/// <summary>
/// <para>zh-cn:默认 Agent 隧道帧处理器。它在 OpenRequest 到达后立即启动本地 HTTP 调用，并通过有界请求体队列与响应帧回调实现端到端流式转发和取消传播。</para>
/// <para>en-us:Default Agent tunnel frame processor. It starts the local HTTP call when OpenRequest arrives and uses a bounded request-body queue plus response-frame callbacks to provide end-to-end streaming and cancellation propagation.</para>
/// </summary>
internal sealed class ReverseTunnelAgentFrameProcessor : IReverseTunnelAgentFrameProcessor
{
    private readonly IReverseTunnelAgentLocalHttpDispatcher localHttpDispatcher;
    private readonly IOptions<ReverseTunnelOptions> options;
    private readonly ConcurrentDictionary<long, PendingRequest> pendingRequests = new();

    /// <summary>
    /// <para>zh-cn:创建 Agent 隧道帧处理器。</para>
    /// <para>en-us:Creates an Agent tunnel frame processor.</para>
    /// </summary>
    /// <param name="localHttpDispatcher">
    /// <para>zh-cn:把隧道 HTTP 请求流式分发给 Agent 本地 HTTP 服务的分发器。</para>
    /// <para>en-us:Dispatcher that streams tunneled HTTP requests to Agent local HTTP services.</para>
    /// </param>
    /// <param name="options">
    /// <para>zh-cn:包含请求体、响应体、帧大小和队列容量限制的网络穿透配置。</para>
    /// <para>en-us:Network tunneling options containing request-body, response-body, frame-size, and queue-capacity limits.</para>
    /// </param>
    public ReverseTunnelAgentFrameProcessor(IReverseTunnelAgentLocalHttpDispatcher localHttpDispatcher, IOptions<ReverseTunnelOptions> options)
    {
        this.localHttpDispatcher = localHttpDispatcher;
        this.options = options;
    }

    /// <inheritdoc />
    public async Task ProcessAsync(TunnelFrameMessage frame, Func<TunnelFrameMessage, CancellationToken, Task> writeAsync, CancellationToken cancellationToken = default)
    {
        if (frame is null)
        {
            throw new ArgumentNullException(nameof(frame));
        }

        if (writeAsync is null)
        {
            throw new ArgumentNullException(nameof(writeAsync));
        }

        switch (frame.Kind)
        {
            case TunnelFrameKindMessage.OpenRequest:
                await OpenRequestAsync(frame, writeAsync, cancellationToken);
                break;
            case TunnelFrameKindMessage.RequestBody:
                await AppendRequestBodyAsync(frame, writeAsync, cancellationToken);
                break;
            case TunnelFrameKindMessage.Complete:
                CompleteRequest(frame);
                break;
            case TunnelFrameKindMessage.Cancel:
                await CancelRequestAsync(frame);
                break;
            case TunnelFrameKindMessage.Pong:
            case TunnelFrameKindMessage.Welcome:
                break;
            default:
                await writeAsync(CreateError(frame, "unsupported_frame", $"Agent does not support inbound frame kind '{frame.Kind}'."), cancellationToken);
                break;
        }
    }

    internal async Task<IReadOnlyList<TunnelFrameMessage>> ProcessAsync(TunnelFrameMessage frame, CancellationToken cancellationToken = default)
    {
        var frames = new List<TunnelFrameMessage>();
        await ProcessAsync(
            frame,
            (outboundFrame, _) =>
            {
                frames.Add(outboundFrame);
                return Task.CompletedTask;
            },
            cancellationToken);
        return frames;
    }

    private async Task OpenRequestAsync(TunnelFrameMessage frame, Func<TunnelFrameMessage, CancellationToken, Task> writeAsync, CancellationToken cancellationToken)
    {
        if (frame.StreamId <= 0)
        {
            await writeAsync(CreateError(frame, "invalid_stream", "OpenRequest must use a positive stream identifier."), cancellationToken);
            return;
        }

        if (pendingRequests.ContainsKey(frame.StreamId))
        {
            await writeAsync(CreateError(frame, "duplicate_stream", "Request stream is already open."), cancellationToken);
            return;
        }

        ReverseTunnelHttpRequestPayload request;
        try
        {
            request = ReverseTunnelPayloadSerializer.DeserializeRequest(frame.Payload.ToByteArray());
        }
        catch (Exception exception)
        {
            await writeAsync(CreateError(frame, "invalid_request_payload", exception.Message), cancellationToken);
            return;
        }

        var pendingRequest = new PendingRequest(
            request,
            Channel.CreateBounded<byte[]>(new BoundedChannelOptions(Math.Max(16, options.Value.OutboundFrameQueueCapacity))
            {
                SingleReader = true,
                SingleWriter = false,
                FullMode = BoundedChannelFullMode.Wait
            }),
            CancellationTokenSource.CreateLinkedTokenSource(cancellationToken));

        if (!pendingRequests.TryAdd(frame.StreamId, pendingRequest))
        {
            pendingRequest.Cancellation.Dispose();
            await writeAsync(CreateError(frame, "duplicate_stream", "Request stream is already open."), cancellationToken);
            return;
        }

        _ = DispatchRequestAsync(frame, pendingRequest, writeAsync);
    }

    private async Task AppendRequestBodyAsync(TunnelFrameMessage frame, Func<TunnelFrameMessage, CancellationToken, Task> writeAsync, CancellationToken cancellationToken)
    {
        if (!pendingRequests.TryGetValue(frame.StreamId, out var pendingRequest))
        {
            await writeAsync(CreateError(frame, "unknown_stream", "Request body arrived for an unknown stream."), cancellationToken);
            return;
        }

        var bodyBytes = Interlocked.Add(ref pendingRequest.BodyBytes, frame.Payload.Length);
        if (bodyBytes > options.Value.MaxRequestBodyBytes)
        {
            await CompletePendingRequestAsync(frame.StreamId, pendingRequest);
            await writeAsync(CreateError(frame, "request_body_too_large", "Reverse tunnel request body exceeds the configured Agent limit."), cancellationToken);
            return;
        }

        await pendingRequest.Body.Writer.WriteAsync(frame.Payload.ToByteArray(), cancellationToken);
    }

    private void CompleteRequest(TunnelFrameMessage frame)
    {
        if (pendingRequests.TryGetValue(frame.StreamId, out var pendingRequest))
        {
            pendingRequest.Body.Writer.TryComplete();
        }
    }

    private async Task CancelRequestAsync(TunnelFrameMessage frame)
    {
        if (pendingRequests.TryGetValue(frame.StreamId, out var pendingRequest))
        {
            await CompletePendingRequestAsync(frame.StreamId, pendingRequest);
        }
    }

    private async Task DispatchRequestAsync(TunnelFrameMessage openFrame, PendingRequest pendingRequest, Func<TunnelFrameMessage, CancellationToken, Task> writeAsync)
    {
        try
        {
            await localHttpDispatcher.DispatchAsync(
                pendingRequest.Request,
                pendingRequest.Body.Reader,
                async (response, token) => await writeAsync(new TunnelFrameMessage
                {
                    ProtocolVersion = ReverseTunnelProtocol.CurrentVersion,
                    SessionId = openFrame.SessionId,
                    StreamId = openFrame.StreamId,
                    Kind = TunnelFrameKindMessage.ResponseHeaders,
                    Payload = ByteString.CopyFrom(ReverseTunnelPayloadSerializer.SerializeResponse(response))
                }, token),
                async (body, token) =>
                {
                    foreach (var frame in CreateBodyFrames(openFrame, body))
                    {
                        await writeAsync(frame, token);
                    }
                },
                pendingRequest.Cancellation.Token);

            await writeAsync(new TunnelFrameMessage
            {
                ProtocolVersion = ReverseTunnelProtocol.CurrentVersion,
                SessionId = openFrame.SessionId,
                StreamId = openFrame.StreamId,
                Kind = TunnelFrameKindMessage.Complete
            }, pendingRequest.Cancellation.Token);
        }
        catch (OperationCanceledException) when (pendingRequest.Cancellation.IsCancellationRequested)
        {
        }
        catch (ReverseTunnelHttpLimitExceededException exception)
        {
            await writeAsync(CreateError(openFrame, exception.Code, exception.Message), CancellationToken.None);
        }
        catch (Exception exception)
        {
            await writeAsync(CreateError(openFrame, "local_dispatch_failed", exception.Message), CancellationToken.None);
        }
        finally
        {
            await CompletePendingRequestAsync(openFrame.StreamId, pendingRequest);
            pendingRequest.Cancellation.Dispose();
        }
    }

    private async Task CompletePendingRequestAsync(long streamId, PendingRequest pendingRequest)
    {
        pendingRequests.TryRemove(streamId, out _);
        pendingRequest.Body.Writer.TryComplete();
        await pendingRequest.Cancellation.CancelAsync();
    }

    private static TunnelFrameMessage CreateError(TunnelFrameMessage frame, string code, string message)
    {
        return new TunnelFrameMessage
        {
            ProtocolVersion = ReverseTunnelProtocol.CurrentVersion,
            SessionId = frame.SessionId,
            StreamId = Math.Max(0, frame.StreamId),
            Kind = TunnelFrameKindMessage.Error,
            Payload = ByteString.CopyFrom(ReverseTunnelPayloadSerializer.SerializeError(new ReverseTunnelErrorPayload
            {
                Code = code,
                Message = message
            }))
        };
    }

    private IEnumerable<TunnelFrameMessage> CreateBodyFrames(TunnelFrameMessage sourceFrame, ReadOnlyMemory<byte> body)
    {
        var maxFramePayloadBytes = Math.Max(1_024, options.Value.MaxFramePayloadBytes);
        for (var offset = 0; offset < body.Length; offset += maxFramePayloadBytes)
        {
            var count = Math.Min(maxFramePayloadBytes, body.Length - offset);
            yield return new TunnelFrameMessage
            {
                ProtocolVersion = ReverseTunnelProtocol.CurrentVersion,
                SessionId = sourceFrame.SessionId,
                StreamId = sourceFrame.StreamId,
                Kind = TunnelFrameKindMessage.ResponseBody,
                Payload = ByteString.CopyFrom(body.Slice(offset, count).Span)
            };
        }
    }

    private sealed record PendingRequest(
        ReverseTunnelHttpRequestPayload Request,
        Channel<byte[]> Body,
        CancellationTokenSource Cancellation)
    {
        public long BodyBytes;
    }
}
