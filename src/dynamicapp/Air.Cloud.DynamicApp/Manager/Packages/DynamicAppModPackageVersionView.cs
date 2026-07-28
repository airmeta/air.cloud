namespace Air.Cloud.DynamicApp.Manager;

/// <summary>
/// <para>zh-cn:DynamicApp Mod 包版本视图，描述一个版本目录中的原始压缩包和解压文件。</para>
/// <para>en-us:DynamicApp Mod package version view describing the original archive and extracted files inside one version directory.</para>
/// </summary>
public sealed class DynamicAppModPackageVersionView
{
    /// <summary>
    /// <para>zh-cn:DynamicApp 应用标识。</para>
    /// <para>en-us:DynamicApp application identifier.</para>
    /// </summary>
    public string DynamicAppId { get; set; } = string.Empty;

    /// <summary>
    /// <para>zh-cn:Mod 标识。</para>
    /// <para>en-us:Mod identifier.</para>
    /// </summary>
    public string ModId { get; set; } = string.Empty;

    /// <summary>
    /// <para>zh-cn:包版本号，对应包存储中的版本目录名。</para>
    /// <para>en-us:Package version, matching the version directory name in the package store.</para>
    /// </summary>
    public string Version { get; set; } = string.Empty;

    /// <summary>
    /// <para>zh-cn:服务端原始压缩包绝对路径；文件不存在时为空。</para>
    /// <para>en-us:Absolute server-side original archive path; empty when the file does not exist.</para>
    /// </summary>
    public string PackageFilePath { get; set; } = string.Empty;

    /// <summary>
    /// <para>zh-cn:服务端解压目录绝对路径；目录不存在时为空。</para>
    /// <para>en-us:Absolute server-side extraction directory; empty when the directory does not exist.</para>
    /// </summary>
    public string ExtractedDirectory { get; set; } = string.Empty;

    /// <summary>
    /// <para>zh-cn:原始压缩包大小，单位为字节；文件不存在时为 0。</para>
    /// <para>en-us:Original archive size in bytes; zero when the file does not exist.</para>
    /// </summary>
    public long PackageBytes { get; set; }

    /// <summary>
    /// <para>zh-cn:版本目录最近一次保存或修改时间，使用 UTC。</para>
    /// <para>en-us:Latest save or modification time of the version directory in UTC.</para>
    /// </summary>
    public DateTimeOffset SavedAtUtc { get; set; }

    /// <summary>
    /// <para>zh-cn:该版本目录下可查验的所有文件，包含 package.zip 和 content 目录中的解压文件。</para>
    /// <para>en-us:All inspectable files in this version directory, including package.zip and extracted files under content.</para>
    /// </summary>
    public IReadOnlyCollection<DynamicAppModPackageFileView> Files { get; set; } = Array.Empty<DynamicAppModPackageFileView>();
}
