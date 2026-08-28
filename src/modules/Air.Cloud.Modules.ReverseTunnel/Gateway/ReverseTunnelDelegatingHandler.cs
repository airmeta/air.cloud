using Air.Cloud.Modules.ReverseTunnel.Common.Options;
using Air.Cloud.Modules.ReverseTunnel.Common.Routing;

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace Air.Cloud.Modules.ReverseTunnel.Gateway;

/// <summary>
/// <para>zh-cn:已废弃的 Ocelot 反向隧道处理器。新 Gateway 不再注册或使用该处理器，隧道路由由 `UseReverseTunnelGateway()` 按 Agent 公开路径直接匹配。</para>
/// <para>en-us:Deprecated Ocelot reverse tunnel handler. New Gateways do not register or use this handler; tunnel routes are matched directly by `UseReverseTunnelGateway()` from Agent public paths.</para>
/// </summary>
public sealed class ReverseTunnelDelegatingHandler : DelegatingHandler
{
    private readonly IReverseTunnelRouteProvider routeProvider;
    private readonly IReverseTunnelInvoker invoker;
    private readonly IOptions<ReverseTunnelOptions> options;
    private readonly IHttpContextAccessor? httpContextAccessor;

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
    /// <para>zh-cn:反向隧道配置，用于确认模块和 Gateway 隧道代理是否启用。</para>
    /// <para>en-us:Reverse tunnel options used to confirm whether the module and Gateway tunnel proxy are enabled.</para>
    /// </param>
    /// <param name="httpContextAccessor">
    /// <para>zh-cn:当前 ASP.NET Core 请求上下文访问器。Ocelot 在部分场景下传入的 `HttpRequestMessage.Content` 为空或被构造成长度为 0 的空内容时，处理器会使用它从原始请求恢复请求体和内容头，避免 POST/PUT/PATCH 在进入隧道或回落到普通下游链路时丢失 `Content-Type`。</para>
    /// <para>en-us:Accessor for the current ASP.NET Core request context. When Ocelot provides a missing `HttpRequestMessage.Content` or an empty zero-length content in some scenarios, the handler uses it to restore the original request body and content headers so POST/PUT/PATCH requests do not lose `Content-Type` either when entering the tunnel or when falling back to the ordinary downstream chain.</para>
    /// </param>
    public ReverseTunnelDelegatingHandler(IReverseTunnelRouteProvider routeProvider, IReverseTunnelInvoker invoker, IOptions<ReverseTunnelOptions> options, IHttpContextAccessor? httpContextAccessor = null)
    {
        this.routeProvider = routeProvider;
        this.invoker = invoker;
        this.options = options;
        this.httpContextAccessor = httpContextAccessor;
    }

    /// <summary>
    /// <para>zh-cn:处理 Ocelot 当前请求。方法会先从当前 ASP.NET Core 请求恢复 Ocelot 可能丢失的请求体和内容头，再判断是否进入隧道；模块未启用、EnableTunnelProxy 未启用或虚拟主机未命中隧道路由时，会继续执行 Ocelot 原有下游处理链。</para>
    /// <para>en-us:Handles the current Ocelot request. The method first restores request content and content headers that Ocelot may have omitted from the current ASP.NET Core request, then decides whether to enter the tunnel; when the module is disabled, EnableTunnelProxy is disabled, or the virtual host does not match a tunnel route, the original Ocelot downstream handler chain continues.</para>
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
        RestoreHttpContextContentIfRequired(request);

        var host = request.RequestUri?.Host;
        if (!options.Value.Enabled ||
            !options.Value.EnableTunnelProxy ||
            string.IsNullOrWhiteSpace(host) ||
            !routeProvider.TryResolve(host, out var route) ||
            route is null)
        {
            return base.SendAsync(request, cancellationToken);
        }

        return invoker.InvokeAsync(route, request, cancellationToken);
    }

    private void RestoreHttpContextContentIfRequired(HttpRequestMessage request)
    {
        var httpRequest = httpContextAccessor?.HttpContext?.Request;
        if (httpRequest is null)
        {
            return;
        }

        if (ShouldReplaceContent(request, httpRequest))
        {
            if (httpRequest.Body.CanSeek)
            {
                httpRequest.Body.Position = 0;
            }

            request.Content?.Dispose();
            request.Content = new StreamContent(httpRequest.Body);
            if (httpRequest.ContentLength.HasValue)
            {
                request.Content.Headers.ContentLength = httpRequest.ContentLength.Value;
            }
        }

        if (request.Content is null)
        {
            return;
        }

        CopyContentHeaderIfPresent(httpRequest, request, "Content-Type");
        CopyContentHeaderIfPresent(httpRequest, request, "Content-Encoding");
        CopyContentHeaderIfPresent(httpRequest, request, "Content-Language");
        CopyContentHeaderIfPresent(httpRequest, request, "Content-Location");
        CopyContentHeaderIfPresent(httpRequest, request, "Content-MD5");
        CopyContentHeaderIfPresent(httpRequest, request, "Content-Range");
    }

    private static bool ShouldReplaceContent(HttpRequestMessage request, HttpRequest httpRequest)
    {
        if (!ShouldRestoreContent(httpRequest))
        {
            return false;
        }

        if (request.Content is null)
        {
            return true;
        }

        if (httpRequest.ContentLength is > 0 &&
            request.Content.Headers.ContentLength != httpRequest.ContentLength)
        {
            return true;
        }

        return !string.IsNullOrWhiteSpace(httpRequest.ContentType) &&
            request.Content.Headers.ContentType is null;
    }

    private static bool ShouldRestoreContent(HttpRequest httpRequest)
    {
        if (httpRequest.ContentLength is > 0)
        {
            return true;
        }

        if (!string.IsNullOrWhiteSpace(httpRequest.ContentType) && AllowsRequestBody(httpRequest.Method))
        {
            return true;
        }

        return httpRequest.Headers.TransferEncoding.Any(value => string.Equals(value, "chunked", StringComparison.OrdinalIgnoreCase)) &&
            AllowsRequestBody(httpRequest.Method);
    }

    private static bool AllowsRequestBody(string method)
    {
        return !HttpMethods.IsGet(method) && !HttpMethods.IsHead(method) && !HttpMethods.IsTrace(method);
    }

    private static void CopyContentHeaderIfPresent(HttpRequest httpRequest, HttpRequestMessage request, string headerName)
    {
        if (!httpRequest.Headers.TryGetValue(headerName, out var values) || values.Count == 0)
        {
            return;
        }

        request.Content?.Headers.Remove(headerName);
        request.Content?.Headers.TryAddWithoutValidation(headerName, values.AsEnumerable());
    }
}
