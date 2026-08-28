using Air.Cloud.Modules.ReverseTunnel.Common.Options;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Air.Cloud.Modules.ReverseTunnel.Common.Runtime;

/// <summary>
/// <para>zh-cn:把 .NET 配置 reload 同步到 AirTunnel 运行期配置访问器的后台服务。拆分 JSON 文件开启 reloadOnChange 后，新配置会通过该服务进入运行期快照。</para>
/// <para>en-us:Background service that synchronizes .NET configuration reloads into the AirTunnel runtime-options accessor. When split JSON files use reloadOnChange, new settings flow into the runtime snapshot through this service.</para>
/// </summary>
public sealed class ReverseTunnelRuntimeOptionsMonitorHostedService : IHostedService, IDisposable
{
    private readonly IOptionsMonitor<ReverseTunnelOptions> optionsMonitor;
    private readonly IReverseTunnelRuntimeOptionsAccessor runtimeOptions;
    private IDisposable? changeRegistration;

    /// <summary>
    /// <para>zh-cn:创建配置 reload 同步服务。</para>
    /// <para>en-us:Creates the configuration-reload synchronization service.</para>
    /// </summary>
    /// <param name="optionsMonitor"><para>zh-cn:.NET 配置监视器。</para><para>en-us:The .NET options monitor.</para></param>
    /// <param name="runtimeOptions"><para>zh-cn:AirTunnel 运行期配置访问器。</para><para>en-us:The AirTunnel runtime-options accessor.</para></param>
    public ReverseTunnelRuntimeOptionsMonitorHostedService(
        IOptionsMonitor<ReverseTunnelOptions> optionsMonitor,
        IReverseTunnelRuntimeOptionsAccessor runtimeOptions)
    {
        this.optionsMonitor = optionsMonitor;
        this.runtimeOptions = runtimeOptions;
    }

    /// <inheritdoc />
    public Task StartAsync(CancellationToken cancellationToken)
    {
        changeRegistration = optionsMonitor.OnChange(options => runtimeOptions.Update(options));
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task StopAsync(CancellationToken cancellationToken)
    {
        changeRegistration?.Dispose();
        changeRegistration = null;
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        changeRegistration?.Dispose();
    }
}
