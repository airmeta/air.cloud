using Air.Cloud.Modules.ReverseTunnel.Common.Options;

namespace Air.Cloud.Modules.ReverseTunnel.Common.Runtime;

/// <summary>
/// <para>zh-cn:AirTunnel 运行期配置变更事件参数。事件携带不可由调用方修改的配置快照和单调递增版本号。</para>
/// <para>en-us:AirTunnel runtime-options change event arguments. The event carries a caller-immutable options snapshot and a monotonically increasing version.</para>
/// </summary>
public sealed class ReverseTunnelOptionsChangedEventArgs : EventArgs
{
    /// <summary>
    /// <para>zh-cn:创建运行期配置变更事件参数。</para>
    /// <para>en-us:Creates runtime-options change event arguments.</para>
    /// </summary>
    /// <param name="options"><para>zh-cn:变更后的配置快照。</para><para>en-us:The changed options snapshot.</para></param>
    /// <param name="version"><para>zh-cn:变更后的运行期版本号。</para><para>en-us:The changed runtime version.</para></param>
    public ReverseTunnelOptionsChangedEventArgs(ReverseTunnelOptions options, long version)
    {
        Options = options ?? throw new ArgumentNullException(nameof(options));
        Version = version;
    }

    /// <summary><para>zh-cn:变更后的配置快照。</para><para>en-us:The changed options snapshot.</para></summary>
    public ReverseTunnelOptions Options { get; }

    /// <summary><para>zh-cn:变更后的运行期版本号。</para><para>en-us:The changed runtime version.</para></summary>
    public long Version { get; }
}
