namespace Air.Cloud.Modules.ReverseTunnel.Common.Protocol;

/// <summary>
/// <para>zh-cn:反向隧道首版协议常量。当前版本仅定义 HTTP 请求转发所需的帧语义，传输层会在后续阶段实现为 gRPC 双向流。</para>
/// <para>en-us:Reverse tunnel first-version protocol constants. This version defines frame semantics required for HTTP forwarding; a later phase will implement the transport as a bidirectional gRPC stream.</para>
/// </summary>
public static class ReverseTunnelProtocol
{
    /// <summary>
    /// <para>zh-cn:当前受支持的协议版本。</para>
    /// <para>en-us:The currently supported protocol version.</para>
    /// </summary>
    public const int CurrentVersion = 1;
}
