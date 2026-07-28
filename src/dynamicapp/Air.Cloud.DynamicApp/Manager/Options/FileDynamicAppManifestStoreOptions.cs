namespace Air.Cloud.DynamicApp.Manager;

/// <summary>
/// <para>zh-cn:文件型 Manifest 历史存储选项。</para>
/// <para>en-us:Options for the file-based manifest history store.</para>
/// </summary>
public sealed class FileDynamicAppManifestStoreOptions
{
    /// <summary>
    /// <para>zh-cn:Manifest 历史文件路径；为空时使用应用基目录下的 dynamicapp-manifests.json。</para>
    /// <para>en-us:Manifest history file path; when empty, dynamicapp-manifests.json under the application base directory is used.</para>
    /// </summary>
    public string? FilePath { get; set; }

    /// <summary>
    /// <para>zh-cn:单个 DynamicApp 最多保留的历史记录数，默认 50。</para>
    /// <para>en-us:Maximum retained history records for one DynamicApp; defaults to 50.</para>
    /// </summary>
    public int MaxRecordsPerApp { get; set; } = 50;
}
