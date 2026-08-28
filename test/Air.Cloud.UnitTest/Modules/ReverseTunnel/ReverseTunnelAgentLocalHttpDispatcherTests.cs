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
            HasBody = true,
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
        Assert.True(restoredRequest.HasBody);
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
                    Name = "Content-Type",
                    Value = "application/json"
                },
                new ReverseTunnelHttpHeader
                {
                    Name = "Host",
                    Value = "attacker.example"
                }
            ]
        }, "body"u8.ToArray());

        Assert.Equal(new Uri("http://127.0.0.1:5101/api/orders?id=1"), handler.LastRequestUri);
        Assert.True(handler.LastRequestHadContent);
        Assert.Equal("body", handler.LastBody);
        Assert.Equal("application/json", handler.LastContentType);
        Assert.Equal(HttpStatusCode.Accepted, (HttpStatusCode)response.Headers.StatusCode);
        Assert.Equal("ok", System.Text.Encoding.UTF8.GetString(response.Body));
        Assert.Contains("X-Trace-Id", handler.LastRequestHeaderNames);
        Assert.DoesNotContain("Host", handler.LastRequestHeaderNames);
    }

    /// <summary>
    /// <para>zh-cn:验证 Agent 使用拆分后的 Protocol、Host、Port 组成本地 HTTP 目标。</para>
    /// <para>en-us:Verifies that the Agent builds its local HTTP target from the split Protocol, Host, and Port fields.</para>
    /// </summary>
    [Fact]
    public async Task Dispatcher_should_build_target_from_split_address_fields()
    {
        var handler = new CapturingHandler(new HttpResponseMessage(HttpStatusCode.OK));
        var options = new ReverseTunnelOptions
        {
            Enabled = true,
            TunnelMode = ReverseTunnelMode.Agent,
            LocalServices =
            [
                new ReverseTunnelLocalServiceOptions
                {
                    ServiceKey = "order-service",
                    Protocol = "https",
                    Host = "orders.internal",
                    Port = 8443
                }
            ]
        };
        options.PostConfigure(options, new ConfigurationBuilder().Build());
        var dispatcher = new ReverseTunnelAgentLocalHttpDispatcher(Options.Create(options), new FixedHttpClientFactory(new HttpClient(handler)));

        await dispatcher.DispatchAsync(new ReverseTunnelHttpRequestPayload
        {
            ServiceKey = "order-service",
            Method = "GET",
            PathAndQuery = "/health"
        }, Array.Empty<byte>());

        Assert.Equal(new Uri("https://orders.internal:8443/health"), handler.LastRequestUri);
    }

    /// <summary>
    /// <para>zh-cn:验证 Agent 兼容旧 Gateway 将查询分隔符编码为 `%3F` 的请求负载，并恢复原始 path/query 语义。</para>
    /// <para>en-us:Verifies that the Agent accepts legacy Gateway payloads with an encoded `%3F` query separator and restores the original path/query semantics.</para>
    /// </summary>
    [Fact]
    public async Task Dispatcher_should_restore_encoded_query_separator()
    {
        var handler = new CapturingHandler(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{\"data\":1}", System.Text.Encoding.UTF8, "application/json")
        });
        var dispatcher = new ReverseTunnelAgentLocalHttpDispatcher(
            Options.Create(CreateOptions()),
            new FixedHttpClientFactory(new HttpClient(handler)));

        var response = await dispatcher.DispatchAsync(new ReverseTunnelHttpRequestPayload
        {
            ServiceKey = "order-service",
            Method = "GET",
            PathAndQuery = "/prod-api/system/quickEntry/statistics%3FentryType=SERVICE"
        }, Array.Empty<byte>());

        Assert.Equal(new Uri("http://127.0.0.1:5101/prod-api/system/quickEntry/statistics?entryType=SERVICE"), handler.LastRequestUri);
        Assert.Equal("{\"data\":1}", System.Text.Encoding.UTF8.GetString(response.Body));
    }

    /// <summary>
    /// <para>zh-cn:验证无请求体的 GET 经 Agent 本地分发时不会创建 HttpContent，避免本地 Ocelot 收到 `Transfer-Encoding: chunked` 后转下游失败。</para>
    /// <para>en-us:Verifies that a body-less GET dispatched by the Agent does not create HttpContent, avoiding local Ocelot downstream failures caused by `Transfer-Encoding: chunked`.</para>
    /// </summary>
    [Fact]
    public async Task Dispatcher_should_not_create_http_content_for_get_without_declared_body()
    {
        var handler = new CapturingHandler(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("ok")
        });
        var dispatcher = new ReverseTunnelAgentLocalHttpDispatcher(
            Options.Create(CreateOptions()),
            new FixedHttpClientFactory(new HttpClient(handler)));

        var response = await dispatcher.DispatchAsync(new ReverseTunnelHttpRequestPayload
        {
            ServiceKey = "order-service",
            Method = "GET",
            PathAndQuery = "/api/orders?id=1",
            HasBody = false,
            Headers =
            [
                new ReverseTunnelHttpHeader
                {
                    Name = "Content-Type",
                    Value = "application/json"
                }
            ]
        }, Array.Empty<byte>());

        Assert.Equal(HttpStatusCode.OK, (HttpStatusCode)response.Headers.StatusCode);
        Assert.False(handler.LastRequestHadContent);
        Assert.Equal(string.Empty, handler.LastBody);
    }

    /// <summary>
    /// <para>zh-cn:验证请求负载中的绝对 URI 会被拒绝，避免 Server 通过 Agent 访问任意非配置目标。</para>
    /// <para>en-us:Verifies that absolute URIs in request payloads are rejected so the Server cannot make the Agent access arbitrary unconfigured targets.</para>
    /// </summary>
    [Fact]
    public async Task Dispatcher_should_normalize_absolute_uri_to_configured_local_service()
    {
        var handler = new CapturingHandler(new HttpResponseMessage(HttpStatusCode.OK));
        var dispatcher = new ReverseTunnelAgentLocalHttpDispatcher(
            Options.Create(CreateOptions()),
            new FixedHttpClientFactory(new HttpClient(handler)));

        await dispatcher.DispatchAsync(new ReverseTunnelHttpRequestPayload
        {
            ServiceKey = "order-service",
            Method = "GET",
            PathAndQuery = "http://169.254.169.254/latest/meta-data"
        }, Array.Empty<byte>());

        Assert.Equal(new Uri("http://127.0.0.1:5101/latest/meta-data"), handler.LastRequestUri);
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
            TunnelMode = ReverseTunnelMode.Agent,
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

        public bool LastRequestHadContent { get; private set; }

        public string LastBody { get; private set; } = string.Empty;

        public string? LastContentType { get; private set; }

        public HashSet<string> LastRequestHeaderNames { get; } = new(StringComparer.OrdinalIgnoreCase);

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastRequestUri = request.RequestUri;
            LastRequestHadContent = request.Content is not null;
            LastBody = request.Content is null ? string.Empty : await request.Content.ReadAsStringAsync(cancellationToken);
            LastContentType = request.Content?.Headers.ContentType?.MediaType;
            foreach (var header in request.Headers)
            {
                LastRequestHeaderNames.Add(header.Key);
            }

            return response;
        }
    }
}
