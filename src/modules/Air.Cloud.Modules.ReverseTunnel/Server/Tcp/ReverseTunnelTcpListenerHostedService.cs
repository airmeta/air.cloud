using Air.Cloud.Modules.ReverseTunnel.Common.Options;
using Air.Cloud.Modules.ReverseTunnel.Common.Protocol;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using System.Net;
using System.Net.Sockets;
using System.Threading.Channels;

namespace Air.Cloud.Modules.ReverseTunnel.Server.Tcp;

/// <summary>
/// <para>zh-cn:AirTunnel Server 侧 TCP 监听后台服务。它按配置绑定端口，并把每条外部 TCP 连接映射到一个已连接 Agent 会话。</para>
/// <para>en-us:AirTunnel Server-side TCP listener background service. It binds configured ports and maps each external TCP connection to a connected Agent session.</para>
/// </summary>
public sealed class ReverseTunnelTcpListenerHostedService : BackgroundService
{
    private readonly IOptions<ReverseTunnelOptions> options;
    private readonly IReverseTunnelSessionRegistry sessionRegistry;
    private readonly ILogger<ReverseTunnelTcpListenerHostedService> logger;

    /// <summary>
    /// <para>zh-cn:创建 TCP 监听后台服务。服务只在模块启用、当前进程包含 Server 角色且 TCP 配置启用时绑定端口。</para>
    /// <para>en-us:Creates the TCP listener background service. It binds ports only when the module is enabled, the current process includes the Server role, and TCP options are enabled.</para>
    /// </summary>
    /// <param name="options">
    /// <para>zh-cn:AirTunnel 配置访问器。</para>
    /// <para>en-us:AirTunnel options accessor.</para>
    /// </param>
    /// <param name="sessionRegistry">
    /// <para>zh-cn:当前 Server 进程内的 Agent 会话注册表。</para>
    /// <para>en-us:Agent session registry for the current Server process.</para>
    /// </param>
    /// <param name="logger">
    /// <para>zh-cn:日志记录器。</para>
    /// <para>en-us:Logger.</para>
    /// </param>
    public ReverseTunnelTcpListenerHostedService(
        IOptions<ReverseTunnelOptions> options,
        IReverseTunnelSessionRegistry sessionRegistry,
        ILogger<ReverseTunnelTcpListenerHostedService> logger)
    {
        this.options = options;
        this.sessionRegistry = sessionRegistry;
        this.logger = logger;
    }

    /// <summary>
    /// <para>zh-cn:启动所有已配置 TCP 监听器，并在主机关闭时释放端口。</para>
    /// <para>en-us:Starts all configured TCP listeners and releases ports when the host stops.</para>
    /// </summary>
    /// <param name="stoppingToken">
    /// <para>zh-cn:主机停止令牌。</para>
    /// <para>en-us:Host stopping token.</para>
    /// </param>
    /// <returns>
    /// <para>zh-cn:表示监听生命周期的异步任务。</para>
    /// <para>en-us:Task representing the listener lifecycle.</para>
    /// </returns>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var settings = options.Value;
        if (!settings.Enabled || !settings.Roles.HasFlag(ReverseTunnelRole.Server) || !settings.Tcp.Enabled)
        {
            return;
        }

        var tasks = settings.Tcp.Listeners.Select(listener => RunListenerAsync(listener, stoppingToken)).ToArray();
        await Task.WhenAll(tasks);
    }

    private async Task RunListenerAsync(ReverseTunnelTcpListenerOptions listenerOptions, CancellationToken cancellationToken)
    {
        var listener = new TcpListener(CreateEndpoint(listenerOptions.ListenHost, listenerOptions.ListenPort));
        using var connectionSlots = new SemaphoreSlim(listenerOptions.MaxConnections, listenerOptions.MaxConnections);
        listener.Start();
        logger.LogInformation(
            "AirTunnel TCP listener {ListenerName} started on {ListenHost}:{ListenPort} for ServiceKey {ServiceKey}.",
            listenerOptions.Name,
            listenerOptions.ListenHost,
            listenerOptions.ListenPort,
            listenerOptions.ServiceKey);

        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var tcpClient = await listener.AcceptTcpClientAsync(cancellationToken);
                if (!await connectionSlots.WaitAsync(0, cancellationToken))
                {
                    tcpClient.Dispose();
                    continue;
                }

                _ = HandleClientAsync(listenerOptions, tcpClient, connectionSlots, cancellationToken);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        finally
        {
            listener.Stop();
        }
    }

    private async Task HandleClientAsync(ReverseTunnelTcpListenerOptions listenerOptions, TcpClient tcpClient, SemaphoreSlim connectionSlots, CancellationToken stoppingToken)
    {
        using (tcpClient)
        {
            try
            {
                if (!sessionRegistry.TryGetForServiceKey(listenerOptions.ServiceKey, out var session) || session is null)
                {
                    return;
                }

                await using var networkStream = tcpClient.GetStream();
                using var linkedCancellation = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
                var idleTimeout = TimeSpan.FromSeconds(listenerOptions.IdleTimeoutSeconds);
                linkedCancellation.CancelAfter(idleTimeout);
                var inboundData = Channel.CreateBounded<byte[]>(new BoundedChannelOptions(Math.Max(16, options.Value.OutboundFrameQueueCapacity))
                {
                    SingleReader = true,
                    SingleWriter = true,
                    FullMode = BoundedChannelFullMode.Wait
                });
                var readClientTask = ReadClientAsync(networkStream, inboundData.Writer, linkedCancellation, idleTimeout);
                await session.RunTcpTunnelAsync(
                    new ReverseTunnelTcpOpenPayload
                    {
                        ServiceKey = listenerOptions.ServiceKey,
                        ListenerName = listenerOptions.Name,
                        ConnectTimeoutSeconds = listenerOptions.ConnectTimeoutSeconds,
                        IdleTimeoutSeconds = listenerOptions.IdleTimeoutSeconds
                    },
                    inboundData.Reader,
                    async (body, token) =>
                    {
                        linkedCancellation.CancelAfter(idleTimeout);
                        await networkStream.WriteAsync(body, token);
                        await networkStream.FlushAsync(token);
                    },
                    idleTimeout,
                    linkedCancellation.Token);
                await readClientTask.WaitAsync(TimeSpan.FromSeconds(2));
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception exception)
            {
                logger.LogWarning(exception, "AirTunnel TCP listener {ListenerName} connection failed.", listenerOptions.Name);
            }
            finally
            {
                connectionSlots.Release();
            }
        }
    }

    private async Task ReadClientAsync(NetworkStream networkStream, ChannelWriter<byte[]> writer, CancellationTokenSource cancellation, TimeSpan idleTimeout)
    {
        var buffer = new byte[Math.Max(1_024, options.Value.MaxFramePayloadBytes)];
        try
        {
            while (!cancellation.IsCancellationRequested)
            {
                var read = await networkStream.ReadAsync(buffer, cancellation.Token);
                if (read <= 0)
                {
                    break;
                }

                cancellation.CancelAfter(idleTimeout);
                await writer.WriteAsync(buffer.AsMemory(0, read).ToArray(), cancellation.Token);
            }
        }
        finally
        {
            writer.TryComplete();
        }
    }

    private static IPEndPoint CreateEndpoint(string host, int port)
    {
        if (string.IsNullOrWhiteSpace(host) || host == "*" || host == "0.0.0.0")
        {
            return new IPEndPoint(IPAddress.Any, port);
        }

        if (host == "::")
        {
            return new IPEndPoint(IPAddress.IPv6Any, port);
        }

        return IPAddress.TryParse(host, out var address)
            ? new IPEndPoint(address, port)
            : new IPEndPoint(Dns.GetHostAddresses(host).First(), port);
    }
}
