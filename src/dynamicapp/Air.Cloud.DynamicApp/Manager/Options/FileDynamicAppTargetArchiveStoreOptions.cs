namespace Air.Cloud.DynamicApp.Manager;

/// <summary>
/// <para>zh-cn:文件型 DynamicApp 目标归档存储选项。</para>
/// <para>en-us:Options for the file-based DynamicApp target archive store.</para>
/// </summary>
public sealed class FileDynamicAppTargetArchiveStoreOptions
{
    /// <summary>
    /// <para>zh-cn:归档文件路径；为空时使用 AppContext.BaseDirectory 下的 dynamicapp-archives.json。</para>
    /// <para>en-us:Archive file path; dynamicapp-archives.json under AppContext.BaseDirectory is used when empty.</para>
    /// </summary>
    public string? FilePath { get; set; }

    /// <summary>
    /// <para>zh-cn:每次归档状态变更后是否立即保存文件，默认 true。</para>
    /// <para>en-us:Whether to save the file immediately after each archive-state change, true by default.</para>
    /// </summary>
    public bool SaveOnChange { get; set; } = true;
}
