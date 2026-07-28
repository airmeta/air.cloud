namespace Air.Cloud.DynamicApp.Manager;

/// <summary>
/// <para>zh-cn:DynamicApp Mod 包文件视图，描述包管理器中展示的单个服务端文件。</para>
/// <para>en-us:DynamicApp Mod package file view describing one server-side file shown by the package manager.</para>
/// </summary>
public sealed class DynamicAppModPackageFileView
{
    /// <summary>
    /// <para>zh-cn:文件名，不包含父级目录。</para>
    /// <para>en-us:File name without parent directories.</para>
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// <para>zh-cn:相对于包存储根目录的文件路径，用于人工查验包内容。</para>
    /// <para>en-us:File path relative to the package-store root, used for manual package inspection.</para>
    /// </summary>
    public string RelativePath { get; set; } = string.Empty;

    /// <summary>
    /// <para>zh-cn:服务端绝对文件路径，仅在通过 Manager Ticket 授权后返回。</para>
    /// <para>en-us:Absolute server-side file path, returned only after Manager Ticket authorization.</para>
    /// </summary>
    public string FullPath { get; set; } = string.Empty;

    /// <summary>
    /// <para>zh-cn:文件大小，单位为字节。</para>
    /// <para>en-us:File size in bytes.</para>
    /// </summary>
    public long Length { get; set; }

    /// <summary>
    /// <para>zh-cn:文件最后修改时间，使用 UTC。</para>
    /// <para>en-us:Last modified file time in UTC.</para>
    /// </summary>
    public DateTimeOffset LastModifiedUtc { get; set; }

    /// <summary>
    /// <para>zh-cn:文件类型，通常为 Package 或 Content。</para>
    /// <para>en-us:File kind, usually Package or Content.</para>
    /// </summary>
    public string Kind { get; set; } = string.Empty;
}
