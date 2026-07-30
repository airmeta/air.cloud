using Air.Cloud.Modules.ReverseTunnel.Common.Diagnostics;
using Air.Cloud.Modules.ReverseTunnel.Common.Http;
using Air.Cloud.Modules.ReverseTunnel.Common.Protocol;
using Air.Cloud.Modules.ReverseTunnel.Server.Grpc;

using Google.Protobuf;

using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading.Channels;

namespace Air.Cloud.Modules.ReverseTunnel.Server;

/// <summary>
/// <para>zh-cn:已连接 Agent 的内存会话快照。对象保存路由选择需要的身份、服务键、最后活跃时间和当前 HTTP 流状态；不保存证书私钥。</para>
/// <para>en-us:In-memory session snapshot for a connected Agent. The object stores identity, service keys, last-seen time, and current HTTP stream state needed for routing; it does not store certificate private keys.</para>
/// </summary>
public sealed class ReverseTunnelSession
{
    private long lastSeenUtcTicks;
    private long nextStreamId;
    private readonly Channel<TunnelFrameMessage> outboundFrames;
    private readonly ConcurrentDictionary<long, PendingInvocation> pendingInvocations = new();
    private readonly ConcurrentDictionary<long, PendingTcpTunnel> pendingTcpTunnels = new();
    private readonly ConcurrentDictionary<long, PendingUdpFlow> pendingUdpFlows = new();
    private readonly SemaphoreSlim concurrentStreamSlots;
    private readonly int maxFramePayloadBytes;
    private readonly long maxRequestBodyBytes;
    private readonly long maxResponseBodyBytes;

    /// <summary>
    /// <para>zh-cn:创建 Agent 会话快照，并标准化服务键集合。last seen 初始值等于 connectedAtUtc。</para>
    /// <para>en-us:Creates an Agent session snapshot and normalizes the service-key set. The initial last-seen value equals connectedAtUtc.</para>
    /// </summary>
    /// <param name="sessionId">
    /// <para>zh-cn:Server 侧会话标识。</para>
    /// <para>en-us:Server-side session identifier.</para>
    /// </param>
    /// <param name="agentId">
    /// <para>zh-cn:已认证或 Hello 帧声明的 Agent 稳定身份。</para>
    /// <para>en-us:Stable Agent identity authenticated or declared by the Hello frame.</para>
    /// </param>
    /// <param name="serviceKeys">
    /// <para>zh-cn:该会话声明可处理的逻辑服务键集合。</para>
    /// <para>en-us:Logical service keys that this session declares it can handle.</para>
    /// </param>
    /// <param name="connectedAtUtc">
    /// <para>zh-cn:会话建立的 UTC 时间。</para>
    /// <para>en-us:UTC time when the session was established.</para>
    /// </param>
    /// <param name="maxConcurrentStreams">
    /// <para>zh-cn:该会话允许同时处理的最大 HTTP 请求流数量。</para>
    /// <para>en-us:Maximum number of HTTP request streams allowed concurrently for this session.</para>
    /// </param>
    /// <param name="outboundFrameQueueCapacity">
    /// <para>zh-cn:Server 发往 Agent 的出站帧队列容量。队列满时写入方等待，形成基础背压。</para>
    /// <para>en-us:Capacity of the outbound frame queue from Server to Agent. Writers wait when the queue is full, providing basic backpressure.</para>
    /// </param>
    /// <param name="maxFramePayloadBytes">
    /// <para>zh-cn:单个请求体或响应体帧允许承载的最大字节数。</para>
    /// <para>en-us:Maximum bytes carried by one request-body or response-body frame.</para>
    /// </param>
    /// <param name="maxResponseBodyBytes">
    /// <para>zh-cn:Gateway 接收 Agent 响应体时允许的最大字节数，超限会终止本次 HTTP 流。</para>
    /// <para>en-us:Maximum bytes allowed while the Gateway receives the Agent response body. Exceeding the limit terminates the HTTP stream.</para>
    /// </param>
    /// <param name="maxRequestBodyBytes">
    /// <para>zh-cn:Gateway 发送请求体给 Agent 时允许的最大字节数，超限会发送 Cancel 帧并返回失败。</para>
    /// <para>en-us:Maximum bytes allowed while the Gateway sends the request body to the Agent. Exceeding the limit sends a Cancel frame and returns a failure.</para>
    /// </param>
    public ReverseTunnelSession(
        string sessionId,
        string agentId,
        IEnumerable<string> serviceKeys,
        DateTimeOffset connectedAtUtc,
        int maxConcurrentStreams = 128,
        int outboundFrameQueueCapacity = 1024,
        int maxFramePayloadBytes = 64 * 1024,
        long maxResponseBodyBytes = 10 * 1024 * 1024,
        long maxRequestBodyBytes = 10 * 1024 * 1024)
    {
        SessionId = sessionId;
        AgentId = agentId;
        ServiceKeys = serviceKeys.Where(key => !string.IsNullOrWhiteSpace(key)).Select(key => key.Trim()).ToHashSet(StringComparer.OrdinalIgnoreCase);
        ConnectedAtUtc = connectedAtUtc;
        lastSeenUtcTicks = connectedAtUtc.UtcTicks;
        concurrentStreamSlots = new SemaphoreSlim(Math.Max(1, maxConcurrentStreams), Math.Max(1, maxConcurrentStreams));
        this.maxFramePayloadBytes = Math.Max(1_024, maxFramePayloadBytes);
        this.maxRequestBodyBytes = Math.Max(1_024, maxRequestBodyBytes);
        this.maxResponseBodyBytes = Math.Max(1_024, maxResponseBodyBytes);
        outboundFrames = Channel.CreateBounded<TunnelFrameMessage>(new BoundedChannelOptions(Math.Max(16, outboundFrameQueueCapacity))
        {
            SingleReader = true,
            SingleWriter = false,
            FullMode = BoundedChannelFullMode.Wait
        });
    }

    /// <summary>
    /// <para>zh-cn:Server 侧会话标识。</para>
    /// <para>en-us:Server-side session identifier.</para>
    /// </summary>
    public string SessionId { get; }

    /// <summary>
    /// <para>zh-cn:Agent 稳定身份。</para>
    /// <para>en-us:Stable Agent identity.</para>
    /// </summary>
    public string AgentId { get; }

    /// <summary>
    /// <para>zh-cn:该会话声明可处理的逻辑服务键集合，匹配时不区分大小写。</para>
    /// <para>en-us:Logical service keys declared by this session, matched case-insensitively.</para>
    /// </summary>
    public IReadOnlySet<string> ServiceKeys { get; }

    /// <summary>
    /// <para>zh-cn:会话建立的 UTC 时间。</para>
    /// <para>en-us:UTC time when the session was established.</para>
    /// </summary>
    public DateTimeOffset ConnectedAtUtc { get; }

    /// <summary>
    /// <para>zh-cn:最近一次收到控制帧或业务帧的 UTC 时间。</para>
    /// <para>en-us:UTC time when the most recent control or business frame was received.</para>
    /// </summary>
    public DateTimeOffset LastSeenUtc => new(Interlocked.Read(ref lastSeenUtcTicks), TimeSpan.Zero);

    /// <summary>
    /// <para>zh-cn:当前会话中尚未完成的 HTTP 请求流数量。该值用于诊断和容量判断，只表示当前进程内的瞬时状态。</para>
    /// <para>en-us:Current number of unfinished HTTP request streams in this session. The value is intended for diagnostics and capacity checks and represents only an in-process instant state.</para>
    /// </summary>
    public int ActiveStreamCount => pendingInvocations.Count;

    /// <summary>
    /// <para>zh-cn:刷新会话最后活跃时间。调用方应传入可信 UTC 时间。</para>
    /// <para>en-us:Refreshes the session last-seen time. Callers should pass a trusted UTC time.</para>
    /// </summary>
    /// <param name="utcNow">
    /// <para>zh-cn:新的 UTC 活跃时间。</para>
    /// <para>en-us:New UTC last-seen time.</para>
    /// </param>
    public void Touch(DateTimeOffset utcNow) => Interlocked.Exchange(ref lastSeenUtcTicks, utcNow.UtcTicks);

    internal ChannelReader<TunnelFrameMessage> OutboundFrames => outboundFrames.Reader;

    internal long AllocateStreamId()
    {
        return Interlocked.Increment(ref nextStreamId);
    }

    internal async Task<ReverseTunnelHttpExchangeResult> InvokeAsync(ReverseTunnelHttpRequestPayload request, byte[] body, TimeSpan timeout, CancellationToken cancellationToken)
    {
        request = request with
        {
            HasBody = request.HasBody || body is { Length: > 0 }
        };

        var requestBody = Channel.CreateBounded<byte[]>(new BoundedChannelOptions(1)
        {
            SingleReader = true,
            SingleWriter = true
        });
        if (body is { Length: > 0 })
        {
            await requestBody.Writer.WriteAsync(body, cancellationToken);
        }

        requestBody.Writer.TryComplete();
        using var content = new StreamBackedChannelContent(requestBody.Reader);
        return await InvokeAsync(request, content, timeout, cancellationToken);
    }

    internal async Task<ReverseTunnelHttpExchangeResult> InvokeAsync(ReverseTunnelHttpRequestPayload request, HttpContent? content, TimeSpan timeout, CancellationToken cancellationToken)
    {
        if (!await concurrentStreamSlots.WaitAsync(0, cancellationToken))
        {
            ReverseTunnelMetrics.RequestRejected(request.ServiceKey, AgentId, "too_many_concurrent_streams");
            return ReverseTunnelHttpExchangeResult.Failure("too_many_concurrent_streams", "Reverse tunnel Agent session is at the configured concurrent stream limit.");
        }

        var streamId = Interlocked.Increment(ref nextStreamId);
        var startedAt = Stopwatch.GetTimestamp();
        var pendingInvocation = new PendingInvocation(request.ServiceKey, AgentId, startedAt, Math.Max(16, maxFramePayloadBytes / 1024));
        if (!pendingInvocations.TryAdd(streamId, pendingInvocation))
        {
            concurrentStreamSlots.Release();
            ReverseTunnelMetrics.RequestRejected(request.ServiceKey, AgentId, "duplicate_stream");
            return ReverseTunnelHttpExchangeResult.Failure("duplicate_stream", "Reverse tunnel stream identifier could not be allocated.");
        }

        ReverseTunnelMetrics.RequestStarted(request.ServiceKey, AgentId);
        using var timeoutCancellation = new CancellationTokenSource(timeout);
        using var linkedCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCancellation.Token);
        try
        {
            await EnqueueAsync(new TunnelFrameMessage
            {
                ProtocolVersion = ReverseTunnelProtocol.CurrentVersion,
                SessionId = SessionId,
                StreamId = streamId,
                Kind = TunnelFrameKindMessage.OpenRequest,
                Payload = ByteString.CopyFrom(ReverseTunnelPayloadSerializer.SerializeRequest(request))
            }, linkedCancellation.Token);

            await EnqueueBodyFramesAsync(streamId, request.ServiceKey, content, linkedCancellation.Token);

            await EnqueueAsync(new TunnelFrameMessage
            {
                ProtocolVersion = ReverseTunnelProtocol.CurrentVersion,
                SessionId = SessionId,
                StreamId = streamId,
                Kind = TunnelFrameKindMessage.Complete
            }, linkedCancellation.Token);

            return await pendingInvocation.ResponseStarted.Task.WaitAsync(linkedCancellation.Token);
        }
        catch (ReverseTunnelHttpLimitExceededException exception)
        {
            await SendCancelAsync(streamId);
            CompleteInvocationWithFailure(streamId, pendingInvocation, exception.Code, exception.Message);
            return ReverseTunnelHttpExchangeResult.Failure(exception.Code, exception.Message);
        }
        catch (OperationCanceledException) when (timeoutCancellation.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
        {
            await SendCancelAsync(streamId);
            ReverseTunnelMetrics.Timeout(request.ServiceKey, AgentId);
            CompleteInvocationWithFailure(streamId, pendingInvocation, "timeout", "Reverse tunnel invocation timed out.");
            return ReverseTunnelHttpExchangeResult.Failure("timeout", "Reverse tunnel invocation timed out.");
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            await SendCancelAsync(streamId);
            ReverseTunnelMetrics.Cancellation(request.ServiceKey, AgentId);
            CompleteInvocationWithFailure(streamId, pendingInvocation, "cancelled", "Reverse tunnel invocation was cancelled.");
            return ReverseTunnelHttpExchangeResult.Failure("cancelled", "Reverse tunnel invocation was cancelled.");
        }
        catch (Exception exception)
        {
            await SendCancelAsync(streamId);
            CompleteInvocationWithFailure(streamId, pendingInvocation, "gateway_stream_failed", exception.Message);
            return ReverseTunnelHttpExchangeResult.Failure("gateway_stream_failed", exception.Message);
        }
    }

    private async Task EnqueueBodyFramesAsync(long streamId, string serviceKey, HttpContent? content, CancellationToken cancellationToken)
    {
        if (content is null)
        {
            return;
        }

        await using var contentStream = await content.ReadAsStreamAsync(cancellationToken);
        var buffer = new byte[maxFramePayloadBytes];
        long totalBytes = 0;
        int read;
        while ((read = await contentStream.ReadAsync(buffer, cancellationToken)) > 0)
        {
            totalBytes += read;
            if (totalBytes > maxRequestBodyBytes)
            {
                throw new ReverseTunnelHttpLimitExceededException("request_body_too_large", "Reverse tunnel request body exceeds the configured Gateway limit.");
            }

            ReverseTunnelMetrics.RequestBodyBytes(serviceKey, AgentId, read);
            await EnqueueAsync(new TunnelFrameMessage
            {
                ProtocolVersion = ReverseTunnelProtocol.CurrentVersion,
                SessionId = SessionId,
                StreamId = streamId,
                Kind = TunnelFrameKindMessage.RequestBody,
                Payload = ByteString.CopyFrom(buffer, 0, read)
            }, cancellationToken);
        }
    }

    internal async ValueTask EnqueueAsync(TunnelFrameMessage frame, CancellationToken cancellationToken)
    {
        if (outboundFrames.Writer.TryWrite(frame))
        {
            return;
        }

        ReverseTunnelMetrics.BackpressureWait(frame.StreamId > 0 ? ResolveServiceKey(frame.StreamId) : "control", AgentId);
        await outboundFrames.Writer.WriteAsync(frame, cancellationToken);
    }

    internal void CompleteOutbound(Exception? exception = null)
    {
        outboundFrames.Writer.TryComplete(exception);
        foreach (var item in pendingInvocations.ToArray())
        {
            CompleteInvocationWithFailure(item.Key, item.Value, "session_closed", "Reverse tunnel Agent session was closed.");
        }
    }

    internal void HandleInboundFrame(TunnelFrameMessage frame)
    {
        HandleInboundFrameAsync(frame, CancellationToken.None).GetAwaiter().GetResult();
    }

    internal async Task HandleInboundFrameAsync(TunnelFrameMessage frame, CancellationToken cancellationToken)
    {
        Touch(DateTimeOffset.UtcNow);
        if (frame.StreamId <= 0)
        {
            return;
        }

        if (frame.Kind is TunnelFrameKindMessage.TcpData or TunnelFrameKindMessage.TcpClose or TunnelFrameKindMessage.TcpError)
        {
            await HandleTcpFrameAsync(frame, cancellationToken);
            return;
        }

        if (frame.Kind is TunnelFrameKindMessage.UdpDatagram or TunnelFrameKindMessage.UdpFlowClose or TunnelFrameKindMessage.UdpError)
        {
            await HandleUdpFrameAsync(frame, cancellationToken);
            return;
        }

        if (!pendingInvocations.TryGetValue(frame.StreamId, out var pendingInvocation))
        {
            return;
        }

        try
        {
            switch (frame.Kind)
            {
                case TunnelFrameKindMessage.ResponseHeaders:
                    var response = ReverseTunnelPayloadSerializer.DeserializeResponse(frame.Payload.ToByteArray());
                    pendingInvocation.ResponseStarted.TrySetResult(ReverseTunnelHttpExchangeResult.Success(
                        response,
                        pendingInvocation.Body.Reader,
                        token => CancelResponseBodyAsync(frame.StreamId, pendingInvocation, token)));
                    break;
                case TunnelFrameKindMessage.ResponseBody:
                    var responseBytes = Interlocked.Add(ref pendingInvocation.ResponseBytes, frame.Payload.Length);
                    if (responseBytes > maxResponseBodyBytes)
                    {
                        pendingInvocation.Body.Writer.TryComplete(new InvalidOperationException("Reverse tunnel response body exceeds the configured Gateway limit."));
                        CompleteInvocationWithFailure(frame.StreamId, pendingInvocation, "response_body_too_large", "Reverse tunnel response body exceeds the configured Gateway limit.");
                        await SendCancelAsync(frame.StreamId);
                        break;
                    }

                    ReverseTunnelMetrics.ResponseBodyBytes(pendingInvocation.ServiceKey, pendingInvocation.AgentId, frame.Payload.Length);
                    await pendingInvocation.Body.Writer.WriteAsync(frame.Payload.ToByteArray(), cancellationToken);
                    break;
                case TunnelFrameKindMessage.Complete:
                    if (!pendingInvocation.ResponseStarted.Task.IsCompleted)
                    {
                        pendingInvocation.ResponseStarted.TrySetResult(ReverseTunnelHttpExchangeResult.Success(
                            new ReverseTunnelHttpResponsePayload { StatusCode = 204 },
                            pendingInvocation.Body.Reader,
                            token => CancelResponseBodyAsync(frame.StreamId, pendingInvocation, token)));
                    }

                    pendingInvocation.Body.Writer.TryComplete();
                    CompleteInvocationWithSuccess(frame.StreamId, pendingInvocation);
                    break;
                case TunnelFrameKindMessage.Error:
                    var error = ReverseTunnelPayloadSerializer.DeserializeError(frame.Payload.ToByteArray());
                    pendingInvocation.Body.Writer.TryComplete(new InvalidOperationException(error.Message));
                    CompleteInvocationWithFailure(frame.StreamId, pendingInvocation, error.Code, error.Message);
                    break;
            }
        }
        catch (Exception exception)
        {
            pendingInvocation.Body.Writer.TryComplete(exception);
            CompleteInvocationWithFailure(frame.StreamId, pendingInvocation, "invalid_agent_response", exception.Message);
        }
    }

    private async Task SendCancelAsync(long streamId)
    {
        try
        {
            await EnqueueAsync(new TunnelFrameMessage
            {
                ProtocolVersion = ReverseTunnelProtocol.CurrentVersion,
                SessionId = SessionId,
                StreamId = streamId,
                Kind = TunnelFrameKindMessage.Cancel
            }, CancellationToken.None);
        }
        catch
        {
        }
    }

    internal async Task RunTcpTunnelAsync(
        ReverseTunnelTcpOpenPayload openPayload,
        ChannelReader<byte[]> inboundData,
        Func<ReadOnlyMemory<byte>, CancellationToken, Task> writeClientAsync,
        TimeSpan timeout,
        CancellationToken cancellationToken)
    {
        var streamId = AllocateStreamId();
        using var linkedCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        linkedCancellation.CancelAfter(timeout);
        var pendingTunnel = new PendingTcpTunnel(openPayload.ServiceKey, AgentId, writeClientAsync, new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously));
        if (!pendingTcpTunnels.TryAdd(streamId, pendingTunnel))
        {
            throw new InvalidOperationException("Reverse tunnel TCP stream identifier could not be allocated.");
        }

        try
        {
            await EnqueueAsync(new TunnelFrameMessage
            {
                ProtocolVersion = ReverseTunnelProtocol.CurrentVersion,
                SessionId = SessionId,
                StreamId = streamId,
                Kind = TunnelFrameKindMessage.TcpOpen,
                Payload = ByteString.CopyFrom(ReverseTunnelPayloadSerializer.SerializeTcpOpen(openPayload))
            }, linkedCancellation.Token);

            var sendTask = SendTcpDataAsync(streamId, openPayload.ServiceKey, inboundData, linkedCancellation.Token);
            var completed = await Task.WhenAny(sendTask, pendingTunnel.Completion.Task.WaitAsync(linkedCancellation.Token));
            if (completed == sendTask)
            {
                await sendTask;
                await SendTcpCloseAsync(streamId, "client_closed", "TCP client closed the inbound stream.");
                pendingTunnel.Completion.TrySetResult();
            }
            else
            {
                await completed;
            }
        }
        catch (OperationCanceledException) when (linkedCancellation.IsCancellationRequested)
        {
            await SendTcpCloseAsync(streamId, "cancelled", "TCP tunnel was cancelled or timed out.");
            pendingTunnel.Completion.TrySetCanceled(cancellationToken);
        }
        finally
        {
            pendingTcpTunnels.TryRemove(streamId, out _);
        }
    }

    internal async Task SendUdpDatagramAsync(
        long streamId,
        ReverseTunnelUdpDatagramPayload datagram,
        Func<ReverseTunnelUdpDatagramPayload, CancellationToken, Task> writeClientAsync,
        CancellationToken cancellationToken)
    {
        pendingUdpFlows.TryAdd(streamId, new PendingUdpFlow(datagram.ServiceKey, AgentId, writeClientAsync));
        await EnqueueAsync(new TunnelFrameMessage
        {
            ProtocolVersion = ReverseTunnelProtocol.CurrentVersion,
            SessionId = SessionId,
            StreamId = streamId,
            Kind = TunnelFrameKindMessage.UdpDatagram,
            Payload = ByteString.CopyFrom(ReverseTunnelPayloadSerializer.SerializeUdpDatagram(datagram))
        }, cancellationToken);
    }

    internal async Task CloseUdpFlowAsync(long streamId, string reason, string message)
    {
        pendingUdpFlows.TryRemove(streamId, out _);
        await EnqueueAsync(new TunnelFrameMessage
        {
            ProtocolVersion = ReverseTunnelProtocol.CurrentVersion,
            SessionId = SessionId,
            StreamId = streamId,
            Kind = TunnelFrameKindMessage.UdpFlowClose,
            Payload = ByteString.CopyFrom(ReverseTunnelPayloadSerializer.SerializeError(new ReverseTunnelErrorPayload
            {
                Code = reason,
                Message = message
            }))
        }, CancellationToken.None);
    }

    private async Task SendTcpDataAsync(long streamId, string serviceKey, ChannelReader<byte[]> inboundData, CancellationToken cancellationToken)
    {
        await foreach (var chunk in inboundData.ReadAllAsync(cancellationToken))
        {
            if (chunk.Length == 0)
            {
                continue;
            }

            ReverseTunnelMetrics.RequestBodyBytes(serviceKey, AgentId, chunk.Length);
            for (var offset = 0; offset < chunk.Length; offset += maxFramePayloadBytes)
            {
                var count = Math.Min(maxFramePayloadBytes, chunk.Length - offset);
                await EnqueueAsync(new TunnelFrameMessage
                {
                    ProtocolVersion = ReverseTunnelProtocol.CurrentVersion,
                    SessionId = SessionId,
                    StreamId = streamId,
                    Kind = TunnelFrameKindMessage.TcpData,
                    Payload = ByteString.CopyFrom(chunk, offset, count)
                }, cancellationToken);
            }
        }
    }

    private async Task HandleTcpFrameAsync(TunnelFrameMessage frame, CancellationToken cancellationToken)
    {
        if (!pendingTcpTunnels.TryGetValue(frame.StreamId, out var pendingTunnel))
        {
            return;
        }

        switch (frame.Kind)
        {
            case TunnelFrameKindMessage.TcpData:
                ReverseTunnelMetrics.ResponseBodyBytes(pendingTunnel.ServiceKey, pendingTunnel.AgentId, frame.Payload.Length);
                await pendingTunnel.WriteClientAsync(frame.Payload.ToByteArray(), cancellationToken);
                break;
            case TunnelFrameKindMessage.TcpClose:
                pendingTunnel.Completion.TrySetResult();
                break;
            case TunnelFrameKindMessage.TcpError:
                var error = ReverseTunnelPayloadSerializer.DeserializeError(frame.Payload.ToByteArray());
                pendingTunnel.Completion.TrySetException(new InvalidOperationException(error.Message));
                break;
        }
    }

    private async Task HandleUdpFrameAsync(TunnelFrameMessage frame, CancellationToken cancellationToken)
    {
        if (!pendingUdpFlows.TryGetValue(frame.StreamId, out var flow))
        {
            return;
        }

        switch (frame.Kind)
        {
            case TunnelFrameKindMessage.UdpDatagram:
                var datagram = ReverseTunnelPayloadSerializer.DeserializeUdpDatagram(frame.Payload.ToByteArray());
                ReverseTunnelMetrics.ResponseBodyBytes(flow.ServiceKey, flow.AgentId, datagram.Data.Length);
                await flow.WriteClientAsync(datagram, cancellationToken);
                break;
            case TunnelFrameKindMessage.UdpFlowClose:
            case TunnelFrameKindMessage.UdpError:
                pendingUdpFlows.TryRemove(frame.StreamId, out _);
                break;
        }
    }

    private async Task SendTcpCloseAsync(long streamId, string reason, string message)
    {
        try
        {
            await EnqueueAsync(new TunnelFrameMessage
            {
                ProtocolVersion = ReverseTunnelProtocol.CurrentVersion,
                SessionId = SessionId,
                StreamId = streamId,
                Kind = TunnelFrameKindMessage.TcpClose,
                Payload = ByteString.CopyFrom(ReverseTunnelPayloadSerializer.SerializeTcpClose(new ReverseTunnelTcpClosePayload
                {
                    Reason = reason,
                    Message = message
                }))
            }, CancellationToken.None);
        }
        catch
        {
        }
    }

    private async Task CancelResponseBodyAsync(long streamId, PendingInvocation pendingInvocation, CancellationToken cancellationToken)
    {
        await SendCancelAsync(streamId);
        ReverseTunnelMetrics.Cancellation(pendingInvocation.ServiceKey, pendingInvocation.AgentId);
        CompleteInvocationWithFailure(streamId, pendingInvocation, "cancelled", "Reverse tunnel response body reading was cancelled.");
    }

    private void CompleteInvocationWithSuccess(long streamId, PendingInvocation pendingInvocation)
    {
        if (!pendingInvocations.TryRemove(streamId, out _))
        {
            return;
        }

        ReverseTunnelMetrics.RequestCompleted(pendingInvocation.ServiceKey, pendingInvocation.AgentId, pendingInvocation.StartedAt);
        concurrentStreamSlots.Release();
    }

    private void CompleteInvocationWithFailure(long streamId, PendingInvocation pendingInvocation, string code, string message)
    {
        pendingInvocation.ResponseStarted.TrySetResult(ReverseTunnelHttpExchangeResult.Failure(code, message));
        pendingInvocation.Body.Writer.TryComplete(new InvalidOperationException(message));
        if (!pendingInvocations.TryRemove(streamId, out _))
        {
            return;
        }

        ReverseTunnelMetrics.RequestFailed(pendingInvocation.ServiceKey, pendingInvocation.AgentId, code, pendingInvocation.StartedAt);
        concurrentStreamSlots.Release();
    }

    private string ResolveServiceKey(long streamId)
    {
        return pendingInvocations.TryGetValue(streamId, out var pendingInvocation) ? pendingInvocation.ServiceKey : "unknown";
    }

    private sealed class PendingInvocation
    {
        public PendingInvocation(string serviceKey, string agentId, long startedAt, int bodyQueueCapacity)
        {
            ServiceKey = serviceKey;
            AgentId = agentId;
            StartedAt = startedAt;
            Body = Channel.CreateBounded<byte[]>(new BoundedChannelOptions(Math.Max(16, bodyQueueCapacity))
            {
                SingleReader = true,
                SingleWriter = false,
                FullMode = BoundedChannelFullMode.Wait
            });
        }

        public string ServiceKey { get; }

        public string AgentId { get; }

        public long StartedAt { get; }

        public TaskCompletionSource<ReverseTunnelHttpExchangeResult> ResponseStarted { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public Channel<byte[]> Body { get; }

        public long ResponseBytes;
    }

    private sealed record PendingTcpTunnel(
        string ServiceKey,
        string AgentId,
        Func<ReadOnlyMemory<byte>, CancellationToken, Task> WriteClientAsync,
        TaskCompletionSource Completion);

    private sealed record PendingUdpFlow(
        string ServiceKey,
        string AgentId,
        Func<ReverseTunnelUdpDatagramPayload, CancellationToken, Task> WriteClientAsync);

    private sealed class StreamBackedChannelContent : HttpContent
    {
        private readonly ChannelReader<byte[]> bodyReader;

        public StreamBackedChannelContent(ChannelReader<byte[]> bodyReader)
        {
            this.bodyReader = bodyReader;
        }

        protected override Task SerializeToStreamAsync(Stream stream, System.Net.TransportContext? context)
        {
            return SerializeToStreamAsync(stream, context, CancellationToken.None);
        }

        protected override async Task SerializeToStreamAsync(Stream stream, System.Net.TransportContext? context, CancellationToken cancellationToken)
        {
            await foreach (var chunk in bodyReader.ReadAllAsync(cancellationToken))
            {
                await stream.WriteAsync(chunk, cancellationToken);
            }
        }

        protected override bool TryComputeLength(out long length)
        {
            length = 0;
            return false;
        }
    }
}
