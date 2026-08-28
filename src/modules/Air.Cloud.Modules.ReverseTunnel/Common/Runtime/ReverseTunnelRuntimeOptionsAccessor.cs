using Air.Cloud.Modules.ReverseTunnel.Common.Options;

using Microsoft.Extensions.Options;

namespace Air.Cloud.Modules.ReverseTunnel.Common.Runtime;

/// <summary>
/// <para>zh-cn:默认 AirTunnel 运行期配置访问器。它以启动时绑定配置初始化，并允许管理端或配置 reload 原子替换当前快照。</para>
/// <para>en-us:Default AirTunnel runtime-options accessor. It initializes from startup-bound options and allows the management endpoint or configuration reloads to atomically replace the current snapshot.</para>
/// </summary>
public sealed class ReverseTunnelRuntimeOptionsAccessor : IReverseTunnelRuntimeOptionsAccessor
{
    private readonly object updateLock = new();
    private ReverseTunnelOptions current;
    private long version;

    /// <summary>
    /// <para>zh-cn:创建运行期配置访问器，并复制启动阶段绑定的 `ReverseTunnelSettings`。</para>
    /// <para>en-us:Creates the runtime-options accessor and copies the startup-bound `ReverseTunnelSettings`.</para>
    /// </summary>
    /// <param name="options"><para>zh-cn:启动阶段绑定的 AirTunnel 配置。</para><para>en-us:Startup-bound AirTunnel options.</para></param>
    public ReverseTunnelRuntimeOptionsAccessor(IOptions<ReverseTunnelOptions> options)
    {
        current = ReverseTunnelOptionsCloner.CloneAndNormalize(options.Value);
    }

    /// <inheritdoc />
    public ReverseTunnelOptions Current => current;

    /// <summary>
    /// <para>zh-cn:兼容标准 IOptions 的当前配置访问属性。</para>
    /// <para>en-us:Current-options property required by the standard IOptions contract.</para>
    /// </summary>
    public ReverseTunnelOptions Value => Current;

    /// <inheritdoc />
    public long Version => Interlocked.Read(ref version);

    /// <inheritdoc />
    public event EventHandler<ReverseTunnelOptionsChangedEventArgs>? Changed;

    /// <inheritdoc />
    public ReverseTunnelOptions Update(ReverseTunnelOptions options)
    {
        var normalized = ReverseTunnelOptionsCloner.CloneAndNormalize(options);
        long nextVersion;
        lock (updateLock)
        {
            current = normalized;
            nextVersion = Interlocked.Increment(ref version);
        }

        Changed?.Invoke(this, new ReverseTunnelOptionsChangedEventArgs(normalized, nextVersion));
        return normalized;
    }
}
