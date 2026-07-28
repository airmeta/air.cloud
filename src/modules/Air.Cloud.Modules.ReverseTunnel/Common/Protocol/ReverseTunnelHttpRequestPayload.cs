namespace Air.Cloud.Modules.ReverseTunnel.Common.Protocol;

/// <summary>
/// <para>zh-cn:打开一笔反向隧道 HTTP 请求流时携带的请求元数据。请求体通过后续 RequestBody 帧传输。</para>
/// <para>en-us:Request metadata carried when opening a reverse tunnel HTTP request stream. The request body is transported by following RequestBody frames.</para>
/// </summary>
public sealed record ReverseTunnelHttpRequestPayload
{
    /// <summary>
    /// <para>zh-cn:逻辑服务键，用于 Agent 在本地服务声明中定位目标 HTTP 根地址。</para>
    /// <para>en-us:Logical service key used by the Agent to locate the target HTTP base address from local service declarations.</para>
    /// </summary>
    public string ServiceKey { get; init; } = string.Empty;

    /// <summary>
    /// <para>zh-cn:HTTP 方法，例如 GET、POST、PUT 或 DELETE。</para>
    /// <para>en-us:HTTP method such as GET, POST, PUT, or DELETE.</para>
    /// </summary>
    public string Method { get; init; } = "GET";

    /// <summary>
    /// <para>zh-cn:下游路径和查询串，必须是以 `/` 开头的相对 path/query，不能是绝对 URI。</para>
    /// <para>en-us:Downstream path and query string. It must be a relative path/query starting with `/`, not an absolute URI.</para>
    /// </summary>
    public string PathAndQuery { get; init; } = "/";

    /// <summary>
    /// <para>zh-cn:是否存在请求体。Server/Gateway 侧根据原始 `HttpRequestMessage.Content` 设置；Agent 侧只有该值为 true 时才会为本地 HTTP 请求创建流式 Content，避免无请求体 GET 被转成 `Transfer-Encoding: chunked`。</para>
    /// <para>en-us:Indicates whether the request carries a body. The Server/Gateway side sets it from the original `HttpRequestMessage.Content`; the Agent side creates streaming Content for the local HTTP request only when this value is true, avoiding body-less GET requests being converted to `Transfer-Encoding: chunked`.</para>
    /// </summary>
    public bool HasBody { get; init; }

    /// <summary>
    /// <para>zh-cn:请求头集合。</para>
    /// <para>en-us:Request header collection.</para>
    /// </summary>
    public List<ReverseTunnelHttpHeader> Headers { get; init; } = [];
}
