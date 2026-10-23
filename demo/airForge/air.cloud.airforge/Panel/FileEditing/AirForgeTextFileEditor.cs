using System.Text;

using Air.Cloud.DynamicApp.Manager;

namespace Air.Cloud.AirForge.Panel.FileEditing;

/// <summary>
/// <para>zh-cn:AirForge 受控文本文件编辑器，只允许访问应用基目录和 DynamicApp 包根目录内的白名单文本文件。</para>
/// <para>en-us:Controlled AirForge text-file editor that only accesses allowlisted text files under the application base directory and DynamicApp package root.</para>
/// </summary>
public sealed class AirForgeTextFileEditor
{
    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".txt", ".yaml", ".yml", ".json", ".properties", ".xml", ".ini", ".conf", ".config", ".toml", ".md"
    };

    private readonly string[] _allowedRoots;
    private readonly long _maxFileBytes;

    /// <summary>
    /// <para>zh-cn:创建受控文本文件编辑器并规范化允许访问的根目录。</para>
    /// <para>en-us:Creates the controlled text-file editor and normalizes the roots that may be accessed.</para>
    /// </summary>
    /// <param name="panelOptions">
    /// <para>zh-cn:AirForge 面板配置，提供文件大小上限。</para>
    /// <para>en-us:AirForge panel options that provide the file-size limit.</para>
    /// </param>
    /// <param name="packageStoreOptions">
    /// <para>zh-cn:DynamicApp 包存储配置，用于允许编辑已解压包中的文本配置文件。</para>
    /// <para>en-us:DynamicApp package-store options used to allow editing text configuration files inside extracted packages.</para>
    /// </param>
    public AirForgeTextFileEditor(AirForgePanelOptions panelOptions, DynamicAppPackageStoreOptions packageStoreOptions)
    {
        ArgumentNullException.ThrowIfNull(panelOptions);
        ArgumentNullException.ThrowIfNull(packageStoreOptions);
        _maxFileBytes = panelOptions.MaxEditableFileBytes > 0 ? panelOptions.MaxEditableFileBytes : 2 * 1024 * 1024;
        _allowedRoots =
        [
            NormalizeRoot(AppContext.BaseDirectory),
            NormalizeRoot(ResolveConfiguredPath(packageStoreOptions.RootPath))
        ];
    }

    /// <summary>
    /// <para>zh-cn:读取一个白名单文本文件；路径必须位于允许根目录内，且文件大小不得超过配置上限。</para>
    /// <para>en-us:Reads one allowlisted text file; the path must stay under an allowed root and the file must not exceed the configured size limit.</para>
    /// </summary>
    /// <param name="path">
    /// <para>zh-cn:服务端绝对路径或相对于应用基目录的路径。</para>
    /// <para>en-us:Server-side absolute path or a path relative to the application base directory.</para>
    /// </param>
    /// <param name="cancellationToken">
    /// <para>zh-cn:取消令牌。</para>
    /// <para>en-us:Cancellation token.</para>
    /// </param>
    /// <returns>
    /// <para>zh-cn:文件内容、规范化路径、大小和修改时间。</para>
    /// <para>en-us:File content, normalized path, size, and last-modified time.</para>
    /// </returns>
    public async Task<AirForgeTextFileSnapshot> ReadAsync(string path, CancellationToken cancellationToken = default)
    {
        var fullPath = ValidatePath(path);
        var info = new FileInfo(fullPath);
        if (!info.Exists)
        {
            throw new FileNotFoundException($"Editable text file `{fullPath}` was not found.", fullPath);
        }

        EnsureLength(info.Length);
        var content = await File.ReadAllTextAsync(fullPath, Encoding.UTF8, cancellationToken).ConfigureAwait(false);
        return CreateSnapshot(fullPath, content, info);
    }

    /// <summary>
    /// <para>zh-cn:保存一个白名单文本文件；使用 UTF-8 写入，并可用预期修改时间阻止覆盖他人刚保存的内容。</para>
    /// <para>en-us:Saves one allowlisted text file as UTF-8 and can use an expected timestamp to prevent overwriting content saved by another operator.</para>
    /// </summary>
    /// <param name="request">
    /// <para>zh-cn:文件路径、文本内容和可选的预期修改时间。</para>
    /// <para>en-us:File path, text content, and optional expected last-modified timestamp.</para>
    /// </param>
    /// <param name="cancellationToken">
    /// <para>zh-cn:取消令牌。</para>
    /// <para>en-us:Cancellation token.</para>
    /// </param>
    /// <returns>
    /// <para>zh-cn:保存后的文件快照。</para>
    /// <para>en-us:The saved file snapshot.</para>
    /// </returns>
    public async Task<AirForgeTextFileSnapshot> WriteAsync(AirForgeTextFileWriteRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var fullPath = ValidatePath(request.Path);
        var info = new FileInfo(fullPath);
        if (!info.Exists)
        {
            throw new FileNotFoundException($"Editable text file `{fullPath}` was not found.", fullPath);
        }

        var content = request.Content ?? string.Empty;
        EnsureLength(Encoding.UTF8.GetByteCount(content));
        if (request.ExpectedLastModifiedUtc is { } expected
            && Math.Abs((info.LastWriteTimeUtc - expected.UtcDateTime).TotalMilliseconds) > 1)
        {
            throw new AirForgeTextFileConflictException($"Editable text file `{fullPath}` changed after it was loaded.");
        }

        await File.WriteAllTextAsync(fullPath, content, new UTF8Encoding(false), cancellationToken).ConfigureAwait(false);
        info.Refresh();
        return CreateSnapshot(fullPath, content, info);
    }

    /// <summary>
    /// <para>zh-cn:判断文件扩展名是否属于可编辑文本白名单；该方法不替代路径和大小校验。</para>
    /// <para>en-us:Determines whether a file extension is on the editable-text allowlist; this method does not replace path or size validation.</para>
    /// </summary>
    /// <param name="path">
    /// <para>zh-cn:待判断文件路径。</para>
    /// <para>en-us:The file path to inspect.</para>
    /// </param>
    /// <returns>
    /// <para>zh-cn:扩展名在白名单中时返回 true。</para>
    /// <para>en-us:True when the extension is allowlisted.</para>
    /// </returns>
    public static bool IsEditable(string path) =>
        !string.IsNullOrWhiteSpace(path) && AllowedExtensions.Contains(Path.GetExtension(path));

    private string ValidatePath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new ArgumentException("Editable text file path is required.", nameof(path));
        }

        if (!IsEditable(path))
        {
            throw new NotSupportedException($"File `{Path.GetFileName(path)}` is not an allowlisted text file.");
        }

        var fullPath = Path.GetFullPath(Path.IsPathFullyQualified(path)
            ? path
            : Path.Combine(AppContext.BaseDirectory, path));
        if (!_allowedRoots.Any(root => fullPath.StartsWith(root, StringComparison.OrdinalIgnoreCase)))
        {
            throw new UnauthorizedAccessException($"File `{fullPath}` is outside the AirForge editable roots.");
        }

        return fullPath;
    }

    private void EnsureLength(long length)
    {
        if (length > _maxFileBytes)
        {
            throw new InvalidOperationException($"Editable text file exceeds max size `{_maxFileBytes}` bytes.");
        }
    }

    private static AirForgeTextFileSnapshot CreateSnapshot(string path, string content, FileInfo info) => new()
    {
        Path = path,
        Content = content,
        Length = Encoding.UTF8.GetByteCount(content),
        LastModifiedUtc = info.LastWriteTimeUtc
    };

    private static string ResolveConfiguredPath(string? path) =>
        string.IsNullOrWhiteSpace(path)
            ? Path.Combine(AppContext.BaseDirectory, "dynamicapp-packages")
            : Path.IsPathFullyQualified(path) ? path : Path.Combine(AppContext.BaseDirectory, path);

    private static string NormalizeRoot(string path)
    {
        var fullPath = Path.GetFullPath(path);
        return fullPath.EndsWith(Path.DirectorySeparatorChar) || fullPath.EndsWith(Path.AltDirectorySeparatorChar)
            ? fullPath
            : fullPath + Path.DirectorySeparatorChar;
    }
}

/// <summary>
/// <para>zh-cn:AirForge 文本文件保存请求。</para>
/// <para>en-us:AirForge text-file save request.</para>
/// </summary>
public sealed class AirForgeTextFileWriteRequest
{
    /// <summary>
    /// <para>zh-cn:服务端文件路径。</para>
    /// <para>en-us:Server-side file path.</para>
    /// </summary>
    public string Path { get; set; } = string.Empty;

    /// <summary>
    /// <para>zh-cn:要保存的文本内容。</para>
    /// <para>en-us:Text content to save.</para>
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// <para>zh-cn:打开文件时的 UTC 修改时间；不为空时用于并发修改检测。</para>
    /// <para>en-us:UTC last-modified time captured when the file was opened; used for concurrency detection when provided.</para>
    /// </summary>
    public DateTimeOffset? ExpectedLastModifiedUtc { get; set; }
}

/// <summary>
/// <para>zh-cn:AirForge 文本文件快照。</para>
/// <para>en-us:AirForge text-file snapshot.</para>
/// </summary>
public sealed class AirForgeTextFileSnapshot
{
    /// <summary>
    /// <para>zh-cn:规范化后的服务端文件路径。</para>
    /// <para>en-us:Normalized server-side file path.</para>
    /// </summary>
    public string Path { get; set; } = string.Empty;

    /// <summary>
    /// <para>zh-cn:UTF-8 文本内容。</para>
    /// <para>en-us:UTF-8 text content.</para>
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// <para>zh-cn:UTF-8 字节长度。</para>
    /// <para>en-us:UTF-8 byte length.</para>
    /// </summary>
    public long Length { get; set; }

    /// <summary>
    /// <para>zh-cn:文件最后修改时间，使用 UTC。</para>
    /// <para>en-us:File last-modified time in UTC.</para>
    /// </summary>
    public DateTimeOffset LastModifiedUtc { get; set; }
}

/// <summary>
/// <para>zh-cn:文本文件并发修改冲突异常。</para>
/// <para>en-us:Text-file concurrent modification conflict exception.</para>
/// </summary>
public sealed class AirForgeTextFileConflictException : InvalidOperationException
{
    /// <summary>
    /// <para>zh-cn:创建文件冲突异常。</para>
    /// <para>en-us:Creates a file conflict exception.</para>
    /// </summary>
    /// <param name="message">
    /// <para>zh-cn:冲突描述。</para>
    /// <para>en-us:Conflict description.</para>
    /// </param>
    public AirForgeTextFileConflictException(string message) : base(message)
    {
    }
}
