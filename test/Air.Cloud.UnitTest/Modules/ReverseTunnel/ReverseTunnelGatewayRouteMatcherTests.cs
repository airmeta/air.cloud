using Air.Cloud.Modules.ReverseTunnel.Common.Options;
using Air.Cloud.Modules.ReverseTunnel.Common.Routing;
using Air.Cloud.Modules.ReverseTunnel.Common.Runtime;
using Air.Cloud.Modules.ReverseTunnel.Gateway;

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace Air.Cloud.UnitTest.Modules.ReverseTunnel;

public sealed class ReverseTunnelGatewayRouteMatcherTests
{
    /// <summary>
    /// <para>zh-cn:验证公开路径按最长前缀和路径段边界匹配，并向 Agent 返回已剥离的相对路径。</para>
    /// <para>en-us:Verifies that public paths match by longest prefix and segment boundary and return the Agent-relative path with the prefix removed.</para>
    /// </summary>
    [Fact]
    public void TryMatch_should_prefer_the_longest_segment_prefix()
    {
        var matcher = CreateMatcher("/orders", "/orders/admin");

        var found = matcher.TryMatch(new PathString("/orders/admin/list"), out var match);

        Assert.True(found);
        Assert.NotNull(match);
        Assert.Equal("/orders/admin", match.Route.PublicPath);
        Assert.Equal("/list", match.AgentPath.Value);
        Assert.False(matcher.TryMatch(new PathString("/orders-v2/list"), out _));
    }

    /// <summary>
    /// <para>zh-cn:验证 AirTunnel 与控制面管理路径不会被公开路由接管。</para>
    /// <para>en-us:Verifies that AirTunnel and control-plane management paths are not taken over by public routes.</para>
    /// </summary>
    [Fact]
    public void TryMatch_should_skip_reserved_management_paths()
    {
        var matcher = CreateMatcher("/");

        Assert.False(matcher.TryMatch(new PathString("/_airtunnel/admin"), out _));
        Assert.False(matcher.TryMatch(new PathString("/_airgateway/control-plane"), out _));
    }

    /// <summary>
    /// <para>zh-cn:验证末尾 catch-all 参数可以解析，并将捕获路径代入重写模板。</para>
    /// <para>en-us:Verifies that a terminal catch-all parameter is parsed and substituted into the rewrite template.</para>
    /// </summary>
    [Fact]
    public void TryMatch_should_parse_terminal_wildcard_and_rewrite_path()
    {
        var matcher = CreateMatcher("/api/orders/{path}", "/orders");

        var found = matcher.TryMatch(new PathString("/api/orders/2026/items"), out var match);

        Assert.True(found);
        Assert.NotNull(match);
        Assert.Equal("/2026/items", match.AgentPath.Value);
    }

    /// <summary>
    /// <para>zh-cn:验证固定公开路径重写时仍保留原请求的后缀。</para>
    /// <para>en-us:Verifies that a fixed public-path rewrite preserves the original request suffix.</para>
    /// </summary>
    [Fact]
    public void TryMatch_should_append_suffix_to_fixed_rewrite()
    {
        var matcher = CreateMatcherWithRewrite("/orders", "/tests");

        Assert.True(matcher.TryMatch(new PathString("/orders/123"), out var match));
        Assert.Equal("/tests/123", match!.AgentPath.Value);
    }

    private static ReverseTunnelGatewayRouteMatcher CreateMatcher(params string[] paths)
    {
        var options = new ReverseTunnelOptions { Enabled = true, TunnelMode = ReverseTunnelMode.None, EnableTunnelProxy = true };
        var accessor = new ReverseTunnelRuntimeOptionsAccessor(Options.Create(options));
        var routes = paths.Select(path => new ReverseTunnelRouteOptions { PublicPath = path, ServiceKey = path.Trim('/').PadRight(32, '0')[..32] }).ToArray();
        return new ReverseTunnelGatewayRouteMatcher(accessor, new StubCatalog(routes));
    }

    private static ReverseTunnelGatewayRouteMatcher CreateMatcherWithRewrite(string path, string rewrite)
    {
        var options = new ReverseTunnelOptions { Enabled = true, TunnelMode = ReverseTunnelMode.None, EnableTunnelProxy = true };
        var accessor = new ReverseTunnelRuntimeOptionsAccessor(Options.Create(options));
        var routes = new[] { new ReverseTunnelRouteOptions { PublicPath = path, RewritePath = rewrite, ServiceKey = "0".PadRight(32, '0') } };
        return new ReverseTunnelGatewayRouteMatcher(accessor, new StubCatalog(routes));
    }

    private sealed class StubCatalog : IReverseTunnelAgentRouteCatalog
    {
        private readonly IReadOnlyList<ReverseTunnelRouteOptions> routes;

        public StubCatalog(IReadOnlyList<ReverseTunnelRouteOptions> routes)
        {
            this.routes = routes;
        }

        public bool TryResolve(string publicPath, out ReverseTunnelRouteOptions? route)
        {
            route = routes.FirstOrDefault(item => string.Equals(item.PublicPath, publicPath, StringComparison.OrdinalIgnoreCase));
            return route is not null;
        }

        public IReadOnlyList<ReverseTunnelRouteOptions> List() => routes;
        public IReadOnlyList<ReverseTunnelTcpListenerOptions> ListTcpListeners() => [];
        public IReadOnlyList<ReverseTunnelUdpListenerOptions> ListUdpListeners() => [];
        public Task RefreshAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}


