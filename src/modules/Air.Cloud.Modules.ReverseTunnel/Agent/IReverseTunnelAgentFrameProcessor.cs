using Air.Cloud.Modules.ReverseTunnel.Server.Grpc;

namespace Air.Cloud.Modules.ReverseTunnel.Agent;

/// <summary>
/// <para>zh-cn:Agent 入站隧道帧处理器。它把 Server 下发的请求帧转换为本地 HTTP 调用，并把响应帧写回 Server。</para>
/// <para>en-us:Agent inbound tunnel frame processor. It converts request frames sent by the Server into local HTTP calls and writes response frames back to the Server.</para>
/// </summary>
internal interface IReverseTunnelAgentFrameProcessor
{
    /// <summary>
    /// <para>zh-cn:处理一帧 Server 发给 Agent 的隧道消息，并通过 writeAsync 持续写回响应帧。OpenRequest 到达后即可启动本地 HTTP 调用，请求体和响应体均按帧流式转发。</para>
    /// <para>en-us:Processes one tunnel message sent by the Server to the Agent and continuously writes response frames through writeAsync. A local HTTP call can start when OpenRequest arrives, and both request and response bodies are streamed by frame.</para>
    /// </summary>
    /// <param name="frame">
    /// <para>zh-cn:Server 下发的隧道帧。</para>
    /// <para>en-us:Tunnel frame sent by the Server.</para>
    /// </param>
    /// <param name="writeAsync">
    /// <para>zh-cn:写回 Server 的响应帧回调。调用方必须保证底层 gRPC 请求流串行写入。</para>
    /// <para>en-us:Callback used to write response frames back to the Server. The caller must serialize writes to the underlying gRPC request stream.</para>
    /// </param>
    /// <param name="cancellationToken">
    /// <para>zh-cn:取消本地 HTTP 分发和帧处理的令牌。</para>
    /// <para>en-us:Token used to cancel local HTTP dispatch and frame processing.</para>
    /// </param>
    /// <returns>
    /// <para>zh-cn:当前输入帧处理完成时结束；本地 HTTP 响应可能在后台继续通过 writeAsync 输出。</para>
    /// <para>en-us:Completes when the current input frame is processed; local HTTP response output may continue in the background through writeAsync.</para>
    /// </returns>
    Task ProcessAsync(TunnelFrameMessage frame, Func<TunnelFrameMessage, CancellationToken, Task> writeAsync, CancellationToken cancellationToken = default);
}
