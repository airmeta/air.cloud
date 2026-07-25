using Air.Cloud.Modules.ReverseTunnel.Common.Protocol;
using Air.Cloud.Modules.ReverseTunnel.Server.Grpc;

using Google.Protobuf;

using System.Collections.Concurrent;
using System.Threading.Channels;

namespace Air.Cloud.Modules.ReverseTunnel.Server;

/// <summary>
/// <para>zh-cn:已连接 Agent 的内存会话快照。对象保存路由选择需要的身份、服务键和最后活跃时间，不保存证书私钥或请求正文。</para>
/// <para>en-us:In-memory session snapshot for a connected Agent. The object stores identity, service keys, and last-seen time needed for routing selection; it does not store certificate private keys or request bodies.</para>
/// </summary>
public sealed class ReverseTunnelSession
{
    private long lastSeenUtcTicks;
    private long nextStreamId;
    private readonly Channel<TunnelFrameMessage> outboundFrames;
    private readonly ConcurrentDictionary<long, PendingInvocation> pendingInvocations = new();
    private readonly SemaphoreSlim concurrentStreamSlots;
    private readonly int maxFramePayloadBytes;
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
    /// <para>zh-cn:已认证或 Hello 帧声明的 Agent 稳定身份。mTLS 接入完成后该值必须来自证书身份。</para>
    /// <para>en-us:Stable Agent identity authenticated or declared by the Hello frame. After mTLS ingress is completed, this value must come from certificate identity.</para>
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
    /// <para>zh-cn:该会话允许同时处理的最大 HTTP 请求流数量。超过该值的新请求会被拒绝，不会进入 Agent 队列。</para>
    /// <para>en-us:Maximum number of HTTP request streams allowed concurrently for this session. New requests over the limit are rejected before they enter the Agent queue.</para>
    /// </param>
    /// <param name="outboundFrameQueueCapacity">
    /// <para>zh-cn:Server 发往 Agent 的出站帧队列容量。队列满时写入方等待，形成基础背压。</para>
    /// <para>en-us:Capacity of the outbound frame queue from Server to Agent. Writers wait when the queue is full, providing basic backpressure.</para>
    /// </param>
    /// <param name="maxFramePayloadBytes">
    /// <para>zh-cn:单个请求体帧允许承载的最大字节数。请求体会按该值拆分，避免单帧过大。</para>
    /// <para>en-us:Maximum bytes carried by one request-body frame. Request bodies are split by this value to avoid oversized frames.</para>
    /// </param>
    /// <param name="maxResponseBodyBytes">
    /// <para>zh-cn:Gateway 聚合 Agent 响应体时允许的最大字节数。超过该值会让本次调用以失败结果结束。</para>
    /// <para>en-us:Maximum bytes allowed while the Gateway aggregates the Agent response body. Exceeding the limit completes the invocation with a failure result.</para>
    /// </param>
    public ReverseTunnelSession(
        string sessionId,
        string agentId,
        IEnumerable<string> serviceKeys,
        DateTimeOffset connectedAtUtc,
        int maxConcurrentStreams = 128,
        int outboundFrameQueueCapacity = 1024,
        int maxFramePayloadBytes = 64 * 1024,
        long maxResponseBodyBytes = 10 * 1024 * 1024)
    {
        SessionId = sessionId;
        AgentId = agentId;
        ServiceKeys = serviceKeys.Where(key => !string.IsNullOrWhiteSpace(key)).Select(key => key.Trim()).ToHashSet(StringComparer.OrdinalIgnoreCase);
        ConnectedAtUtc = connectedAtUtc;
        lastSeenUtcTicks = connectedAtUtc.UtcTicks;
        concurrentStreamSlots = new SemaphoreSlim(Math.Max(1, maxConcurrentStreams), Math.Max(1, maxConcurrentStreams));
        this.maxFramePayloadBytes = Math.Max(1_024, maxFramePayloadBytes);
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
    /// <para>zh-cn:最近一次收到控制帧或业务帧的 UTC 时间。读取使用原子操作，适合并发摘除检查。</para>
    /// <para>en-us:UTC time when the most recent control or business frame was received. Reads use an atomic operation, making it suitable for concurrent eviction checks.</para>
    /// </summary>
    public DateTimeOffset LastSeenUtc => new(Interlocked.Read(ref lastSeenUtcTicks), TimeSpan.Zero);

    /// <summary>
    /// <para>zh-cn:刷新会话最后活跃时间。该方法不验证时间是否单调递增，调用方应传入可信 UTC 时间。</para>
    /// <para>en-us:Refreshes the session last-seen time. The method does not verify monotonicity, so callers should pass a trusted UTC time.</para>
    /// </summary>
    /// <param name="utcNow">
    /// <para>zh-cn:新的 UTC 活跃时间。</para>
    /// <para>en-us:New UTC last-seen time.</para>
    /// </param>
    public void Touch(DateTimeOffset utcNow) => Interlocked.Exchange(ref lastSeenUtcTicks, utcNow.UtcTicks);

    internal ChannelReader<TunnelFrameMessage> OutboundFrames => outboundFrames.Reader;

    internal async Task<ReverseTunnelHttpExchangeResult> InvokeAsync(ReverseTunnelHttpRequestPayload request, byte[] body, TimeSpan timeout, CancellationToken cancellationToken)
    {
        if (!await concurrentStreamSlots.WaitAsync(0, cancellationToken))
        {
            return ReverseTunnelHttpExchangeResult.Failure("too_many_concurrent_streams", "Reverse tunnel Agent session is at the configured concurrent stream limit.");
        }

        var streamId = Interlocked.Increment(ref nextStreamId);
        var pendingInvocation = new PendingInvocation();
        if (!pendingInvocations.TryAdd(streamId, pendingInvocation))
        {
            concurrentStreamSlots.Release();
            return ReverseTunnelHttpExchangeResult.Failure("duplicate_stream", "Reverse tunnel stream identifier could not be allocated.");
        }

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

            await EnqueueBodyFramesAsync(streamId, body, linkedCancellation.Token);

            await EnqueueAsync(new TunnelFrameMessage
            {
                ProtocolVersion = ReverseTunnelProtocol.CurrentVersion,
                SessionId = SessionId,
                StreamId = streamId,
                Kind = TunnelFrameKindMessage.Complete
            }, linkedCancellation.Token);

            return await pendingInvocation.Completion.Task.WaitAsync(linkedCancellation.Token);
        }
        catch (OperationCanceledException) when (timeoutCancellation.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
        {
            return ReverseTunnelHttpExchangeResult.Failure("timeout", "Reverse tunnel invocation timed out.");
        }
        finally
        {
            pendingInvocations.TryRemove(streamId, out _);
            concurrentStreamSlots.Release();
        }
    }

    private async Task EnqueueBodyFramesAsync(long streamId, byte[] body, CancellationToken cancellationToken)
    {
        if (body is not { Length: > 0 })
        {
            return;
        }

        for (var offset = 0; offset < body.Length; offset += maxFramePayloadBytes)
        {
            var count = Math.Min(maxFramePayloadBytes, body.Length - offset);
            await EnqueueAsync(new TunnelFrameMessage
            {
                ProtocolVersion = ReverseTunnelProtocol.CurrentVersion,
                SessionId = SessionId,
                StreamId = streamId,
                Kind = TunnelFrameKindMessage.RequestBody,
                Payload = ByteString.CopyFrom(body, offset, count)
            }, cancellationToken);
        }
    }

    internal ValueTask EnqueueAsync(TunnelFrameMessage frame, CancellationToken cancellationToken)
    {
        return outboundFrames.Writer.WriteAsync(frame, cancellationToken);
    }

    internal void CompleteOutbound(Exception? exception = null)
    {
        outboundFrames.Writer.TryComplete(exception);
        foreach (var pendingInvocation in pendingInvocations.Values)
        {
            pendingInvocation.Completion.TrySetResult(ReverseTunnelHttpExchangeResult.Failure("session_closed", "Reverse tunnel Agent session was closed."));
        }
    }

    internal void HandleInboundFrame(TunnelFrameMessage frame)
    {
        Touch(DateTimeOffset.UtcNow);
        if (frame.StreamId <= 0)
        {
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
                    pendingInvocation.Response = ReverseTunnelPayloadSerializer.DeserializeResponse(frame.Payload.ToByteArray());
                    break;
                case TunnelFrameKindMessage.ResponseBody:
                    if (pendingInvocation.Body.Length + frame.Payload.Length > maxResponseBodyBytes)
                    {
                        pendingInvocation.Completion.TrySetResult(ReverseTunnelHttpExchangeResult.Failure("response_body_too_large", "Reverse tunnel response body exceeds the configured Gateway limit."));
                        break;
                    }

                    pendingInvocation.Body.Write(frame.Payload.Span);
                    break;
                case TunnelFrameKindMessage.Complete:
                    pendingInvocation.Completion.TrySetResult(ReverseTunnelHttpExchangeResult.Success(
                        pendingInvocation.Response ?? new ReverseTunnelHttpResponsePayload { StatusCode = 204 },
                        pendingInvocation.Body.ToArray()));
                    break;
                case TunnelFrameKindMessage.Error:
                    var error = ReverseTunnelPayloadSerializer.DeserializeError(frame.Payload.ToByteArray());
                    pendingInvocation.Completion.TrySetResult(ReverseTunnelHttpExchangeResult.Failure(error.Code, error.Message));
                    break;
            }
        }
        catch (Exception exception)
        {
            pendingInvocation.Completion.TrySetResult(ReverseTunnelHttpExchangeResult.Failure("invalid_agent_response", exception.Message));
        }
    }

    private sealed class PendingInvocation
    {
        public TaskCompletionSource<ReverseTunnelHttpExchangeResult> Completion { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public ReverseTunnelHttpResponsePayload? Response { get; set; }

        public MemoryStream Body { get; } = new();
    }
}
