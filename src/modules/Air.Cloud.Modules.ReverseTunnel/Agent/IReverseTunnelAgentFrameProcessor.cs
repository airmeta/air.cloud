using Air.Cloud.Modules.ReverseTunnel.Server.Grpc;

namespace Air.Cloud.Modules.ReverseTunnel.Agent;

/// <summary>
/// <para>zh-cn:Agent 入站隧道帧处理器。它把 Server 下发的请求帧转换为本地 HTTP 调用，并生成需要写回 Server 的响应帧。</para>
/// <para>en-us:Agent inbound tunnel frame processor. It converts request frames sent by the Server into local HTTP calls and produces response frames to write back to the Server.</para>
/// </summary>
internal interface IReverseTunnelAgentFrameProcessor
{
    /// <summary>
    /// <para>zh-cn:处理一帧 Server 发给 Agent 的隧道消息，并返回零到多帧响应。请求体会在 Complete 帧到达前缓冲在内存中。</para>
    /// <para>en-us:Processes one tunnel message sent by the Server to the Agent and returns zero or more response frames. Request bodies are buffered in memory until the Complete frame arrives.</para>
    /// </summary>
    /// <param name="frame">
    /// <para>zh-cn:Server 下发的隧道帧。</para>
    /// <para>en-us:Tunnel frame sent by the Server.</para>
    /// </param>
    /// <param name="cancellationToken">
    /// <para>zh-cn:取消本地 HTTP 分发的令牌。</para>
    /// <para>en-us:Token used to cancel local HTTP dispatch.</para>
    /// </param>
    /// <returns>
    /// <para>zh-cn:需要写回 Server 的响应帧集合。</para>
    /// <para>en-us:Response frames that should be written back to the Server.</para>
    /// </returns>
    Task<IReadOnlyList<TunnelFrameMessage>> ProcessAsync(TunnelFrameMessage frame, CancellationToken cancellationToken = default);
}
