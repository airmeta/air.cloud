namespace Air.Cloud.Modules.ReverseTunnel.Common.Protocol;

/// <summary>
/// <para>zh-cn:TCP 穿透连接关闭帧负载，用于传递可诊断的关闭原因。</para>
/// <para>en-us:Payload for closing a TCP tunnel connection, carrying a diagnosable close reason.</para>
/// </summary>
public sealed class ReverseTunnelTcpClosePayload
{
    /// <summary><para>zh-cn:关闭原因代码。</para><para>en-us:Close reason code.</para></summary>
    public string Reason { get; set; } = "closed";

    /// <summary><para>zh-cn:关闭原因说明。</para><para>en-us:Close reason message.</para></summary>
    public string Message { get; set; } = string.Empty;
}
