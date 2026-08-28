using Air.Cloud.Modules.ReverseTunnel.Common.Diagnostics;
using Air.Cloud.Modules.ReverseTunnel.Common.Options;
using Air.Cloud.Modules.ReverseTunnel.Common.Http;
using Air.Cloud.Modules.ReverseTunnel.Common.Protocol;
using Air.Cloud.Modules.ReverseTunnel.Common.Runtime;
using Air.Cloud.Modules.ReverseTunnel.Server;

using Microsoft.Extensions.Options;
using Microsoft.Extensions.DependencyInjection;

using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;

namespace Air.Cloud.Modules.ReverseTunnel.Gateway;

/// <summary>
/// <para>zh-cn:默认 Gateway 侧 AirTunnel 调用器。当前进程为 Server 模式时优先使用本进程 Agent 会话注册表；其它模式可按 `Gateway.RemoteServerAddress` 远程调用独立 AirTunnel Server。跨 Server 进程的全局会话注册表不在当前实现范围内。</para>
/// <para>en-us:Default Gateway-side AirTunnel invoker. When the current process uses Server mode it prefers the in-process Agent session registry; other modes can call a standalone AirTunnel Server through `Gateway.RemoteServerAddress`. A global session registry across Server processes is outside the current implementation scope.</para>
/// </summary>
public sealed class ReverseTunnelInvoker : IReverseTunnelInvoker
{
    private readonly IReverseTunnelSessionRegistry sessionRegistry;
    private readonly IReverseTunnelRuntimeOptionsAccessor options;
    private readonly IHttpClientFactory? httpClientFactory;

    /// <summary>
    /// <para>zh-cn:创建 Gateway 侧反向隧道调用器。</para>
    /// <para>en-us:Creates a Gateway-side reverse tunnel invoker.</para>
    /// </summary>
    /// <param name="sessionRegistry">
    /// <para>zh-cn:当前进程内已连接 Agent 会话注册表。</para>
    /// <para>en-us:Registry of connected Agent sessions in the current process.</para>
    /// </param>
    /// <param name="options">
    /// <para>zh-cn:反向隧道配置，用于读取模块开关、角色和请求体限制。</para>
    /// <para>en-us:Reverse tunnel options used to read module switches, roles, and request-body limits.</para>
    /// </param>
    public ReverseTunnelInvoker(IReverseTunnelSessionRegistry sessionRegistry, IOptions<ReverseTunnelOptions> options)
        : this(sessionRegistry, new ReverseTunnelRuntimeOptionsAccessor(options), null)
    {
    }

    /// <summary>
    /// <para>zh-cn:使用运行期配置访问器创建 Gateway 侧反向隧道调用器。</para>
    /// <para>en-us:Creates the Gateway-side reverse tunnel invoker with a runtime-options accessor.</para>
    /// </summary>
    /// <param name="sessionRegistry">
    /// <para>zh-cn:当前进程内已连接 Agent 会话注册表。</para>
    /// <para>en-us:Registry of connected Agent sessions in the current process.</para>
    /// </param>
    /// <param name="options">
    /// <para>zh-cn:运行期 AirTunnel 配置。</para>
    /// <para>en-us:Runtime AirTunnel options.</para>
    /// </param>
    public ReverseTunnelInvoker(IReverseTunnelSessionRegistry sessionRegistry, IReverseTunnelRuntimeOptionsAccessor options)
        : this(sessionRegistry, options, null)
    {
    }

    /// <summary>
    /// <para>zh-cn:创建 Gateway 侧 AirTunnel 调用器。当前进程启用 Server 角色时优先使用进程内会话表；否则会按 `ReverseTunnelSettings:Gateway:RemoteServerAddress` 调用独立 AirTunnel Server。</para>
    /// <para>en-us:Creates a Gateway-side AirTunnel invoker. When the current process uses Server mode it uses the in-process session registry first; otherwise it calls the standalone AirTunnel Server configured by `ReverseTunnelSettings:Gateway:RemoteServerAddress`.</para>
    /// </summary>
    /// <param name="sessionRegistry">
    /// <para>zh-cn:当前进程内已连接 Agent 会话注册表。仅当前进程承载 Server 角色时用于转发请求。</para>
    /// <para>en-us:Registry of connected Agent sessions in the current process. It is used for forwarding only when the current process uses Server mode.</para>
    /// </param>
    /// <param name="options">
    /// <para>zh-cn:AirTunnel 配置，用于读取角色、远程 Server 地址、调用超时和请求/响应大小限制。</para>
    /// <para>en-us:AirTunnel options used to read roles, remote Server address, invocation timeout, and request/response size limits.</para>
    /// </param>
    /// <param name="httpClientFactory">
    /// <para>zh-cn:HTTP 客户端工厂。Gateway 独立进程模式下用于调用 AirTunnel Server；为空时只支持进程内调用。</para>
    /// <para>en-us:HTTP client factory. It is used to call AirTunnel Server in standalone Gateway mode; when null only in-process invocation is supported.</para>
    /// </param>
    public ReverseTunnelInvoker(IReverseTunnelSessionRegistry sessionRegistry, IOptions<ReverseTunnelOptions> options, IHttpClientFactory? httpClientFactory)
        : this(sessionRegistry, new ReverseTunnelRuntimeOptionsAccessor(options), httpClientFactory)
    {
    }

    /// <summary>
    /// <para>zh-cn:创建 Gateway 侧 AirTunnel 调用器。当当前进程启用 Server 角色时优先使用进程内会话表；否则会按运行期配置调用独立 AirTunnel Server。</para>
    /// <para>en-us:Creates a Gateway-side AirTunnel invoker. When the current process uses Server mode it uses the in-process session registry first; otherwise it calls the standalone AirTunnel Server from runtime options.</para>
    /// </summary>
    /// <param name="sessionRegistry">
    /// <para>zh-cn:当前进程内已连接 Agent 会话注册表。仅当前进程承载 Server 角色时用于转发请求。</para>
    /// <para>en-us:Registry of connected Agent sessions in the current process. It is used for forwarding only when the current process uses Server mode.</para>
    /// </param>
    /// <param name="options">
    /// <para>zh-cn:运行期 AirTunnel 配置，用于读取角色、远程 Server 地址、调用超时和请求/响应大小限制。</para>
    /// <para>en-us:Runtime AirTunnel options used to read roles, remote Server address, invocation timeout, and request/response size limits.</para>
    /// </param>
    /// <param name="httpClientFactory">
    /// <para>zh-cn:HTTP 客户端工厂。Gateway 独立进程模式下用于调用 AirTunnel Server；为空时只支持进程内调用。</para>
    /// <para>en-us:HTTP client factory. It is used to call AirTunnel Server in standalone Gateway mode; when null only in-process invocation is supported.</para>
    /// </param>
    [ActivatorUtilitiesConstructor]
    public ReverseTunnelInvoker(IReverseTunnelSessionRegistry sessionRegistry, IReverseTunnelRuntimeOptionsAccessor options, IHttpClientFactory? httpClientFactory)
    {
        this.sessionRegistry = sessionRegistry;
        this.options = options;
        this.httpClientFactory = httpClientFactory;
    }

    /// <inheritdoc />
    public async Task<HttpResponseMessage> InvokeAsync(ReverseTunnelRouteOptions route, HttpRequestMessage request, CancellationToken cancellationToken = default)
    {
        if (route is null)
        {
            throw new ArgumentNullException(nameof(route));
        }

        if (request is null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        var pathAndQuery = request.RequestUri?.PathAndQuery ?? "/";
        using var activity = ReverseTunnelMetrics.StartGatewayInvoke(route.ServiceKey, route.PublicPath, request.Method.Method, pathAndQuery);
        var settings = options.Current;
        if (!settings.Enabled || !settings.EnableTunnelProxy)
        {
            const string code = "reverse_tunnel_gateway_disabled";
            const string message = "Reverse tunnel Gateway proxy is not enabled.";
            ReverseTunnelMetrics.SetGatewayFailure(activity, code, message, (int)HttpStatusCode.ServiceUnavailable);
            return CreateErrorResponse(HttpStatusCode.ServiceUnavailable, code, message);
        }

        if (settings.TunnelMode != ReverseTunnelMode.Server &&
            !string.IsNullOrWhiteSpace(settings.Gateway.RemoteServerAddress))
        {
            return await InvokeRemoteServerAsync(route, request, activity, cancellationToken);
        }

        if (!sessionRegistry.TryGetForServiceKey(route.ServiceKey, out var session) || session is null)
        {
            const string code = "agent_unavailable";
            var message = $"No reverse tunnel Agent session is available for service '{route.ServiceKey}'.";
            ReverseTunnelMetrics.SetGatewayFailure(activity, code, message, (int)HttpStatusCode.ServiceUnavailable);
            return CreateErrorResponse(HttpStatusCode.ServiceUnavailable, code, message);
        }

        ReverseTunnelMetrics.SetGatewayAgent(activity, session.AgentId, session.SessionId);
        var payload = new ReverseTunnelHttpRequestPayload
        {
            ServiceKey = route.ServiceKey,
            Method = request.Method.Method,
            PathAndQuery = pathAndQuery,
            HasBody = request.Content is not null,
            Headers = CollectRequestHeaders(request)
        };
        InjectTraceContext(payload.Headers);

        var result = await session.InvokeAsync(payload, request.Content, TimeSpan.FromSeconds(route.TimeoutSeconds), cancellationToken);
        if (!result.Succeeded || result.Response is null)
        {
            var statusCode = ResolveFailureStatusCode(result.ErrorCode);
            var errorCode = result.ErrorCode ?? "agent_error";
            var errorMessage = result.ErrorMessage ?? "Reverse tunnel Agent returned an error.";
            ReverseTunnelMetrics.SetGatewayFailure(activity, errorCode, errorMessage, (int)statusCode);
            return CreateErrorResponse(statusCode, errorCode, errorMessage);
        }

        ReverseTunnelMetrics.SetGatewaySuccess(activity, result.Response.StatusCode);
        return CreateSuccessResponse(result.Response, result);
    }

    private static HttpStatusCode ResolveFailureStatusCode(string? errorCode)
    {
        return errorCode switch
        {
            "agent_unavailable" => HttpStatusCode.ServiceUnavailable,
            "reverse_tunnel_gateway_disabled" => HttpStatusCode.ServiceUnavailable,
            "timeout" => HttpStatusCode.GatewayTimeout,
            "cancelled" => (HttpStatusCode)499,
            "too_many_concurrent_streams" => HttpStatusCode.TooManyRequests,
            "request_body_too_large" => HttpStatusCode.RequestEntityTooLarge,
            "response_body_too_large" => HttpStatusCode.BadGateway,
            "session_closed" => HttpStatusCode.ServiceUnavailable,
            "local_dispatch_failed" => HttpStatusCode.BadGateway,
            "invalid_agent_response" => HttpStatusCode.BadGateway,
            "gateway_stream_failed" => HttpStatusCode.BadGateway,
            _ => HttpStatusCode.BadGateway
        };
    }

    private static List<ReverseTunnelHttpHeader> CollectRequestHeaders(HttpRequestMessage request)
    {
        var headers = new List<ReverseTunnelHttpHeader>();
        foreach (var header in request.Headers)
        {
            AddHeaders(headers, header.Key, header.Value);
        }

        if (request.Content is not null)
        {
            foreach (var header in request.Content.Headers)
            {
                AddHeaders(headers, header.Key, header.Value);
            }
        }

        return headers;
    }

    private static HttpResponseMessage CreateSuccessResponse(ReverseTunnelHttpResponsePayload payload, ReverseTunnelHttpExchangeResult result)
    {
        var response = new HttpResponseMessage((HttpStatusCode)Math.Clamp(payload.StatusCode, 100, 599))
        {
            ReasonPhrase = string.IsNullOrWhiteSpace(payload.ReasonPhrase) ? null : payload.ReasonPhrase
        };

        if (result.BodyReader is not null)
        {
            response.Content = new ReverseTunnelChannelReaderHttpContent(result.BodyReader, result.CancelBodyAsync);
        }
        else if (result.Body is { Length: > 0 } || payload.Headers.Any(header => header.Name.StartsWith("Content-", StringComparison.OrdinalIgnoreCase)))
        {
            response.Content = new ByteArrayContent(result.Body ?? Array.Empty<byte>());
        }

        foreach (var header in payload.Headers.Where(item => !string.IsNullOrWhiteSpace(item.Name) && !ReverseTunnelHttpHeaders.HopByHopHeaders.Contains(item.Name)))
        {
            if (!response.Headers.TryAddWithoutValidation(header.Name, header.Value))
            {
                response.Content ??= new ByteArrayContent(Array.Empty<byte>());
                response.Content.Headers.TryAddWithoutValidation(header.Name, header.Value);
            }
        }

        return response;
    }

    private static HttpResponseMessage CreateErrorResponse(HttpStatusCode statusCode, string code, string message)
    {
        return new HttpResponseMessage(statusCode)
        {
            Content = JsonContent.Create(new
            {
                code,
                message
            })
        };
    }

    private static void AddHeaders(ICollection<ReverseTunnelHttpHeader> target, string name, IEnumerable<string> values)
    {
        if (ReverseTunnelHttpHeaders.HopByHopHeaders.Contains(name))
        {
            return;
        }

        foreach (var value in values)
        {
            target.Add(new ReverseTunnelHttpHeader
            {
                Name = name,
                Value = value
            });
        }
    }

    private static void InjectTraceContext(ICollection<ReverseTunnelHttpHeader> target)
    {
        var activity = Activity.Current;
        if (activity?.Id is null || target.Any(header => string.Equals(header.Name, "traceparent", StringComparison.OrdinalIgnoreCase)))
        {
            return;
        }

        target.Add(new ReverseTunnelHttpHeader
        {
            Name = "traceparent",
            Value = activity.Id
        });

        if (!string.IsNullOrWhiteSpace(activity.TraceStateString) &&
            !target.Any(header => string.Equals(header.Name, "tracestate", StringComparison.OrdinalIgnoreCase)))
        {
            target.Add(new ReverseTunnelHttpHeader
            {
                Name = "tracestate",
                Value = activity.TraceStateString
            });
        }
    }

    private async Task<HttpResponseMessage> InvokeRemoteServerAsync(ReverseTunnelRouteOptions route, HttpRequestMessage request, Activity? activity, CancellationToken cancellationToken)
    {
        var gateway = options.Current.Gateway;
        if (httpClientFactory is null || string.IsNullOrWhiteSpace(gateway.RemoteServerAddress))
        {
            const string code = "reverse_tunnel_remote_server_not_configured";
            const string message = "Reverse tunnel proxy is enabled without Server mode, but ReverseTunnelSettings:Gateway:RemoteServerAddress is not configured.";
            ReverseTunnelMetrics.SetGatewayFailure(activity, code, message, (int)HttpStatusCode.ServiceUnavailable);
            return CreateErrorResponse(HttpStatusCode.ServiceUnavailable, code, message);
        }

        using var remoteRequest = CreateRemoteServerRequest(route, request, gateway);
        try
        {
            var client = httpClientFactory.CreateClient("AirCloudReverseTunnelGateway");
            var response = await client.SendAsync(remoteRequest, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            ReverseTunnelMetrics.SetGatewaySuccess(activity, (int)response.StatusCode);
            return response;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            const string code = "cancelled";
            const string message = "Reverse tunnel remote invocation was cancelled.";
            ReverseTunnelMetrics.SetGatewayFailure(activity, code, message, 499);
            return CreateErrorResponse((HttpStatusCode)499, code, message);
        }
        catch (Exception exception)
        {
            const string code = "remote_server_unavailable";
            ReverseTunnelMetrics.SetGatewayFailure(activity, code, exception.Message, (int)HttpStatusCode.BadGateway);
            return CreateErrorResponse(HttpStatusCode.BadGateway, code, exception.Message);
        }
    }

    private static HttpRequestMessage CreateRemoteServerRequest(ReverseTunnelRouteOptions route, HttpRequestMessage request, ReverseTunnelGatewayOptions gateway)
    {
        var remoteUri = CreateRemoteServerUri(route, request, gateway);
        var remoteRequest = new HttpRequestMessage(request.Method, remoteUri)
        {
            Version = request.Version,
            VersionPolicy = request.VersionPolicy
        };

        foreach (var header in request.Headers)
        {
            if (!ReverseTunnelHttpHeaders.HopByHopHeaders.Contains(header.Key))
            {
                remoteRequest.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }
        }

        if (request.Content is not null)
        {
            remoteRequest.Content = request.Content;
        }

        remoteRequest.Headers.Remove(ReverseTunnelHttpHeaders.GatewayTimeoutSecondsHeaderName);
        remoteRequest.Headers.TryAddWithoutValidation(ReverseTunnelHttpHeaders.GatewayTimeoutSecondsHeaderName, Math.Max(1, route.TimeoutSeconds).ToString(System.Globalization.CultureInfo.InvariantCulture));

        if (!string.IsNullOrWhiteSpace(gateway.AccessTokenEnvironmentVariable))
        {
            var token = Environment.GetEnvironmentVariable(gateway.AccessTokenEnvironmentVariable);
            if (!string.IsNullOrEmpty(token))
            {
                remoteRequest.Headers.Remove(ReverseTunnelHttpHeaders.GatewayAccessTokenHeaderName);
                remoteRequest.Headers.TryAddWithoutValidation(ReverseTunnelHttpHeaders.GatewayAccessTokenHeaderName, token);
            }
        }

        return remoteRequest;
    }

    private static Uri CreateRemoteServerUri(ReverseTunnelRouteOptions route, HttpRequestMessage request, ReverseTunnelGatewayOptions gateway)
    {
        var pathAndQuery = request.RequestUri?.PathAndQuery ?? "/";
        var queryIndex = pathAndQuery.IndexOf('?');
        var path = queryIndex >= 0 ? pathAndQuery[..queryIndex] : pathAndQuery;
        var query = queryIndex >= 0 ? pathAndQuery[queryIndex..] : string.Empty;
        var escapedServiceKey = Uri.EscapeDataString(route.ServiceKey);
        var relativePath = $"{gateway.InvokePathPrefix}/{escapedServiceKey}{(path.StartsWith('/') ? path : "/" + path)}{query}";
        return new Uri(new Uri(gateway.RemoteServerAddress, UriKind.Absolute), relativePath);
    }
}
