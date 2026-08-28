using Air.Cloud.Modules.ReverseTunnel.Common.Options;

namespace Air.Cloud.Modules.ReverseTunnel.Common.Routing;

/// <summary>
/// <para>zh-cn:由已保存 Agent 配置派生 HTTP 隧道路由的只读目录。</para>
/// <para>en-us:Read-only catalog of HTTP tunnel routes derived from saved Agent configurations.</para>
/// </summary>
public interface IReverseTunnelAgentRouteCatalog
{
    /// <summary>
    /// <para>zh-cn:尝试按 Gateway 公开路径读取派生路由。</para>
    /// <para>en-us:Attempts to read a derived route by Gateway public path.</para>
    /// </summary>
    bool TryResolve(string publicPath, out ReverseTunnelRouteOptions? route);

    /// <summary>
    /// <para>zh-cn:读取当前由 Agent 配置汇总出的路由快照。</para>
    /// <para>en-us:Reads the current route snapshot aggregated from Agent configurations.</para>
    /// </summary>
    IReadOnlyList<ReverseTunnelRouteOptions> List();

    /// <summary>
    /// <para>zh-cn:读取由 Agent TCP 本地服务配置派生出的 Server 监听器。</para>
    /// <para>en-us:Reads Server listeners derived from Agent TCP local-service configuration.</para>
    /// </summary>
    IReadOnlyList<ReverseTunnelTcpListenerOptions> ListTcpListeners();

    /// <summary>
    /// <para>zh-cn:读取由 Agent UDP 本地服务配置派生出的 Server 监听器。</para>
    /// <para>en-us:Reads Server listeners derived from Agent UDP local-service configuration.</para>
    /// </summary>
    IReadOnlyList<ReverseTunnelUdpListenerOptions> ListUdpListeners();

    /// <summary>
    /// <para>zh-cn:从持久化的 Agent 目标配置重新构建目录。保存配置后调用该方法使新路由立即生效。</para>
    /// <para>en-us:Rebuilds the catalog from persisted desired Agent configurations so saved routes take effect immediately.</para>
    /// </summary>
    Task RefreshAsync(CancellationToken cancellationToken = default);
}
