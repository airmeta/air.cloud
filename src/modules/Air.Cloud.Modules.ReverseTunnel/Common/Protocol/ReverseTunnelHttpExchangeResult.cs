using System.Threading.Channels;

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
    /// <para>zh-cn:Agent 返回的 HTTP 响应体流。成功结果优先使用该读取器向 Gateway 下游响应流写出数据；旧的聚合式调用仍可使用 Body。</para>
    /// <para>en-us:HTTP response body stream returned by the Agent. Successful results prefer this reader when writing the Gateway downstream response stream; legacy aggregate calls may still use Body.</para>
    /// </summary>
    public ChannelReader<byte[]>? BodyReader { get; init; }

    /// <summary>
    /// <para>zh-cn:响应体读取被上游取消、写出失败或响应对象提前释放时调用的取消回调。Gateway 使用该回调通知 Server 向 Agent 发送 Cancel 帧，避免长响应继续占用隧道流。</para>
    /// <para>en-us:Cancellation callback invoked when response-body reading is canceled by the upstream, writing fails, or the response object is disposed early. The Gateway uses it to ask the Server to send a Cancel frame to the Agent so long responses do not keep occupying a tunnel stream.</para>
    /// </summary>
    public Func<CancellationToken, Task>? CancelBodyAsync { get; init; }

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
    /// <para>zh-cn:创建流式成功交换结果。调用方负责持续读取 bodyReader，直到 Agent 发送 Complete、Error 或会话关闭。</para>
    /// <para>en-us:Creates a streaming successful exchange result. The caller is responsible for reading bodyReader until the Agent sends Complete, Error, or the session closes.</para>
    /// </summary>
    /// <param name="response">
    /// <para>zh-cn:HTTP 响应元数据。</para>
    /// <para>en-us:HTTP response metadata.</para>
    /// </param>
    /// <param name="bodyReader">
    /// <para>zh-cn:HTTP 响应体帧读取器。</para>
    /// <para>en-us:HTTP response body frame reader.</para>
    /// </param>
    /// <param name="cancelBodyAsync">
    /// <para>zh-cn:响应体读取取消或提前释放时执行的取消回调；可为空。</para>
    /// <para>en-us:Cancellation callback executed when response-body reading is canceled or released early; it may be empty.</para>
    /// </param>
    /// <returns>
    /// <para>zh-cn:流式成功交换结果。</para>
    /// <para>en-us:A streaming successful exchange result.</para>
    /// </returns>
    public static ReverseTunnelHttpExchangeResult Success(ReverseTunnelHttpResponsePayload response, ChannelReader<byte[]> bodyReader, Func<CancellationToken, Task>? cancelBodyAsync = null)
    {
        return new ReverseTunnelHttpExchangeResult
        {
            Succeeded = true,
            Response = response,
            BodyReader = bodyReader,
            CancelBodyAsync = cancelBodyAsync
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
