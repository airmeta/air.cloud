using Air.Cloud.Modules.ReverseTunnel.Common.Options;
using Air.Cloud.Modules.ReverseTunnel.Common.Http;
using Air.Cloud.Modules.ReverseTunnel.Common.Protocol;
using Air.Cloud.Modules.ReverseTunnel.Common.Runtime;

using Microsoft.Extensions.Options;

using System.Net.Http;
using System.Threading.Channels;

namespace Air.Cloud.Modules.ReverseTunnel.Agent;

/// <summary>
/// <para>zh-cn:基于 `HttpClient` 的 Agent 本地 HTTP 分发器。目标地址只来自 `ReverseTunnelSettings:LocalServices`，请求负载只能提供相对 path/query。</para>
/// <para>en-us:`HttpClient`-based Agent local HTTP dispatcher. Target addresses come only from `ReverseTunnelSettings:LocalServices`, and the request payload may provide only a relative path/query.</para>
/// </summary>
public sealed class ReverseTunnelAgentLocalHttpDispatcher : IReverseTunnelAgentLocalHttpDispatcher
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

    private readonly IReverseTunnelRuntimeOptionsAccessor options;
    private readonly IHttpClientFactory httpClientFactory;

    /// <summary>
    /// <para>zh-cn:创建 Agent 本地 HTTP 分发器。</para>
    /// <para>en-us:Creates an Agent local HTTP dispatcher.</para>
    /// </summary>
    /// <param name="startupOptions">
    /// <para>zh-cn:包含 LocalServices 和请求体大小限制的反向隧道配置。</para>
    /// <para>en-us:Reverse tunnel options containing LocalServices and request-body size limits.</para>
    /// </param>
    /// <param name="runtimeOptions">
    /// <para>zh-cn:可选的运行期配置访问器。</para>
    /// <para>en-us:Optional runtime options accessor.</para>
    /// </param>
    /// <param name="httpClientFactory">
    /// <para>zh-cn:用于创建本地 HTTP 调用客户端的工厂。</para>
    /// <para>en-us:Factory used to create clients for local HTTP calls.</para>
    /// </param>
    public ReverseTunnelAgentLocalHttpDispatcher(
        IOptions<ReverseTunnelOptions> startupOptions,
        IHttpClientFactory httpClientFactory,
        IReverseTunnelRuntimeOptionsAccessor? runtimeOptions = null)
    {
        this.options = runtimeOptions ?? new ReverseTunnelRuntimeOptionsAccessor(startupOptions);
        this.httpClientFactory = httpClientFactory;
    }

    /// <inheritdoc />
    public async Task DispatchAsync(
        ReverseTunnelHttpRequestPayload request,
        ChannelReader<byte[]> requestBodyReader,
        Func<ReverseTunnelHttpResponsePayload, CancellationToken, Task> onResponseHeaders,
        Func<ReadOnlyMemory<byte>, CancellationToken, Task> onResponseBody,
        CancellationToken cancellationToken = default)
    {
        if (request is null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        if (requestBodyReader is null)
        {
            throw new ArgumentNullException(nameof(requestBodyReader));
        }

        if (onResponseHeaders is null)
        {
            throw new ArgumentNullException(nameof(onResponseHeaders));
        }

        if (onResponseBody is null)
        {
            throw new ArgumentNullException(nameof(onResponseBody));
        }

        var current = options.Current;
        var targetBaseAddress = ResolveLocalService(current, request.ServiceKey);
        var targetUri = BuildTargetUri(targetBaseAddress, request.PathAndQuery);
        using var httpRequest = new HttpRequestMessage(new HttpMethod(NormalizeMethod(request.Method)), targetUri);

        if (request.HasBody)
        {
            httpRequest.Content = new ReverseTunnelChannelReaderHttpContent(requestBodyReader);
        }

        CopyRequestHeaders(request.Headers, httpRequest, request.HasBody);

        var httpClient = httpClientFactory.CreateClient("AirCloudReverseTunnelAgentLocal");
        using var response = await httpClient.SendAsync(httpRequest, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        await onResponseHeaders(new ReverseTunnelHttpResponsePayload
        {
            StatusCode = (int)response.StatusCode,
            ReasonPhrase = response.ReasonPhrase ?? string.Empty,
            Headers = CollectResponseHeaders(response)
        }, cancellationToken);

        if (response.Content is null)
        {
            return;
        }

        await StreamContentWithLimitAsync(response.Content, current.MaxResponseBodyBytes, Math.Max(1_024, current.MaxFramePayloadBytes), onResponseBody, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<ReverseTunnelLocalHttpResponse> DispatchAsync(ReverseTunnelHttpRequestPayload request, byte[] body, CancellationToken cancellationToken = default)
    {
        body ??= Array.Empty<byte>();
        request = request with
        {
            HasBody = request.HasBody || body.Length > 0
        };

        if (body.LongLength > options.Current.MaxRequestBodyBytes)
        {
            throw new InvalidOperationException("Reverse tunnel request body exceeds the configured Agent limit.");
        }

        var requestBody = Channel.CreateBounded<byte[]>(new BoundedChannelOptions(1)
        {
            SingleReader = true,
            SingleWriter = true
        });
        if (body.Length > 0)
        {
            await requestBody.Writer.WriteAsync(body, cancellationToken);
        }

        requestBody.Writer.TryComplete();

        ReverseTunnelHttpResponsePayload? headers = null;
        using var responseBody = new MemoryStream();
        await DispatchAsync(
            request,
            requestBody.Reader,
            (payload, _) =>
            {
                headers = payload;
                return Task.CompletedTask;
            },
            (chunk, _) =>
            {
                responseBody.Write(chunk.Span);
                return Task.CompletedTask;
            },
            cancellationToken);

        return new ReverseTunnelLocalHttpResponse
        {
            Headers = headers ?? new ReverseTunnelHttpResponsePayload { StatusCode = 204 },
            Body = responseBody.ToArray()
        };
    }

    private static Uri ResolveLocalService(ReverseTunnelOptions options, string serviceKey)
    {
        if (string.IsNullOrWhiteSpace(serviceKey))
        {
            throw new InvalidOperationException("Reverse tunnel request ServiceKey is required.");
        }

        var service = options.LocalServices.FirstOrDefault(item => string.Equals(item.ServiceKey, serviceKey.Trim(), StringComparison.OrdinalIgnoreCase));
        if (service is null)
        {
            throw new InvalidOperationException($"Reverse tunnel local service '{serviceKey}' is not configured on this Agent.");
        }

        var protocol = service.Protocol?.Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(service.Host) || service.Port is < 1 or > 65_535 ||
            (protocol != Uri.UriSchemeHttp && protocol != Uri.UriSchemeHttps))
        {
            throw new InvalidOperationException($"Reverse tunnel local service '{serviceKey}' must specify a valid HTTP/HTTPS Protocol, Host, and Port.");
        }

        var host = service.Host.Contains(':', StringComparison.Ordinal) && !service.Host.StartsWith("[", StringComparison.Ordinal)
            ? "[" + service.Host + "]"
            : service.Host;
        return new Uri($"{protocol}://{host}:{service.Port}", UriKind.Absolute);
    }

    private static Uri BuildTargetUri(Uri baseAddress, string pathAndQuery)
    {
        var normalizedPath = string.IsNullOrWhiteSpace(pathAndQuery) ? "/" : pathAndQuery.Trim();
        if (Uri.TryCreate(normalizedPath, UriKind.Absolute, out var absoluteUri))
        {
            normalizedPath = absoluteUri.PathAndQuery;
        }

        // Older Gateway/Ocelot paths could escape the query separator as %3F.
        // Recover that separator at the Agent boundary so the configured local
        // service receives the same path and query semantics as the original request.
        var encodedQuerySeparator = normalizedPath.IndexOf("%3F", StringComparison.OrdinalIgnoreCase);
        if (encodedQuerySeparator >= 0)
        {
            normalizedPath = normalizedPath[..encodedQuerySeparator] + "?" + normalizedPath[(encodedQuerySeparator + 3)..];
        }

        if (!normalizedPath.StartsWith("/", StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Reverse tunnel request PathAndQuery must start with '/'.");
        }

        var querySeparator = normalizedPath.IndexOf('?');
        if (querySeparator >= 0)
        {
            var uriBuilder = new UriBuilder(baseAddress)
            {
                Path = normalizedPath[..querySeparator],
                Query = normalizedPath[(querySeparator + 1)..]
            };
            return uriBuilder.Uri;
        }

        return new Uri(baseAddress, normalizedPath);
    }

    private static void CopyRequestHeaders(IEnumerable<ReverseTunnelHttpHeader> headers, HttpRequestMessage httpRequest, bool hasBody)
    {
        foreach (var header in headers.Where(item => !string.IsNullOrWhiteSpace(item.Name) && !HopByHopHeaders.Contains(item.Name)))
        {
            if (!httpRequest.Headers.TryAddWithoutValidation(header.Name, header.Value))
            {
                if (!hasBody)
                {
                    continue;
                }

                httpRequest.Content ??= new ByteArrayContent(Array.Empty<byte>());
                httpRequest.Content.Headers.TryAddWithoutValidation(header.Name, header.Value);
            }
        }
    }

    private static List<ReverseTunnelHttpHeader> CollectResponseHeaders(HttpResponseMessage response)
    {
        var headers = new List<ReverseTunnelHttpHeader>();
        foreach (var header in response.Headers)
        {
            AddHeaders(headers, header.Key, header.Value);
        }

        if (response.Content is not null)
        {
            foreach (var header in response.Content.Headers)
            {
                AddHeaders(headers, header.Key, header.Value);
            }
        }

        return headers;
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

    private static string NormalizeMethod(string method)
    {
        return string.IsNullOrWhiteSpace(method) ? "GET" : method.Trim().ToUpperInvariant();
    }

    private static async Task StreamContentWithLimitAsync(
        HttpContent content,
        long maxBytes,
        int chunkSize,
        Func<ReadOnlyMemory<byte>, CancellationToken, Task> onChunk,
        CancellationToken cancellationToken)
    {
        await using var contentStream = await content.ReadAsStreamAsync(cancellationToken);
        var chunk = new byte[Math.Max(1_024, chunkSize)];
        long totalBytes = 0;
        int read;
        while ((read = await contentStream.ReadAsync(chunk, cancellationToken)) > 0)
        {
            totalBytes += read;
            if (totalBytes > maxBytes)
            {
                throw new ReverseTunnelHttpLimitExceededException("response_body_too_large", "Reverse tunnel response body exceeds the configured Agent limit.");
            }

            await onChunk(chunk.AsMemory(0, read), cancellationToken);
        }
    }
}
