using Air.Cloud.Modules.ReverseTunnel.Common.Protocol;

using System.Threading.Channels;

namespace Air.Cloud.Modules.ReverseTunnel.Agent;

/// <summary>
/// <para>zh-cn:Agent 本地 HTTP 分发器。它只允许访问 Agent 配置中声明的 LocalServices，不接受请求负载中的绝对 URI。</para>
/// <para>en-us:Agent local HTTP dispatcher. It only allows access to LocalServices declared in Agent configuration and does not accept absolute URIs from request payloads.</para>
/// </summary>
public interface IReverseTunnelAgentLocalHttpDispatcher
{
    /// <summary>
    /// <para>zh-cn:把 Server 下发的隧道 HTTP 请求以流式方式分发给 Agent 本地服务。请求体从 requestBodyReader 按帧读取；响应头一旦可用即调用 onResponseHeaders，响应体按读取到的块持续调用 onResponseBody。</para>
    /// <para>en-us:Dispatches a tunneled HTTP request from the Server to an Agent local service in streaming mode. The request body is read from requestBodyReader by frame; response headers are emitted through onResponseHeaders as soon as they are available, and response body chunks are emitted through onResponseBody as they are read.</para>
    /// </summary>
    /// <param name="request">
    /// <para>zh-cn:请求元数据，包含 ServiceKey、方法、相对 path/query 和请求头。</para>
    /// <para>en-us:Request metadata containing service key, method, relative path/query, and request headers.</para>
    /// </param>
    /// <param name="requestBodyReader">
    /// <para>zh-cn:请求体帧读取器。读取器完成表示请求体结束；读取器异常表示请求被取消或上游失败。</para>
    /// <para>en-us:Request body frame reader. Reader completion means the request body ended; reader failure means the request was canceled or the upstream failed.</para>
    /// </param>
    /// <param name="onResponseHeaders">
    /// <para>zh-cn:本地 HTTP 响应头可用时调用的回调。</para>
    /// <para>en-us:Callback invoked when local HTTP response headers become available.</para>
    /// </param>
    /// <param name="onResponseBody">
    /// <para>zh-cn:本地 HTTP 响应体分块可用时调用的回调。</para>
    /// <para>en-us:Callback invoked when a local HTTP response body chunk becomes available.</para>
    /// </param>
    /// <param name="cancellationToken">
    /// <para>zh-cn:取消本地 HTTP 调用以及请求/响应流复制的令牌。</para>
    /// <para>en-us:Token used to cancel the local HTTP call and request/response stream copies.</para>
    /// </param>
    /// <returns>
    /// <para>zh-cn:本地 HTTP 响应流完全发送给回调后完成。</para>
    /// <para>en-us:Completes after the local HTTP response stream has been fully emitted to the callbacks.</para>
    /// </returns>
    Task DispatchAsync(
        ReverseTunnelHttpRequestPayload request,
        ChannelReader<byte[]> requestBodyReader,
        Func<ReverseTunnelHttpResponsePayload, CancellationToken, Task> onResponseHeaders,
        Func<ReadOnlyMemory<byte>, CancellationToken, Task> onResponseBody,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// <para>zh-cn:把 Server 下发的隧道 HTTP 请求分发给 Agent 本地服务，并返回完整响应。</para>
    /// <para>en-us:Dispatches a tunneled HTTP request from the Server to an Agent local service and returns the full response.</para>
    /// </summary>
    /// <param name="request">
    /// <para>zh-cn:请求元数据，包含 ServiceKey、方法、相对 path/query 和请求头。</para>
    /// <para>en-us:Request metadata containing service key, method, relative path/query, and request headers.</para>
    /// </param>
    /// <param name="body">
    /// <para>zh-cn:请求体字节；无请求体时传入空数组。</para>
    /// <para>en-us:Request body bytes; pass an empty array when there is no request body.</para>
    /// </param>
    /// <param name="cancellationToken">
    /// <para>zh-cn:取消本地 HTTP 调用的令牌。</para>
    /// <para>en-us:Token used to cancel the local HTTP call.</para>
    /// </param>
    /// <returns>
    /// <para>zh-cn:本地 HTTP 响应。</para>
    /// <para>en-us:The local HTTP response.</para>
    /// </returns>
    Task<ReverseTunnelLocalHttpResponse> DispatchAsync(ReverseTunnelHttpRequestPayload request, byte[] body, CancellationToken cancellationToken = default);
}
