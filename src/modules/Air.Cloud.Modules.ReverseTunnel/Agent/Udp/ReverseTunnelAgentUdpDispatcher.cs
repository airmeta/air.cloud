using Air.Cloud.Modules.ReverseTunnel.Common.Options;
using Air.Cloud.Modules.ReverseTunnel.Common.Protocol;
using Air.Cloud.Modules.ReverseTunnel.Common.Runtime;

using Microsoft.Extensions.Options;

using System.Collections.Concurrent;
using System.Net.Sockets;

namespace Air.Cloud.Modules.ReverseTunnel.Agent.Udp;

/// <summary>
/// <para>zh-cn:默认 Agent 侧 UDP 穿透分发器。它按隧道流标识复用 UDP socket，并在 Server 关闭流表时释放资源。</para>
/// <para>en-us:Default Agent-side UDP tunnel dispatcher. It reuses UDP sockets by tunnel stream identifier and releases resources when the Server closes a flow.</para>
/// </summary>
public sealed class ReverseTunnelAgentUdpDispatcher : IReverseTunnelAgentUdpDispatcher
{
    private readonly IReverseTunnelRuntimeOptionsAccessor options;
    private readonly ConcurrentDictionary<long, UdpFlow> flows = new();

    /// <summary>
    /// <para>zh-cn:创建 UDP 穿透分发器。</para>
    /// <para>en-us:Creates a UDP tunnel dispatcher.</para>
    /// </summary>
    /// <param name="startupOptions">
    /// <para>zh-cn:包含 Agent 本地 UDP 服务目标的 AirTunnel 配置。</para>
    /// <para>en-us:AirTunnel options containing Agent-local UDP targets.</para>
    /// </param>
    /// <param name="runtimeOptions">
    /// <para>zh-cn:可选的运行期配置访问器。</para>
    /// <para>en-us:Optional runtime options accessor.</para>
    /// </param>
    public ReverseTunnelAgentUdpDispatcher(
        IOptions<ReverseTunnelOptions> startupOptions,
        IReverseTunnelRuntimeOptionsAccessor? runtimeOptions = null)
    {
        this.options = runtimeOptions ?? new ReverseTunnelRuntimeOptionsAccessor(startupOptions);
    }

    /// <inheritdoc />
    public async Task SendAsync(
        long streamId,
        ReverseTunnelUdpDatagramPayload payload,
        Func<ReverseTunnelUdpDatagramPayload, CancellationToken, Task> sendDatagramAsync,
        Func<string, string, CancellationToken, Task> sendErrorAsync,
        CancellationToken cancellationToken)
    {
        var service = options.Current.Udp.LocalServices.FirstOrDefault(item => string.Equals(item.ServiceKey, payload.ServiceKey, StringComparison.OrdinalIgnoreCase));
        if (service is null)
        {
            await sendErrorAsync("udp_service_not_found", $"UDP ServiceKey '{payload.ServiceKey}' is not configured on the Agent.", cancellationToken);
            return;
        }

        var flow = flows.GetOrAdd(streamId, _ => CreateFlow(streamId, payload, service, sendDatagramAsync, sendErrorAsync));
        try
        {
            await flow.Client.SendAsync(payload.Data, payload.Data.Length);
        }
        catch (Exception exception)
        {
            Close(streamId);
            await sendErrorAsync("udp_local_dispatch_failed", exception.Message, CancellationToken.None);
        }
    }

    /// <inheritdoc />
    public void Close(long streamId)
    {
        if (!flows.TryRemove(streamId, out var flow))
        {
            return;
        }

        flow.Dispose();
    }

    private UdpFlow CreateFlow(
        long streamId,
        ReverseTunnelUdpDatagramPayload payload,
        ReverseTunnelUdpLocalServiceOptions service,
        Func<ReverseTunnelUdpDatagramPayload, CancellationToken, Task> sendDatagramAsync,
        Func<string, string, CancellationToken, Task> sendErrorAsync)
    {
        var client = new UdpClient();
        client.Connect(service.Host, service.Port);
        var cancellation = new CancellationTokenSource();
        var flow = new UdpFlow(client, cancellation);
        _ = ReceiveLoopAsync(streamId, payload.ServiceKey, payload.ListenerName, flow, sendDatagramAsync, sendErrorAsync);
        return flow;
    }

    private async Task ReceiveLoopAsync(
        long streamId,
        string serviceKey,
        string listenerName,
        UdpFlow flow,
        Func<ReverseTunnelUdpDatagramPayload, CancellationToken, Task> sendDatagramAsync,
        Func<string, string, CancellationToken, Task> sendErrorAsync)
    {
        try
        {
            while (!flow.Cancellation.IsCancellationRequested)
            {
                var result = await flow.Client.ReceiveAsync(flow.Cancellation.Token);
                await sendDatagramAsync(new ReverseTunnelUdpDatagramPayload
                {
                    ServiceKey = serviceKey,
                    ListenerName = listenerName,
                    Data = result.Buffer
                }, flow.Cancellation.Token);
            }
        }
        catch (OperationCanceledException) when (flow.Cancellation.IsCancellationRequested)
        {
        }
        catch (Exception exception)
        {
            Close(streamId);
            await sendErrorAsync("udp_local_receive_failed", exception.Message, CancellationToken.None);
        }
    }

    private sealed class UdpFlow : IDisposable
    {
        public UdpFlow(UdpClient client, CancellationTokenSource cancellation)
        {
            Client = client;
            Cancellation = cancellation;
        }

        public UdpClient Client { get; }

        public CancellationTokenSource Cancellation { get; }

        public void Dispose()
        {
            Cancellation.Cancel();
            Client.Dispose();
            Cancellation.Dispose();
        }
    }
}
