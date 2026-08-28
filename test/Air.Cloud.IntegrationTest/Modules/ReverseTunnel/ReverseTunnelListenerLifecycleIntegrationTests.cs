using Air.Cloud.Modules.ReverseTunnel.Common.Options;
using Air.Cloud.Modules.ReverseTunnel.Common.Runtime;
using Air.Cloud.Modules.ReverseTunnel.Server;
using Air.Cloud.Modules.ReverseTunnel.Server.Tcp;
using Air.Cloud.Modules.ReverseTunnel.Server.Udp;

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

using System.Net;
using System.Net.Sockets;

namespace Air.Cloud.IntegrationTest.Modules.ReverseTunnel;

/// <summary>
/// <para>zh-cn:验证 TCP 和 UDP listener 在运行期配置变化时能够先释放旧资源，再绑定新配置。</para>
/// <para>en-us:Verifies that TCP and UDP listeners release old resources before binding updated runtime configuration.</para>
/// </summary>
public sealed class ReverseTunnelListenerLifecycleIntegrationTests
{
    /// <summary>
    /// <para>zh-cn:验证 TCP listener 可以停止、重新绑定同一端口并保持服务可用。</para>
    /// <para>en-us:Verifies that a TCP listener can stop, rebind the same port, and remain available.</para>
    /// </summary>
    [Fact]
    public async Task Tcp_listener_should_release_and_rebind_port_after_runtime_update()
    {
        var port = GetFreeTcpPort();
        var accessor = CreateAccessor(CreateOptions(port, tcpEnabled: true, udpEnabled: false, listenerName: "tcp-one"));
        var registry = new InMemoryReverseTunnelSessionRegistry(accessor);
        var service = new ReverseTunnelTcpListenerHostedService(accessor, registry, NullLogger<ReverseTunnelTcpListenerHostedService>.Instance);

        await service.StartAsync(CancellationToken.None);
        try
        {
            await WaitForTcpStateAsync(port, expectedOpen: true);

            accessor.Update(CreateOptions(port, tcpEnabled: false, udpEnabled: false, listenerName: "tcp-disabled"));
            await WaitForTcpStateAsync(port, expectedOpen: false);

            accessor.Update(CreateOptions(port, tcpEnabled: true, udpEnabled: false, listenerName: "tcp-two"));
            await WaitForTcpStateAsync(port, expectedOpen: true);
        }
        finally
        {
            await service.StopAsync(CancellationToken.None);
        }
    }

    /// <summary>
    /// <para>zh-cn:验证 UDP listener 可以停止、重新绑定同一端口并保持服务可用。</para>
    /// <para>en-us:Verifies that a UDP listener can stop, rebind the same port, and remain available.</para>
    /// </summary>
    [Fact]
    public async Task Udp_listener_should_release_and_rebind_port_after_runtime_update()
    {
        var port = GetFreeUdpPort();
        var accessor = CreateAccessor(CreateOptions(port, tcpEnabled: false, udpEnabled: true, listenerName: "udp-one"));
        var registry = new InMemoryReverseTunnelSessionRegistry(accessor);
        var service = new ReverseTunnelUdpListenerHostedService(accessor, registry, NullLogger<ReverseTunnelUdpListenerHostedService>.Instance);

        await service.StartAsync(CancellationToken.None);
        try
        {
            await WaitForUdpStateAsync(port, expectedOpen: true);

            accessor.Update(CreateOptions(port, tcpEnabled: false, udpEnabled: false, listenerName: "udp-disabled"));
            await WaitForUdpStateAsync(port, expectedOpen: false);

            accessor.Update(CreateOptions(port, tcpEnabled: false, udpEnabled: true, listenerName: "udp-two"));
            await WaitForUdpStateAsync(port, expectedOpen: true);
        }
        finally
        {
            await service.StopAsync(CancellationToken.None);
        }
    }

    private static ReverseTunnelRuntimeOptionsAccessor CreateAccessor(ReverseTunnelOptions options)
        => new(Options.Create(options));

    private static ReverseTunnelOptions CreateOptions(int port, bool tcpEnabled, bool udpEnabled, string listenerName)
    {
        var options = new ReverseTunnelOptions
        {
            Enabled = true,
            TunnelMode = ReverseTunnelMode.Server,
            Tcp = new ReverseTunnelTcpOptions
            {
                Enabled = tcpEnabled,
                Listeners =
                [
                    new ReverseTunnelTcpListenerOptions
                    {
                        Name = listenerName,
                        ServiceKey = "listener-test",
                        ListenHost = "127.0.0.1",
                        ListenPort = port
                    }
                ]
            },
            Udp = new ReverseTunnelUdpOptions
            {
                Enabled = udpEnabled,
                Listeners =
                [
                    new ReverseTunnelUdpListenerOptions
                    {
                        Name = listenerName,
                        ServiceKey = "listener-test",
                        ListenHost = "127.0.0.1",
                        ListenPort = port
                    }
                ]
            }
        };
        options.PostConfigure(options, new ConfigurationManager());
        return options;
    }

    private static int GetFreeTcpPort()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        return ((IPEndPoint)listener.LocalEndpoint).Port;
    }

    private static int GetFreeUdpPort()
    {
        using var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        socket.Bind(new IPEndPoint(IPAddress.Loopback, 0));
        return ((IPEndPoint)socket.LocalEndPoint!).Port;
    }

    private static async Task WaitForTcpStateAsync(int port, bool expectedOpen)
    {
        var deadline = DateTime.UtcNow.AddSeconds(8);
        while (DateTime.UtcNow < deadline)
        {
            var open = false;
            try
            {
                using var client = new TcpClient();
                await client.ConnectAsync(IPAddress.Loopback, port).WaitAsync(TimeSpan.FromMilliseconds(500));
                open = true;
            }
            catch
            {
            }

            if (open == expectedOpen)
            {
                return;
            }

            await Task.Delay(100);
        }

        Assert.Equal(expectedOpen, await IsTcpPortOpenAsync(port));
    }

    private static async Task<bool> IsTcpPortOpenAsync(int port)
    {
        try
        {
            using var client = new TcpClient();
            await client.ConnectAsync(IPAddress.Loopback, port).WaitAsync(TimeSpan.FromMilliseconds(500));
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static async Task WaitForUdpStateAsync(int port, bool expectedOpen)
    {
        var deadline = DateTime.UtcNow.AddSeconds(8);
        while (DateTime.UtcNow < deadline)
        {
            var open = !CanBindUdpPort(port);
            if (open == expectedOpen)
            {
                return;
            }

            await Task.Delay(100);
        }

        Assert.Equal(expectedOpen, !CanBindUdpPort(port));
    }

    private static bool CanBindUdpPort(int port)
    {
        try
        {
            using var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp)
            {
                ExclusiveAddressUse = true
            };
            socket.Bind(new IPEndPoint(IPAddress.Loopback, port));
            return true;
        }
        catch (SocketException)
        {
            return false;
        }
    }
}


