using Air.Cloud.DynamicApp.Contracts.Manifest;

namespace Air.Cloud.DynamicApp.Contracts.Runtime;

/// <summary>
/// <para>zh-cn:DynamicApp Manifest 历史记录，保存发布时刻的清单快照和记录标识。</para>
/// <para>en-us:DynamicApp manifest history record that stores the published manifest snapshot and record identity.</para>
/// </summary>
public sealed class DynamicAppManifestRecord
{
    /// <summary>
    /// <para>zh-cn:记录标识，默认使用无横线 Guid。</para>
    /// <para>en-us:Record identifier; defaults to a hyphenless Guid.</para>
    /// </summary>
    public string RecordId { get; set; } = Guid.NewGuid().ToString("N");

    /// <summary>
    /// <para>zh-cn:Manifest 保存到历史存储的 UTC 时间。</para>
    /// <para>en-us:UTC timestamp when the manifest was saved to history storage.</para>
    /// </summary>
    public DateTimeOffset SavedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// <para>zh-cn:发布时的 Manifest 快照。</para>
    /// <para>en-us:Manifest snapshot captured at publish time.</para>
    /// </summary>
    public DynamicAppManifest Manifest { get; set; } = new();
}
