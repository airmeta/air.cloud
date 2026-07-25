namespace Air.Cloud.Modules.ReverseTunnel.Agent;

/// <summary>
/// <para>zh-cn:Agent 出站连接客户端。它负责用当前 mTLS 证书连接 Server 的双向流，并把 Server 下发的请求帧交给 Agent 处理器。</para>
/// <para>en-us:Agent outbound connection client. It connects to the Server bidirectional stream with the current mTLS certificate and hands Server request frames to the Agent processor.</para>
/// </summary>
public interface IReverseTunnelAgentConnectionClient
{
    /// <summary>
    /// <para>zh-cn:启动一次 Agent 到 Server 的反向隧道连接。该方法保持运行直到连接断开或取消令牌触发。</para>
    /// <para>en-us:Starts one Agent-to-Server reverse tunnel connection. The method keeps running until the connection closes or the cancellation token is triggered.</para>
    /// </summary>
    /// <param name="cancellationToken">
    /// <para>zh-cn:取消连接和心跳循环的令牌。</para>
    /// <para>en-us:Token used to cancel the connection and heartbeat loop.</para>
    /// </param>
    /// <returns>
    /// <para>zh-cn:表示连接生命周期的异步任务。</para>
    /// <para>en-us:Task representing the connection lifecycle.</para>
    /// </returns>
    Task RunOnceAsync(CancellationToken cancellationToken = default);
}
