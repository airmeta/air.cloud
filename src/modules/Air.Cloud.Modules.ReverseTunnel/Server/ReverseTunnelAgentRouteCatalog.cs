using Air.Cloud.Modules.ReverseTunnel.Common.Options;
using Air.Cloud.Modules.ReverseTunnel.Common.Routing;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using System.Text.Json;

namespace Air.Cloud.Modules.ReverseTunnel.Server;

/// <summary>
/// <para>zh-cn:从 Server 持久化的 Agent 目标配置汇总 HTTP 路由以及 TCP、UDP 监听器。Agent 只声明本地目标和 Server 映射端口，Server 负责把这些声明转换为只读运行入口。</para>
/// <para>en-us:Aggregates HTTP routes and TCP/UDP listeners from Server-persisted desired Agent configurations. Agents declare local targets and Server mapping ports; the Server converts them into read-only runtime entries.</para>
/// </summary>
public sealed class ReverseTunnelAgentRouteCatalog : IReverseTunnelAgentRouteCatalog
{
    private readonly IReverseTunnelDesiredAgentConfigurationStore desiredConfigurations;
    private volatile IReadOnlyDictionary<string, ReverseTunnelRouteOptions> routes =
        new Dictionary<string, ReverseTunnelRouteOptions>(StringComparer.OrdinalIgnoreCase);
    private volatile IReadOnlyList<ReverseTunnelTcpListenerOptions> tcpListeners = [];
    private volatile IReadOnlyList<ReverseTunnelUdpListenerOptions> udpListeners = [];

    /// <summary>
    /// <para>zh-cn:创建 Agent 派生路由目录。</para>
    /// <para>en-us:Creates the Agent-derived route catalog.</para>
    /// </summary>
    /// <param name="desiredConfigurations">
    /// <para>zh-cn:Server 端持久化的 Agent 目标配置存储。</para>
    /// <para>en-us:Server-side store for persisted desired Agent configurations.</para>
    /// </param>
    public ReverseTunnelAgentRouteCatalog(IReverseTunnelDesiredAgentConfigurationStore desiredConfigurations)
    {
        this.desiredConfigurations = desiredConfigurations;
    }

    /// <inheritdoc />
    public bool TryResolve(string publicPath, out ReverseTunnelRouteOptions? route)
    {
        route = null;
        var key = ReverseTunnelServiceKeyFactory.NormalizeHttpPublicPath(publicPath);
        return !string.IsNullOrWhiteSpace(key) && routes.TryGetValue(key, out route);
    }

    /// <inheritdoc />
    public IReadOnlyList<ReverseTunnelRouteOptions> List()
        => routes.Values
            .Select(Clone)
            .OrderBy(item => item.PublicPath, StringComparer.OrdinalIgnoreCase)
            .ToArray();

    /// <inheritdoc />
    public IReadOnlyList<ReverseTunnelTcpListenerOptions> ListTcpListeners()
        => tcpListeners.Select(Clone).ToArray();

    /// <inheritdoc />
    public IReadOnlyList<ReverseTunnelUdpListenerOptions> ListUdpListeners()
        => udpListeners.Select(Clone).ToArray();

    /// <inheritdoc />
    public async Task RefreshAsync(CancellationToken cancellationToken = default)
    {
        var snapshots = await desiredConfigurations.ListAsync(cancellationToken).ConfigureAwait(false);
        var next = new Dictionary<string, ReverseTunnelRouteOptions>(StringComparer.OrdinalIgnoreCase);
        var nextTcp = new Dictionary<string, ReverseTunnelTcpListenerOptions>(StringComparer.OrdinalIgnoreCase);
        var nextUdp = new Dictionary<string, ReverseTunnelUdpListenerOptions>(StringComparer.OrdinalIgnoreCase);

        foreach (var snapshot in snapshots)
        {
            var configuration = snapshot.Configuration;
            foreach (var service in configuration.LocalServices ?? [])
            {
                if (!ReverseTunnelServiceKeyFactory.HasDerivedHttpServiceKey(service))
                {
                    continue;
                }

                var derived = new ReverseTunnelRouteOptions
                {
                    PublicPath = service!.PublicPath,
                    RewritePath = service.RewritePath,
                    ServiceKey = service.ServiceKey,
                    TimeoutSeconds = 30
                };
                derived.Normalize();
                if (string.IsNullOrWhiteSpace(derived.PublicPath) || string.IsNullOrWhiteSpace(derived.ServiceKey))
                {
                    continue;
                }

                next.TryAdd(derived.PublicPath, derived);
            }

            if (configuration.Tcp?.Enabled == true)
            {
                foreach (var service in configuration.Tcp.LocalServices ?? [])
                {
                    if (service is null)
                    {
                        continue;
                    }

                    var serviceKey = service.ServiceKey?.Trim();
                    if (!ReverseTunnelServiceKeyFactory.HasDerivedTcpServiceKey(service) || string.IsNullOrWhiteSpace(serviceKey))
                    {
                        continue;
                    }

                    var listener = new ReverseTunnelTcpListenerOptions
                    {
                        Name = $"agent-{snapshot.AgentId}-{serviceKey}",
                        ServiceKey = serviceKey,
                        ListenHost = service.ServerListenHost,
                        ListenPort = service.ServerListenPort
                    };
                    listener.Normalize();
                    nextTcp.TryAdd(CreateTcpListenerKey(listener), listener);
                }
            }

            if (configuration.Udp?.Enabled == true)
            {
                foreach (var service in configuration.Udp.LocalServices ?? [])
                {
                    if (service is null)
                    {
                        continue;
                    }

                    var serviceKey = service.ServiceKey?.Trim();
                    if (!ReverseTunnelServiceKeyFactory.HasDerivedUdpServiceKey(service) || string.IsNullOrWhiteSpace(serviceKey))
                    {
                        continue;
                    }

                    var listener = new ReverseTunnelUdpListenerOptions
                    {
                        Name = $"agent-{snapshot.AgentId}-{serviceKey}",
                        ServiceKey = serviceKey,
                        ListenHost = service.ServerListenHost,
                        ListenPort = service.ServerListenPort
                    };
                    listener.Normalize();
                    nextUdp.TryAdd(CreateUdpListenerKey(listener), listener);
                }
            }
        }

        routes = next;
        tcpListeners = nextTcp.Values
            .OrderBy(item => item.ListenHost, StringComparer.OrdinalIgnoreCase)
            .ThenBy(item => item.ListenPort)
            .ThenBy(item => item.ServiceKey, StringComparer.OrdinalIgnoreCase)
            .Select(Clone)
            .ToArray();
        udpListeners = nextUdp.Values
            .OrderBy(item => item.ListenHost, StringComparer.OrdinalIgnoreCase)
            .ThenBy(item => item.ListenPort)
            .ThenBy(item => item.ServiceKey, StringComparer.OrdinalIgnoreCase)
            .Select(Clone)
            .ToArray();
    }

    private static ReverseTunnelRouteOptions Clone(ReverseTunnelRouteOptions source)
        => new()
        {
            PublicPath = source.PublicPath,
            RewritePath = source.RewritePath,
            VirtualHost = source.VirtualHost,
            ServiceKey = source.ServiceKey,
            TimeoutSeconds = source.TimeoutSeconds
        };

    private static ReverseTunnelTcpListenerOptions Clone(ReverseTunnelTcpListenerOptions source)
        => new()
        {
            Name = source.Name,
            ServiceKey = source.ServiceKey,
            ListenHost = source.ListenHost,
            ListenPort = source.ListenPort,
            MaxConnections = source.MaxConnections,
            ConnectTimeoutSeconds = source.ConnectTimeoutSeconds,
            IdleTimeoutSeconds = source.IdleTimeoutSeconds
        };

    private static ReverseTunnelUdpListenerOptions Clone(ReverseTunnelUdpListenerOptions source)
        => new()
        {
            Name = source.Name,
            ServiceKey = source.ServiceKey,
            ListenHost = source.ListenHost,
            ListenPort = source.ListenPort,
            MaxFlows = source.MaxFlows,
            FlowIdleTimeoutSeconds = source.FlowIdleTimeoutSeconds,
            MaxDatagramBytes = source.MaxDatagramBytes
        };

    private static string CreateTcpListenerKey(ReverseTunnelTcpListenerOptions listener)
        => string.Join('|', listener.ServiceKey, listener.ListenHost, listener.ListenPort);

    private static string CreateUdpListenerKey(ReverseTunnelUdpListenerOptions listener)
        => string.Join('|', listener.ServiceKey, listener.ListenHost, listener.ListenPort);
}

/// <summary>
/// <para>zh-cn:应用启动时加载 Agent 派生路由目录，并在配置文件异常时保留宿主启动流程。</para>
/// <para>en-us:Loads the Agent-derived route catalog at application startup while keeping host startup resilient to malformed configuration files.</para>
/// </summary>
public sealed class ReverseTunnelAgentRouteCatalogHostedService : IHostedService
{
    private readonly IReverseTunnelAgentRouteCatalog catalog;
    private readonly ILogger<ReverseTunnelAgentRouteCatalogHostedService> logger;

    /// <summary>
    /// <para>zh-cn:创建路由目录启动服务。</para>
    /// <para>en-us:Creates the route-catalog hosted service.</para>
    /// </summary>
    public ReverseTunnelAgentRouteCatalogHostedService(
        IReverseTunnelAgentRouteCatalog catalog,
        ILogger<ReverseTunnelAgentRouteCatalogHostedService> logger)
    {
        this.catalog = catalog;
        this.logger = logger;
    }

    /// <inheritdoc />
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            await catalog.RefreshAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception exception) when (exception is IOException or JsonException)
        {
            logger.LogWarning(exception, "AirTunnel Agent-derived route catalog could not be loaded at startup.");
        }
    }

    /// <inheritdoc />
    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
