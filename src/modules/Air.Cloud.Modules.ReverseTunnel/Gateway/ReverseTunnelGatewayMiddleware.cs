using Air.Cloud.Modules.ReverseTunnel.Common.Http;

using Microsoft.AspNetCore.Http;

using System.Net;

namespace Air.Cloud.Modules.ReverseTunnel.Gateway;

/// <summary>
/// <para>zh-cn:在 Ocelot 之前执行的 AirTunnel Gateway HTTP 中间件。它按公开路径匹配 Agent 服务、剥离公开前缀并经由现有调用器转发请求；未命中时不改变后续管道行为。</para>
/// <para>en-us:AirTunnel Gateway HTTP middleware that runs before Ocelot. It matches Agent services by public path, removes the public prefix, and forwards through the existing invoker; unmatched requests preserve downstream pipeline behavior.</para>
/// </summary>
public sealed class ReverseTunnelGatewayMiddleware
{
    private readonly RequestDelegate next;

    /// <summary>
    /// <para>zh-cn:创建 Gateway HTTP 中间件。</para>
    /// <para>en-us:Creates the Gateway HTTP middleware.</para>
    /// </summary>
    /// <param name="next"><para>zh-cn:未命中隧道路由时要继续执行的后续中间件。</para><para>en-us:Next middleware to execute when no tunnel route matches.</para></param>
    public ReverseTunnelGatewayMiddleware(RequestDelegate next)
    {
        this.next = next;
    }

    /// <summary>
    /// <para>zh-cn:处理当前 HTTP 请求。已命中时将响应状态、头和内容写回当前响应；调用器返回错误响应时同样透传，只有未命中路由才继续后续中间件。</para>
    /// <para>en-us:Processes the current HTTP request. For a match, writes response status, headers, and content to the current response; invoker error responses are also passed through, and only unmatched routes continue to the next middleware.</para>
    /// </summary>
    /// <param name="context"><para>zh-cn:当前 HTTP 上下文。</para><para>en-us:Current HTTP context.</para></param>
    /// <param name="routeMatcher"><para>zh-cn:公开路径路由匹配器。</para><para>en-us:Public-path route matcher.</para></param>
    /// <param name="invoker"><para>zh-cn:已连接 Agent 隧道调用器。</para><para>en-us:Connected-Agent tunnel invoker.</para></param>
    /// <returns><para>zh-cn:请求处理任务。</para><para>en-us:Request-processing task.</para></returns>
    public async Task InvokeAsync(
        HttpContext context,
        IReverseTunnelGatewayRouteMatcher routeMatcher,
        IReverseTunnelInvoker invoker)
    {
        if (!routeMatcher.TryMatch(context.Request.Path, out var match) || match is null)
        {
            await next(context);
            return;
        }

        using var request = CreateRequest(context, match.AgentPath);
        using var response = await invoker.InvokeAsync(match.Route, request, context.RequestAborted);
        await WriteResponseAsync(context, response);
    }

    private static HttpRequestMessage CreateRequest(HttpContext context, PathString agentPath)
    {
        var path = string.IsNullOrEmpty(agentPath.Value) ? "/" : agentPath.Value!;
        // Set Path and Query independently. Concatenating a PathString with a
        // query can cause the '?' to be escaped as %3F by Uri, changing the
        // downstream request semantics for rewritten routes.
        var uriBuilder = new UriBuilder("http", "reverse-tunnel.internal")
        {
            Path = path,
            Query = context.Request.QueryString.HasValue
                ? context.Request.QueryString.Value![1..]
                : string.Empty
        };
        var uri = uriBuilder.Uri;
        var request = new HttpRequestMessage(new HttpMethod(context.Request.Method), uri);
        if (HasBody(context.Request))
        {
            request.Content = new StreamContent(context.Request.Body);
        }

        foreach (var header in context.Request.Headers)
        {
            if (ReverseTunnelHttpHeaders.HopByHopHeaders.Contains(header.Key))
            {
                continue;
            }

            if (!request.Headers.TryAddWithoutValidation(header.Key, header.Value.AsEnumerable()))
            {
                request.Content ??= new ByteArrayContent([]);
                request.Content.Headers.TryAddWithoutValidation(header.Key, header.Value.AsEnumerable());
            }
        }

        return request;
    }

    private static async Task WriteResponseAsync(HttpContext context, HttpResponseMessage response)
    {
        context.Response.StatusCode = (int)response.StatusCode;
        CopyHeaders(context.Response, response.Headers);
        if (response.Content is null)
        {
            return;
        }

        CopyHeaders(context.Response, response.Content.Headers);
        await response.Content.CopyToAsync(context.Response.Body, context.RequestAborted);
    }

    private static void CopyHeaders(HttpResponse response, IEnumerable<KeyValuePair<string, IEnumerable<string>>> headers)
    {
        foreach (var header in headers)
        {
            if (ReverseTunnelHttpHeaders.HopByHopHeaders.Contains(header.Key) ||
                string.Equals(header.Key, "Content-Length", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            response.Headers[header.Key] = header.Value.ToArray();
        }
    }

    private static bool HasBody(HttpRequest request)
        => request.ContentLength is > 0 ||
           (!string.IsNullOrWhiteSpace(request.ContentType) &&
            !HttpMethods.IsGet(request.Method) &&
            !HttpMethods.IsHead(request.Method) &&
            !HttpMethods.IsTrace(request.Method)) ||
           request.Headers.TransferEncoding.Any(item => string.Equals(item, "chunked", StringComparison.OrdinalIgnoreCase));
}
