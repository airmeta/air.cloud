namespace Air.Cloud.DynamicApp.Contracts.Runtime;

/// <summary>
/// <para>zh-cn:DynamicApp 已归档目标记录，用于隔离坏包、错�?Manifest 或明确不应再被面板默认展示和启动�?Mod 版本。</para>
/// <para>en-us:DynamicApp archived target record used to isolate bad packages, invalid manifests, or Mod versions that should no longer be shown by default or started from the panel.</para>
/// </summary>
public sealed class DynamicAppArchivedTarget
{
    /// <summary>
    /// <para>zh-cn:被归档的 Mod 版本键。</para>
    /// <para>en-us:The archived Mod version key.</para>
    /// </summary>
    public DynamicModVersionKey Key { get; set; }

    /// <summary>
    /// <para>zh-cn:归档原因；通常记录坏包、错�?Manifest 或人工治理说明。</para>
    /// <para>en-us:The archive reason, usually describing the bad package, invalid manifest, or manual governance decision.</para>
    /// </summary>
    public string Reason { get; set; } = string.Empty;

    /// <summary>
    /// <para>zh-cn:归档发生�?UTC 时间。</para>
    /// <para>en-us:The UTC time when the target was archived.</para>
    /// </summary>
    public DateTimeOffset ArchivedAtUtc { get; set; }
}
