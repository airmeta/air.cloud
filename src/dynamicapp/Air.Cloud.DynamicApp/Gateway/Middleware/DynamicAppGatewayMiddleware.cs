using Air.Cloud.Core;
using Air.Cloud.Core.Modules.AppPrint;
using Air.Cloud.DynamicApp.Contracts.Runtime;

using Microsoft.AspNetCore.Http;

using System.Diagnostics;
using System.Net;

namespace Air.Cloud.DynamicApp.Gateway;

/// <summary>
/// <para>zh-cn:DynamicApp 动态 Gateway 转发中间件，按独立路由规则选择目标并重写下游路径。</para>
/// <para>en-us:DynamicApp dynamic Gateway forwarding middleware that selects targets and rewrites downstream paths through independent route rules.</para>
/// </summary>
public sealed class DynamicAppGatewayMiddleware
{
    private static readonly HashSet<string> HopByHopHeaders = new(StringComparer.OrdinalIgnoreCase)
    {
        "connection", "keep-alive", "proxy-authenticate", "proxy-authorization", "te", "trailer", "transfer-encoding", "upgrade"
    };

    private readonly IDynamicAppRouteCatalog _catalog;
    private readonly HttpClient _httpClient;
    private readonly RequestDelegate _next;
    private readonly DynamicAppGatewayOptions _options;

    /// <summary>
    /// <para>zh-cn:创建动态 Gateway 中间件。</para>
    /// <para>en-us:Creates the dynamic Gateway middleware.</para>
    /// </summary>
    public DynamicAppGatewayMiddleware(RequestDelegate next, IDynamicAppRouteCatalog catalog, IHttpClientFactory httpClientFactory, DynamicAppGatewayOptions options)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
        _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
        _httpClient = (httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory))).CreateClient(nameof(DynamicAppGatewayMiddleware));
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    /// <summary>
    /// <para>zh-cn:处理请求；已由 ASP.NET Endpoint 或前置控制面中间件处理的请求继续传递，其余请求必须命中动态路由。</para>
    /// <para>en-us:Handles requests; endpoint and control-plane requests already handled earlier continue, while all remaining requests must match a dynamic route.</para>
    /// </summary>
    public async Task InvokeAsync(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (context.GetEndpoint() != null)
        {
            await _next(context).ConfigureAwait(false);
            return;
        }

        var request = new DynamicAppRouteRequest
        {
            HttpMethod = context.Request.Method,
            Path = context.Request.Path.Value ?? "/"
        };
        var startedAt = Stopwatch.GetTimestamp();

        if (_options.RejectUpgradeRequests && context.WebSockets.IsWebSocketRequest)
        {
            await RejectAsync(context, "DynamicApp Gateway does not proxy WebSocket requests.", "dynamicapp.gateway.websocket_not_supported", StatusCodes.Status426UpgradeRequired, request, null, startedAt).ConfigureAwait(false);
            return;
        }

        if (_options.MaxRequestBodyBytes > 0 && context.Request.ContentLength > _options.MaxRequestBodyBytes)
        {
            await RejectAsync(context, "DynamicApp Gateway request body is too large.", "dynamicapp.gateway.request_too_large", StatusCodes.Status413PayloadTooLarge, request, null, startedAt).ConfigureAwait(false);
            return;
        }

        var resolution = await _catalog.ResolveAsync(request, context.RequestAborted).ConfigureAwait(false);
        if (!resolution.Succeeded || resolution.Match == null || resolution.Target == null)
        {
            var statusCode = resolution.ErrorCode == "dynamicapp.target_unavailable"
                || resolution.ErrorCode == "dynamicapp.specified_target_unavailable"
                ? StatusCodes.Status503ServiceUnavailable
                : resolution.ErrorCode == "dynamicapp.route_not_found"
                    ? StatusCodes.Status404NotFound
                    : StatusCodes.Status404NotFound;
            await RejectAsync(context, resolution.Message, resolution.ErrorCode, statusCode, request, resolution.Match, startedAt).ConfigureAwait(false);
            return;
        }

        using var timeout = new CancellationTokenSource(NormalizeTimeout(_options.ForwardTimeout));
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(context.RequestAborted, timeout.Token);
        try
        {
            var upstreamUri = BuildRequestUri(resolution.Match, resolution.Target, context.Request.QueryString);
            using var upstreamRequest = CreateUpstreamRequest(context, upstreamUri);
            using var upstreamResponse = await _httpClient.SendAsync(upstreamRequest, HttpCompletionOption.ResponseHeadersRead, linked.Token).ConfigureAwait(false);
            await CopyResponseAsync(context, upstreamResponse).ConfigureAwait(false);
            LogForwarded(request, resolution, context, upstreamResponse.StatusCode, startedAt);
        }
        catch (TaskCanceledException) when (!context.RequestAborted.IsCancellationRequested)
        {
            context.Response.StatusCode = StatusCodes.Status504GatewayTimeout;
            await WriteGatewayFailureAsync(context, "DynamicApp upstream request timed out.", "dynamicapp.gateway.timeout").ConfigureAwait(false);
            LogFailure(request, resolution, context, StatusCodes.Status504GatewayTimeout, "timeout", startedAt);
        }
        catch (HttpRequestException exception)
        {
            context.Response.StatusCode = StatusCodes.Status502BadGateway;
            await WriteGatewayFailureAsync(context, exception.Message, "dynamicapp.gateway.upstream_error").ConfigureAwait(false);
            LogFailure(request, resolution, context, StatusCodes.Status502BadGateway, "upstream_error", startedAt, exception);
        }
    }

    private async Task RejectAsync(HttpContext context, string message, string errorCode, int statusCode, DynamicAppRouteRequest request, DynamicAppGatewayRouteMatch? match, long startedAt)
    {
        context.Response.StatusCode = statusCode;
        await WriteGatewayFailureAsync(context, message, errorCode).ConfigureAwait(false);
        LogRejected(request, match, context, errorCode, startedAt);
    }

    private void LogForwarded(DynamicAppRouteRequest request, DynamicAppGatewayRouteResolution resolution, HttpContext context, HttpStatusCode statusCode, long startedAt)
    {
        if (!_options.EnableAccessLogging || resolution.Match == null || resolution.Target == null) return;
        PrintAccess("forwarded", AppPrintLevel.Information, $"DynamicApp Gateway forwarded {request.HttpMethod} {request.Path} through route `{resolution.Match.Route.RouteId}`.", new Dictionary<string, object>
        {
            ["Method"] = request.HttpMethod, ["Path"] = request.Path, ["RouteId"] = resolution.Match.Route.RouteId,
            ["TargetKey"] = resolution.Target.Key.ToString(), ["DeploymentId"] = resolution.Target.DeploymentId,
            ["StatusCode"] = (int)statusCode, ["ElapsedMilliseconds"] = GetElapsedMilliseconds(startedAt)
        });
    }

    private void LogRejected(DynamicAppRouteRequest request, DynamicAppGatewayRouteMatch? match, HttpContext context, string reason, long startedAt)
    {
        if (!_options.EnableAccessLogging) return;
        var parameters = new Dictionary<string, object>
        {
            ["Method"] = request.HttpMethod, ["Path"] = request.Path, ["StatusCode"] = context.Response.StatusCode,
            ["Reason"] = reason, ["ElapsedMilliseconds"] = GetElapsedMilliseconds(startedAt)
        };
        if (match != null) parameters["RouteId"] = match.Route.RouteId;
        PrintAccess("rejected", AppPrintLevel.Warn, $"DynamicApp Gateway rejected {request.HttpMethod} {request.Path}.", parameters);
    }

    private void LogFailure(DynamicAppRouteRequest request, DynamicAppGatewayRouteResolution resolution, HttpContext context, int statusCode, string reason, long startedAt, Exception? exception = null)
    {
        if (!_options.EnableAccessLogging || resolution.Match == null || resolution.Target == null) return;
        var parameters = new Dictionary<string, object>
        {
            ["Method"] = request.HttpMethod, ["Path"] = request.Path, ["RouteId"] = resolution.Match.Route.RouteId,
            ["TargetKey"] = resolution.Target.Key.ToString(), ["DeploymentId"] = resolution.Target.DeploymentId,
            ["StatusCode"] = statusCode, ["Reason"] = reason, ["ElapsedMilliseconds"] = GetElapsedMilliseconds(startedAt)
        };
        if (exception != null) { parameters["ExceptionType"] = exception.GetType().FullName ?? exception.GetType().Name; parameters["ExceptionMessage"] = exception.Message; }
        PrintAccess("failed", AppPrintLevel.Error, $"DynamicApp Gateway failed {request.HttpMethod} {request.Path} through route `{resolution.Match.Route.RouteId}`.", parameters);
    }

    private static double GetElapsedMilliseconds(long startedAt) => Math.Round(Stopwatch.GetElapsedTime(startedAt).TotalMilliseconds, 2);

    private static void PrintAccess(string operation, AppPrintLevel level, string message, Dictionary<string, object> parameters)
    {
        parameters["Operation"] = operation;
        AppRealization.Output.Print("DynamicApp Gateway Access", message, level, AdditionalParams: parameters);
    }

    private static Uri BuildRequestUri(DynamicAppGatewayRouteMatch match, DynamicAppRouteTarget target, QueryString query)
    {
        var path = match.BuildDownstreamPath();
        var uri = new Uri(target.BaseAddress, path);
        if (!query.HasValue) return uri;
        var builder = new UriBuilder(uri) { Query = query.Value!.TrimStart('?') };
        return builder.Uri;
    }

    private HttpRequestMessage CreateUpstreamRequest(HttpContext context, Uri upstreamUri)
    {
        var request = new HttpRequestMessage(new HttpMethod(context.Request.Method), upstreamUri);
        foreach (var header in context.Request.Headers)
        {
            if (HopByHopHeaders.Contains(header.Key) || (!_options.ForwardHostHeader && string.Equals(header.Key, "Host", StringComparison.OrdinalIgnoreCase))) continue;
            if (!request.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray()))
            {
                request.Content ??= new StreamContent(context.Request.Body);
                request.Content.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray());
            }
        }

        if (HasBody(context.Request.Method)) request.Content ??= new StreamContent(context.Request.Body);
        request.Headers.TryAddWithoutValidation("X-Forwarded-Host", context.Request.Host.Value);
        request.Headers.TryAddWithoutValidation("X-Forwarded-Proto", context.Request.Scheme);
        request.Headers.TryAddWithoutValidation("X-Forwarded-Path", context.Request.Path.Value);
        return request;
    }

    private static async Task CopyResponseAsync(HttpContext context, HttpResponseMessage upstreamResponse)
    {
        context.Response.StatusCode = (int)upstreamResponse.StatusCode;
        foreach (var header in upstreamResponse.Headers.Concat(upstreamResponse.Content.Headers))
        {
            if (!HopByHopHeaders.Contains(header.Key)) context.Response.Headers[header.Key] = header.Value.ToArray();
        }
        context.Response.Headers.Remove("transfer-encoding");
        await upstreamResponse.Content.CopyToAsync(context.Response.Body, context.RequestAborted).ConfigureAwait(false);
    }

    private static bool HasBody(string method) => HttpMethods.IsPost(method) || HttpMethods.IsPut(method) || HttpMethods.IsPatch(method) || HttpMethods.IsDelete(method);

    private static Task WriteGatewayFailureAsync(HttpContext context, string message, string errorCode)
    {
        context.Response.ContentType = "application/json; charset=utf-8";
        return context.Response.WriteAsJsonAsync(DynamicAppApiResponse<object>.Failure(message, errorCode), context.RequestAborted);
    }

    private static TimeSpan NormalizeTimeout(TimeSpan timeout) => timeout <= TimeSpan.Zero ? TimeSpan.FromSeconds(100) : timeout;
}
