using Air.Cloud.Modules.ReverseTunnel.Common.Options;
using Air.Cloud.Modules.ReverseTunnel.Common.Protocol;

using Microsoft.Extensions.Options;

using System.Net.Http;

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

    private readonly IOptions<ReverseTunnelOptions> options;
    private readonly IHttpClientFactory httpClientFactory;

    /// <summary>
    /// <para>zh-cn:创建 Agent 本地 HTTP 分发器。</para>
    /// <para>en-us:Creates an Agent local HTTP dispatcher.</para>
    /// </summary>
    /// <param name="options">
    /// <para>zh-cn:包含 LocalServices 和请求体大小限制的反向隧道配置。</para>
    /// <para>en-us:Reverse tunnel options containing LocalServices and request-body size limits.</para>
    /// </param>
    /// <param name="httpClientFactory">
    /// <para>zh-cn:用于创建本地 HTTP 调用客户端的工厂。</para>
    /// <para>en-us:Factory used to create clients for local HTTP calls.</para>
    /// </param>
    public ReverseTunnelAgentLocalHttpDispatcher(IOptions<ReverseTunnelOptions> options, IHttpClientFactory httpClientFactory)
    {
        this.options = options;
        this.httpClientFactory = httpClientFactory;
    }

    /// <inheritdoc />
    public async Task<ReverseTunnelLocalHttpResponse> DispatchAsync(ReverseTunnelHttpRequestPayload request, byte[] body, CancellationToken cancellationToken = default)
    {
        if (request is null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        body ??= Array.Empty<byte>();
        if (body.LongLength > options.Value.MaxRequestBodyBytes)
        {
            throw new InvalidOperationException("Reverse tunnel request body exceeds the configured Agent limit.");
        }

        var targetBaseAddress = ResolveLocalService(request.ServiceKey);
        var targetUri = BuildTargetUri(targetBaseAddress, request.PathAndQuery);
        using var httpRequest = new HttpRequestMessage(new HttpMethod(NormalizeMethod(request.Method)), targetUri);

        if (body.Length > 0 || MethodAllowsBody(httpRequest.Method))
        {
            httpRequest.Content = new ByteArrayContent(body);
        }

        CopyRequestHeaders(request.Headers, httpRequest);

        var httpClient = httpClientFactory.CreateClient("AirCloudReverseTunnelAgentLocal");
        using var response = await httpClient.SendAsync(httpRequest, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        var responseBody = response.Content is null
            ? Array.Empty<byte>()
            : await ReadContentWithLimitAsync(response.Content, options.Value.MaxResponseBodyBytes, cancellationToken);

        return new ReverseTunnelLocalHttpResponse
        {
            Headers = new ReverseTunnelHttpResponsePayload
            {
                StatusCode = (int)response.StatusCode,
                ReasonPhrase = response.ReasonPhrase ?? string.Empty,
                Headers = CollectResponseHeaders(response)
            },
            Body = responseBody
        };
    }

    private Uri ResolveLocalService(string serviceKey)
    {
        if (string.IsNullOrWhiteSpace(serviceKey))
        {
            throw new InvalidOperationException("Reverse tunnel request ServiceKey is required.");
        }

        var service = options.Value.LocalServices.FirstOrDefault(item => string.Equals(item.ServiceKey, serviceKey.Trim(), StringComparison.OrdinalIgnoreCase));
        if (service is null)
        {
            throw new InvalidOperationException($"Reverse tunnel local service '{serviceKey}' is not configured on this Agent.");
        }

        if (!Uri.TryCreate(service.LocalAddress, UriKind.Absolute, out var localAddress) ||
            (localAddress.Scheme != Uri.UriSchemeHttp && localAddress.Scheme != Uri.UriSchemeHttps))
        {
            throw new InvalidOperationException($"Reverse tunnel local service '{serviceKey}' must use an absolute HTTP or HTTPS address.");
        }

        return localAddress;
    }

    private static Uri BuildTargetUri(Uri baseAddress, string pathAndQuery)
    {
        var normalizedPath = string.IsNullOrWhiteSpace(pathAndQuery) ? "/" : pathAndQuery.Trim();
        if (Uri.TryCreate(normalizedPath, UriKind.Absolute, out _))
        {
            throw new InvalidOperationException("Reverse tunnel request PathAndQuery must not be an absolute URI.");
        }

        if (!normalizedPath.StartsWith("/", StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Reverse tunnel request PathAndQuery must start with '/'.");
        }

        return new Uri(baseAddress, normalizedPath);
    }

    private static void CopyRequestHeaders(IEnumerable<ReverseTunnelHttpHeader> headers, HttpRequestMessage httpRequest)
    {
        foreach (var header in headers.Where(item => !string.IsNullOrWhiteSpace(item.Name) && !HopByHopHeaders.Contains(item.Name)))
        {
            if (!httpRequest.Headers.TryAddWithoutValidation(header.Name, header.Value))
            {
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

    private static bool MethodAllowsBody(HttpMethod method)
    {
        return method != HttpMethod.Get && method != HttpMethod.Head && method != HttpMethod.Delete;
    }

    private static async Task<byte[]> ReadContentWithLimitAsync(HttpContent content, long maxBytes, CancellationToken cancellationToken)
    {
        await using var contentStream = await content.ReadAsStreamAsync(cancellationToken);
        using var buffer = new MemoryStream();
        var chunk = new byte[81920];
        int read;
        while ((read = await contentStream.ReadAsync(chunk, cancellationToken)) > 0)
        {
            if (buffer.Length + read > maxBytes)
            {
                throw new InvalidOperationException("Reverse tunnel response body exceeds the configured Agent limit.");
            }

            buffer.Write(chunk, 0, read);
        }

        return buffer.ToArray();
    }
}
