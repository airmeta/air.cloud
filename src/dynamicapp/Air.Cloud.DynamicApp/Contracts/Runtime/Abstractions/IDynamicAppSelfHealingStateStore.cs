namespace Air.Cloud.DynamicApp.Contracts.Runtime;

/// <summary>
/// <para>zh-cn:DynamicApp 自愈状态存储接口，负责持久�?Process 自愈重试计数和最近失败上下文。</para>
/// <para>en-us:DynamicApp self-healing state store interface that persists Process self-healing retry counters and latest failure context.</para>
/// </summary>
public interface IDynamicAppSelfHealingStateStore
{
    /// <summary>
    /// <para>zh-cn:按目标键读取一条自愈状态快照；不存在时返回 null。</para>
    /// <para>en-us:Reads one self-healing state snapshot by target key; returns null when it does not exist.</para>
    /// </summary>
    /// <param name="key">
    /// <para>zh-cn:目标 Mod 版本键。</para>
    /// <para>en-us:The target Mod version key.</para>
    /// </param>
    /// <returns>
    /// <para>zh-cn:自愈状态快照副本，�?null。</para>
    /// <para>en-us:A copy of the self-healing state snapshot, or null.</para>
    /// </returns>
    DynamicAppSelfHealingState? Get(DynamicModVersionKey key);

    /// <summary>
    /// <para>zh-cn:新增或更新一条自愈状态，并由实现决定是否立即持久化。</para>
    /// <para>en-us:Adds or updates one self-healing state and lets the implementation decide whether to persist immediately.</para>
    /// </summary>
    /// <param name="state">
    /// <para>zh-cn:需要保存的自愈状态。</para>
    /// <para>en-us:The self-healing state to save.</para>
    /// </param>
    void Upsert(DynamicAppSelfHealingState state);

    /// <summary>
    /// <para>zh-cn:移除指定目标的自愈状态；常用于目标恢复健康、人工重置或归档坏版本。</para>
    /// <para>en-us:Removes the self-healing state for the specified target; commonly used after recovery, manual reset, or bad-version archiving.</para>
    /// </summary>
    /// <param name="key">
    /// <para>zh-cn:目标 Mod 版本键。</para>
    /// <para>en-us:The target Mod version key.</para>
    /// </param>
    /// <returns>
    /// <para>zh-cn:是否实际移除了状态。</para>
    /// <para>en-us:Whether a state entry was actually removed.</para>
    /// </returns>
    bool Remove(DynamicModVersionKey key);

    /// <summary>
    /// <para>zh-cn:列出所有自愈状态快照副本。</para>
    /// <para>en-us:Lists copies of all self-healing state snapshots.</para>
    /// </summary>
    /// <returns>
    /// <para>zh-cn:自愈状态快照集合。</para>
    /// <para>en-us:The self-healing state snapshot collection.</para>
    /// </returns>
    IReadOnlyCollection<DynamicAppSelfHealingState> List();
}
