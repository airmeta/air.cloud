using Air.Cloud.Modules.ReverseTunnel.Common.Options;
using Air.Cloud.Modules.ReverseTunnel.Common.Runtime;

using Microsoft.Extensions.Options;

namespace Air.Cloud.Modules.ReverseTunnel.Common.Routing;

/// <summary>
/// <para>zh-cn:基于 Agent 路由目录的只读路由解析器。旧的 Server 显式 Routes/VirtualHost 配置不再参与匹配。</para>
/// <para>en-us:Read-only route resolver backed by the Agent route catalog. Legacy server-side Routes/VirtualHost configuration is no longer considered.</para>
/// </summary>
public sealed class ReverseTunnelRouteProvider : IReverseTunnelRouteProvider
{
    private readonly IReverseTunnelRuntimeOptionsAccessor options;
    private readonly IReverseTunnelAgentRouteCatalog? agentRouteCatalog;

    /// <summary>
    /// <para>zh-cn:创建路由解析器。</para>
    /// <para>en-us:Creates the route resolver.</para>
    /// </summary>
    /// <param name="options">
    /// <para>zh-cn:已绑定并后置配置的反向隧道选项。</para>
    /// <para>en-us:Bound and post-configured reverse tunnel options.</para>
    /// </param>
    /// <param name="agentRouteCatalog">
    /// <para>zh-cn:由 Agent 配置派生的路由目录。未提供目录时不会解析任何旧的显式路由。</para>
    /// <para>en-us:Route catalog derived from Agent configuration. When omitted, no legacy explicit route is resolved.</para>
    /// </param>
    public ReverseTunnelRouteProvider(
        IReverseTunnelRuntimeOptionsAccessor options,
        IReverseTunnelAgentRouteCatalog? agentRouteCatalog = null)
    {
        this.options = options;
        this.agentRouteCatalog = agentRouteCatalog;
    }

    /// <summary>
    /// <para>zh-cn:使用静态 IOptions 创建路由解析器，适用于只读宿主和测试。</para>
    /// <para>en-us:Creates the route resolver from static IOptions for read-only hosts and tests.</para>
    /// </summary>
    /// <param name="options">
    /// <para>zh-cn:静态 AirTunnel 配置。</para>
    /// <para>en-us:Static AirTunnel options.</para>
    /// </param>
    public ReverseTunnelRouteProvider(IOptions<ReverseTunnelOptions> options)
        : this(new ReverseTunnelRuntimeOptionsAccessor(options))
    {
    }

    /// <inheritdoc />
    public bool TryResolve(string publicPath, out ReverseTunnelRouteOptions? route)
    {
        route = null;
        var normalizedPublicPath = ReverseTunnelServiceKeyFactory.NormalizeHttpPublicPath(publicPath);
        var settings = options.Current;

        if (!settings.Enabled || string.IsNullOrWhiteSpace(normalizedPublicPath))
        {
            return false;
        }

        return agentRouteCatalog?.TryResolve(normalizedPublicPath, out route) == true;
    }
}
