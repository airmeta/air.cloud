namespace Air.Cloud.Modules.ReverseTunnel.Common.Protocol;

/// <summary>
/// <para>zh-cn:AirTunnel 协议常量。当前协议版本通过 gRPC 双向流承载控制帧、HTTP 请求/响应流、TCP 字节流和 UDP 数据报帧。</para>
/// <para>en-us:AirTunnel protocol constants. The current protocol version uses bidirectional gRPC streams to carry control frames, HTTP request/response streams, TCP byte streams, and UDP datagram frames.</para>
/// </summary>
public static class ReverseTunnelProtocol
{
    /// <summary>
    /// <para>zh-cn:当前受支持的协议版本。</para>
    /// <para>en-us:The currently supported protocol version.</para>
    /// </summary>
    public const int CurrentVersion = 1;
}
