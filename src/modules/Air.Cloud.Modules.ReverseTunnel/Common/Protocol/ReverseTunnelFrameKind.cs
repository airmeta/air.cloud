namespace Air.Cloud.Modules.ReverseTunnel.Common.Protocol;

/// <summary>
/// <para>zh-cn:反向隧道帧类型。连接控制帧使用流标识 0，请求与响应数据帧必须使用正数流标识。</para>
/// <para>en-us:Reverse tunnel frame kinds. Connection-control frames use stream identifier 0, while request and response data frames must use a positive stream identifier.</para>
/// </summary>
public enum ReverseTunnelFrameKind
{
    /// <summary><para>zh-cn:Agent 发起连接时的身份与能力声明。</para><para>en-us:Agent identity and capability declaration that opens a connection.</para></summary>
    Hello = 1,

    /// <summary><para>zh-cn:Server 对连接的接受结果和会话信息。</para><para>en-us:Server acceptance result and session information.</para></summary>
    Welcome = 2,

    /// <summary><para>zh-cn:连接保活请求。</para><para>en-us:Connection keepalive request.</para></summary>
    Ping = 3,

    /// <summary><para>zh-cn:连接保活响应。</para><para>en-us:Connection keepalive response.</para></summary>
    Pong = 4,

    /// <summary><para>zh-cn:打开一笔 HTTP 请求流。</para><para>en-us:Opens an HTTP request stream.</para></summary>
    OpenRequest = 10,

    /// <summary><para>zh-cn:请求体分片。</para><para>en-us:Request body chunk.</para></summary>
    RequestBody = 11,

    /// <summary><para>zh-cn:响应状态与响应头。</para><para>en-us:Response status and headers.</para></summary>
    ResponseHeaders = 12,

    /// <summary><para>zh-cn:响应体分片。</para><para>en-us:Response body chunk.</para></summary>
    ResponseBody = 13,

    /// <summary><para>zh-cn:正常结束一笔请求流。</para><para>en-us:Completes a request stream normally.</para></summary>
    Complete = 14,

    /// <summary><para>zh-cn:取消一笔仍在执行的请求流。</para><para>en-us:Cancels an in-flight request stream.</para></summary>
    Cancel = 15,

    /// <summary><para>zh-cn:报告连接或请求流错误。</para><para>en-us:Reports a connection or request-stream error.</para></summary>
    Error = 16
}
