using Air.Cloud.Modules.ReverseTunnel.Common.Options;
using Air.Cloud.Modules.ReverseTunnel.Common.Protocol;
using Air.Cloud.Modules.ReverseTunnel.Common.Routing;
using Air.Cloud.Modules.ReverseTunnel.Gateway;
using Air.Cloud.Modules.ReverseTunnel.Server;
using Air.Cloud.Modules.ReverseTunnel.Server.Grpc;

using Google.Protobuf;

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

using System.Net;

namespace Air.Cloud.UnitTest.Modules.ReverseTunnel;

/// <summary>
/// <para>zh-cn:反向隧道 Gateway 侧处理器和调用器测试集合。</para>
/// <para>en-us:Test suite for reverse tunnel Gateway-side handler and invoker.</para>
/// </summary>
public sealed class ReverseTunnelGatewayTests
{
    /// <summary>
    /// <para>zh-cn:验证 Ocelot opt-in 处理器在虚拟主机命中隧道路由时，不继续访问普通下游地址，而是调用隧道调用器。</para>
    /// <para>en-us:Verifies that the Ocelot opt-in handler invokes the tunnel invoker instead of continuing to an ordinary downstream address when the virtual host matches a tunnel route.</para>
    /// </summary>
    [Fact]
    public async Task Delegating_handler_should_invoke_tunnel_for_matching_virtual_host()
    {
        var options = CreateGatewayOptions(enabled: true);
        var invoker = new CapturingInvoker(new HttpResponseMessage(HttpStatusCode.Accepted));
        using var handler = new ReverseTunnelDelegatingHandler(new ReverseTunnelRouteProvider(Options.Create(options)), invoker, Options.Create(options))
        {
            InnerHandler = new StaticResponseHandler(HttpStatusCode.OK)
        };
        using var client = new HttpMessageInvoker(handler);

        var response = await client.SendAsync(new HttpRequestMessage(HttpMethod.Get, "http://tunnel-order-service/orders"), CancellationToken.None);

        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        Assert.Equal("order-service", invoker.LastRoute?.ServiceKey);
    }

    /// <summary>
    /// <para>zh-cn:验证 Ocelot 传入的下游 `HttpRequestMessage.Content` 为空但原始 ASP.NET 请求仍带有请求体和内容头时，隧道处理器会从当前上下文恢复内容，避免 JSON POST 经隧道后被本地服务识别为无媒体类型请求。</para>
    /// <para>en-us:Verifies that when Ocelot provides a downstream `HttpRequestMessage` without content while the original ASP.NET request still carries a body and content headers, the tunnel handler restores the content from the current context so JSON POST requests are not seen by the local service as media-type-less requests.</para>
    /// </summary>
    [Fact]
    public async Task Delegating_handler_should_restore_content_from_http_context_when_ocelot_request_content_is_missing()
    {
        var options = CreateGatewayOptions(enabled: true);
        var invoker = new CapturingInvoker(new HttpResponseMessage(HttpStatusCode.Accepted));
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Method = HttpMethods.Post;
        httpContext.Request.ContentType = "application/json";
        httpContext.Request.ContentLength = 2;
        httpContext.Request.Body = new MemoryStream("{}"u8.ToArray());
        using var handler = new ReverseTunnelDelegatingHandler(
            new ReverseTunnelRouteProvider(Options.Create(options)),
            invoker,
            Options.Create(options),
            new HttpContextAccessor { HttpContext = httpContext })
        {
            InnerHandler = new StaticResponseHandler(HttpStatusCode.OK)
        };
        using var client = new HttpMessageInvoker(handler);
        using var request = new HttpRequestMessage(HttpMethod.Post, "http://tunnel-order-service/v1/identity/login");

        var response = await client.SendAsync(request, CancellationToken.None);

        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        Assert.NotNull(invoker.LastRequest?.Content);
        Assert.Equal("application/json", invoker.LastRequest.Content!.Headers.ContentType?.MediaType);
        Assert.Equal("{}", await invoker.LastRequest.Content.ReadAsStringAsync());
    }

    /// <summary>
    /// <para>zh-cn:验证模块禁用时 opt-in 处理器仍继续执行 Ocelot 原有下游链路。</para>
    /// <para>en-us:Verifies that the opt-in handler continues the original Ocelot downstream chain while the module is disabled.</para>
    /// </summary>
    [Fact]
    public async Task Delegating_handler_should_fallback_to_inner_handler_when_disabled()
    {
        var options = CreateGatewayOptions(enabled: false);
        var invoker = new CapturingInvoker(new HttpResponseMessage(HttpStatusCode.Accepted));
        using var handler = new ReverseTunnelDelegatingHandler(new ReverseTunnelRouteProvider(Options.Create(options)), invoker, Options.Create(options))
        {
            InnerHandler = new StaticResponseHandler(HttpStatusCode.OK)
        };
        using var client = new HttpMessageInvoker(handler);

        var response = await client.SendAsync(new HttpRequestMessage(HttpMethod.Get, "http://tunnel-order-service/orders"), CancellationToken.None);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Null(invoker.LastRoute);
    }

    /// <summary>
    /// <para>zh-cn:验证没有可用 Agent 会话时，Gateway 调用器返回 503，而不是让请求落到不可直连的虚拟下游地址。</para>
    /// <para>en-us:Verifies that the Gateway invoker returns 503 when no Agent session is available instead of letting the request fall through to an unreachable virtual downstream address.</para>
    /// </summary>
    [Fact]
    public async Task Invoker_should_return_service_unavailable_when_no_agent_session_exists()
    {
        var options = CreateGatewayOptions(enabled: true);
        var invoker = new ReverseTunnelInvoker(new InMemoryReverseTunnelSessionRegistry(Options.Create(options)), Options.Create(options));

        var response = await invoker.InvokeAsync(options.Routes[0], new HttpRequestMessage(HttpMethod.Get, "http://tunnel-order-service/orders"));

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
    }

    /// <summary>
    /// <para>zh-cn:验证 Gateway 在请求体超过配置上限时直接返回 413，不把超限请求写入 Agent 会话队列。</para>
    /// <para>en-us:Verifies that the Gateway returns 413 when the request body exceeds the configured limit and does not enqueue the oversized request into the Agent session.</para>
    /// </summary>
    [Fact]
    public async Task Invoker_should_return_payload_too_large_when_request_body_exceeds_limit()
    {
        var options = CreateGatewayOptions(enabled: true, maxRequestBodyBytes: 1024);
        var optionsAccessor = Options.Create(options);
        var registry = new InMemoryReverseTunnelSessionRegistry(optionsAccessor);
        registry.Register(new ReverseTunnelSession("session-one", "agent-a", ["order-service"], DateTimeOffset.UtcNow, maxRequestBodyBytes: options.MaxRequestBodyBytes));
        var invoker = new ReverseTunnelInvoker(registry, optionsAccessor);
        using var request = new HttpRequestMessage(HttpMethod.Post, "http://tunnel-order-service/orders")
        {
            Content = new ByteArrayContent(new byte[2048])
        };

        var response = await invoker.InvokeAsync(options.Routes[0], request);

        Assert.Equal(HttpStatusCode.RequestEntityTooLarge, response.StatusCode);
    }

    /// <summary>
    /// <para>zh-cn:验证 Gateway 已经收到 Agent 响应头后，如果上游提前释放响应体，Session 会向 Agent 发送 Cancel 帧，避免长响应继续占用隧道流。</para>
    /// <para>en-us:Verifies that after the Gateway has received Agent response headers, releasing the upstream response body early makes the session send a Cancel frame to the Agent so long responses do not keep occupying the tunnel stream.</para>
    /// </summary>
    [Fact]
    public async Task Invoker_should_send_cancel_when_streaming_response_content_is_disposed_early()
    {
        var options = CreateGatewayOptions(enabled: true);
        var optionsAccessor = Options.Create(options);
        var registry = new InMemoryReverseTunnelSessionRegistry(optionsAccessor);
        var session = new ReverseTunnelSession(
            "session-one",
            "agent-a",
            ["order-service"],
            DateTimeOffset.UtcNow,
            maxConcurrentStreams: 1,
            outboundFrameQueueCapacity: 16,
            maxFramePayloadBytes: 1024,
            maxResponseBodyBytes: 4096,
            maxRequestBodyBytes: options.MaxRequestBodyBytes);
        registry.Register(session);
        var invoker = new ReverseTunnelInvoker(registry, optionsAccessor);
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        using var request = new HttpRequestMessage(HttpMethod.Get, "http://tunnel-order-service/orders");

        var responseTask = invoker.InvokeAsync(options.Routes[0], request, timeout.Token);
        await WaitForOutboundFrameAsync(session, TunnelFrameKindMessage.Complete, timeout.Token);
        await session.HandleInboundFrameAsync(new TunnelFrameMessage
        {
            ProtocolVersion = ReverseTunnelProtocol.CurrentVersion,
            SessionId = session.SessionId,
            StreamId = 1,
            Kind = TunnelFrameKindMessage.ResponseHeaders,
            Payload = ByteString.CopyFrom(ReverseTunnelPayloadSerializer.SerializeResponse(new ReverseTunnelHttpResponsePayload
            {
                StatusCode = 200
            }))
        }, timeout.Token);
        using var response = await responseTask;

        response.Content!.Dispose();
        var cancelFrame = await WaitForOutboundFrameAsync(session, TunnelFrameKindMessage.Cancel, timeout.Token);

        Assert.Equal(1, cancelFrame.StreamId);
    }

    /// <summary>
    /// <para>zh-cn:验证上游取消 Gateway 调用时，取消错误码会映射为 499，便于日志和监控中区分客户端取消与后端失败。</para>
    /// <para>en-us:Verifies that when the upstream cancels a Gateway invocation, the cancellation error code maps to 499 so logs and monitoring can distinguish client cancellation from backend failure.</para>
    /// </summary>
    [Fact]
    public async Task Invoker_should_map_upstream_cancellation_to_499()
    {
        var options = CreateGatewayOptions(enabled: true);
        var optionsAccessor = Options.Create(options);
        var registry = new InMemoryReverseTunnelSessionRegistry(optionsAccessor);
        var session = new ReverseTunnelSession(
            "session-one",
            "agent-a",
            ["order-service"],
            DateTimeOffset.UtcNow,
            maxConcurrentStreams: 1,
            outboundFrameQueueCapacity: 16,
            maxFramePayloadBytes: 1024,
            maxResponseBodyBytes: 4096,
            maxRequestBodyBytes: options.MaxRequestBodyBytes);
        registry.Register(session);
        var invoker = new ReverseTunnelInvoker(registry, optionsAccessor);
        using var cancellation = new CancellationTokenSource();
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        using var request = new HttpRequestMessage(HttpMethod.Get, "http://tunnel-order-service/orders");

        var responseTask = invoker.InvokeAsync(options.Routes[0], request, cancellation.Token);
        await WaitForOutboundFrameAsync(session, TunnelFrameKindMessage.Complete, timeout.Token);
        await cancellation.CancelAsync();
        using var response = await responseTask.WaitAsync(timeout.Token);

        Assert.Equal(499, (int)response.StatusCode);
    }

    private static ReverseTunnelOptions CreateGatewayOptions(bool enabled, long maxRequestBodyBytes = 1024 * 1024)
    {
        var options = new ReverseTunnelOptions
        {
            Enabled = enabled,
            Roles = ReverseTunnelRole.Gateway,
            MaxRequestBodyBytes = maxRequestBodyBytes,
            Routes =
            [
                new ReverseTunnelRouteOptions
                {
                    VirtualHost = "tunnel-order-service",
                    ServiceKey = "order-service"
                }
            ]
        };
        options.PostConfigure(options, new ConfigurationBuilder().Build());
        return options;
    }

    private static async Task<TunnelFrameMessage> WaitForOutboundFrameAsync(ReverseTunnelSession session, TunnelFrameKindMessage expectedKind, CancellationToken cancellationToken)
    {
        while (true)
        {
            var frame = await session.OutboundFrames.ReadAsync(cancellationToken);
            if (frame.Kind == expectedKind)
            {
                return frame;
            }
        }
    }

    private sealed class CapturingInvoker : IReverseTunnelInvoker
    {
        private readonly HttpResponseMessage response;

        public CapturingInvoker(HttpResponseMessage response)
        {
            this.response = response;
        }

        public ReverseTunnelRouteOptions? LastRoute { get; private set; }

        public HttpRequestMessage? LastRequest { get; private set; }

        public Task<HttpResponseMessage> InvokeAsync(ReverseTunnelRouteOptions route, HttpRequestMessage request, CancellationToken cancellationToken = default)
        {
            LastRoute = route;
            LastRequest = request;
            return Task.FromResult(response);
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
}
