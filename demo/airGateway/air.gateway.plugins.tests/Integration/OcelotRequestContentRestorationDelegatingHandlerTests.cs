using System.Net;
using System.Text;

using air.gateway.Middleware;

using Microsoft.AspNetCore.Http;

namespace air.gateway.plugins.tests.Integration;

/// <summary>
/// <para>zh-cn:Ocelot 下游请求内容恢复处理器测试。</para>
/// <para>en-us:Tests for the Ocelot downstream request-content restoration handler.</para>
/// </summary>
public sealed class OcelotRequestContentRestorationDelegatingHandlerTests
{
    /// <summary>
    /// <para>zh-cn:验证 Ocelot 映射出空内容、但入口 POST 仍有 JSON body 和内容头时，处理器会在发送前恢复完整内容。</para>
    /// <para>en-us:Verifies that when Ocelot maps empty content while the incoming POST still has a JSON body and content headers, the handler restores complete content before sending.</para>
    /// </summary>
    [Fact]
    public async Task Handler_should_restore_json_body_and_content_type_when_ocelot_content_is_missing()
    {
        var httpContext = new DefaultHttpContext();
        var payload = "{\"path\":\"/probe\"}";
        httpContext.Request.Method = HttpMethods.Post;
        httpContext.Request.ContentType = "application/json";
        httpContext.Request.ContentLength = Encoding.UTF8.GetByteCount(payload);
        httpContext.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(payload));
        var downstreamHandler = new CapturingHandler();
        using var handler = new OcelotRequestContentRestorationDelegatingHandler(new HttpContextAccessor { HttpContext = httpContext })
        {
            InnerHandler = downstreamHandler
        };
        using var client = new HttpMessageInvoker(handler);
        using var request = new HttpRequestMessage(HttpMethod.Post, "http://air.forge/mods/auth/hffcj_auth_api/route/validate");

        using var response = await client.SendAsync(request, CancellationToken.None);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(downstreamHandler.LastRequest?.Content);
        Assert.Equal("application/json", downstreamHandler.LastRequest!.Content!.Headers.ContentType?.MediaType);
        Assert.Equal(payload, await downstreamHandler.LastRequest.Content.ReadAsStringAsync());
    }

    /// <summary>
    /// <para>zh-cn:验证无 body 的 GET 请求不会被人为附加内容。</para>
    /// <para>en-us:Verifies that a body-less GET request is not given artificial content.</para>
    /// </summary>
    [Fact]
    public async Task Handler_should_not_add_content_to_bodyless_get_request()
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Method = HttpMethods.Get;
        var downstreamHandler = new CapturingHandler();
        using var handler = new OcelotRequestContentRestorationDelegatingHandler(new HttpContextAccessor { HttpContext = httpContext })
        {
            InnerHandler = downstreamHandler
        };
        using var client = new HttpMessageInvoker(handler);
        using var request = new HttpRequestMessage(HttpMethod.Get, "http://air.forge/mods/system/health");

        using var response = await client.SendAsync(request, CancellationToken.None);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Null(downstreamHandler.LastRequest?.Content);
    }

    private sealed class CapturingHandler : HttpMessageHandler
    {
        public HttpRequestMessage? LastRequest { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastRequest = request;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
        }
    }
}
