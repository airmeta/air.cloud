namespace Air.Cloud.Modules.ReverseTunnel.Common.Protocol;

/// <summary>
/// <para>zh-cn:一次反向隧道 HTTP 交换结果。成功时包含 Agent 返回的响应元数据和响应体，失败时包含稳定错误码和错误说明。</para>
/// <para>en-us:Result of one reverse tunnel HTTP exchange. On success it contains response metadata and body returned by the Agent; on failure it contains a stable error code and message.</para>
/// </summary>
public sealed record ReverseTunnelHttpExchangeResult
{
    /// <summary>
    /// <para>zh-cn:本次交换是否成功完成。</para>
    /// <para>en-us:Whether the exchange completed successfully.</para>
    /// </summary>
    public bool Succeeded { get; init; }

    /// <summary>
    /// <para>zh-cn:失败错误码；成功时为空。</para>
    /// <para>en-us:Failure error code; empty on success.</para>
    /// </summary>
    public string? ErrorCode { get; init; }

    /// <summary>
    /// <para>zh-cn:不包含敏感材料的失败说明；成功时为空。</para>
    /// <para>en-us:Failure message without sensitive material; empty on success.</para>
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// <para>zh-cn:Agent 返回的 HTTP 响应元数据；失败时为空。</para>
    /// <para>en-us:HTTP response metadata returned by the Agent; empty on failure.</para>
    /// </summary>
    public ReverseTunnelHttpResponsePayload? Response { get; init; }

    /// <summary>
    /// <para>zh-cn:Agent 返回的 HTTP 响应体。</para>
    /// <para>en-us:HTTP response body returned by the Agent.</para>
    /// </summary>
    public byte[] Body { get; init; } = Array.Empty<byte>();

    /// <summary>
    /// <para>zh-cn:创建成功交换结果。</para>
    /// <para>en-us:Creates a successful exchange result.</para>
    /// </summary>
    /// <param name="response">
    /// <para>zh-cn:HTTP 响应元数据。</para>
    /// <para>en-us:HTTP response metadata.</para>
    /// </param>
    /// <param name="body">
    /// <para>zh-cn:HTTP 响应体。</para>
    /// <para>en-us:HTTP response body.</para>
    /// </param>
    /// <returns>
    /// <para>zh-cn:成功交换结果。</para>
    /// <para>en-us:A successful exchange result.</para>
    /// </returns>
    public static ReverseTunnelHttpExchangeResult Success(ReverseTunnelHttpResponsePayload response, byte[] body)
    {
        return new ReverseTunnelHttpExchangeResult
        {
            Succeeded = true,
            Response = response,
            Body = body ?? Array.Empty<byte>()
        };
    }

    /// <summary>
    /// <para>zh-cn:创建失败交换结果。</para>
    /// <para>en-us:Creates a failed exchange result.</para>
    /// </summary>
    /// <param name="errorCode">
    /// <para>zh-cn:稳定错误码。</para>
    /// <para>en-us:Stable error code.</para>
    /// </param>
    /// <param name="errorMessage">
    /// <para>zh-cn:不包含敏感材料的错误说明。</para>
    /// <para>en-us:Error message without sensitive material.</para>
    /// </param>
    /// <returns>
    /// <para>zh-cn:失败交换结果。</para>
    /// <para>en-us:A failed exchange result.</para>
    /// </returns>
    public static ReverseTunnelHttpExchangeResult Failure(string errorCode, string errorMessage)
    {
        return new ReverseTunnelHttpExchangeResult
        {
            Succeeded = false,
            ErrorCode = errorCode,
            ErrorMessage = errorMessage
        };
    }
}
