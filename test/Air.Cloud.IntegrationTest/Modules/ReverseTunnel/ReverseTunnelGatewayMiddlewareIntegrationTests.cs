using Air.Cloud.Modules.ReverseTunnel.Common.Options;
using Air.Cloud.Modules.ReverseTunnel.Common.Routing;
using Air.Cloud.Modules.ReverseTunnel.Extensions;
using Air.Cloud.Modules.ReverseTunnel.Gateway;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

using System.Net;
using System.Net.Http;
using System.Text;

namespace Air.Cloud.IntegrationTest.Modules.ReverseTunnel;

/// <summary>
/// <para>zh-cn:验证 Gateway 自有 HTTP 隧道路由中间件。</para>
/// <para>en-us:Integration coverage for the Gateway-native HTTP tunnel-routing middleware.</para>
/// </summary>
public sealed class ReverseTunnelGatewayMiddlewareIntegrationTests
{
    /// <summary>
    /// <para>zh-cn:验证命中公开路径时中间件剥离前缀、调用隧道调用器，并透传 Agent 响应。</para>
    /// <para>en-us:Verifies that a public-path match strips its prefix, calls the tunnel invoker, and passes through the Agent response.</para>
    /// </summary>
    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Module", "ReverseTunnel")]
    public async Task UseReverseTunnelGateway_should_forward_a_matching_public_path()
    {
        var invoker = new CapturingInvoker();
        await using var app = await CreateAppAsync(invoker);
        using var client = app.GetTestClient();

        using var response = await client.PostAsync("/orders/api/list?limit=10", new StringContent("payload", Encoding.UTF8, "text/plain"));

        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        Assert.Equal("agent-response", await response.Content.ReadAsStringAsync());
        Assert.Equal("/api/list?limit=10", invoker.PathAndQuery);
        Assert.Equal("payload", invoker.RequestBody);
        Assert.Equal("orders-key", invoker.ServiceKey);
    }

    /// <summary>
    /// <para>zh-cn:验证独立 Server 角色启用 Gateway-native 中间件后，可以直接使用进程内 Agent 会话转发公开 HTTP 路由。</para>
    /// <para>en-us:Verifies that a standalone Server-role host can forward a public HTTP route through an in-process Agent session after enabling the Gateway-native middleware.</para>
    /// </summary>
    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Module", "ReverseTunnel")]
    public async Task UseReverseTunnelGateway_should_forward_when_host_uses_server_mode()
    {
        var invoker = new CapturingInvoker();
        await using var app = await CreateAppAsync(invoker, ReverseTunnelMode.Server);
        using var client = app.GetTestClient();

        using var response = await client.GetAsync("/orders/api/list");

        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        Assert.Equal("/api/list", invoker.PathAndQuery);
        Assert.Equal("orders-key", invoker.ServiceKey);
    }

    /// <summary>
    /// <para>zh-cn:验证 Gateway 中间件解析末尾通配符并将捕获路径应用到重写目标。</para>
    /// <para>en-us:Verifies that the Gateway middleware parses a terminal wildcard and applies the captured path to the rewrite target.</para>
    /// </summary>
    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Module", "ReverseTunnel")]
    public async Task UseReverseTunnelGateway_should_rewrite_terminal_wildcard_path()
    {
        var invoker = new CapturingInvoker();
        await using var app = await CreateAppAsync(invoker);
        using var client = app.GetTestClient();

        using var response = await client.GetAsync("/api/orders/2026/items");

        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        Assert.Equal("/tests/2026/items", invoker.PathAndQuery);
        Assert.Equal("wildcard-key", invoker.ServiceKey);
    }

    /// <summary>
    /// <para>zh-cn:验证配置了 RewritePath 的公开路由仍将查询串作为查询部分传给 Agent，而不是编码进路径。</para>
    /// <para>en-us:Verifies that a route with RewritePath keeps the query string as a query component instead of encoding it into the path before forwarding to the Agent.</para>
    /// </summary>
    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Module", "ReverseTunnel")]
    public async Task UseReverseTunnelGateway_should_preserve_query_when_rewriting_path()
    {
        var invoker = new CapturingInvoker();
        await using var app = await CreateAppAsync(invoker);
        using var client = app.GetTestClient();

        using var response = await client.GetAsync("/rewrite/system/quickEntry/statistics?entryType=SERVICE");

        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        Assert.Equal("/prod-api/system/quickEntry/statistics?entryType=SERVICE", invoker.PathAndQuery);
        Assert.Equal("rewrite-key", invoker.ServiceKey);
    }

    /// <summary>
    /// <para>zh-cn:验证未命中公开路径时中间件继续后续管道。</para>
    /// <para>en-us:Verifies that an unmatched public path continues through the downstream pipeline.</para>
    /// </summary>
    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Module", "ReverseTunnel")]
    public async Task UseReverseTunnelGateway_should_continue_when_no_route_matches()
    {
        var invoker = new CapturingInvoker();
        await using var app = await CreateAppAsync(invoker);
        using var client = app.GetTestClient();

        using var response = await client.GetAsync("/ordinary-route");

        Assert.Equal((HttpStatusCode)StatusCodes.Status418ImATeapot, response.StatusCode);
        Assert.Null(invoker.PathAndQuery);
    }

    private static async Task<WebApplication> CreateAppAsync(CapturingInvoker invoker, ReverseTunnelMode tunnelMode = ReverseTunnelMode.None)
    {
        var options = new ReverseTunnelOptions { Enabled = true, TunnelMode = tunnelMode, EnableTunnelProxy = true };
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddReverseTunnel();
        builder.Services.AddSingleton<IOptions<ReverseTunnelOptions>>(Options.Create(options));
        builder.Services.RemoveAll<IReverseTunnelAgentRouteCatalog>();
        builder.Services.AddSingleton<IReverseTunnelAgentRouteCatalog>(new StubCatalog());
        builder.Services.RemoveAll<IReverseTunnelInvoker>();
        builder.Services.AddSingleton<IReverseTunnelInvoker>(invoker);
        var app = builder.Build();
        app.UseReverseTunnelGateway();
        app.Run(context =>
        {
            context.Response.StatusCode = StatusCodes.Status418ImATeapot;
            return Task.CompletedTask;
        });
        await app.StartAsync();
        return app;
    }

    private sealed class StubCatalog : IReverseTunnelAgentRouteCatalog
    {
        private static readonly ReverseTunnelRouteOptions[] Routes =
        [
            new() { PublicPath = "/orders", ServiceKey = "orders-key", TimeoutSeconds = 30 },
            new() { PublicPath = "/api/orders/{path}", RewritePath = "/tests/{path}", ServiceKey = "wildcard-key", TimeoutSeconds = 30 },
            new() { PublicPath = "/rewrite", RewritePath = "/prod-api", ServiceKey = "rewrite-key", TimeoutSeconds = 30 }
        ];

        public bool TryResolve(string publicPath, out ReverseTunnelRouteOptions? route)
        {
            route = Routes.FirstOrDefault(item => string.Equals(item.PublicPath, publicPath, StringComparison.OrdinalIgnoreCase));
            return route is not null;
        }

        public IReadOnlyList<ReverseTunnelRouteOptions> List() => Routes;
        public IReadOnlyList<ReverseTunnelTcpListenerOptions> ListTcpListeners() => [];
        public IReadOnlyList<ReverseTunnelUdpListenerOptions> ListUdpListeners() => [];
        public Task RefreshAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class CapturingInvoker : IReverseTunnelInvoker
    {
        public string? PathAndQuery { get; private set; }
        public string? RequestBody { get; private set; }
        public string? ServiceKey { get; private set; }

        public async Task<HttpResponseMessage> InvokeAsync(ReverseTunnelRouteOptions route, HttpRequestMessage request, CancellationToken cancellationToken = default)
        {
            ServiceKey = route.ServiceKey;
            PathAndQuery = request.RequestUri?.PathAndQuery;
            RequestBody = request.Content is null ? null : await request.Content.ReadAsStringAsync(cancellationToken);
            var response = new HttpResponseMessage(HttpStatusCode.Accepted)
            {
                Content = new StringContent("agent-response", Encoding.UTF8, "text/plain")
            };
            response.Headers.TryAddWithoutValidation("X-AirTunnel-Test", "forwarded");
            return response;
        }
    }
}


