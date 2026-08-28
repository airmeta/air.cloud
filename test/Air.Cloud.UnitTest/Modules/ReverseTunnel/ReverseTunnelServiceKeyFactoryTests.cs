using Air.Cloud.Modules.ReverseTunnel.Common.Options;
using Air.Cloud.Modules.ReverseTunnel.Common.Routing;

namespace Air.Cloud.UnitTest.Modules.ReverseTunnel;

public sealed class ReverseTunnelServiceKeyFactoryTests
{
    /// <summary>
    /// <para>zh-cn:验证 HTTP ServiceKey 只由规范化公开路径生成，并且会覆盖手工传入的旧键。</para>
    /// <para>en-us:Verifies that an HTTP ServiceKey is generated only from the normalized public path and replaces a manually supplied legacy key.</para>
    /// </summary>
    [Fact]
    public void Apply_should_generate_a_stable_http_service_key_from_public_path()
    {
        var options = new ReverseTunnelOptions
        {
            Routes = [new ReverseTunnelRouteOptions { VirtualHost = "legacy", ServiceKey = "legacy" }],
            LocalServices = [new ReverseTunnelLocalServiceOptions
            {
                ServiceName = "Orders",
                PublicPath = " /Orders/ ",
                ServiceKey = "manual-key",
                LocalAddress = "http://127.0.0.1:6101"
            }]
        };

        ReverseTunnelServiceKeyFactory.Apply(options);
        var service = Assert.Single(options.LocalServices);

        Assert.Empty(options.Routes);
        Assert.Equal("/orders", service.PublicPath);
        Assert.NotEqual("manual-key", service.ServiceKey);
        Assert.Matches("^[0-9a-f]{32}$", service.ServiceKey);
        Assert.True(ReverseTunnelServiceKeyFactory.HasDerivedHttpServiceKey(service));
    }

    /// <summary>
    /// <para>zh-cn:验证相同 TCP/UDP 监听端点会生成确定性键，不同协议不会共享内部键。</para>
    /// <para>en-us:Verifies that identical TCP/UDP listener endpoints generate deterministic keys and different protocols do not share an internal key.</para>
    /// </summary>
    [Fact]
    public void Apply_should_generate_protocol_scoped_keys_from_listener_endpoints()
    {
        var options = new ReverseTunnelOptions
        {
            Tcp = new ReverseTunnelTcpOptions
            {
                LocalServices = [new ReverseTunnelTcpLocalServiceOptions { ServerListenHost = "0.0.0.0", ServerListenPort = 16379 }]
            },
            Udp = new ReverseTunnelUdpOptions
            {
                LocalServices = [new ReverseTunnelUdpLocalServiceOptions { ServerListenHost = "0.0.0.0", ServerListenPort = 16379 }]
            }
        };

        ReverseTunnelServiceKeyFactory.Apply(options);

        var tcp = Assert.Single(options.Tcp.LocalServices);
        var udp = Assert.Single(options.Udp.LocalServices);
        Assert.True(ReverseTunnelServiceKeyFactory.HasDerivedTcpServiceKey(tcp));
        Assert.True(ReverseTunnelServiceKeyFactory.HasDerivedUdpServiceKey(udp));
        Assert.NotEqual(tcp.ServiceKey, udp.ServiceKey);
    }
}


