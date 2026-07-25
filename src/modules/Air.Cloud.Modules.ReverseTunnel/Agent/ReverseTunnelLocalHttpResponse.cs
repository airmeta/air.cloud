using Air.Cloud.Modules.ReverseTunnel.Common.Protocol;

namespace Air.Cloud.Modules.ReverseTunnel.Agent;

/// <summary>
/// <para>zh-cn:Agent 本地 HTTP 分发结果。响应体已读取为字节数组，便于后续映射为 ResponseBody 帧。</para>
/// <para>en-us:Result of Agent local HTTP dispatch. The response body is read as bytes so it can be mapped to ResponseBody frames later.</para>
/// </summary>
public sealed record ReverseTunnelLocalHttpResponse
{
    /// <summary>
    /// <para>zh-cn:HTTP 响应元数据。</para>
    /// <para>en-us:HTTP response metadata.</para>
    /// </summary>
    public ReverseTunnelHttpResponsePayload Headers { get; init; } = new();

    /// <summary>
    /// <para>zh-cn:HTTP 响应体字节。</para>
    /// <para>en-us:HTTP response body bytes.</para>
    /// </summary>
    public byte[] Body { get; init; } = Array.Empty<byte>();
}
