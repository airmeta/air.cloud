using Air.Cloud.Modules.ReverseTunnel.Common.Options;
using Air.Cloud.Modules.ReverseTunnel.Common.Routing;
using Air.Cloud.Modules.ReverseTunnel.Gateway;
using Air.Cloud.Modules.ReverseTunnel.Server;

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

    private static ReverseTunnelOptions CreateGatewayOptions(bool enabled)
    {
        var options = new ReverseTunnelOptions
        {
            Enabled = enabled,
            Roles = ReverseTunnelRole.Gateway,
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

    private sealed class CapturingInvoker : IReverseTunnelInvoker
    {
        private readonly HttpResponseMessage response;

        public CapturingInvoker(HttpResponseMessage response)
        {
            this.response = response;
        }

        public ReverseTunnelRouteOptions? LastRoute { get; private set; }

        public Task<HttpResponseMessage> InvokeAsync(ReverseTunnelRouteOptions route, HttpRequestMessage request, CancellationToken cancellationToken = default)
        {
            LastRoute = route;
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
