using Air.Cloud.Modules.ReverseTunnel.Common.Options;

namespace Air.Cloud.Modules.ReverseTunnel.Common.Routing;

/// <summary>
/// <para>zh-cn:根据 Gateway 公开路径查找 Agent 派生反向隧道路由的服务。该服务不调用 Consul，也不会把不可直连的 Agent 暴露为普通下游地址。</para>
/// <para>en-us:Service that resolves an Agent-derived reverse tunnel route from a Gateway public path. It does not call Consul and never exposes an unreachable Agent as an ordinary downstream address.</para>
/// </summary>
public interface IReverseTunnelRouteProvider
{
    /// <summary>
    /// <para>zh-cn:尝试解析公开路径对应的 Agent 派生隧道路由。</para>
    /// <para>en-us:Attempts to resolve the Agent-derived tunnel route for a public path.</para>
    /// </summary>
    /// <param name="publicPath">
    /// <para>zh-cn:Gateway 请求路径或公开路径前缀。</para>
    /// <para>en-us:Gateway request path or public path prefix.</para>
    /// </param>
    /// <param name="route">
    /// <para>zh-cn:找到时返回的路由。</para>
    /// <para>en-us:The route returned when found.</para>
    /// </param>
    /// <returns>
    /// <para>zh-cn:找到已启用模块的匹配路由时返回 true。</para>
    /// <para>en-us:True when a matching route is found while the module is enabled.</para>
    /// </returns>
    bool TryResolve(string publicPath, out ReverseTunnelRouteOptions? route);
}
