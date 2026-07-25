namespace Air.Cloud.Modules.ReverseTunnel.Common.Protocol;

/// <summary>
/// <para>zh-cn:反向隧道 HTTP 帧中的头键值。使用数组而不是字典，以保留重复响应头和请求头。</para>
/// <para>en-us:Header key-value pair carried by reverse tunnel HTTP frames. An array is used instead of a dictionary so repeated request and response headers can be preserved.</para>
/// </summary>
public sealed record ReverseTunnelHttpHeader
{
    /// <summary>
    /// <para>zh-cn:HTTP 头名称。</para>
    /// <para>en-us:HTTP header name.</para>
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// <para>zh-cn:HTTP 头值。</para>
    /// <para>en-us:HTTP header value.</para>
    /// </summary>
    public string Value { get; init; } = string.Empty;
}
