using Air.Cloud.Modules.ReverseTunnel.Common.Options;
using Air.Cloud.Modules.ReverseTunnel.Common.Protocol;
using Air.Cloud.Modules.ReverseTunnel.Common.Runtime;

using Microsoft.Extensions.Options;

using System.Net.Sockets;
using System.Threading.Channels;

namespace Air.Cloud.Modules.ReverseTunnel.Agent.Tcp;

/// <summary>
/// <para>zh-cn:默认 Agent 侧 TCP 穿透分发器。目标地址只从 `ReverseTunnelSettings:Tcp:LocalServices` 读取。</para>
/// <para>en-us:Default Agent-side TCP tunnel dispatcher. Target addresses are read only from `ReverseTunnelSettings:Tcp:LocalServices`.</para>
/// </summary>
public sealed class ReverseTunnelAgentTcpDispatcher : IReverseTunnelAgentTcpDispatcher
{
    private readonly IReverseTunnelRuntimeOptionsAccessor options;

    /// <summary>
    /// <para>zh-cn:创建 TCP 穿透分发器。</para>
    /// <para>en-us:Creates a TCP tunnel dispatcher.</para>
    /// </summary>
    /// <param name="startupOptions">
    /// <para>zh-cn:包含 Agent 本地 TCP 服务目标、帧大小和队列策略的 AirTunnel 配置。</para>
    /// <para>en-us:AirTunnel options containing Agent-local TCP targets, frame size, and queue policy.</para>
    /// </param>
    /// <param name="runtimeOptions">
    /// <para>zh-cn:可选的运行期配置访问器。</para>
    /// <para>en-us:Optional runtime options accessor.</para>
    /// </param>
    public ReverseTunnelAgentTcpDispatcher(
        IOptions<ReverseTunnelOptions> startupOptions,
        IReverseTunnelRuntimeOptionsAccessor? runtimeOptions = null)
    {
        this.options = runtimeOptions ?? new ReverseTunnelRuntimeOptionsAccessor(startupOptions);
    }

    /// <inheritdoc />
    public async Task DispatchAsync(
        ReverseTunnelTcpOpenPayload payload,
        ChannelReader<byte[]> inboundFrames,
        Func<ReadOnlyMemory<byte>, CancellationToken, Task> sendDataAsync,
        Func<CancellationToken, Task> sendCloseAsync,
        Func<string, string, CancellationToken, Task> sendErrorAsync,
        CancellationToken cancellationToken)
    {
        var current = options.Current;
        var service = current.Tcp.LocalServices.FirstOrDefault(item => string.Equals(item.ServiceKey, payload.ServiceKey, StringComparison.OrdinalIgnoreCase));
        if (service is null)
        {
            await sendErrorAsync("tcp_service_not_found", $"TCP ServiceKey '{payload.ServiceKey}' is not configured on the Agent.", cancellationToken);
            return;
        }

        using var tcpClient = new TcpClient();
        try
        {
            using var connectTimeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            connectTimeout.CancelAfter(TimeSpan.FromSeconds(payload.ConnectTimeoutSeconds <= 0 ? 5 : payload.ConnectTimeoutSeconds));
            await tcpClient.ConnectAsync(service.Host, service.Port, connectTimeout.Token);
            await using var networkStream = tcpClient.GetStream();
            using var linkedCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            var writeLocalTask = WriteLocalAsync(networkStream, inboundFrames, linkedCancellation.Token);
            var readLocalTask = ReadLocalAsync(networkStream, sendDataAsync, linkedCancellation.Token);
            var completed = await Task.WhenAny(writeLocalTask, readLocalTask);
            await linkedCancellation.CancelAsync();
            await completed;
            await sendCloseAsync(CancellationToken.None);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (Exception exception)
        {
            await sendErrorAsync("tcp_local_dispatch_failed", exception.Message, CancellationToken.None);
        }
    }

    private static async Task WriteLocalAsync(NetworkStream networkStream, ChannelReader<byte[]> inboundFrames, CancellationToken cancellationToken)
    {
        await foreach (var chunk in inboundFrames.ReadAllAsync(cancellationToken))
        {
            if (chunk.Length == 0)
            {
                continue;
            }

            await networkStream.WriteAsync(chunk, cancellationToken);
            await networkStream.FlushAsync(cancellationToken);
        }
    }

    private async Task ReadLocalAsync(NetworkStream networkStream, Func<ReadOnlyMemory<byte>, CancellationToken, Task> sendDataAsync, CancellationToken cancellationToken)
    {
        var buffer = new byte[Math.Max(1_024, options.Current.MaxFramePayloadBytes)];
        while (!cancellationToken.IsCancellationRequested)
        {
            var read = await networkStream.ReadAsync(buffer, cancellationToken);
            if (read <= 0)
            {
                break;
            }

            await sendDataAsync(buffer.AsMemory(0, read), cancellationToken);
        }
    }
}
