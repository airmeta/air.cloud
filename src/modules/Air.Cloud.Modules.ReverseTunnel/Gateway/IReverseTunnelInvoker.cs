using Air.Cloud.Modules.ReverseTunnel.Common.Options;

namespace Air.Cloud.Modules.ReverseTunnel.Gateway;

/// <summary>
/// <para>zh-cn:Gateway 侧反向隧道调用器。它把 Ocelot 传入的 HTTP 请求转换为隧道协议帧，并等待已连接 Agent 返回 HTTP 响应。</para>
/// <para>en-us:Gateway-side reverse tunnel invoker. It converts HTTP requests passed by Ocelot into tunnel protocol frames and waits for a connected Agent to return an HTTP response.</para>
/// </summary>
public interface IReverseTunnelInvoker
{
    /// <summary>
    /// <para>zh-cn:通过指定隧道路由调用已连接 Agent。没有可用 Agent、超时或 Agent 返回错误时，会返回 HTTP 错误响应而不是继续访问普通下游地址。</para>
    /// <para>en-us:Invokes a connected Agent through the specified tunnel route. When no Agent is available, the call times out, or the Agent returns an error, an HTTP error response is returned instead of continuing to an ordinary downstream address.</para>
    /// </summary>
    /// <param name="route">
    /// <para>zh-cn:由 Ocelot 虚拟下游主机解析出的隧道路由。</para>
    /// <para>en-us:Tunnel route resolved from the Ocelot virtual downstream host.</para>
    /// </param>
    /// <param name="request">
    /// <para>zh-cn:Ocelot 当前处理的 HTTP 请求。</para>
    /// <para>en-us:HTTP request currently handled by Ocelot.</para>
    /// </param>
    /// <param name="cancellationToken">
    /// <para>zh-cn:取消本次隧道调用的令牌。</para>
    /// <para>en-us:Token used to cancel the tunnel invocation.</para>
    /// </param>
    /// <returns>
    /// <para>zh-cn:可直接返回给 Ocelot 的 HTTP 响应。</para>
    /// <para>en-us:HTTP response that can be returned directly to Ocelot.</para>
    /// </returns>
    Task<HttpResponseMessage> InvokeAsync(ReverseTunnelRouteOptions route, HttpRequestMessage request, CancellationToken cancellationToken = default);
}
