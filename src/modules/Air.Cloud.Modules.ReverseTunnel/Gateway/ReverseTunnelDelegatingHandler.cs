using Air.Cloud.Modules.ReverseTunnel.Common.Options;
using Air.Cloud.Modules.ReverseTunnel.Common.Routing;

using Microsoft.Extensions.Options;

namespace Air.Cloud.Modules.ReverseTunnel.Gateway;

/// <summary>
/// <para>zh-cn:Ocelot opt-in 反向隧道处理器。只有 Ocelot 路由显式声明该 DelegatingHandler 且虚拟下游主机能解析到隧道路由时，才会把请求交给隧道调用器。</para>
/// <para>en-us:Ocelot opt-in reverse tunnel handler. A request is handed to the tunnel invoker only when the Ocelot route explicitly declares this DelegatingHandler and the virtual downstream host resolves to a tunnel route.</para>
/// </summary>
public sealed class ReverseTunnelDelegatingHandler : DelegatingHandler
{
    private readonly IReverseTunnelRouteProvider routeProvider;
    private readonly IReverseTunnelInvoker invoker;
    private readonly IOptions<ReverseTunnelOptions> options;

    /// <summary>
    /// <para>zh-cn:创建 Ocelot 反向隧道处理器。</para>
    /// <para>en-us:Creates an Ocelot reverse tunnel handler.</para>
    /// </summary>
    /// <param name="routeProvider">
    /// <para>zh-cn:根据 Ocelot 下游虚拟主机解析隧道路由的提供器。</para>
    /// <para>en-us:Provider that resolves tunnel routes from Ocelot downstream virtual hosts.</para>
    /// </param>
    /// <param name="invoker">
    /// <para>zh-cn:把 HTTP 请求发送给已连接 Agent 的隧道调用器。</para>
    /// <para>en-us:Tunnel invoker that sends HTTP requests to connected Agents.</para>
    /// </param>
    /// <param name="options">
    /// <para>zh-cn:反向隧道配置，用于确认模块和 Gateway 角色是否启用。</para>
    /// <para>en-us:Reverse tunnel options used to confirm whether the module and Gateway role are enabled.</para>
    /// </param>
    public ReverseTunnelDelegatingHandler(IReverseTunnelRouteProvider routeProvider, IReverseTunnelInvoker invoker, IOptions<ReverseTunnelOptions> options)
    {
        this.routeProvider = routeProvider;
        this.invoker = invoker;
        this.options = options;
    }

    /// <summary>
    /// <para>zh-cn:处理 Ocelot 当前请求。模块未启用、Gateway 角色未启用或虚拟主机未命中隧道路由时，会继续执行 Ocelot 原有下游处理链。</para>
    /// <para>en-us:Handles the current Ocelot request. When the module is disabled, the Gateway role is disabled, or the virtual host does not match a tunnel route, the original Ocelot downstream handler chain continues.</para>
    /// </summary>
    /// <param name="request">
    /// <para>zh-cn:Ocelot 当前下游请求。</para>
    /// <para>en-us:Current Ocelot downstream request.</para>
    /// </param>
    /// <param name="cancellationToken">
    /// <para>zh-cn:取消请求处理的令牌。</para>
    /// <para>en-us:Token used to cancel request handling.</para>
    /// </param>
    /// <returns>
    /// <para>zh-cn:隧道响应或 Ocelot 原始下游链路响应。</para>
    /// <para>en-us:Either a tunnel response or the response from the original Ocelot downstream chain.</para>
    /// </returns>
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var host = request.RequestUri?.Host;
        if (!options.Value.Enabled ||
            !options.Value.Roles.HasFlag(ReverseTunnelRole.Gateway) ||
            string.IsNullOrWhiteSpace(host) ||
            !routeProvider.TryResolve(host, out var route) ||
            route is null)
        {
            return base.SendAsync(request, cancellationToken);
        }

        return invoker.InvokeAsync(route, request, cancellationToken);
    }
}
