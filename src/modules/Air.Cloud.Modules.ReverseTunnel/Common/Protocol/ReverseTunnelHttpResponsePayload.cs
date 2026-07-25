namespace Air.Cloud.Modules.ReverseTunnel.Common.Protocol;

/// <summary>
/// <para>zh-cn:反向隧道 HTTP 响应头帧负载。响应体通过后续 ResponseBody 帧传输。</para>
/// <para>en-us:Payload for a reverse tunnel HTTP response-header frame. The response body is transported by following ResponseBody frames.</para>
/// </summary>
public sealed record ReverseTunnelHttpResponsePayload
{
    /// <summary>
    /// <para>zh-cn:HTTP 响应状态码。</para>
    /// <para>en-us:HTTP response status code.</para>
    /// </summary>
    public int StatusCode { get; init; }

    /// <summary>
    /// <para>zh-cn:HTTP 响应原因短语；可为空。</para>
    /// <para>en-us:HTTP response reason phrase; may be empty.</para>
    /// </summary>
    public string ReasonPhrase { get; init; } = string.Empty;

    /// <summary>
    /// <para>zh-cn:响应头集合。</para>
    /// <para>en-us:Response header collection.</para>
    /// </summary>
    public List<ReverseTunnelHttpHeader> Headers { get; init; } = [];
}
