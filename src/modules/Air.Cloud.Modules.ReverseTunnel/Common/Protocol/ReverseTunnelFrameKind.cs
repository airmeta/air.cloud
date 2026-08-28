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

    /// <summary><para>zh-cn:打开一条 HTTP 请求流。</para><para>en-us:Opens an HTTP request stream.</para></summary>
    OpenRequest = 10,

    /// <summary><para>zh-cn:请求体分片。</para><para>en-us:Request body chunk.</para></summary>
    RequestBody = 11,

    /// <summary><para>zh-cn:响应状态与响应头。</para><para>en-us:Response status and headers.</para></summary>
    ResponseHeaders = 12,

    /// <summary><para>zh-cn:响应体分片。</para><para>en-us:Response body chunk.</para></summary>
    ResponseBody = 13,

    /// <summary><para>zh-cn:正常结束一条请求流。</para><para>en-us:Completes a request stream normally.</para></summary>
    Complete = 14,

    /// <summary><para>zh-cn:取消一条仍在执行的请求流。</para><para>en-us:Cancels an in-flight request stream.</para></summary>
    Cancel = 15,

    /// <summary><para>zh-cn:报告连接或请求流错误。</para><para>en-us:Reports a connection or request-stream error.</para></summary>
    Error = 16,

    /// <summary><para>zh-cn:打开一条 TCP 字节流隧道连接。</para><para>en-us:Opens a TCP byte-stream tunnel connection.</para></summary>
    TcpOpen = 20,

    /// <summary><para>zh-cn:TCP 字节流数据分片。</para><para>en-us:TCP byte-stream data chunk.</para></summary>
    TcpData = 21,

    /// <summary><para>zh-cn:关闭一条 TCP 字节流隧道连接。</para><para>en-us:Closes a TCP byte-stream tunnel connection.</para></summary>
    TcpClose = 22,

    /// <summary><para>zh-cn:报告 TCP 字节流隧道连接错误。</para><para>en-us:Reports a TCP byte-stream tunnel connection error.</para></summary>
    TcpError = 23,

    /// <summary><para>zh-cn:UDP 数据报隧道帧。</para><para>en-us:UDP datagram tunnel frame.</para></summary>
    UdpDatagram = 30,

    /// <summary><para>zh-cn:关闭一条 UDP 流表项。</para><para>en-us:Closes a UDP flow entry.</para></summary>
    UdpFlowClose = 31,

    /// <summary><para>zh-cn:报告 UDP 数据报隧道错误。</para><para>en-us:Reports a UDP datagram tunnel error.</para></summary>
    UdpError = 32,

    /// <summary><para>zh-cn:Server 向 Agent 提供运行期配置快照。</para><para>en-us:Offers a runtime configuration snapshot from the Server to the Agent.</para></summary>
    ConfigurationOffer = 40,

    /// <summary><para>zh-cn:Agent 确认已接受并应用运行期配置快照。</para><para>en-us:Confirms that the Agent accepted and applied a runtime configuration snapshot.</para></summary>
    ConfigurationAck = 41,

    /// <summary><para>zh-cn:Agent 拒绝未通过校验或持久化的运行期配置快照。</para><para>en-us:Rejects a runtime configuration snapshot that failed validation or persistence.</para></summary>
    ConfigurationReject = 42
}
