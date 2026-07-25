using Air.Cloud.Modules.ReverseTunnel.Common.Options;
using Air.Cloud.Modules.ReverseTunnel.Common.Protocol;
using Air.Cloud.Modules.ReverseTunnel.Server;

using Microsoft.Extensions.Options;

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

        if (!options.Value.Enabled || !options.Value.Roles.HasFlag(ReverseTunnelRole.Gateway))
        {
            return CreateErrorResponse(HttpStatusCode.ServiceUnavailable, "reverse_tunnel_gateway_disabled", "Reverse tunnel Gateway role is not enabled.");
        }

        if (!sessionRegistry.TryGetForServiceKey(route.ServiceKey, out var session) || session is null)
        {
            return CreateErrorResponse(HttpStatusCode.ServiceUnavailable, "agent_unavailable", $"No reverse tunnel Agent session is available for service '{route.ServiceKey}'.");
        }

        byte[] body;
        try
        {
            body = request.Content is null
                ? Array.Empty<byte>()
                : await ReadContentWithLimitAsync(request.Content, options.Value.MaxRequestBodyBytes, "request_body_too_large", "Reverse tunnel request body exceeds the configured Gateway limit.", cancellationToken);
        }
        catch (ReverseTunnelHttpLimitExceededException exception)
        {
            return CreateErrorResponse(HttpStatusCode.RequestEntityTooLarge, exception.Code, exception.Message);
        }

        var payload = new ReverseTunnelHttpRequestPayload
        {
            ServiceKey = route.ServiceKey,
            Method = request.Method.Method,
            PathAndQuery = request.RequestUri?.PathAndQuery ?? "/",
            Headers = CollectRequestHeaders(request)
        };

        var result = await session.InvokeAsync(payload, body, TimeSpan.FromSeconds(route.TimeoutSeconds), cancellationToken);
        if (!result.Succeeded || result.Response is null)
        {
            return CreateErrorResponse(ResolveFailureStatusCode(result.ErrorCode), result.ErrorCode ?? "agent_error", result.ErrorMessage ?? "Reverse tunnel Agent returned an error.");
        }

        return CreateSuccessResponse(result.Response, result.Body);
    }

    private static async Task<byte[]> ReadContentWithLimitAsync(HttpContent content, long maxBytes, string code, string message, CancellationToken cancellationToken)
    {
        await using var contentStream = await content.ReadAsStreamAsync(cancellationToken);
        using var buffer = new MemoryStream();
        var chunk = new byte[81920];
        int read;
        while ((read = await contentStream.ReadAsync(chunk, cancellationToken)) > 0)
        {
            if (buffer.Length + read > maxBytes)
            {
                throw new ReverseTunnelHttpLimitExceededException(code, message);
            }

            buffer.Write(chunk, 0, read);
        }

        return buffer.ToArray();
    }

    private static HttpStatusCode ResolveFailureStatusCode(string? errorCode)
    {
        return errorCode switch
        {
            "timeout" => HttpStatusCode.GatewayTimeout,
            "too_many_concurrent_streams" => HttpStatusCode.TooManyRequests,
            "request_body_too_large" => HttpStatusCode.RequestEntityTooLarge,
            "response_body_too_large" => HttpStatusCode.BadGateway,
            "session_closed" => HttpStatusCode.ServiceUnavailable,
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

    private static HttpResponseMessage CreateSuccessResponse(ReverseTunnelHttpResponsePayload payload, byte[] body)
    {
        var response = new HttpResponseMessage((HttpStatusCode)Math.Clamp(payload.StatusCode, 100, 599))
        {
            ReasonPhrase = string.IsNullOrWhiteSpace(payload.ReasonPhrase) ? null : payload.ReasonPhrase
        };

        var hasBody = body is { Length: > 0 };
        if (hasBody || payload.Headers.Any(header => header.Name.StartsWith("Content-", StringComparison.OrdinalIgnoreCase)))
        {
            response.Content = new ByteArrayContent(body ?? Array.Empty<byte>());
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

    private sealed class ReverseTunnelHttpLimitExceededException : Exception
    {
        public ReverseTunnelHttpLimitExceededException(string code, string message)
            : base(message)
        {
            Code = code;
        }

        public string Code { get; }
    }
}
