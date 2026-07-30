using Air.Cloud.Modules.ReverseTunnel.Common.Protocol;

namespace Air.Cloud.Modules.ReverseTunnel.Agent.Udp;

/// <summary>
/// <para>zh-cn:Agent 侧 UDP 穿透分发器。它根据 ServiceKey 查找 Agent 本地 UDP 目标，并维护按隧道流标识隔离的 UDP 流。</para>
/// <para>en-us:Agent-side UDP tunnel dispatcher. It resolves Agent-local UDP targets by ServiceKey and keeps UDP flows isolated by tunnel stream identifier.</para>
/// </summary>
public interface IReverseTunnelAgentUdpDispatcher
{
    /// <summary>
    /// <para>zh-cn:发送一个 UDP 数据报到 Agent 本地目标，并确保该流的响应读取循环已经启动。</para>
    /// <para>en-us:Sends one UDP datagram to the Agent-local target and ensures the response receive loop for the flow has started.</para>
    /// </summary>
    /// <param name="streamId">
    /// <para>zh-cn:隧道流标识。</para>
    /// <para>en-us:Tunnel stream identifier.</para>
    /// </param>
    /// <param name="payload">
    /// <para>zh-cn:UDP 数据报负载。</para>
    /// <para>en-us:UDP datagram payload.</para>
    /// </param>
    /// <param name="sendDatagramAsync">
    /// <para>zh-cn:把 Agent 本地 UDP 响应发送回 Server 的回调。</para>
    /// <para>en-us:Callback that sends Agent-local UDP responses back to the Server.</para>
    /// </param>
    /// <param name="sendErrorAsync">
    /// <para>zh-cn:通知 Server 当前 UDP 流出现错误的回调。</para>
    /// <para>en-us:Callback that notifies the Server that the UDP flow has failed.</para>
    /// </param>
    /// <param name="cancellationToken">
    /// <para>zh-cn:取消令牌。</para>
    /// <para>en-us:Cancellation token.</para>
    /// </param>
    /// <returns>
    /// <para>zh-cn:表示发送完成的异步任务。</para>
    /// <para>en-us:Task representing completion of the send operation.</para>
    /// </returns>
    Task SendAsync(
        long streamId,
        ReverseTunnelUdpDatagramPayload payload,
        Func<ReverseTunnelUdpDatagramPayload, CancellationToken, Task> sendDatagramAsync,
        Func<string, string, CancellationToken, Task> sendErrorAsync,
        CancellationToken cancellationToken);

    /// <summary>
    /// <para>zh-cn:关闭指定 UDP 流，并释放 Agent 本地 UDP socket。</para>
    /// <para>en-us:Closes the specified UDP flow and releases the Agent-local UDP socket.</para>
    /// </summary>
    /// <param name="streamId">
    /// <para>zh-cn:隧道流标识。</para>
    /// <para>en-us:Tunnel stream identifier.</para>
    /// </param>
    void Close(long streamId);
}
