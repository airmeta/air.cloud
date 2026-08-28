using Air.Cloud.Modules.ReverseTunnel.Common.Options;
using Air.Cloud.Modules.ReverseTunnel.Common.Routing;
using Air.Cloud.Modules.ReverseTunnel.Common.Runtime;
using Air.Cloud.Modules.ReverseTunnel.Server;

using Microsoft.Extensions.Configuration;

namespace Air.Cloud.UnitTest.Modules.ReverseTunnel;

public sealed class ReverseTunnelAgentRouteCatalogTests
{
    /// <summary>
    /// <para>zh-cn:验证 Agent HTTP 本地服务会自动生成由公开路径定位的 Server 路由。</para>
    /// <para>en-us:Verifies that an Agent HTTP local service automatically produces a Server route located by public path.</para>
    /// </summary>
    [Fact]
    public async Task Refresh_should_derive_default_route_from_agent_local_service()
    {
        var store = new StubDesiredAgentConfigurationStore(
            CreateSnapshot("agent-a", new ReverseTunnelOptions
            {
                LocalServices = [new ReverseTunnelLocalServiceOptions
                {
                    PublicPath = " /orders/ ",
                    LocalAddress = "http://127.0.0.1:6101"
                }]
            }));
        var catalog = new ReverseTunnelAgentRouteCatalog(store);

        await catalog.RefreshAsync();

        Assert.True(catalog.TryResolve("/ORDERS", out var route));
        Assert.NotNull(route);
        Assert.Equal("/orders", route.PublicPath);
        Assert.Equal(ReverseTunnelServiceKeyFactory.Apply(new ReverseTunnelOptions
        {
            LocalServices = [new ReverseTunnelLocalServiceOptions { PublicPath = "/orders" }]
        }).LocalServices[0].ServiceKey, route.ServiceKey);
        Assert.Equal(30, route.TimeoutSeconds);
    }

    /// <summary>
    /// <para>zh-cn:验证已废弃的显式 Routes 不会覆盖由 Agent 公开路径派生的入口。</para>
    /// <para>en-us:Verifies that deprecated explicit Routes do not override an entry derived from an Agent public path.</para>
    /// </summary>
    [Fact]
    public async Task Refresh_should_prefer_explicit_agent_route_for_service_key()
    {
        var store = new StubDesiredAgentConfigurationStore(
            CreateSnapshot("agent-a", new ReverseTunnelOptions
            {
                Routes = [new ReverseTunnelRouteOptions
                {
                    VirtualHost = "orders.internal",
                    ServiceKey = "orders",
                    TimeoutSeconds = 45
                }],
                LocalServices = [new ReverseTunnelLocalServiceOptions
                {
                    PublicPath = "/orders",
                    LocalAddress = "http://127.0.0.1:6101"
                }]
            }));
        var catalog = new ReverseTunnelAgentRouteCatalog(store);

        await catalog.RefreshAsync();

        Assert.True(catalog.TryResolve("/orders", out var route));
        Assert.NotNull(route);
        Assert.Equal("/orders", route.PublicPath);
        Assert.Equal(30, route.TimeoutSeconds);
        Assert.False(catalog.TryResolve("/orders/internal", out _));
    }

    /// <summary>
    /// <para>zh-cn:验证多个 Agent 声明同一 ServiceKey 时只生成一个逻辑入口。</para>
    /// <para>en-us:Verifies that multiple Agents declaring one ServiceKey produce one logical entry.</para>
    /// </summary>
    [Fact]
    public async Task Refresh_should_merge_duplicate_service_keys_into_one_logical_route()
    {
        var store = new StubDesiredAgentConfigurationStore(
            CreateSnapshot("agent-a", CreateConfiguration("orders", "http://127.0.0.1:6101")),
            CreateSnapshot("agent-b", CreateConfiguration("orders", "http://127.0.0.1:6102")));
        var catalog = new ReverseTunnelAgentRouteCatalog(store);

        await catalog.RefreshAsync();

        var routes = catalog.List();
        var route = Assert.Single(routes);
        Assert.Equal("/orders", route.PublicPath);
        Assert.Equal(ReverseTunnelServiceKeyFactory.Apply(new ReverseTunnelOptions
        {
            LocalServices = [new ReverseTunnelLocalServiceOptions { PublicPath = "/orders" }]
        }).LocalServices[0].ServiceKey, route.ServiceKey);
    }

    /// <summary>
    /// <para>zh-cn:验证 Agent TCP/UDP 本地目标会派生为 Server 监听器，并保持本地端口与对外端口独立。</para>
    /// <para>en-us:Verifies that Agent TCP/UDP targets derive Server listeners while keeping local and exposed ports independent.</para>
    /// </summary>
    [Fact]
    public async Task Refresh_should_derive_tcp_and_udp_listeners_from_agent_services()
    {
        var configuration = new ReverseTunnelOptions
        {
            Tcp = new ReverseTunnelTcpOptions
            {
                Enabled = true,
                LocalServices = [new ReverseTunnelTcpLocalServiceOptions
                {
                    ServiceKey = "redis",
                    Host = "10.0.0.5",
                    Port = 6379,
                    ServerListenHost = "0.0.0.0",
                    ServerListenPort = 16379
                }]
            },
            Udp = new ReverseTunnelUdpOptions
            {
                Enabled = true,
                LocalServices = [new ReverseTunnelUdpLocalServiceOptions
                {
                    ServiceKey = "dns",
                    Host = "10.0.0.6",
                    Port = 53,
                    ServerListenHost = "127.0.0.1",
                    ServerListenPort = 1053
                }]
            }
        };
        var catalog = new ReverseTunnelAgentRouteCatalog(
            new StubDesiredAgentConfigurationStore(CreateSnapshot("agent-a", configuration)));

        await catalog.RefreshAsync();

        var tcp = Assert.Single(catalog.ListTcpListeners());
        Assert.Matches("^[0-9a-f]{32}$", tcp.ServiceKey);
        Assert.Equal("0.0.0.0", tcp.ListenHost);
        Assert.Equal(16379, tcp.ListenPort);
        var udp = Assert.Single(catalog.ListUdpListeners());
        Assert.Matches("^[0-9a-f]{32}$", udp.ServiceKey);
        Assert.Equal("127.0.0.1", udp.ListenHost);
        Assert.Equal(1053, udp.ListenPort);
    }

    private static ReverseTunnelOptions CreateConfiguration(string serviceKey, string localAddress)
        => new()
        {
            LocalServices = [new ReverseTunnelLocalServiceOptions
            {
                PublicPath = "/" + serviceKey,
                LocalAddress = localAddress
            }]
        };

    private static ReverseTunnelAgentConfigurationSnapshot CreateSnapshot(string agentId, ReverseTunnelOptions configuration)
    {
        ReverseTunnelServiceKeyFactory.Apply(configuration);
        configuration.PostConfigure(configuration, new ConfigurationBuilder().Build());
        return ReverseTunnelAgentConfigurationSnapshotSerializer.Create(agentId, configuration, 1);
    }

    private sealed class StubDesiredAgentConfigurationStore : IReverseTunnelDesiredAgentConfigurationStore
    {
        private readonly IReadOnlyList<ReverseTunnelAgentConfigurationSnapshot> snapshots;

        public StubDesiredAgentConfigurationStore(params ReverseTunnelAgentConfigurationSnapshot[] snapshots)
        {
            this.snapshots = snapshots;
        }

        public Task<ReverseTunnelAgentConfigurationSnapshot?> GetAsync(string agentId, CancellationToken cancellationToken = default)
            => Task.FromResult(snapshots.FirstOrDefault(item => string.Equals(item.AgentId, agentId, StringComparison.OrdinalIgnoreCase)));

        public Task SaveAsync(ReverseTunnelAgentConfigurationSnapshot snapshot, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task<IReadOnlyList<ReverseTunnelAgentConfigurationSnapshot>> ListAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(snapshots);
    }
}


