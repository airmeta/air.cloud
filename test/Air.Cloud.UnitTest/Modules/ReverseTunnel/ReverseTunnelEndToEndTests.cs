using Air.Cloud.Modules.ReverseTunnel.Agent;
using Air.Cloud.Modules.ReverseTunnel.Agent.Tcp;
using Air.Cloud.Modules.ReverseTunnel.Agent.Udp;
using Air.Cloud.Modules.ReverseTunnel.Common.Diagnostics;
using Air.Cloud.Modules.ReverseTunnel.Common.Options;
using Air.Cloud.Modules.ReverseTunnel.Common.Protocol;
using Air.Cloud.Modules.ReverseTunnel.Common.Routing;
using Air.Cloud.Modules.ReverseTunnel.Gateway;
using Air.Cloud.Modules.ReverseTunnel.Server;
using Air.Cloud.Modules.ReverseTunnel.Server.Grpc;

using Google.Protobuf;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

using System.Diagnostics;
using System.Net;
using System.Text;
using System.Threading.Channels;

namespace Air.Cloud.UnitTest.Modules.ReverseTunnel;

/// <summary>
/// <para>zh-cn:反向隧道 Gateway 到 Agent 的内存级端到端测试集合。</para>
/// <para>en-us:In-memory end-to-end test suite for reverse tunnel Gateway-to-Agent behavior.</para>
/// </summary>
public sealed class ReverseTunnelEndToEndTests
{
    /// <summary>
    /// <para>zh-cn:验证 Ocelot opt-in handler 发起的请求可以经过 Gateway 调用器、Server 会话队列、Agent 帧处理器和本地 HTTP 分发器后返回响应。</para>
    /// <para>en-us:Verifies that a request started by the Ocelot opt-in handler can flow through the Gateway invoker, Server session queue, Agent frame processor, and local HTTP dispatcher and return a response.</para>
    /// </summary>
    [Fact]
    public async Task Gateway_handler_should_complete_in_memory_tunnel_http_exchange()
    {
        var options = CreateOptions();
        var optionsAccessor = Options.Create(options);
        var registry = new InMemoryReverseTunnelSessionRegistry(optionsAccessor);
        var session = new ReverseTunnelSession("session-alpha", "agent-alpha", ["order-service"], DateTimeOffset.UtcNow);
        registry.Register(session);
        var localHttpHandler = new CapturingLocalHttpHandler(new HttpResponseMessage(HttpStatusCode.Created)
        {
            ReasonPhrase = "Created",
            Content = new StringContent("agent-ok", Encoding.UTF8, "text/plain")
        });
        var localDispatcher = new ReverseTunnelAgentLocalHttpDispatcher(optionsAccessor, new FixedHttpClientFactory(new HttpClient(localHttpHandler)));
        var frameProcessor = CreateFrameProcessor(localDispatcher, optionsAccessor);
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        var agentLoop = RunSingleAgentExchangeAsync(session, frameProcessor, timeout.Token);
        var invoker = new ReverseTunnelInvoker(registry, optionsAccessor);
        using var handler = new ReverseTunnelDelegatingHandler(new ReverseTunnelRouteProvider(optionsAccessor), invoker, optionsAccessor)
        {
            InnerHandler = new StaticResponseHandler(HttpStatusCode.BadRequest)
        };
        using var client = new HttpMessageInvoker(handler);
        using var request = new HttpRequestMessage(HttpMethod.Post, "http://tunnel-order-service/api/orders?id=9")
        {
            Content = new StringContent("gateway-body", Encoding.UTF8, "application/json")
        };
        request.Headers.TryAddWithoutValidation("X-Trace-Id", "trace-e2e");

        using var response = await client.SendAsync(request, timeout.Token);
        await agentLoop;
        var responseBody = await response.Content!.ReadAsStringAsync(timeout.Token);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal("agent-ok", responseBody);
        Assert.Equal(new Uri("http://127.0.0.1:5101/api/orders?id=9"), localHttpHandler.LastRequestUri);
        Assert.Equal("POST", localHttpHandler.LastMethod);
        Assert.Equal("gateway-body", localHttpHandler.LastBody);
        Assert.Equal("application/json", localHttpHandler.LastContentType);
        Assert.Contains("X-Trace-Id", localHttpHandler.LastRequestHeaderNames);
    }

    /// <summary>
    /// <para>zh-cn:验证 Agent 发送本地 HTTP 响应体时会按配置拆分 ResponseBody 帧，避免单个 gRPC 帧承载过大 payload。</para>
    /// <para>en-us:Verifies that the Agent splits local HTTP response bodies into ResponseBody frames by configuration, avoiding oversized payloads in one gRPC frame.</para>
    /// </summary>
    [Fact]
    public async Task Agent_frame_processor_should_split_response_body_by_configured_frame_payload_limit()
    {
        ReverseTunnelMetrics.ResetSnapshotForTesting();
        var options = CreateOptions(maxFramePayloadBytes: 1024);
        var optionsAccessor = Options.Create(options);
        var localHttpHandler = new CapturingLocalHttpHandler(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new ByteArrayContent(new byte[2500])
        });
        var localDispatcher = new ReverseTunnelAgentLocalHttpDispatcher(optionsAccessor, new FixedHttpClientFactory(new HttpClient(localHttpHandler)));
        var frameProcessor = CreateFrameProcessor(localDispatcher, optionsAccessor);
        var frames = new List<TunnelFrameMessage>();
        var completed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        await frameProcessor.ProcessAsync(new TunnelFrameMessage
        {
            ProtocolVersion = ReverseTunnelProtocol.CurrentVersion,
            SessionId = "session-alpha",
            StreamId = 1,
            Kind = TunnelFrameKindMessage.OpenRequest,
            Payload = ByteString.CopyFrom(ReverseTunnelPayloadSerializer.SerializeRequest(new ReverseTunnelHttpRequestPayload
            {
                ServiceKey = "order-service",
                Method = "GET",
                PathAndQuery = "/api/orders"
            }))
        }, (responseFrame, _) =>
        {
            lock (frames)
            {
                frames.Add(responseFrame);
            }

            if (responseFrame.Kind == TunnelFrameKindMessage.Complete)
            {
                completed.TrySetResult();
            }

            return Task.CompletedTask;
        });

        await frameProcessor.ProcessAsync(new TunnelFrameMessage
        {
            ProtocolVersion = ReverseTunnelProtocol.CurrentVersion,
            SessionId = "session-alpha",
            StreamId = 1,
            Kind = TunnelFrameKindMessage.Complete
        }, (responseFrame, _) =>
        {
            lock (frames)
            {
                frames.Add(responseFrame);
            }

            if (responseFrame.Kind == TunnelFrameKindMessage.Complete)
            {
                completed.TrySetResult();
            }

            return Task.CompletedTask;
        });

        await completed.Task.WaitAsync(TimeSpan.FromSeconds(5));

        int[] bodyFrameLengths;
        lock (frames)
        {
            bodyFrameLengths = frames
                .Where(frame => frame.Kind == TunnelFrameKindMessage.ResponseBody)
                .Select(frame => frame.Payload.Length)
                .ToArray();
        }

        Assert.Equal([1024, 1024, 452], bodyFrameLengths);
        var metricSnapshot = ReverseTunnelMetrics.GetSnapshot();
        var dimension = Assert.Single(metricSnapshot.Dimensions, item => item.ServiceKey == "order-service" && item.AgentId == "unknown");
        Assert.Equal(1, dimension.Counters.RequestsTotal);
        Assert.Equal(1, dimension.Counters.DurationSamples);
        Assert.Equal(2500, dimension.Counters.ResponseBodyBytesTotal);
        Assert.Equal(0, dimension.Counters.ActiveStreams);
    }

    /// <summary>
    /// <para>zh-cn:验证 Agent 本地 HTTP 响应体超过配置上限时，帧处理器返回稳定的 `response_body_too_large` 错误码，便于 Gateway 指标和排障区分大小策略与普通本地调用失败。</para>
    /// <para>en-us:Verifies that when the Agent local HTTP response body exceeds the configured limit, the frame processor returns the stable `response_body_too_large` code so Gateway metrics and diagnostics can distinguish size-policy failures from ordinary local dispatch failures.</para>
    /// </summary>
    [Fact]
    public async Task Agent_frame_processor_should_return_response_body_too_large_when_local_response_exceeds_limit()
    {
        var options = CreateOptions(maxResponseBodyBytes: 1024);
        var optionsAccessor = Options.Create(options);
        var localHttpHandler = new CapturingLocalHttpHandler(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new ByteArrayContent(new byte[2048])
        });
        var localDispatcher = new ReverseTunnelAgentLocalHttpDispatcher(optionsAccessor, new FixedHttpClientFactory(new HttpClient(localHttpHandler)));
        var frameProcessor = CreateFrameProcessor(localDispatcher, optionsAccessor);
        var frames = new List<TunnelFrameMessage>();
        var errored = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));

        await frameProcessor.ProcessAsync(new TunnelFrameMessage
        {
            ProtocolVersion = ReverseTunnelProtocol.CurrentVersion,
            SessionId = "session-alpha",
            StreamId = 1,
            Kind = TunnelFrameKindMessage.OpenRequest,
            Payload = ByteString.CopyFrom(ReverseTunnelPayloadSerializer.SerializeRequest(new ReverseTunnelHttpRequestPayload
            {
                ServiceKey = "order-service",
                Method = "GET",
                PathAndQuery = "/api/orders"
            }))
        }, (responseFrame, _) =>
        {
            lock (frames)
            {
                frames.Add(responseFrame);
            }

            if (responseFrame.Kind == TunnelFrameKindMessage.Error)
            {
                errored.TrySetResult();
            }

            return Task.CompletedTask;
        }, timeout.Token);

        await frameProcessor.ProcessAsync(new TunnelFrameMessage
        {
            ProtocolVersion = ReverseTunnelProtocol.CurrentVersion,
            SessionId = "session-alpha",
            StreamId = 1,
            Kind = TunnelFrameKindMessage.Complete
        }, (responseFrame, _) =>
        {
            lock (frames)
            {
                frames.Add(responseFrame);
            }

            if (responseFrame.Kind == TunnelFrameKindMessage.Error)
            {
                errored.TrySetResult();
            }

            return Task.CompletedTask;
        }, timeout.Token);

        await errored.Task.WaitAsync(timeout.Token);

        TunnelFrameMessage errorFrame;
        lock (frames)
        {
            errorFrame = Assert.Single(frames, frame => frame.Kind == TunnelFrameKindMessage.Error);
        }

        var error = ReverseTunnelPayloadSerializer.DeserializeError(errorFrame.Payload.ToByteArray());
        Assert.Equal("response_body_too_large", error.Code);
    }

    [Fact]
    public async Task Agent_frame_processor_should_start_local_dispatch_before_request_complete()
    {
        var options = CreateOptions();
        var dispatcher = new StreamingStartDispatcher();
        var frameProcessor = CreateFrameProcessor(dispatcher, Options.Create(options));
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var frames = new List<TunnelFrameMessage>();
        var responseCompleted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        await frameProcessor.ProcessAsync(new TunnelFrameMessage
        {
            ProtocolVersion = ReverseTunnelProtocol.CurrentVersion,
            SessionId = "session-alpha",
            StreamId = 1,
            Kind = TunnelFrameKindMessage.OpenRequest,
            Payload = ByteString.CopyFrom(ReverseTunnelPayloadSerializer.SerializeRequest(new ReverseTunnelHttpRequestPayload
            {
                ServiceKey = "order-service",
                Method = "POST",
                PathAndQuery = "/api/orders"
            }))
        }, (responseFrame, _) =>
        {
            frames.Add(responseFrame);
            if (responseFrame.Kind == TunnelFrameKindMessage.Complete)
            {
                responseCompleted.TrySetResult();
            }

            return Task.CompletedTask;
        }, timeout.Token);

        await dispatcher.Started.Task.WaitAsync(timeout.Token);

        Assert.DoesNotContain(frames, frame => frame.Kind == TunnelFrameKindMessage.ResponseHeaders);

        await frameProcessor.ProcessAsync(new TunnelFrameMessage
        {
            ProtocolVersion = ReverseTunnelProtocol.CurrentVersion,
            SessionId = "session-alpha",
            StreamId = 1,
            Kind = TunnelFrameKindMessage.Complete
        }, (responseFrame, _) =>
        {
            frames.Add(responseFrame);
            if (responseFrame.Kind == TunnelFrameKindMessage.Complete)
            {
                responseCompleted.TrySetResult();
            }

            return Task.CompletedTask;
        }, timeout.Token);
        await dispatcher.Completed.Task.WaitAsync(timeout.Token);
        await responseCompleted.Task.WaitAsync(timeout.Token);

        Assert.Contains(frames, frame => frame.Kind == TunnelFrameKindMessage.ResponseHeaders);
        Assert.Contains(frames, frame => frame.Kind == TunnelFrameKindMessage.Complete);
    }

    [Fact]
    public async Task Gateway_invoker_should_emit_activity_and_forward_trace_context()
    {
        var stoppedActivities = new List<Activity>();
        using var listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == ReverseTunnelMetrics.ActivitySourceName,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
            ActivityStopped = activity => stoppedActivities.Add(activity)
        };
        ActivitySource.AddActivityListener(listener);
        var options = CreateOptions();
        var optionsAccessor = Options.Create(options);
        var registry = new InMemoryReverseTunnelSessionRegistry(optionsAccessor);
        var session = new ReverseTunnelSession("session-alpha", "agent-alpha", ["order-service"], DateTimeOffset.UtcNow);
        registry.Register(session);
        var localHttpHandler = new CapturingLocalHttpHandler(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("trace-ok", Encoding.UTF8, "text/plain")
        });
        var localDispatcher = new ReverseTunnelAgentLocalHttpDispatcher(optionsAccessor, new FixedHttpClientFactory(new HttpClient(localHttpHandler)));
        var frameProcessor = CreateFrameProcessor(localDispatcher, optionsAccessor);
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        var agentLoop = RunSingleAgentExchangeAsync(session, frameProcessor, timeout.Token);
        var invoker = new ReverseTunnelInvoker(registry, optionsAccessor);
        using var request = new HttpRequestMessage(HttpMethod.Get, "http://tunnel-order-service/api/orders/trace");

        using var response = await invoker.InvokeAsync(options.Routes[0], request, timeout.Token);
        await agentLoop;
        await response.Content!.ReadAsStringAsync(timeout.Token);

        Assert.True(localHttpHandler.LastRequestHeaders.TryGetValue("traceparent", out var traceparent));
        Assert.StartsWith("00-", traceparent, StringComparison.Ordinal);
        var activity = Assert.Single(stoppedActivities, item => item.OperationName == "airtunnel.gateway.invoke");
        Assert.Equal("order-service", activity.GetTagItem("reverse_tunnel.service_key"));
        Assert.Equal("agent-alpha", activity.GetTagItem("reverse_tunnel.agent_id"));
        Assert.Equal(200, activity.GetTagItem("http.response.status_code"));
    }

    private static async Task RunSingleAgentExchangeAsync(ReverseTunnelSession session, ReverseTunnelAgentFrameProcessor frameProcessor, CancellationToken cancellationToken)
    {
        var responseCompleted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        while (true)
        {
            var readTask = session.OutboundFrames.ReadAsync(cancellationToken).AsTask();
            var completedTask = await Task.WhenAny(readTask, responseCompleted.Task);
            if (completedTask == responseCompleted.Task)
            {
                await responseCompleted.Task;
                return;
            }

            var frame = await readTask;
            await frameProcessor.ProcessAsync(frame, (response, _) =>
            {
                session.HandleInboundFrame(response);
                if (response.Kind == TunnelFrameKindMessage.Complete && response.StreamId > 0)
                {
                    responseCompleted.TrySetResult();
                }

                return Task.CompletedTask;
            }, cancellationToken);
        }
    }

    private static ReverseTunnelOptions CreateOptions(int maxFramePayloadBytes = 64 * 1024, long maxResponseBodyBytes = 10 * 1024 * 1024)
    {
        var options = new ReverseTunnelOptions
        {
            Enabled = true,
            Roles = ReverseTunnelRole.Gateway | ReverseTunnelRole.Agent,
            MaxRequestBodyBytes = 1024 * 1024,
            MaxResponseBodyBytes = maxResponseBodyBytes,
            MaxFramePayloadBytes = maxFramePayloadBytes,
            Routes =
            [
                new ReverseTunnelRouteOptions
                {
                    VirtualHost = "tunnel-order-service",
                    ServiceKey = "order-service",
                    TimeoutSeconds = 5
                }
            ],
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

    private static ReverseTunnelAgentFrameProcessor CreateFrameProcessor(IReverseTunnelAgentLocalHttpDispatcher dispatcher, IOptions<ReverseTunnelOptions> options)
    {
        return new ReverseTunnelAgentFrameProcessor(dispatcher, new NoopTcpDispatcher(), new NoopUdpDispatcher(), options);
    }

    private sealed class FixedHttpClientFactory : IHttpClientFactory
    {
        private readonly HttpClient client;

        public FixedHttpClientFactory(HttpClient client)
        {
            this.client = client;
        }

        public HttpClient CreateClient(string name) => client;
    }

    private sealed class StreamingStartDispatcher : IReverseTunnelAgentLocalHttpDispatcher
    {
        public TaskCompletionSource Started { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public TaskCompletionSource Completed { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public async Task DispatchAsync(
            ReverseTunnelHttpRequestPayload request,
            ChannelReader<byte[]> requestBodyReader,
            Func<ReverseTunnelHttpResponsePayload, CancellationToken, Task> onResponseHeaders,
            Func<ReadOnlyMemory<byte>, CancellationToken, Task> onResponseBody,
            CancellationToken cancellationToken = default)
        {
            Started.TrySetResult();
            await foreach (var _ in requestBodyReader.ReadAllAsync(cancellationToken))
            {
            }

            await onResponseHeaders(new ReverseTunnelHttpResponsePayload { StatusCode = 200 }, cancellationToken);
            Completed.TrySetResult();
        }

        public Task<ReverseTunnelLocalHttpResponse> DispatchAsync(ReverseTunnelHttpRequestPayload request, byte[] body, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new ReverseTunnelLocalHttpResponse
            {
                Headers = new ReverseTunnelHttpResponsePayload { StatusCode = 200 }
            });
        }
    }

    private sealed class CapturingLocalHttpHandler : HttpMessageHandler
    {
        private readonly HttpResponseMessage response;

        public CapturingLocalHttpHandler(HttpResponseMessage response)
        {
            this.response = response;
        }

        public Uri? LastRequestUri { get; private set; }

        public string? LastMethod { get; private set; }

        public string? LastBody { get; private set; }

        public string? LastContentType { get; private set; }

        public HashSet<string> LastRequestHeaderNames { get; } = new(StringComparer.OrdinalIgnoreCase);

        public Dictionary<string, string> LastRequestHeaders { get; } = new(StringComparer.OrdinalIgnoreCase);

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastRequestUri = request.RequestUri;
            LastMethod = request.Method.Method;
            LastBody = request.Content is null ? string.Empty : await request.Content.ReadAsStringAsync(cancellationToken);
            LastContentType = request.Content?.Headers.ContentType?.MediaType;
            foreach (var header in request.Headers)
            {
                LastRequestHeaderNames.Add(header.Key);
                LastRequestHeaders[header.Key] = string.Join(",", header.Value);
            }

            return response;
        }
    }

    private sealed class StaticResponseHandler : HttpMessageHandler
    {
        private readonly HttpStatusCode statusCode;

        public StaticResponseHandler(HttpStatusCode statusCode)
        {
            this.statusCode = statusCode;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(new HttpResponseMessage(statusCode));
        }
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
        {
            return sendErrorAsync("noop_tcp_dispatcher", "No TCP dispatcher is configured for this HTTP-only test.", cancellationToken);
        }
    }

    private sealed class NoopUdpDispatcher : IReverseTunnelAgentUdpDispatcher
    {
        public Task SendAsync(
            long streamId,
            ReverseTunnelUdpDatagramPayload payload,
            Func<ReverseTunnelUdpDatagramPayload, CancellationToken, Task> sendDatagramAsync,
            Func<string, string, CancellationToken, Task> sendErrorAsync,
            CancellationToken cancellationToken)
        {
            return sendErrorAsync("noop_udp_dispatcher", "No UDP dispatcher is configured for this HTTP-only test.", cancellationToken);
        }

        public void Close(long streamId)
        {
        }
    }
}
