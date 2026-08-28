using Air.Cloud.Modules.ReverseTunnel.Common.Runtime;
using Air.Cloud.Modules.ReverseTunnel.Common.Options;

namespace Air.Cloud.Modules.ReverseTunnel.Server;

/// <summary>
/// <para>zh-cn:Server 端 Agent 目标配置存储。存储内容用于 Agent 重连后的配置补发。</para>
/// <para>en-us:Server-side desired Agent configuration store. Stored snapshots are replayed when an Agent reconnects.</para>
/// </summary>
public interface IReverseTunnelDesiredAgentConfigurationStore
{
    /// <summary>
    /// <para>zh-cn:读取指定 Agent 的最后一个目标配置快照。</para>
    /// <para>en-us:Reads the latest desired configuration snapshot for an Agent.</para>
    /// </summary>
    Task<ReverseTunnelAgentConfigurationSnapshot?> GetAsync(string agentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// <para>zh-cn:保存指定 Agent 的目标配置快照。</para>
    /// <para>en-us:Saves the desired configuration snapshot for an Agent.</para>
    /// </summary>
    Task SaveAsync(ReverseTunnelAgentConfigurationSnapshot snapshot, CancellationToken cancellationToken = default);

    /// <summary>
    /// <para>zh-cn:读取全部已保存的 Agent 目标配置。</para>
    /// <para>en-us:Reads all stored desired Agent configuration snapshots.</para>
    /// </summary>
    Task<IReadOnlyList<ReverseTunnelAgentConfigurationSnapshot>> ListAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// <para>zh-cn:按期望版本以 Compare-and-Swap 方式保存完整 Agent 配置。内置文件存储会在同一存储临界区内完成版本检查、冲突校验和保存；其他实现应覆盖此方法提供相同原子性。</para>
    /// <para>en-us:Saves a complete Agent configuration with an expected-version Compare-and-Swap. The built-in file store performs version checking, conflict validation, and persistence in one critical section; other implementations should override this method with the same atomicity.</para>
    /// </summary>
    /// <param name="agentId"><para>zh-cn:目标 AgentId。</para><para>en-us:Target AgentId.</para></param>
    /// <param name="configuration"><para>zh-cn:待保存的完整运行配置。</para><para>en-us:Complete runtime configuration to save.</para></param>
    /// <param name="expectedVersion"><para>zh-cn:页面读取配置时取得的版本；当前版本不一致时拒绝保存。</para><para>en-us:Version read by the page; the save is rejected when the current version differs.</para></param>
    /// <param name="validate"><para>zh-cn:在存储锁内执行的候选配置校验，可返回跨 Agent 冲突。</para><para>en-us:Candidate validation executed while the store lock is held; may return cross-Agent conflicts.</para></param>
    /// <param name="cancellationToken"><para>zh-cn:取消令牌。</para><para>en-us:Cancellation token.</para></param>
    /// <returns><para>zh-cn:保存结果，包含版本冲突、业务冲突或新快照。</para><para>en-us:Save result containing version conflicts, validation conflicts, or the new snapshot.</para></returns>
    async Task<ReverseTunnelDesiredAgentConfigurationSaveResult> TrySaveAsync(
        string agentId,
        ReverseTunnelOptions configuration,
        long expectedVersion,
        Func<IReadOnlyList<ReverseTunnelAgentConfigurationSnapshot>, IReadOnlyList<string>>? validate = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        var current = await GetAsync(agentId, cancellationToken).ConfigureAwait(false);
        var currentVersion = current?.Version ?? 0;
        if (currentVersion != expectedVersion)
        {
            return ReverseTunnelDesiredAgentConfigurationSaveResult.Conflict;
        }

        var existing = await ListAsync(cancellationToken).ConfigureAwait(false);
        var conflicts = validate?.Invoke(existing) ?? [];
        if (conflicts.Count > 0)
        {
            return ReverseTunnelDesiredAgentConfigurationSaveResult.ValidationConflict(conflicts);
        }

        var version = Math.Max(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(), currentVersion + 1);
        var snapshot = ReverseTunnelAgentConfigurationSnapshotSerializer.Create(agentId, configuration, version);
        await SaveAsync(snapshot, cancellationToken).ConfigureAwait(false);
        return ReverseTunnelDesiredAgentConfigurationSaveResult.Success(snapshot);
    }
}

/// <summary>
/// <para>zh-cn:完整 Agent 配置保存结果。 en-us:Result of a complete Agent configuration save.</para>
/// </summary>
public sealed record ReverseTunnelDesiredAgentConfigurationSaveResult(
    ReverseTunnelAgentConfigurationSnapshot? Snapshot,
    bool VersionConflict,
    IReadOnlyList<string> Conflicts)
{
    /// <summary><para>zh-cn:是否已成功保存新快照。 en-us:Whether a new snapshot was saved successfully.</para></summary>
    public bool Succeeded => Snapshot is not null && !VersionConflict && Conflicts.Count == 0;

    /// <summary><para>zh-cn:没有通过期望版本校验的结果。 en-us:Result indicating that the expected-version check failed.</para></summary>
    public static ReverseTunnelDesiredAgentConfigurationSaveResult Conflict { get; } = new(null, true, []);

    /// <summary><para>zh-cn:候选配置存在业务冲突的结果。 en-us:Result indicating validation conflicts in the candidate configuration.</para></summary>
    public static ReverseTunnelDesiredAgentConfigurationSaveResult ValidationConflict(IReadOnlyList<string> conflicts)
        => new(null, false, conflicts);

    /// <summary><para>zh-cn:配置已保存的结果。 en-us:Result indicating that the configuration was saved.</para></summary>
    public static ReverseTunnelDesiredAgentConfigurationSaveResult Success(ReverseTunnelAgentConfigurationSnapshot snapshot)
        => new(snapshot, false, []);
}
