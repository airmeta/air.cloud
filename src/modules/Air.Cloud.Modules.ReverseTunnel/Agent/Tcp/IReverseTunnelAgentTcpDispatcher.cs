using Air.Cloud.Modules.ReverseTunnel.Common.Protocol;

using System.Threading.Channels;

namespace Air.Cloud.Modules.ReverseTunnel.Agent.Tcp;

/// <summary>
/// <para>zh-cn:Agent 侧 TCP 穿透分发器。它根据 ServiceKey 查找 Agent 本地 TCP 目标，并在本地 socket 与隧道帧之间做双向转发。</para>
/// <para>en-us:Agent-side TCP tunnel dispatcher. It resolves the Agent-local TCP target by ServiceKey and relays bidirectionally between the local socket and tunnel frames.</para>
/// </summary>
public interface IReverseTunnelAgentTcpDispatcher
{
    /// <summary>
    /// <para>zh-cn:打开并运行一条 TCP 穿透连接。方法在任一方向关闭、出错或取消后返回，并负责释放本地 TCP 连接。</para>
    /// <para>en-us:Opens and runs a TCP tunnel connection. The method returns after either direction closes, fails, or is cancelled, and releases the local TCP connection.</para>
    /// </summary>
    /// <param name="payload">
    /// <para>zh-cn:TCP 打开帧负载。</para>
    /// <para>en-us:TCP open frame payload.</para>
    /// </param>
    /// <param name="inboundFrames">
    /// <para>zh-cn:Server 发往 Agent 本地目标的数据帧。</para>
    /// <para>en-us:Data frames sent by the Server to the Agent-local target.</para>
    /// </param>
    /// <param name="sendDataAsync">
    /// <para>zh-cn:把 Agent 本地目标返回的数据发送给 Server 的回调。</para>
    /// <para>en-us:Callback that sends data returned by the Agent-local target back to the Server.</para>
    /// </param>
    /// <param name="sendCloseAsync">
    /// <para>zh-cn:通知 Server 当前 TCP 连接已经关闭的回调。</para>
    /// <para>en-us:Callback that notifies the Server that the TCP connection has closed.</para>
    /// </param>
    /// <param name="sendErrorAsync">
    /// <para>zh-cn:通知 Server 当前 TCP 连接出现错误的回调。</para>
    /// <para>en-us:Callback that notifies the Server that the TCP connection has failed.</para>
    /// </param>
    /// <param name="cancellationToken">
    /// <para>zh-cn:取消令牌，用于关闭本地 socket 和停止双向转发。</para>
    /// <para>en-us:Cancellation token used to close the local socket and stop bidirectional relay.</para>
    /// </param>
    /// <returns>
    /// <para>zh-cn:表示 TCP 穿透连接生命周期的异步任务。</para>
    /// <para>en-us:Task representing the TCP tunnel connection lifecycle.</para>
    /// </returns>
    Task DispatchAsync(
        ReverseTunnelTcpOpenPayload payload,
        ChannelReader<byte[]> inboundFrames,
        Func<ReadOnlyMemory<byte>, CancellationToken, Task> sendDataAsync,
        Func<CancellationToken, Task> sendCloseAsync,
        Func<string, string, CancellationToken, Task> sendErrorAsync,
        CancellationToken cancellationToken);
}
