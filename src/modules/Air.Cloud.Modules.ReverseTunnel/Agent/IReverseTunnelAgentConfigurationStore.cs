using Air.Cloud.Modules.ReverseTunnel.Common.Runtime;

namespace Air.Cloud.Modules.ReverseTunnel.Agent;

/// <summary>
/// <para>zh-cn:Agent 最后一次有效配置快照存储。 en-us:Store for the Agent's last valid runtime configuration snapshot.</para>
/// </summary>
public interface IReverseTunnelAgentConfigurationStore
{
    /// <summary><para>zh-cn:当前已应用的远端配置版本。 en-us:Currently applied remote configuration version.</para></summary>
    long CurrentVersion { get; }

    /// <summary><para>zh-cn:读取本地快照。 en-us:Reads the local snapshot.</para></summary>
    Task<ReverseTunnelAgentConfigurationSnapshot?> LoadAsync(CancellationToken cancellationToken = default);

    /// <summary><para>zh-cn:原子保存快照。 en-us:Atomically saves the snapshot.</para></summary>
    Task SaveAsync(ReverseTunnelAgentConfigurationSnapshot snapshot, CancellationToken cancellationToken = default);
}
