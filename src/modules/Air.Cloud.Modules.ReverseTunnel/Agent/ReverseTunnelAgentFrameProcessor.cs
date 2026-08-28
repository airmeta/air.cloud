using Air.Cloud.Modules.ReverseTunnel.Common.Options;
using Air.Cloud.Modules.ReverseTunnel.Common.Http;
using Air.Cloud.Modules.ReverseTunnel.Common.Diagnostics;
using Air.Cloud.Modules.ReverseTunnel.Common.Protocol;
using Air.Cloud.Modules.ReverseTunnel.Common.Runtime;
using Air.Cloud.Modules.ReverseTunnel.Agent.Tcp;
using Air.Cloud.Modules.ReverseTunnel.Agent.Udp;
using Air.Cloud.Modules.ReverseTunnel.Server.Grpc;

using Google.Protobuf;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading.Channels;

namespace Air.Cloud.Modules.ReverseTunnel.Agent;

/// <summary>
/// <para>zh-cn:默认 Agent 隧道帧处理器。它在 OpenRequest 到达后立即启动本地 HTTP 调用，并通过有界请求体队列与响应帧回调实现端到端流式转发和取消传播。</para>
/// <para>en-us:Default Agent tunnel frame processor. It starts the local HTTP call when OpenRequest arrives and uses a bounded request-body queue plus response-frame callbacks to provide end-to-end streaming and cancellation propagation.</para>
/// </summary>
internal sealed class ReverseTunnelAgentFrameProcessor : IReverseTunnelAgentFrameProcessor
{
    private readonly IReverseTunnelAgentLocalHttpDispatcher localHttpDispatcher;
    private readonly IReverseTunnelAgentTcpDispatcher tcpDispatcher;
    private readonly IReverseTunnelAgentUdpDispatcher udpDispatcher;
    private readonly IReverseTunnelRuntimeOptionsAccessor options;
    private readonly IReverseTunnelAgentConfigurationStore configurationStore;
    private readonly IEnumerable<IValidateOptions<ReverseTunnelOptions>> validators;
    private readonly ConcurrentDictionary<long, PendingRequest> pendingRequests = new();
    private readonly ConcurrentDictionary<long, PendingTcpConnection> pendingTcpConnections = new();

    /// <summary>
    /// <para>zh-cn:创建 Agent 隧道帧处理器。</para>
    /// <para>en-us:Creates an Agent tunnel frame processor.</para>
    /// </summary>
    /// <param name="localHttpDispatcher">
    /// <para>zh-cn:把隧道 HTTP 请求流式分发给 Agent 本地 HTTP 服务的分发器。</para>
    /// <para>en-us:Dispatcher that streams tunneled HTTP requests to Agent local HTTP services.</para>
    /// </param>
    /// <param name="tcpDispatcher">
    /// <para>zh-cn:把隧道 TCP 字节流分发给 Agent 本地 TCP 服务的分发器。</para>
    /// <para>en-us:Dispatcher that relays tunneled TCP byte streams to Agent local TCP services.</para>
    /// </param>
    /// <param name="udpDispatcher">
    /// <para>zh-cn:把隧道 UDP 数据报分发给 Agent 本地 UDP 服务的分发器。</para>
    /// <para>en-us:Dispatcher that relays tunneled UDP datagrams to Agent local UDP services.</para>
    /// </param>
    /// <param name="startupOptions">
    /// <para>zh-cn:包含请求体、响应体、帧大小和队列容量限制的网络穿透配置。</para>
    /// <para>en-us:Network tunneling options containing request-body, response-body, frame-size, and queue-capacity limits.</para>
    /// </param>
    /// <param name="runtimeOptions">
    /// <para>zh-cn:可选的运行期配置访问器。</para>
    /// <para>en-us:Optional runtime options accessor.</para>
    /// </param>
    /// <param name="configurationStore">
    /// <para>zh-cn:可选的 Agent 配置快照存储。</para>
    /// <para>en-us:Optional Agent configuration snapshot store.</para>
    /// </param>
    /// <param name="validators">
    /// <para>zh-cn:用于校验下发配置的选项校验器集合。</para>
    /// <para>en-us:Option validators used to validate pushed configuration.</para>
    /// </param>
    public ReverseTunnelAgentFrameProcessor(
        IReverseTunnelAgentLocalHttpDispatcher localHttpDispatcher,
        IReverseTunnelAgentTcpDispatcher tcpDispatcher,
        IReverseTunnelAgentUdpDispatcher udpDispatcher,
        IOptions<ReverseTunnelOptions> startupOptions,
        IReverseTunnelRuntimeOptionsAccessor? runtimeOptions = null,
        IReverseTunnelAgentConfigurationStore? configurationStore = null,
        IEnumerable<IValidateOptions<ReverseTunnelOptions>>? validators = null)
    {
        this.localHttpDispatcher = localHttpDispatcher;
        this.tcpDispatcher = tcpDispatcher;
        this.udpDispatcher = udpDispatcher;
        this.options = runtimeOptions ?? new ReverseTunnelRuntimeOptionsAccessor(startupOptions);
        this.configurationStore = configurationStore ?? new InMemoryReverseTunnelAgentConfigurationStore();
        this.validators = validators ?? [];
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
            case TunnelFrameKindMessage.ConfigurationOffer:
                await ProcessConfigurationOfferAsync(frame, writeAsync, cancellationToken);
                break;
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
            case TunnelFrameKindMessage.TcpOpen:
                await OpenTcpAsync(frame, writeAsync, cancellationToken);
                break;
            case TunnelFrameKindMessage.TcpData:
                await AppendTcpDataAsync(frame, writeAsync, cancellationToken);
                break;
            case TunnelFrameKindMessage.TcpClose:
            case TunnelFrameKindMessage.TcpError:
                await CloseTcpAsync(frame);
                break;
            case TunnelFrameKindMessage.UdpDatagram:
                await SendUdpDatagramAsync(frame, writeAsync, cancellationToken);
                break;
            case TunnelFrameKindMessage.UdpFlowClose:
            case TunnelFrameKindMessage.UdpError:
                udpDispatcher.Close(frame.StreamId);
                break;
            case TunnelFrameKindMessage.Pong:
            case TunnelFrameKindMessage.Welcome:
                break;
            default:
                await writeAsync(CreateError(frame, "unsupported_frame", $"Agent does not support inbound frame kind '{frame.Kind}'."), cancellationToken);
                break;
        }
    }

    private async Task ProcessConfigurationOfferAsync(
        TunnelFrameMessage frame,
        Func<TunnelFrameMessage, CancellationToken, Task> writeAsync,
        CancellationToken cancellationToken)
    {
        ReverseTunnelAgentConfigurationSnapshot snapshot;
        try
        {
            snapshot = ReverseTunnelAgentConfigurationSnapshotSerializer.Deserialize(frame.Payload.Span);
            if (!ReverseTunnelAgentConfigurationSnapshotSerializer.VerifyHash(snapshot))
            {
                throw new InvalidOperationException("Configuration snapshot SHA-256 validation failed.");
            }

            var current = options.Current;
            if (!string.Equals(snapshot.AgentId, current.AgentId, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException($"Configuration snapshot AgentId '{snapshot.AgentId}' does not match '{current.AgentId}'.");
            }

            if (snapshot.Version <= configurationStore.CurrentVersion)
            {
                throw new InvalidOperationException($"Configuration snapshot version {snapshot.Version} is not newer than {configurationStore.CurrentVersion}.");
            }

            var normalized = ReverseTunnelAgentConfigurationMerger.Merge(options.Current, snapshot.Configuration);
            var failures = validators
                .Select(validator => validator.Validate(Options.DefaultName, normalized))
                .Where(result => result.Failed)
                .SelectMany(result => result.Failures ?? [])
                .Distinct(StringComparer.Ordinal)
                .ToArray();
            if (failures.Length > 0)
            {
                throw new InvalidOperationException(string.Join("; ", failures));
            }

            snapshot.Configuration = normalized;
            ReverseTunnelAgentConfigurationSnapshotSerializer.RefreshHash(snapshot);
            await configurationStore.SaveAsync(snapshot, cancellationToken);
            options.Update(normalized);
            await writeAsync(CreateConfigurationResult(frame, snapshot, true, null), cancellationToken);
        }
        catch (Exception exception)
        {
            var version = 0L;
            try
            {
                version = ReverseTunnelAgentConfigurationSnapshotSerializer.Deserialize(frame.Payload.Span).Version;
            }
            catch
            {
            }

            await writeAsync(CreateConfigurationResult(frame, new ReverseTunnelAgentConfigurationSnapshot
            {
                Version = version,
                AgentId = options.Current.AgentId
            }, false, exception.Message), cancellationToken);
        }
    }

    private static TunnelFrameMessage CreateConfigurationResult(
        TunnelFrameMessage source,
        ReverseTunnelAgentConfigurationSnapshot snapshot,
        bool accepted,
        string? failureReason)
    {
        var result = new ReverseTunnelAgentConfigurationResult
        {
            Version = snapshot.Version,
            AgentId = snapshot.AgentId,
            Accepted = accepted,
            FailureReason = failureReason
        };
        return new TunnelFrameMessage
        {
            ProtocolVersion = ReverseTunnelProtocol.CurrentVersion,
            SessionId = source.SessionId,
            StreamId = 0,
            Kind = accepted ? TunnelFrameKindMessage.ConfigurationAck : TunnelFrameKindMessage.ConfigurationReject,
            Payload = ByteString.CopyFrom(System.Text.Encoding.UTF8.GetBytes(
                System.Text.Json.JsonSerializer.Serialize(result)))
        };
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
            CancellationTokenSource.CreateLinkedTokenSource(cancellationToken),
            Stopwatch.GetTimestamp());

        if (!pendingRequests.TryAdd(frame.StreamId, pendingRequest))
        {
            pendingRequest.Cancellation.Dispose();
            await writeAsync(CreateError(frame, "duplicate_stream", "Request stream is already open."), cancellationToken);
            return;
        }

        ReverseTunnelMetrics.RequestStarted(request.ServiceKey, options.Value.AgentId);
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
            CompleteMetricsWithFailure(pendingRequest, "request_body_too_large", false);
            await CompletePendingRequestAsync(frame.StreamId, pendingRequest);
            await writeAsync(CreateError(frame, "request_body_too_large", "Reverse tunnel request body exceeds the configured Agent limit."), cancellationToken);
            return;
        }

        await pendingRequest.Body.Writer.WriteAsync(frame.Payload.ToByteArray(), cancellationToken);
        ReverseTunnelMetrics.RequestBodyBytes(pendingRequest.Request.ServiceKey, options.Value.AgentId, frame.Payload.Length);
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
            CompleteMetricsWithFailure(pendingRequest, "cancelled", true);
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
                    ReverseTunnelMetrics.ResponseBodyBytes(pendingRequest.Request.ServiceKey, options.Value.AgentId, body.Length);
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
            CompleteMetricsWithSuccess(pendingRequest);
        }
        catch (OperationCanceledException) when (pendingRequest.Cancellation.IsCancellationRequested)
        {
            CompleteMetricsWithFailure(pendingRequest, "cancelled", true);
        }
        catch (ReverseTunnelHttpLimitExceededException exception)
        {
            CompleteMetricsWithFailure(pendingRequest, exception.Code, false);
            await writeAsync(CreateError(openFrame, exception.Code, exception.Message), CancellationToken.None);
        }
        catch (Exception exception)
        {
            CompleteMetricsWithFailure(pendingRequest, "local_dispatch_failed", false);
            await writeAsync(CreateError(openFrame, "local_dispatch_failed", exception.Message), CancellationToken.None);
        }
        finally
        {
            await CompletePendingRequestAsync(openFrame.StreamId, pendingRequest);
            pendingRequest.Cancellation.Dispose();
        }
    }

    private async Task OpenTcpAsync(TunnelFrameMessage frame, Func<TunnelFrameMessage, CancellationToken, Task> writeAsync, CancellationToken cancellationToken)
    {
        if (frame.StreamId <= 0)
        {
            await writeAsync(CreateError(frame, "invalid_stream", "TcpOpen must use a positive stream identifier."), cancellationToken);
            return;
        }

        ReverseTunnelTcpOpenPayload payload;
        try
        {
            payload = ReverseTunnelPayloadSerializer.DeserializeTcpOpen(frame.Payload.ToByteArray());
        }
        catch (Exception exception)
        {
            await writeAsync(CreateError(frame, "invalid_tcp_open_payload", exception.Message), cancellationToken);
            return;
        }

        var pendingConnection = new PendingTcpConnection(
            payload,
            Channel.CreateBounded<byte[]>(new BoundedChannelOptions(Math.Max(16, options.Value.OutboundFrameQueueCapacity))
            {
                SingleReader = true,
                SingleWriter = false,
                FullMode = BoundedChannelFullMode.Wait
            }),
            CancellationTokenSource.CreateLinkedTokenSource(cancellationToken));

        if (!pendingTcpConnections.TryAdd(frame.StreamId, pendingConnection))
        {
            pendingConnection.Cancellation.Dispose();
            await writeAsync(CreateError(frame, "duplicate_tcp_stream", "TCP stream is already open."), cancellationToken);
            return;
        }

        _ = DispatchTcpAsync(frame, pendingConnection, writeAsync);
    }

    private async Task AppendTcpDataAsync(TunnelFrameMessage frame, Func<TunnelFrameMessage, CancellationToken, Task> writeAsync, CancellationToken cancellationToken)
    {
        if (!pendingTcpConnections.TryGetValue(frame.StreamId, out var pendingConnection))
        {
            await writeAsync(CreateError(frame, "unknown_tcp_stream", "TCP data arrived for an unknown stream."), cancellationToken);
            return;
        }

        await pendingConnection.InboundFrames.Writer.WriteAsync(frame.Payload.ToByteArray(), cancellationToken);
    }

    private async Task CloseTcpAsync(TunnelFrameMessage frame)
    {
        if (!pendingTcpConnections.TryGetValue(frame.StreamId, out var pendingConnection))
        {
            return;
        }

        pendingConnection.InboundFrames.Writer.TryComplete();
        await pendingConnection.Cancellation.CancelAsync();
    }

    private async Task DispatchTcpAsync(TunnelFrameMessage openFrame, PendingTcpConnection pendingConnection, Func<TunnelFrameMessage, CancellationToken, Task> writeAsync)
    {
        try
        {
            await tcpDispatcher.DispatchAsync(
                pendingConnection.Payload,
                pendingConnection.InboundFrames.Reader,
                async (body, token) => await writeAsync(new TunnelFrameMessage
                {
                    ProtocolVersion = ReverseTunnelProtocol.CurrentVersion,
                    SessionId = openFrame.SessionId,
                    StreamId = openFrame.StreamId,
                    Kind = TunnelFrameKindMessage.TcpData,
                    Payload = ByteString.CopyFrom(body.Span)
                }, token),
                async token => await writeAsync(new TunnelFrameMessage
                {
                    ProtocolVersion = ReverseTunnelProtocol.CurrentVersion,
                    SessionId = openFrame.SessionId,
                    StreamId = openFrame.StreamId,
                    Kind = TunnelFrameKindMessage.TcpClose,
                    Payload = ByteString.CopyFrom(ReverseTunnelPayloadSerializer.SerializeTcpClose(new ReverseTunnelTcpClosePayload
                    {
                        Reason = "closed"
                    }))
                }, token),
                async (code, message, token) => await writeAsync(CreateError(openFrame, code, message, TunnelFrameKindMessage.TcpError), token),
                pendingConnection.Cancellation.Token);
        }
        finally
        {
            pendingTcpConnections.TryRemove(openFrame.StreamId, out _);
            pendingConnection.InboundFrames.Writer.TryComplete();
            pendingConnection.Cancellation.Dispose();
        }
    }

    private async Task SendUdpDatagramAsync(TunnelFrameMessage frame, Func<TunnelFrameMessage, CancellationToken, Task> writeAsync, CancellationToken cancellationToken)
    {
        ReverseTunnelUdpDatagramPayload payload;
        try
        {
            payload = ReverseTunnelPayloadSerializer.DeserializeUdpDatagram(frame.Payload.ToByteArray());
        }
        catch (Exception exception)
        {
            await writeAsync(CreateError(frame, "invalid_udp_datagram_payload", exception.Message, TunnelFrameKindMessage.UdpError), cancellationToken);
            return;
        }

        if (payload.Data.Length > options.Value.Udp.Listeners.Select(item => item.MaxDatagramBytes).DefaultIfEmpty(65_507).Max())
        {
            await writeAsync(CreateError(frame, "udp_datagram_too_large", "UDP datagram exceeds the configured Agent limit.", TunnelFrameKindMessage.UdpError), cancellationToken);
            return;
        }

        await udpDispatcher.SendAsync(
            frame.StreamId,
            payload,
            async (datagram, token) => await writeAsync(new TunnelFrameMessage
            {
                ProtocolVersion = ReverseTunnelProtocol.CurrentVersion,
                SessionId = frame.SessionId,
                StreamId = frame.StreamId,
                Kind = TunnelFrameKindMessage.UdpDatagram,
                Payload = ByteString.CopyFrom(ReverseTunnelPayloadSerializer.SerializeUdpDatagram(datagram))
            }, token),
            async (code, message, token) => await writeAsync(CreateError(frame, code, message, TunnelFrameKindMessage.UdpError), token),
            cancellationToken);
    }

    private async Task CompletePendingRequestAsync(long streamId, PendingRequest pendingRequest)
    {
        pendingRequests.TryRemove(streamId, out _);
        pendingRequest.Body.Writer.TryComplete();
        await pendingRequest.Cancellation.CancelAsync();
    }

    private void CompleteMetricsWithSuccess(PendingRequest pendingRequest)
    {
        if (pendingRequest.TryCompleteMetrics())
        {
            ReverseTunnelMetrics.RequestCompleted(pendingRequest.Request.ServiceKey, options.Value.AgentId, pendingRequest.StartedAt);
        }
    }

    private void CompleteMetricsWithFailure(PendingRequest pendingRequest, string reason, bool cancellation)
    {
        if (!pendingRequest.TryCompleteMetrics())
        {
            return;
        }

        if (cancellation)
        {
            ReverseTunnelMetrics.Cancellation(pendingRequest.Request.ServiceKey, options.Value.AgentId);
        }

        ReverseTunnelMetrics.RequestFailed(pendingRequest.Request.ServiceKey, options.Value.AgentId, reason, pendingRequest.StartedAt);
    }

    private static TunnelFrameMessage CreateError(TunnelFrameMessage frame, string code, string message)
    {
        return CreateError(frame, code, message, TunnelFrameKindMessage.Error);
    }

    private static TunnelFrameMessage CreateError(TunnelFrameMessage frame, string code, string message, TunnelFrameKindMessage kind)
    {
        return new TunnelFrameMessage
        {
            ProtocolVersion = ReverseTunnelProtocol.CurrentVersion,
            SessionId = frame.SessionId,
            StreamId = Math.Max(0, frame.StreamId),
            Kind = kind,
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
        CancellationTokenSource Cancellation,
        long StartedAt)
    {
        private int metricsCompleted;

        public long BodyBytes;

        public bool TryCompleteMetrics()
        {
            return Interlocked.Exchange(ref metricsCompleted, 1) == 0;
        }
    }

    private sealed record PendingTcpConnection(
        ReverseTunnelTcpOpenPayload Payload,
        Channel<byte[]> InboundFrames,
        CancellationTokenSource Cancellation);

    private sealed class InMemoryReverseTunnelAgentConfigurationStore : IReverseTunnelAgentConfigurationStore
    {
        public long CurrentVersion { get; private set; }

        public Task<ReverseTunnelAgentConfigurationSnapshot?> LoadAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<ReverseTunnelAgentConfigurationSnapshot?>(null);

        public Task SaveAsync(ReverseTunnelAgentConfigurationSnapshot snapshot, CancellationToken cancellationToken = default)
        {
            CurrentVersion = snapshot.Version;
            return Task.CompletedTask;
        }
    }
}
