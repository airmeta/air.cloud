using Air.Cloud.Modules.ReverseTunnel.Common.Diagnostics;
using Air.Cloud.Modules.ReverseTunnel.Common.Options;
using Air.Cloud.Modules.ReverseTunnel.Common.Http;
using Air.Cloud.Modules.ReverseTunnel.Common.Protocol;
using Air.Cloud.Modules.ReverseTunnel.Server;

using Microsoft.Extensions.Options;

using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;

namespace Air.Cloud.Modules.ReverseTunnel.Gateway;

/// <summary>
/// <para>zh-cn:默认 Gateway 侧反向隧道调用器。首版只使用当前进程内的 Agent 会话注册表，因此多 Gateway 共享会话不在当前范围内。</para>
/// <para>en-us:Default Gateway-side reverse tunnel invoker. The first version uses only the current process Agent-session registry, so cross-Gateway shared sessions are out of scope.</para>
/// </summary>
public sealed class ReverseTunnelInvoker : IReverseTunnelInvoker
{
    private static readonly HashSet<string> HopByHopHeaders = new(StringComparer.OrdinalIgnoreCase)
    {
        "Connection",
        "Keep-Alive",
        "Proxy-Authenticate",
        "Proxy-Authorization",
        "TE",
        "Trailer",
        "Transfer-Encoding",
        "Upgrade",
        "Host",
        "Content-Length"
    };

    private readonly IReverseTunnelSessionRegistry sessionRegistry;
    private readonly IOptions<ReverseTunnelOptions> options;

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
    {
        this.sessionRegistry = sessionRegistry;
        this.options = options;
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
        using var activity = ReverseTunnelMetrics.StartGatewayInvoke(route.ServiceKey, route.VirtualHost, request.Method.Method, pathAndQuery);
        if (!options.Value.Enabled || !options.Value.Roles.HasFlag(ReverseTunnelRole.Gateway))
        {
            const string code = "reverse_tunnel_gateway_disabled";
            const string message = "Reverse tunnel Gateway role is not enabled.";
            ReverseTunnelMetrics.SetGatewayFailure(activity, code, message, (int)HttpStatusCode.ServiceUnavailable);
            return CreateErrorResponse(HttpStatusCode.ServiceUnavailable, code, message);
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

        foreach (var header in payload.Headers.Where(item => !string.IsNullOrWhiteSpace(item.Name) && !HopByHopHeaders.Contains(item.Name)))
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
        if (HopByHopHeaders.Contains(name))
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
}
