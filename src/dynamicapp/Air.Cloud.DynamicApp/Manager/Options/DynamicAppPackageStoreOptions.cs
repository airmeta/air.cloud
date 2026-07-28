namespace Air.Cloud.DynamicApp.Manager;

/// <summary>
/// <para>zh-cn:DynamicApp Mod 包文件存储选项，用于限制上传目录、包大小和覆盖策略。</para>
/// <para>en-us:DynamicApp Mod package storage options used to constrain upload directories, package size, and overwrite behavior.</para>
/// </summary>
public sealed class DynamicAppPackageStoreOptions
{
    /// <summary>
    /// <para>zh-cn:Mod 包和解压产物的根目录；为空时使用应用基目录下的 dynamicapp-packages。</para>
    /// <para>en-us:Root directory for Mod packages and extracted artifacts; dynamicapp-packages under the application base directory is used when empty.</para>
    /// </summary>
    public string RootPath { get; set; } = Path.Combine(AppContext.BaseDirectory, "dynamicapp-packages");

    /// <summary>
    /// <para>zh-cn:允许上传的最大压缩包字节数，默认 200 MB。</para>
    /// <para>en-us:Maximum allowed uploaded package size in bytes, 200 MB by default.</para>
    /// </summary>
    public long MaxPackageBytes { get; set; } = 200 * 1024 * 1024;

    /// <summary>
    /// <para>zh-cn:是否允许同一 DynamicApp/Mod/Version 覆盖已有包目录；默认不允许。</para>
    /// <para>en-us:Whether the same DynamicApp/Mod/Version may overwrite an existing package directory; disabled by default.</para>
    /// </summary>
    public bool AllowOverwrite { get; set; }
}
