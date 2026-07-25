using Air.Cloud.Modules.ReverseTunnel.Common.Options;

namespace Air.Cloud.Modules.ReverseTunnel.Common.Routing;

/// <summary>
/// <para>zh-cn:根据 Ocelot 虚拟下游主机查找反向隧道路由的服务。该服务只解析显式配置的路由，不调用 Consul，也不会把不可直连的 Agent 暴露为普通下游地址。</para>
/// <para>en-us:Service that resolves a reverse tunnel route from an Ocelot virtual downstream host. It resolves only explicit configuration, does not call Consul, and never exposes an unreachable Agent as an ordinary downstream address.</para>
/// </summary>
public interface IReverseTunnelRouteProvider
{
    /// <summary>
    /// <para>zh-cn:尝试解析虚拟主机对应的隧道路由。</para>
    /// <para>en-us:Attempts to resolve the tunnel route for a virtual host.</para>
    /// </summary>
    /// <param name="virtualHost">
    /// <para>zh-cn:Ocelot 下游配置中的虚拟主机名。</para>
    /// <para>en-us:The virtual host name in Ocelot downstream configuration.</para>
    /// </param>
    /// <param name="route">
    /// <para>zh-cn:找到时返回的路由。</para>
    /// <para>en-us:The route returned when found.</para>
    /// </param>
    /// <returns>
    /// <para>zh-cn:找到已启用模块的匹配路由时返回 true。</para>
    /// <para>en-us:True when a matching route is found while the module is enabled.</para>
    /// </returns>
    bool TryResolve(string virtualHost, out ReverseTunnelRouteOptions? route);
}
