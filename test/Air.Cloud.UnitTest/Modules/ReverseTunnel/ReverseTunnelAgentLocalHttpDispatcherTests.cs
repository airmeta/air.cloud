using Air.Cloud.Modules.ReverseTunnel.Agent;
using Air.Cloud.Modules.ReverseTunnel.Common.Http;
using Air.Cloud.Modules.ReverseTunnel.Common.Options;
using Air.Cloud.Modules.ReverseTunnel.Common.Protocol;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

using System.Net;
using System.Net.Http;

namespace Air.Cloud.UnitTest.Modules.ReverseTunnel;

/// <summary>
/// <para>zh-cn:反向隧道 Agent 本地 HTTP 分发测试集合。</para>
/// <para>en-us:Test suite for reverse tunnel Agent local HTTP dispatch.</para>
/// </summary>
public sealed class ReverseTunnelAgentLocalHttpDispatcherTests
{
    /// <summary>
    /// <para>zh-cn:验证 HTTP 请求和响应元数据负载可以稳定序列化和反序列化。</para>
    /// <para>en-us:Verifies that HTTP request and response metadata payloads can be serialized and deserialized stably.</para>
    /// </summary>
    [Fact]
    public void Payload_serializer_should_round_trip_http_metadata()
    {
        var request = new ReverseTunnelHttpRequestPayload
        {
            ServiceKey = "order-service",
            Method = "POST",
            PathAndQuery = "/api/orders?id=1",
            Headers =
            [
                new ReverseTunnelHttpHeader
                {
                    Name = "X-Trace-Id",
                    Value = "trace-1"
                }
            ]
        };
        var response = new ReverseTunnelHttpResponsePayload
        {
            StatusCode = 201,
            ReasonPhrase = "Created",
            Headers =
            [
                new ReverseTunnelHttpHeader
                {
                    Name = "Content-Type",
                    Value = "application/json"
                }
            ]
        };

        var restoredRequest = ReverseTunnelPayloadSerializer.DeserializeRequest(ReverseTunnelPayloadSerializer.SerializeRequest(request));
        var restoredResponse = ReverseTunnelPayloadSerializer.DeserializeResponse(ReverseTunnelPayloadSerializer.SerializeResponse(response));

        Assert.Equal("order-service", restoredRequest.ServiceKey);
        Assert.Equal("POST", restoredRequest.Method);
        Assert.Equal("/api/orders?id=1", restoredRequest.PathAndQuery);
        Assert.Equal("X-Trace-Id", restoredRequest.Headers[0].Name);
        Assert.Equal(201, restoredResponse.StatusCode);
        Assert.Equal("application/json", restoredResponse.Headers[0].Value);
    }

    /// <summary>
    /// <para>zh-cn:验证本地 HTTP 分发器只使用配置的 LocalServices 根地址，并保留请求 path/query。</para>
    /// <para>en-us:Verifies that the local HTTP dispatcher uses only configured LocalServices base addresses and preserves request path/query.</para>
    /// </summary>
    [Fact]
    public async Task Dispatcher_should_forward_to_configured_local_service()
    {
        var handler = new CapturingHandler(new HttpResponseMessage(HttpStatusCode.Accepted)
        {
            ReasonPhrase = "Accepted",
            Content = new StringContent("ok")
        });
        var dispatcher = new ReverseTunnelAgentLocalHttpDispatcher(
            Options.Create(CreateOptions()),
            new FixedHttpClientFactory(new HttpClient(handler)));

        var response = await dispatcher.DispatchAsync(new ReverseTunnelHttpRequestPayload
        {
            ServiceKey = "order-service",
            Method = "POST",
            PathAndQuery = "/api/orders?id=1",
            Headers =
            [
                new ReverseTunnelHttpHeader
                {
                    Name = "X-Trace-Id",
                    Value = "trace-1"
                },
                new ReverseTunnelHttpHeader
                {
                    Name = "Host",
                    Value = "attacker.example"
                }
            ]
        }, "body"u8.ToArray());

        Assert.Equal(new Uri("http://127.0.0.1:5101/api/orders?id=1"), handler.LastRequestUri);
        Assert.Equal(HttpStatusCode.Accepted, (HttpStatusCode)response.Headers.StatusCode);
        Assert.Equal("ok", System.Text.Encoding.UTF8.GetString(response.Body));
        Assert.Contains("X-Trace-Id", handler.LastRequestHeaderNames);
        Assert.DoesNotContain("Host", handler.LastRequestHeaderNames);
    }

    /// <summary>
    /// <para>zh-cn:验证请求负载中的绝对 URI 会被拒绝，避免 Server 通过 Agent 访问任意非配置目标。</para>
    /// <para>en-us:Verifies that absolute URIs in request payloads are rejected so the Server cannot make the Agent access arbitrary unconfigured targets.</para>
    /// </summary>
    [Fact]
    public async Task Dispatcher_should_reject_absolute_uri_from_request_payload()
    {
        var dispatcher = new ReverseTunnelAgentLocalHttpDispatcher(
            Options.Create(CreateOptions()),
            new FixedHttpClientFactory(new HttpClient(new CapturingHandler(new HttpResponseMessage(HttpStatusCode.OK)))));

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => dispatcher.DispatchAsync(new ReverseTunnelHttpRequestPayload
        {
            ServiceKey = "order-service",
            Method = "GET",
            PathAndQuery = "http://169.254.169.254/latest/meta-data"
        }, Array.Empty<byte>()));

        Assert.Contains("must not be an absolute URI", exception.Message, StringComparison.Ordinal);
    }

    /// <summary>
    /// <para>zh-cn:验证本地 HTTP 服务返回超过配置上限的响应体时，Agent 会终止本次分发，避免把过大的响应聚合到内存。</para>
    /// <para>en-us:Verifies that the Agent stops dispatching when the local HTTP service returns a response body over the configured limit, avoiding oversized response aggregation in memory.</para>
    /// </summary>
    [Fact]
    public async Task Dispatcher_should_reject_response_body_larger_than_configured_limit()
    {
        var dispatcher = new ReverseTunnelAgentLocalHttpDispatcher(
            Options.Create(CreateOptions(maxResponseBodyBytes: 1024)),
            new FixedHttpClientFactory(new HttpClient(new CapturingHandler(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new ByteArrayContent(new byte[2048])
            }))));

        var exception = await Assert.ThrowsAsync<ReverseTunnelHttpLimitExceededException>(() => dispatcher.DispatchAsync(new ReverseTunnelHttpRequestPayload
        {
            ServiceKey = "order-service",
            Method = "GET",
            PathAndQuery = "/api/orders"
        }, Array.Empty<byte>()));

        Assert.Equal("response_body_too_large", exception.Code);
        Assert.Contains("response body exceeds", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    private static ReverseTunnelOptions CreateOptions(long maxResponseBodyBytes = 1024)
    {
        var options = new ReverseTunnelOptions
        {
            Enabled = true,
            Roles = ReverseTunnelRole.Agent,
            MaxRequestBodyBytes = 1024,
            MaxResponseBodyBytes = maxResponseBodyBytes,
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

    private sealed class CapturingHandler : HttpMessageHandler
    {
        private readonly HttpResponseMessage response;

        public CapturingHandler(HttpResponseMessage response)
        {
            this.response = response;
        }

        public Uri? LastRequestUri { get; private set; }

        public HashSet<string> LastRequestHeaderNames { get; } = new(StringComparer.OrdinalIgnoreCase);

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastRequestUri = request.RequestUri;
            foreach (var header in request.Headers)
            {
                LastRequestHeaderNames.Add(header.Key);
            }

            return Task.FromResult(response);
        }
    }
}
