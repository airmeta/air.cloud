using Air.Cloud.Modules.ReverseTunnel.Common.Options;

namespace Air.Cloud.Modules.ReverseTunnel.Common.Runtime;

/// <summary>
/// <para>zh-cn:AirTunnel 运行期配置访问器。消费者应通过该接口读取当前配置，使管理端、拆分配置文件和配置 reload 能在不中断进程的情况下影响新请求。</para>
/// <para>en-us:AirTunnel runtime-options accessor. Consumers should read current settings through this interface so the management endpoint, split configuration files, and configuration reloads can affect new work without restarting the process.</para>
/// </summary>
public interface IReverseTunnelRuntimeOptionsAccessor : Microsoft.Extensions.Options.IOptions<ReverseTunnelOptions>
{
    /// <summary><para>zh-cn:当前运行期配置快照。调用方不得修改返回实例。</para><para>en-us:Current runtime options snapshot. Callers must not mutate the returned instance.</para></summary>
    ReverseTunnelOptions Current { get; }

    /// <summary><para>zh-cn:当前配置版本号。每次成功更新都会递增。</para><para>en-us:Current options version. Each successful update increments it.</para></summary>
    long Version { get; }

    /// <summary><para>zh-cn:配置变更事件。事件处理器不应长时间阻塞。</para><para>en-us:Options changed event. Handlers should not block for a long time.</para></summary>
    event EventHandler<ReverseTunnelOptionsChangedEventArgs>? Changed;

    /// <summary>
    /// <para>zh-cn:替换运行期配置快照，标准化配置值并触发变更事件。该方法不会直接写入磁盘。</para>
    /// <para>en-us:Replaces the runtime options snapshot, normalizes values, and raises the change event. This method does not write to disk.</para>
    /// </summary>
    /// <param name="options"><para>zh-cn:新的配置。</para><para>en-us:The new options.</para></param>
    /// <returns><para>zh-cn:标准化后的配置快照。</para><para>en-us:The normalized options snapshot.</para></returns>
    ReverseTunnelOptions Update(ReverseTunnelOptions options);
}
