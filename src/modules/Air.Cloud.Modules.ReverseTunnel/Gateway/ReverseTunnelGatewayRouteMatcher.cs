using Air.Cloud.Modules.ReverseTunnel.Common.Options;
using Air.Cloud.Modules.ReverseTunnel.Common.Routing;
using Air.Cloud.Modules.ReverseTunnel.Common.Runtime;

using Microsoft.AspNetCore.Http;

namespace Air.Cloud.Modules.ReverseTunnel.Gateway;

/// <summary>
/// <para>zh-cn:基于已保存 Agent 配置的 Gateway 自有 HTTP 路由匹配器。它只匹配由 Agent `PublicPath` 派生的路由，支持末尾 catch-all 和 `RewritePath`，未命中请求继续交给后续 Gateway 或 Ocelot 管道。</para>
/// <para>en-us:Gateway-native HTTP route matcher backed by saved Agent configuration. It matches only routes derived from Agent `PublicPath`, supports terminal catch-all parameters and `RewritePath`, and leaves unmatched requests to the remaining Gateway or Ocelot pipeline.</para>
/// </summary>
public sealed class ReverseTunnelGatewayRouteMatcher : IReverseTunnelGatewayRouteMatcher
{
    private static readonly string[] ReservedPrefixes = ["/_airtunnel", "/_airgateway", "/_control-plane"];
    private readonly IReverseTunnelRuntimeOptionsAccessor options;
    private readonly IReverseTunnelAgentRouteCatalog routeCatalog;

    /// <summary>
    /// <para>zh-cn:创建 Gateway 自有路由匹配器。</para>
    /// <para>en-us:Creates the Gateway-native route matcher.</para>
    /// </summary>
    /// <param name="options"><para>zh-cn:运行期 AirTunnel 配置访问器。</para><para>en-us:Runtime AirTunnel options accessor.</para></param>
    /// <param name="routeCatalog"><para>zh-cn:从 Agent 配置构建的只读路由目录。</para><para>en-us:Read-only route catalog built from Agent configuration.</para></param>
    public ReverseTunnelGatewayRouteMatcher(
        IReverseTunnelRuntimeOptionsAccessor options,
        IReverseTunnelAgentRouteCatalog routeCatalog)
    {
        this.options = options;
        this.routeCatalog = routeCatalog;
    }

    /// <inheritdoc />
    public bool TryMatch(PathString requestPath, out ReverseTunnelGatewayRouteMatch? match)
    {
        match = null;
        var settings = options.Current;
        var rawPath = string.IsNullOrWhiteSpace(requestPath.Value) ? "/" : requestPath.Value!;
        if (!settings.Enabled ||
            !settings.EnableTunnelProxy ||
            IsReservedPath(rawPath))
        {
            return false;
        }

        var candidate = routeCatalog.List()
            .Where(item => !string.IsNullOrWhiteSpace(item.PublicPath) && !string.IsNullOrWhiteSpace(item.ServiceKey))
            .OrderByDescending(item => item.PublicPath.Length)
            .Select(item => (Route: item, Matched: ReverseTunnelRouteTemplate.TryMatch(item.PublicPath, rawPath, out var suffix), Suffix: suffix))
            .FirstOrDefault(item => item.Matched);
        if (candidate.Route is null)
        {
            return false;
        }

        var remaining = ReverseTunnelRouteTemplate.ApplyRewrite(candidate.Route.RewritePath, candidate.Suffix);
        match = new ReverseTunnelGatewayRouteMatch(candidate.Route, new PathString(remaining));
        return true;
    }

    private static bool IsReservedPath(string path)
        => ReservedPrefixes.Any(prefix => IsSegmentPrefix(path, prefix));

    private static bool IsSegmentPrefix(string path, string configuredPrefix)
        => ReverseTunnelRouteTemplate.TryMatch(configuredPrefix, path, out _);
}
