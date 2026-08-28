namespace Air.Cloud.Modules.ReverseTunnel.Server;

/// <summary>
/// <para>zh-cn:Agent 会话在处理新请求或数据帧时读取的运行期限制。该对象不保存连接状态，可由配置热更新后重新生成。</para>
/// <para>en-us:Runtime limits read by an Agent session while handling new requests or data frames. The object does not store connection state and can be recreated after configuration hot updates.</para>
/// </summary>
/// <param name="MaxConcurrentStreams"><para>zh-cn:每个会话允许的新 HTTP 并发流数量。</para><para>en-us:Allowed new HTTP concurrent streams per session.</para></param>
/// <param name="OutboundFrameQueueCapacity"><para>zh-cn:出站帧队列容量。已有会话队列容量不调整，新会话生效。</para><para>en-us:Outbound frame queue capacity. Existing session queues are not resized; new sessions use the value.</para></param>
/// <param name="MaxFramePayloadBytes"><para>zh-cn:单个数据帧最大 payload 字节数。</para><para>en-us:Maximum payload bytes per data frame.</para></param>
/// <param name="MaxRequestBodyBytes"><para>zh-cn:单次请求体最大字节数。</para><para>en-us:Maximum bytes per request body.</para></param>
/// <param name="MaxResponseBodyBytes"><para>zh-cn:单次响应体最大字节数。</para><para>en-us:Maximum bytes per response body.</para></param>
public sealed record ReverseTunnelSessionLimits(
    int MaxConcurrentStreams,
    int OutboundFrameQueueCapacity,
    int MaxFramePayloadBytes,
    long MaxRequestBodyBytes,
    long MaxResponseBodyBytes);
