using Air.Cloud.Modules.ReverseTunnel.Common.Protocol;

namespace Air.Cloud.Modules.ReverseTunnel.Agent;

/// <summary>
/// <para>zh-cn:Agent 本地 HTTP 分发器。它只允许访问 Agent 配置中声明的 LocalServices，不接受请求负载中的绝对 URI。</para>
/// <para>en-us:Agent local HTTP dispatcher. It only allows access to LocalServices declared in Agent configuration and does not accept absolute URIs from request payloads.</para>
/// </summary>
public interface IReverseTunnelAgentLocalHttpDispatcher
{
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
