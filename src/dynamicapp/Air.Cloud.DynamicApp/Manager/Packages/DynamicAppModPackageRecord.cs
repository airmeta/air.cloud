using Air.Cloud.DynamicApp.Contracts.Runtime;

namespace Air.Cloud.DynamicApp.Manager;

/// <summary>
/// <para>zh-cn:DynamicApp Mod 压缩包保存记录，描述压缩包、解压目录和可启动入口文件。</para>
/// <para>en-us:DynamicApp Mod package save record describing the archive, extraction directory, and startable entry file.</para>
/// </summary>
public sealed class DynamicAppModPackageRecord
{
    /// <summary>
    /// <para>zh-cn:压缩包对应的 DynamicApp/Mod/Version 目标键。</para>
    /// <para>en-us:DynamicApp/Mod/Version target key for the package.</para>
    /// </summary>
    public DynamicModVersionKey Key { get; set; }

    /// <summary>
    /// <para>zh-cn:上传时的原始文件名。</para>
    /// <para>en-us:Original uploaded file name.</para>
    /// </summary>
    public string OriginalFileName { get; set; } = string.Empty;

    /// <summary>
    /// <para>zh-cn:服务端保存的压缩包绝对路径。</para>
    /// <para>en-us:Absolute server-side archive path.</para>
    /// </summary>
    public string PackageFilePath { get; set; } = string.Empty;

    /// <summary>
    /// <para>zh-cn:服务端解压后的目录绝对路径。</para>
    /// <para>en-us:Absolute server-side extraction directory.</para>
    /// </summary>
    public string ExtractedDirectory { get; set; } = string.Empty;

    /// <summary>
    /// <para>zh-cn:服务端可启动入口文件绝对路径。</para>
    /// <para>en-us:Absolute server-side startable entry file path.</para>
    /// </summary>
    public string EntryFilePath { get; set; } = string.Empty;

    /// <summary>
    /// <para>zh-cn:保存的压缩包字节数。</para>
    /// <para>en-us:Saved archive size in bytes.</para>
    /// </summary>
    public long PackageBytes { get; set; }

    /// <summary>
    /// <para>zh-cn:保存时间 UTC。</para>
    /// <para>en-us:UTC save time.</para>
    /// </summary>
    public DateTimeOffset SavedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// <para>zh-cn:本次保存是否覆盖了已有目录。</para>
    /// <para>en-us:Whether this save overwrote an existing directory.</para>
    /// </summary>
    public bool Overwritten { get; set; }
}
