using Air.Cloud.Modules.ReverseTunnel.Common.Options;
using Air.Cloud.Modules.ReverseTunnel.Common.Protocol;
using Air.Cloud.Modules.ReverseTunnel.Server.Grpc;

using Google.Protobuf;

using Microsoft.Extensions.Options;

namespace Air.Cloud.Modules.ReverseTunnel.Agent;

/// <summary>
/// <para>zh-cn:默认 Agent 隧道帧处理器。当前实现按流聚合请求体，在收到 Complete 后执行本地 HTTP 分发并生成响应头、响应体和完成帧。</para>
/// <para>en-us:Default Agent tunnel frame processor. The current implementation aggregates request bodies by stream and dispatches local HTTP after Complete, then produces response-header, response-body, and completion frames.</para>
/// </summary>
internal sealed class ReverseTunnelAgentFrameProcessor : IReverseTunnelAgentFrameProcessor
{
    private readonly IReverseTunnelAgentLocalHttpDispatcher localHttpDispatcher;
    private readonly IOptions<ReverseTunnelOptions> options;
    private readonly Dictionary<long, PendingRequest> pendingRequests = [];

    /// <summary>
    /// <para>zh-cn:创建 Agent 隧道帧处理器。</para>
    /// <para>en-us:Creates an Agent tunnel frame processor.</para>
    /// </summary>
    /// <param name="localHttpDispatcher">
    /// <para>zh-cn:把完整请求分发给 Agent 本地 HTTP 服务的分发器。</para>
    /// <para>en-us:Dispatcher that sends complete requests to Agent local HTTP services.</para>
    /// </param>
    /// <param name="options">
    /// <para>zh-cn:包含最大请求体大小等限制的反向隧道配置。</para>
    /// <para>en-us:Reverse tunnel options containing limits such as maximum request body size.</para>
    /// </param>
    public ReverseTunnelAgentFrameProcessor(IReverseTunnelAgentLocalHttpDispatcher localHttpDispatcher, IOptions<ReverseTunnelOptions> options)
    {
        this.localHttpDispatcher = localHttpDispatcher;
        this.options = options;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<TunnelFrameMessage>> ProcessAsync(TunnelFrameMessage frame, CancellationToken cancellationToken = default)
    {
        if (frame is null)
        {
            throw new ArgumentNullException(nameof(frame));
        }

        return frame.Kind switch
        {
            TunnelFrameKindMessage.OpenRequest => OpenRequest(frame),
            TunnelFrameKindMessage.RequestBody => AppendRequestBody(frame),
            TunnelFrameKindMessage.Complete => await CompleteRequestAsync(frame, cancellationToken),
            TunnelFrameKindMessage.Cancel => CancelRequest(frame),
            TunnelFrameKindMessage.Pong => Array.Empty<TunnelFrameMessage>(),
            TunnelFrameKindMessage.Welcome => Array.Empty<TunnelFrameMessage>(),
            _ => CreateError(frame, "unsupported_frame", $"Agent does not support inbound frame kind '{frame.Kind}'.")
        };
    }

    private IReadOnlyList<TunnelFrameMessage> OpenRequest(TunnelFrameMessage frame)
    {
        if (frame.StreamId <= 0)
        {
            return CreateError(frame, "invalid_stream", "OpenRequest must use a positive stream identifier.");
        }

        if (pendingRequests.ContainsKey(frame.StreamId))
        {
            return CreateError(frame, "duplicate_stream", "Request stream is already open.");
        }

        try
        {
            var request = ReverseTunnelPayloadSerializer.DeserializeRequest(frame.Payload.ToByteArray());
            pendingRequests[frame.StreamId] = new PendingRequest(request);
            return Array.Empty<TunnelFrameMessage>();
        }
        catch (Exception exception)
        {
            return CreateError(frame, "invalid_request_payload", exception.Message);
        }
    }

    private IReadOnlyList<TunnelFrameMessage> AppendRequestBody(TunnelFrameMessage frame)
    {
        if (!pendingRequests.TryGetValue(frame.StreamId, out var pendingRequest))
        {
            return CreateError(frame, "unknown_stream", "Request body arrived for an unknown stream.");
        }

        if (pendingRequest.Body.Length + frame.Payload.Length > options.Value.MaxRequestBodyBytes)
        {
            pendingRequests.Remove(frame.StreamId);
            return CreateError(frame, "request_body_too_large", "Reverse tunnel request body exceeds the configured Agent limit.");
        }

        pendingRequest.Body.Write(frame.Payload.Span);
        return Array.Empty<TunnelFrameMessage>();
    }

    private async Task<IReadOnlyList<TunnelFrameMessage>> CompleteRequestAsync(TunnelFrameMessage frame, CancellationToken cancellationToken)
    {
        if (!pendingRequests.Remove(frame.StreamId, out var pendingRequest))
        {
            return CreateError(frame, "unknown_stream", "Complete arrived for an unknown stream.");
        }

        try
        {
            var response = await localHttpDispatcher.DispatchAsync(pendingRequest.Request, pendingRequest.Body.ToArray(), cancellationToken);
            var frames = new List<TunnelFrameMessage>
            {
                new()
                {
                    ProtocolVersion = ReverseTunnelProtocol.CurrentVersion,
                    SessionId = frame.SessionId,
                    StreamId = frame.StreamId,
                    Kind = TunnelFrameKindMessage.ResponseHeaders,
                    Payload = ByteString.CopyFrom(ReverseTunnelPayloadSerializer.SerializeResponse(response.Headers))
                }
            };

            AddBodyFrames(frames, frame, response.Body);

            frames.Add(new TunnelFrameMessage
            {
                ProtocolVersion = ReverseTunnelProtocol.CurrentVersion,
                SessionId = frame.SessionId,
                StreamId = frame.StreamId,
                Kind = TunnelFrameKindMessage.Complete
            });
            return frames;
        }
        catch (Exception exception)
        {
            return CreateError(frame, "local_dispatch_failed", exception.Message);
        }
    }

    private IReadOnlyList<TunnelFrameMessage> CancelRequest(TunnelFrameMessage frame)
    {
        pendingRequests.Remove(frame.StreamId);
        return Array.Empty<TunnelFrameMessage>();
    }

    private static IReadOnlyList<TunnelFrameMessage> CreateError(TunnelFrameMessage frame, string code, string message)
    {
        return
        [
            new TunnelFrameMessage
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
            }
        ];
    }

    private void AddBodyFrames(ICollection<TunnelFrameMessage> frames, TunnelFrameMessage sourceFrame, byte[] body)
    {
        if (body is not { Length: > 0 })
        {
            return;
        }

        var maxFramePayloadBytes = Math.Max(1_024, options.Value.MaxFramePayloadBytes);
        for (var offset = 0; offset < body.Length; offset += maxFramePayloadBytes)
        {
            var count = Math.Min(maxFramePayloadBytes, body.Length - offset);
            frames.Add(new TunnelFrameMessage
            {
                ProtocolVersion = ReverseTunnelProtocol.CurrentVersion,
                SessionId = sourceFrame.SessionId,
                StreamId = sourceFrame.StreamId,
                Kind = TunnelFrameKindMessage.ResponseBody,
                Payload = ByteString.CopyFrom(body, offset, count)
            });
        }
    }

    private sealed record PendingRequest(ReverseTunnelHttpRequestPayload Request)
    {
        public MemoryStream Body { get; } = new();
    }
}
