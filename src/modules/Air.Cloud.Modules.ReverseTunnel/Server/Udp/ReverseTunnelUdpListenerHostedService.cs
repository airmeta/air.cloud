using Air.Cloud.Modules.ReverseTunnel.Common.Options;
using Air.Cloud.Modules.ReverseTunnel.Common.Protocol;
using Air.Cloud.Modules.ReverseTunnel.Common.Routing;
using Air.Cloud.Modules.ReverseTunnel.Common.Runtime;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;

namespace Air.Cloud.Modules.ReverseTunnel.Server.Udp;

/// <summary>
/// <para>zh-cn:AirTunnel Server 侧 UDP 监听后台服务。它按客户端端点维护 UDP 流表，并通过 Agent 会话转发单个数据报。</para>
/// <para>en-us:AirTunnel Server-side UDP listener background service. It maintains UDP flow entries by client endpoint and forwards individual datagrams through Agent sessions.</para>
/// </summary>
public sealed class ReverseTunnelUdpListenerHostedService : BackgroundService
{
    private readonly IReverseTunnelRuntimeOptionsAccessor options;
    private readonly IReverseTunnelSessionRegistry sessionRegistry;
    private readonly IReverseTunnelAgentRouteCatalog? agentRouteCatalog;
    private readonly ILogger<ReverseTunnelUdpListenerHostedService> logger;
    private readonly ConcurrentDictionary<string, ListenerRegistration> listeners = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// <para>zh-cn:创建 UDP 监听后台服务。服务只在模块启用、当前进程包含 Server 角色且 UDP 配置启用时绑定端口。</para>
    /// <para>en-us:Creates the UDP listener background service. It binds ports only when the module is enabled, the current process uses Server mode, and UDP options are enabled.</para>
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
    /// <param name="agentRouteCatalog">
    /// <para>zh-cn:由 Agent 本地服务配置派生的监听器目录，可选。</para>
    /// <para>en-us:Optional catalog of listeners derived from Agent local-service configuration.</para>
    /// </param>
    public ReverseTunnelUdpListenerHostedService(
        IReverseTunnelRuntimeOptionsAccessor options,
        IReverseTunnelSessionRegistry sessionRegistry,
        ILogger<ReverseTunnelUdpListenerHostedService> logger,
        IReverseTunnelAgentRouteCatalog? agentRouteCatalog = null)
    {
        this.options = options;
        this.sessionRegistry = sessionRegistry;
        this.agentRouteCatalog = agentRouteCatalog;
        this.logger = logger;
    }

    /// <summary>
    /// <para>zh-cn:启动所有已配置 UDP 监听器，并在主机关闭时释放端口和流表。</para>
    /// <para>en-us:Starts all configured UDP listeners and releases ports and flow entries when the host stops.</para>
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
        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                await ReconcileListenersAsync(stoppingToken);
                await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
        }
        finally
        {
            await StopAllListenersAsync();
        }
    }

    private async Task ReconcileListenersAsync(CancellationToken stoppingToken)
    {
        var settings = options.Current;
        var desired = settings.Enabled && settings.TunnelMode == ReverseTunnelMode.Server && settings.Udp.Enabled
            ? settings.Udp.Listeners
                .Concat(agentRouteCatalog?.ListUdpListeners() ?? [])
                .Where(IsRunnable)
                .GroupBy(CreateBindingKey, StringComparer.OrdinalIgnoreCase)
                .Select(group => group.First())
                .ToDictionary(CreateListenerKey, StringComparer.OrdinalIgnoreCase)
            : new Dictionary<string, ReverseTunnelUdpListenerOptions>(StringComparer.OrdinalIgnoreCase);

        var stopped = new List<ListenerRegistration>();
        foreach (var item in listeners.ToArray())
        {
            if (item.Value.Task.IsCompleted || !desired.ContainsKey(item.Key))
            {
                if (listeners.TryRemove(item.Key, out var registration))
                {
                    registration.Stop();
                    stopped.Add(registration);
                }
            }
        }

        foreach (var registration in stopped)
        {
            await ObserveAndDisposeAsync(registration);
        }

        foreach (var item in desired)
        {
            listeners.GetOrAdd(item.Key, _ =>
            {
                var cancellation = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
                var task = Task.Run(() => RunListenerAsync(item.Value, cancellation.Token), CancellationToken.None);
                return new ListenerRegistration(cancellation, task);
            });
        }
    }

    private async Task StopAllListenersAsync()
    {
        var registrations = listeners.ToArray();
        listeners.Clear();
        foreach (var item in registrations)
        {
            item.Value.Stop();
        }

        foreach (var item in registrations)
        {
            await ObserveAndDisposeAsync(item.Value);
        }
    }

    private async Task ObserveAndDisposeAsync(ListenerRegistration registration)
    {
        try
        {
            await registration.Task;
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "AirTunnel UDP listener stopped with an error.");
        }
        finally
        {
            registration.Dispose();
        }
    }

    private async Task RunListenerAsync(ReverseTunnelUdpListenerOptions listenerOptions, CancellationToken cancellationToken)
    {
        using var udpClient = new UdpClient(CreateEndpoint(listenerOptions.ListenHost, listenerOptions.ListenPort));
        var flows = new ConcurrentDictionary<string, UdpFlowEntry>(StringComparer.OrdinalIgnoreCase);
        logger.LogInformation(
            "AirTunnel UDP listener {ListenerName} started on {ListenHost}:{ListenPort} for ServiceKey {ServiceKey}.",
            listenerOptions.Name,
            listenerOptions.ListenHost,
            listenerOptions.ListenPort,
            listenerOptions.ServiceKey);

        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var result = await udpClient.ReceiveAsync(cancellationToken);
                if (result.Buffer.Length > listenerOptions.MaxDatagramBytes)
                {
                    continue;
                }

                RemoveExpiredFlows(listenerOptions, flows);
                var flowKey = result.RemoteEndPoint.ToString();
                if (!flows.TryGetValue(flowKey, out var flow))
                {
                    if (flows.Count >= listenerOptions.MaxFlows)
                    {
                        continue;
                    }

                    if (!sessionRegistry.TryGetForServiceKey(listenerOptions.ServiceKey, out var session) || session is null)
                    {
                        continue;
                    }

                    flow = new UdpFlowEntry(session, session.AllocateStreamId(), result.RemoteEndPoint, DateTimeOffset.UtcNow);
                    flows[flowKey] = flow;
                }

                flow.Touch(DateTimeOffset.UtcNow);
                await flow.Session.SendUdpDatagramAsync(
                    flow.StreamId,
                    new ReverseTunnelUdpDatagramPayload
                    {
                        ServiceKey = listenerOptions.ServiceKey,
                        ListenerName = listenerOptions.Name,
                        Data = result.Buffer
                    },
                    async (datagram, token) =>
                    {
                        if (datagram.Data.Length > listenerOptions.MaxDatagramBytes)
                        {
                            return;
                        }

                        await udpClient.SendAsync(datagram.Data, datagram.Data.Length, flow.RemoteEndPoint);
                    },
                    cancellationToken);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        finally
        {
            foreach (var flow in flows.Values)
            {
                await flow.Session.CloseUdpFlowAsync(flow.StreamId, "listener_stopped", "UDP listener stopped.");
            }
        }
    }

    private static void RemoveExpiredFlows(ReverseTunnelUdpListenerOptions listenerOptions, ConcurrentDictionary<string, UdpFlowEntry> flows)
    {
        var expiresBefore = DateTimeOffset.UtcNow.AddSeconds(-listenerOptions.FlowIdleTimeoutSeconds);
        foreach (var item in flows.ToArray())
        {
            if (item.Value.LastSeenUtc > expiresBefore)
            {
                continue;
            }

            if (flows.TryRemove(item.Key, out var removed))
            {
                _ = removed.Session.CloseUdpFlowAsync(removed.StreamId, "flow_idle_timeout", "UDP flow expired because it was idle.");
            }
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

    private static bool IsRunnable(ReverseTunnelUdpListenerOptions listener)
    {
        return listener is not null &&
            !string.IsNullOrWhiteSpace(listener.ServiceKey) &&
            listener.ListenPort > 0;
    }

    private static string CreateListenerKey(ReverseTunnelUdpListenerOptions listener)
    {
        return string.Join('|',
            listener.Name,
            listener.ServiceKey,
            listener.ListenHost,
            listener.ListenPort,
            listener.MaxFlows,
            listener.FlowIdleTimeoutSeconds,
            listener.MaxDatagramBytes);
    }

    private static string CreateBindingKey(ReverseTunnelUdpListenerOptions listener)
        => string.Join('|', listener.ServiceKey, listener.ListenHost, listener.ListenPort);

    private sealed class ListenerRegistration : IDisposable
    {
        public ListenerRegistration(CancellationTokenSource cancellation, Task task)
        {
            Cancellation = cancellation;
            Task = task;
        }

        public CancellationTokenSource Cancellation { get; }

        public Task Task { get; }

        public void Stop()
        {
            Cancellation.Cancel();
        }

        public void Dispose()
        {
            Cancellation.Dispose();
        }
    }

    private sealed class UdpFlowEntry
    {
        private long lastSeenTicks;

        public UdpFlowEntry(ReverseTunnelSession session, long streamId, IPEndPoint remoteEndPoint, DateTimeOffset lastSeenUtc)
        {
            Session = session;
            StreamId = streamId;
            RemoteEndPoint = remoteEndPoint;
            lastSeenTicks = lastSeenUtc.UtcTicks;
        }

        public ReverseTunnelSession Session { get; }

        public long StreamId { get; }

        public IPEndPoint RemoteEndPoint { get; }

        public DateTimeOffset LastSeenUtc => new(Interlocked.Read(ref lastSeenTicks), TimeSpan.Zero);

        public void Touch(DateTimeOffset utcNow)
        {
            Interlocked.Exchange(ref lastSeenTicks, utcNow.UtcTicks);
        }
    }
}
