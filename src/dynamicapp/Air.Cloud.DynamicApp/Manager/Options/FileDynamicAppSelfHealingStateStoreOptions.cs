namespace Air.Cloud.DynamicApp.Manager;

/// <summary>
/// <para>zh-cn:文件型 DynamicApp 自愈状态存储选项。</para>
/// <para>en-us:Options for the file-based DynamicApp self-healing state store.</para>
/// </summary>
public sealed class FileDynamicAppSelfHealingStateStoreOptions
{
    /// <summary>
    /// <para>zh-cn:自愈状态文件路径；为空时使用 AppContext.BaseDirectory 下的 dynamicapp-self-healing.json。</para>
    /// <para>en-us:Self-healing state file path; dynamicapp-self-healing.json under AppContext.BaseDirectory is used when empty.</para>
    /// </summary>
    public string? FilePath { get; set; }

    /// <summary>
    /// <para>zh-cn:每次状态变更后是否立即保存文件，默认 true。</para>
    /// <para>en-us:Whether to save the file immediately after each state change, true by default.</para>
    /// </summary>
    public bool SaveOnChange { get; set; } = true;
}
