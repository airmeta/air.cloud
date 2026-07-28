using Air.Cloud.DynamicApp.Contracts.Identifiers;
using Air.Cloud.DynamicApp.Contracts.Runtime;

using Microsoft.AspNetCore.Http;

namespace Air.Cloud.DynamicApp.Gateway;

/// <summary>
/// <para>zh-cn:DynamicApp Gateway 转发中间件，将 /mods/{modId}/{path} 请求转发到当前 Active/Healthy Mod。</para>
/// <para>en-us:DynamicApp Gateway forwarding middleware that proxies /mods/{modId}/{path} requests to the current Active/Healthy Mod.</para>
/// </summary>
public sealed class DynamicAppGatewayMiddleware
{
    private static readonly HashSet<string> HopByHopHeaders = new(StringComparer.OrdinalIgnoreCase)
    {
        "connection",
        "keep-alive",
        "proxy-authenticate",
        "proxy-authorization",
        "te",
        "trailer",
        "transfer-encoding",
        "upgrade"
    };

    private readonly IDynamicAppRouteCatalog _catalog;
    private readonly HttpClient _httpClient;
    private readonly RequestDelegate _next;
    private readonly DynamicAppGatewayOptions _options;

    /// <summary>
    /// <para>zh-cn:创建 DynamicApp Gateway 中间件。</para>
    /// <para>en-us:Creates the DynamicApp Gateway middleware.</para>
    /// </summary>
    /// <param name="next">
    /// <para>zh-cn:下一个中间件。</para>
    /// <para>en-us:The next middleware.</para>
    /// </param>
    /// <param name="catalog">
    /// <para>zh-cn:活动路由目录。</para>
    /// <para>en-us:The active route catalog.</para>
    /// </param>
    /// <param name="httpClientFactory">
    /// <para>zh-cn:用于创建转发请求客户端的工厂。</para>
    /// <para>en-us:The factory used to create the forwarding HTTP client.</para>
    /// </param>
    /// <param name="options">
    /// <para>zh-cn:Gateway 转发选项。</para>
    /// <para>en-us:The gateway forwarding options.</para>
    /// </param>
    public DynamicAppGatewayMiddleware(RequestDelegate next, IDynamicAppRouteCatalog catalog, IHttpClientFactory httpClientFactory, DynamicAppGatewayOptions options)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
        _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
        _httpClient = (httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory))).CreateClient(nameof(DynamicAppGatewayMiddleware));
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    /// <summary>
    /// <para>zh-cn:处理当前 HTTP 请求；非 DynamicApp 路由会继续交给后续中间件。</para>
    /// <para>en-us:Handles the current HTTP request; non-DynamicApp routes continue to the next middleware.</para>
    /// </summary>
    /// <param name="context">
    /// <para>zh-cn:当前 HTTP 上下文。</para>
    /// <para>en-us:The current HTTP context.</para>
    /// </param>
    /// <returns>
    /// <para>zh-cn:异步处理任务。</para>
    /// <para>en-us:The asynchronous handling task.</para>
    /// </returns>
    public async Task InvokeAsync(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (!TryCreateRouteRequest(context, out var request))
        {
            await _next(context).ConfigureAwait(false);
            return;
        }

        if (_options.RejectUpgradeRequests && context.WebSockets.IsWebSocketRequest)
        {
            context.Response.StatusCode = StatusCodes.Status426UpgradeRequired;
            await WriteGatewayFailureAsync(context, "DynamicApp Gateway does not proxy WebSocket requests.", "dynamicapp.gateway.websocket_not_supported").ConfigureAwait(false);
            return;
        }

        if (_options.MaxRequestBodyBytes > 0 && context.Request.ContentLength > _options.MaxRequestBodyBytes)
        {
            context.Response.StatusCode = StatusCodes.Status413PayloadTooLarge;
            await WriteGatewayFailureAsync(context, "DynamicApp Gateway request body is too large.", "dynamicapp.gateway.request_too_large").ConfigureAwait(false);
            return;
        }

        var target = await _catalog.ResolveAsync(request, context.RequestAborted).ConfigureAwait(false);
        if (target == null)
        {
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            await WriteGatewayFailureAsync(context, "No active DynamicApp Mod target was found.", "dynamicapp.gateway.target_not_found").ConfigureAwait(false);
            return;
        }

        using var timeout = new CancellationTokenSource(NormalizeTimeout(_options.ForwardTimeout));
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(context.RequestAborted, timeout.Token);
        try
        {
            var upstreamUri = BuildRequestUri(target, context.Request.Path, context.Request.QueryString);
            using var upstreamRequest = CreateUpstreamRequest(context, upstreamUri);
            using var upstreamResponse = await _httpClient.SendAsync(upstreamRequest, HttpCompletionOption.ResponseHeadersRead, linked.Token).ConfigureAwait(false);
            await CopyResponseAsync(context, upstreamResponse).ConfigureAwait(false);
        }
        catch (TaskCanceledException) when (!context.RequestAborted.IsCancellationRequested)
        {
            context.Response.StatusCode = StatusCodes.Status504GatewayTimeout;
            await WriteGatewayFailureAsync(context, "DynamicApp upstream request timed out.", "dynamicapp.gateway.timeout").ConfigureAwait(false);
        }
        catch (HttpRequestException exception)
        {
            context.Response.StatusCode = StatusCodes.Status502BadGateway;
            await WriteGatewayFailureAsync(context, exception.Message, "dynamicapp.gateway.upstream_error").ConfigureAwait(false);
        }
    }

    private bool TryCreateRouteRequest(HttpContext context, out DynamicAppRouteRequest request)
    {
        request = null!;
        if (string.IsNullOrWhiteSpace(_options.DynamicAppId))
        {
            return false;
        }

        var routePrefix = NormalizePrefix(_options.RoutePrefix);
        var path = context.Request.Path.Value ?? string.Empty;
        if (!path.StartsWith(routePrefix + "/", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var remaining = path[routePrefix.Length..].Trim('/');
        var modSegment = remaining.Split('/', 2, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
        if (string.IsNullOrWhiteSpace(modSegment))
        {
            return false;
        }

        request = new DynamicAppRouteRequest
        {
            DynamicAppId = new DynamicAppId(_options.DynamicAppId),
            ModId = new DynamicModId(modSegment),
            Path = path
        };
        return true;
    }

    private Uri BuildRequestUri(DynamicAppRouteTarget target, PathString path, QueryString query)
    {
        var upstream = DynamicAppRouteTargetBuilder.BuildUpstreamUri(target, path.Value ?? "/");
        if (!query.HasValue)
        {
            return upstream;
        }

        var builder = new UriBuilder(upstream)
        {
            Query = query.Value!.TrimStart('?')
        };
        return builder.Uri;
    }

    private HttpRequestMessage CreateUpstreamRequest(HttpContext context, Uri upstreamUri)
    {
        var request = new HttpRequestMessage(new HttpMethod(context.Request.Method), upstreamUri);
        foreach (var header in context.Request.Headers)
        {
            if (ShouldSkipRequestHeader(header.Key))
            {
                continue;
            }

            if (!request.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray()))
            {
                request.Content ??= new StreamContent(context.Request.Body);
                request.Content.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray());
            }
        }

        if (HasBody(context.Request.Method))
        {
            request.Content ??= new StreamContent(context.Request.Body);
        }

        request.Headers.TryAddWithoutValidation("X-Forwarded-Host", context.Request.Host.Value);
        request.Headers.TryAddWithoutValidation("X-Forwarded-Proto", context.Request.Scheme);
        request.Headers.TryAddWithoutValidation("X-Forwarded-Path", context.Request.Path.Value);
        return request;
    }

    private bool ShouldSkipRequestHeader(string header)
    {
        return HopByHopHeaders.Contains(header)
            || (!_options.ForwardHostHeader && string.Equals(header, "Host", StringComparison.OrdinalIgnoreCase));
    }

    private static async Task CopyResponseAsync(HttpContext context, HttpResponseMessage upstreamResponse)
    {
        context.Response.StatusCode = (int)upstreamResponse.StatusCode;
        foreach (var header in upstreamResponse.Headers)
        {
            if (!HopByHopHeaders.Contains(header.Key))
            {
                context.Response.Headers[header.Key] = header.Value.ToArray();
            }
        }

        foreach (var header in upstreamResponse.Content.Headers)
        {
            if (!HopByHopHeaders.Contains(header.Key))
            {
                context.Response.Headers[header.Key] = header.Value.ToArray();
            }
        }

        context.Response.Headers.Remove("transfer-encoding");
        await upstreamResponse.Content.CopyToAsync(context.Response.Body, context.RequestAborted).ConfigureAwait(false);
    }

    private static bool HasBody(string method)
    {
        return HttpMethods.IsPost(method)
            || HttpMethods.IsPut(method)
            || HttpMethods.IsPatch(method)
            || HttpMethods.IsDelete(method);
    }

    private static Task WriteGatewayFailureAsync(HttpContext context, string message, string errorCode)
    {
        context.Response.ContentType = "application/json; charset=utf-8";
        return context.Response.WriteAsJsonAsync(DynamicAppApiResponse<object>.Failure(message, errorCode), context.RequestAborted);
    }

    private static TimeSpan NormalizeTimeout(TimeSpan timeout) => timeout <= TimeSpan.Zero ? TimeSpan.FromSeconds(100) : timeout;

    private static string NormalizePrefix(string prefix)
    {
        if (string.IsNullOrWhiteSpace(prefix))
        {
            return "/mods";
        }

        return "/" + prefix.Trim('/');
    }
}
