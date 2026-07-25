using Air.Cloud.Modules.ReverseTunnel.Agent;
using Air.Cloud.Modules.ReverseTunnel.Common.Options;
using Air.Cloud.Modules.ReverseTunnel.Common.Routing;
using Air.Cloud.Modules.ReverseTunnel.Gateway;
using Air.Cloud.Modules.ReverseTunnel.Server;
using Air.Cloud.Modules.ReverseTunnel.Server.Grpc;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

using System.Net;
using System.Text;

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
        var frameProcessor = new ReverseTunnelAgentFrameProcessor(localDispatcher, optionsAccessor);
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
        Assert.Contains("X-Trace-Id", localHttpHandler.LastRequestHeaderNames);
    }

    private static async Task RunSingleAgentExchangeAsync(ReverseTunnelSession session, ReverseTunnelAgentFrameProcessor frameProcessor, CancellationToken cancellationToken)
    {
        while (true)
        {
            var frame = await session.OutboundFrames.ReadAsync(cancellationToken);
            var responses = await frameProcessor.ProcessAsync(frame, cancellationToken);
            foreach (var response in responses)
            {
                session.HandleInboundFrame(response);
            }

            if (frame.Kind == TunnelFrameKindMessage.Complete && frame.StreamId > 0)
            {
                return;
            }
        }
    }

    private static ReverseTunnelOptions CreateOptions()
    {
        var options = new ReverseTunnelOptions
        {
            Enabled = true,
            Roles = ReverseTunnelRole.Gateway | ReverseTunnelRole.Agent,
            MaxRequestBodyBytes = 1024 * 1024,
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

    private sealed class FixedHttpClientFactory : IHttpClientFactory
    {
        private readonly HttpClient client;

        public FixedHttpClientFactory(HttpClient client)
        {
            this.client = client;
        }

        public HttpClient CreateClient(string name) => client;
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

        public HashSet<string> LastRequestHeaderNames { get; } = new(StringComparer.OrdinalIgnoreCase);

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastRequestUri = request.RequestUri;
            LastMethod = request.Method.Method;
            LastBody = request.Content is null ? string.Empty : await request.Content.ReadAsStringAsync(cancellationToken);
            foreach (var header in request.Headers)
            {
                LastRequestHeaderNames.Add(header.Key);
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
}
