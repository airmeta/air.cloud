using System.Text.Json;
using System.Text.Json.Serialization;

using Air.Cloud.DynamicApp.Contracts.Runtime;

namespace Air.Cloud.DynamicApp.Manager;

/// <summary>
/// <para>zh-cn:文件型 DynamicApp 目标归档存储，负责持久化坏包或错误 Manifest 版本的治理状态。</para>
/// <para>en-us:File-based DynamicApp target archive store that persists governance state for bad-package or invalid-manifest versions.</para>
/// </summary>
public sealed class FileDynamicAppTargetArchiveStore : IDynamicAppTargetArchiveStore
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
        Converters =
        {
            new JsonStringEnumConverter()
        }
    };

    private readonly Dictionary<string, DynamicAppArchivedTarget> _archives = new(StringComparer.OrdinalIgnoreCase);
    private readonly string _filePath;
    private readonly FileDynamicAppTargetArchiveStoreOptions _options;
    private readonly object _syncRoot = new();

    /// <summary>
    /// <para>zh-cn:创建文件型目标归档存储，并从磁盘恢复已有归档记录。</para>
    /// <para>en-us:Creates the file-based target archive store and restores existing archive records from disk.</para>
    /// </summary>
    /// <param name="options">
    /// <para>zh-cn:文件型目标归档存储选项。</para>
    /// <para>en-us:The file-based target archive store options.</para>
    /// </param>
    public FileDynamicAppTargetArchiveStore(FileDynamicAppTargetArchiveStoreOptions options)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _filePath = ResolveFilePath(options.FilePath, "dynamicapp-archives.json");
        Load();
    }

    /// <summary>
    /// <para>zh-cn:归档指定目标版本，并按配置持久化归档文件。</para>
    /// <para>en-us:Archives the specified target version and persists the archive file according to options.</para>
    /// </summary>
    /// <param name="key">
    /// <para>zh-cn:需要归档的 Mod 版本键。</para>
    /// <para>en-us:The Mod version key to archive.</para>
    /// </param>
    /// <param name="reason">
    /// <para>zh-cn:归档原因；为空时写入默认治理说明。</para>
    /// <para>en-us:The archive reason; a default governance note is written when it is empty.</para>
    /// </param>
    /// <returns>
    /// <para>zh-cn:归档记录副本。</para>
    /// <para>en-us:A copy of the archive record.</para>
    /// </returns>
    public DynamicAppArchivedTarget Archive(DynamicModVersionKey key, string? reason)
    {
        lock (_syncRoot)
        {
            var record = new DynamicAppArchivedTarget
            {
                Key = key,
                Reason = string.IsNullOrWhiteSpace(reason) ? "Archived by DynamicApp Manager governance." : reason.Trim(),
                ArchivedAtUtc = DateTimeOffset.UtcNow
            };
            _archives[key.ToString()] = record;
            SaveIfNeeded();
            return Clone(record);
        }
    }

    /// <summary>
    /// <para>zh-cn:取消归档指定目标版本，并按配置持久化归档文件。</para>
    /// <para>en-us:Unarchives the specified target version and persists the archive file according to options.</para>
    /// </summary>
    /// <param name="key">
    /// <para>zh-cn:需要取消归档的 Mod 版本键。</para>
    /// <para>en-us:The Mod version key to unarchive.</para>
    /// </param>
    /// <returns>
    /// <para>zh-cn:是否实际取消了归档。</para>
    /// <para>en-us:Whether an archive entry was actually removed.</para>
    /// </returns>
    public bool Unarchive(DynamicModVersionKey key)
    {
        lock (_syncRoot)
        {
            var removed = _archives.Remove(key.ToString());
            if (removed)
            {
                SaveIfNeeded();
            }

            return removed;
        }
    }

    /// <summary>
    /// <para>zh-cn:判断指定目标版本是否已归档。</para>
    /// <para>en-us:Determines whether the specified target version is archived.</para>
    /// </summary>
    /// <param name="key">
    /// <para>zh-cn:目标 Mod 版本键。</para>
    /// <para>en-us:The target Mod version key.</para>
    /// </param>
    /// <returns>
    /// <para>zh-cn:目标版本是否已归档。</para>
    /// <para>en-us:Whether the target version is archived.</para>
    /// </returns>
    public bool IsArchived(DynamicModVersionKey key)
    {
        lock (_syncRoot)
        {
            return _archives.ContainsKey(key.ToString());
        }
    }

    /// <summary>
    /// <para>zh-cn:列出全部归档记录副本。</para>
    /// <para>en-us:Lists copies of all archived target records.</para>
    /// </summary>
    /// <returns>
    /// <para>zh-cn:归档记录集合。</para>
    /// <para>en-us:The archive record collection.</para>
    /// </returns>
    public IReadOnlyCollection<DynamicAppArchivedTarget> List()
    {
        lock (_syncRoot)
        {
            return _archives.Values.Select(Clone).ToList();
        }
    }

    private void Load()
    {
        if (!File.Exists(_filePath))
        {
            return;
        }

        var snapshot = JsonSerializer.Deserialize<ArchiveSnapshot>(File.ReadAllText(_filePath), JsonOptions);
        if (snapshot?.Archives == null)
        {
            return;
        }

        foreach (var archive in snapshot.Archives)
        {
            _archives[archive.Key.ToString()] = Clone(archive);
        }
    }

    private void SaveIfNeeded()
    {
        if (!_options.SaveOnChange)
        {
            return;
        }

        var directory = Path.GetDirectoryName(_filePath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var snapshot = new ArchiveSnapshot
        {
            Archives = _archives.Values.Select(Clone).ToList()
        };
        File.WriteAllText(_filePath, JsonSerializer.Serialize(snapshot, JsonOptions));
    }

    private static DynamicAppArchivedTarget Clone(DynamicAppArchivedTarget archive) => new()
    {
        Key = archive.Key,
        Reason = archive.Reason,
        ArchivedAtUtc = archive.ArchivedAtUtc
    };

    private sealed class ArchiveSnapshot
    {
        public List<DynamicAppArchivedTarget> Archives { get; set; } = new();
    }

    private static string ResolveFilePath(string? filePath, string defaultFileName)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            return Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, defaultFileName));
        }

        return Path.IsPathFullyQualified(filePath)
            ? Path.GetFullPath(filePath)
            : Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, filePath));
    }
}
