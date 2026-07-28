using Air.Cloud.DynamicApp.Contracts.Runtime;

namespace Air.Cloud.DynamicApp.Manager;

/// <summary>
/// <para>zh-cn:DynamicApp Mod 压缩包保存请求，描述上传包所属目标、可选入口文件和覆盖策略。</para>
/// <para>en-us:DynamicApp Mod package save request describing the target, optional entry file, and overwrite policy for an uploaded package.</para>
/// </summary>
public sealed class DynamicAppModPackageSaveRequest
{
    /// <summary>
    /// <para>zh-cn:压缩包对应的 DynamicApp/Mod/Version 目标键。</para>
    /// <para>en-us:DynamicApp/Mod/Version target key for the package.</para>
    /// </summary>
    public DynamicModVersionKey Key { get; set; }

    /// <summary>
    /// <para>zh-cn:上传时的原始文件名，仅用于审计显示，不参与路径拼接。</para>
    /// <para>en-us:Original uploaded file name used only for audit display and never for path composition.</para>
    /// </summary>
    public string OriginalFileName { get; set; } = string.Empty;

    /// <summary>
    /// <para>zh-cn:压缩包内的入口文件相对路径，例如 air.cloud.auth.entry.dll；为空时由包存储在解压目录中推断第一个 .dll。</para>
    /// <para>en-us:Relative entry file path inside the package, such as air.cloud.auth.entry.dll; when empty, the package store infers the first .dll from the extracted directory.</para>
    /// </summary>
    public string EntryFile { get; set; } = string.Empty;

    /// <summary>
    /// <para>zh-cn:是否覆盖该目标版本已经存在的包目录；仍会受到全局 AllowOverwrite 限制。</para>
    /// <para>en-us:Whether to overwrite an existing package directory for the target version; still constrained by the global AllowOverwrite option.</para>
    /// </summary>
    public bool Overwrite { get; set; }
}
