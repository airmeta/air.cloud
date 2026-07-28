using System.Text.Json;
using System.Text.Json.Serialization;

using Air.Cloud.DynamicApp.Contracts.Identifiers;
using Air.Cloud.DynamicApp.Contracts.Manifest;
using Air.Cloud.DynamicApp.Contracts.Runtime;

namespace Air.Cloud.DynamicApp.Manager;

/// <summary>
/// <para>zh-cn:文件型 Manifest 历史存储，适合需要重启后保留发布记录的单机 Manager。</para>
/// <para>en-us:File-based manifest history store for single-node Managers that need publish records to survive restarts.</para>
/// </summary>
public sealed class FileDynamicAppManifestStore : IDynamicAppManifestStore
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
        Converters =
        {
            new JsonStringEnumConverter()
        }
    };

    private readonly string _filePath;
    private readonly int _maxRecordsPerApp;
    private readonly object _syncRoot = new();
    private readonly List<DynamicAppManifestRecord> _records = new();

    /// <summary>
    /// <para>zh-cn:创建文件型 Manifest 历史存储，并加载已有历史文件。</para>
    /// <para>en-us:Creates the file-based manifest history store and loads existing history from disk.</para>
    /// </summary>
    /// <param name="options">
    /// <para>zh-cn:文件型 Manifest 历史存储选项。</para>
    /// <para>en-us:File-based manifest history store options.</para>
    /// </param>
    public FileDynamicAppManifestStore(FileDynamicAppManifestStoreOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        _filePath = ResolveFilePath(options.FilePath, "dynamicapp-manifests.json");
        _maxRecordsPerApp = Math.Max(1, options.MaxRecordsPerApp);
        Load();
    }

    /// <summary>
    /// <para>zh-cn:保存 Manifest 快照，按 DynamicApp 保留最近的指定数量记录。</para>
    /// <para>en-us:Saves a manifest snapshot and keeps the configured number of recent records per DynamicApp.</para>
    /// </summary>
    /// <param name="manifest">
    /// <para>zh-cn:待保存的 Manifest。</para>
    /// <para>en-us:Manifest to save.</para>
    /// </param>
    /// <param name="cancellationToken">
    /// <para>zh-cn:取消令牌。</para>
    /// <para>en-us:Cancellation token.</para>
    /// </param>
    /// <returns>
    /// <para>zh-cn:保存后的记录。</para>
    /// <para>en-us:The saved record.</para>
    /// </returns>
    public Task<DynamicAppManifestRecord> SaveAsync(DynamicAppManifest manifest, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(manifest);
        cancellationToken.ThrowIfCancellationRequested();
        lock (_syncRoot)
        {
            var record = new DynamicAppManifestRecord
            {
                Manifest = manifest,
                SavedAtUtc = DateTimeOffset.UtcNow
            };
            _records.Add(record);
            TrimUnsafe(manifest.Id);
            SaveUnsafe();
            return Task.FromResult(record);
        }
    }

    /// <summary>
    /// <para>zh-cn:从文件快照中列出所有 DynamicApp 的 Manifest 历史记录，结果按保存时间倒序返回。</para>
    /// <para>en-us:Lists manifest history records for all DynamicApps from the file snapshot in descending save order.</para>
    /// </summary>
    /// <param name="cancellationToken">
    /// <para>zh-cn:取消令牌。</para>
    /// <para>en-us:Cancellation token.</para>
    /// </param>
    /// <returns>
    /// <para>zh-cn:全部 Manifest 历史记录集合。</para>
    /// <para>en-us:All manifest history records.</para>
    /// </returns>
    public Task<IReadOnlyCollection<DynamicAppManifestRecord>> ListAllAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (_syncRoot)
        {
            return Task.FromResult<IReadOnlyCollection<DynamicAppManifestRecord>>(_records
                .OrderByDescending(item => item.SavedAtUtc)
                .ToList());
        }
    }

    /// <summary>
    /// <para>zh-cn:查询指定 DynamicApp 的 Manifest 历史记录。</para>
    /// <para>en-us:Queries manifest history records for the specified DynamicApp.</para>
    /// </summary>
    /// <param name="dynamicAppId">
    /// <para>zh-cn:DynamicApp 标识。</para>
    /// <para>en-us:DynamicApp identifier.</para>
    /// </param>
    /// <param name="cancellationToken">
    /// <para>zh-cn:取消令牌。</para>
    /// <para>en-us:Cancellation token.</para>
    /// </param>
    /// <returns>
    /// <para>zh-cn:倒序历史记录集合。</para>
    /// <para>en-us:History records in descending order.</para>
    /// </returns>
    public Task<IReadOnlyCollection<DynamicAppManifestRecord>> ListAsync(DynamicAppId dynamicAppId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (_syncRoot)
        {
            return Task.FromResult<IReadOnlyCollection<DynamicAppManifestRecord>>(_records
                .Where(item => item.Manifest.Id == dynamicAppId)
                .OrderByDescending(item => item.SavedAtUtc)
                .ToList());
        }
    }

    /// <summary>
    /// <para>zh-cn:从文件快照中查找最近一次包含指定 DynamicApp/Mod/Version 的 Manifest 历史记录。</para>
    /// <para>en-us:Finds the latest manifest history record containing the specified DynamicApp/Mod/Version from the file snapshot.</para>
    /// </summary>
    /// <param name="key">
    /// <para>zh-cn:目标 Mod 版本键。</para>
    /// <para>en-us:The target Mod version key.</para>
    /// </param>
    /// <param name="cancellationToken">
    /// <para>zh-cn:取消令牌。</para>
    /// <para>en-us:Cancellation token.</para>
    /// </param>
    /// <returns>
    /// <para>zh-cn:匹配的 Manifest 历史记录；不存在时为 null。</para>
    /// <para>en-us:The matching manifest history record; null when no record exists.</para>
    /// </returns>
    public Task<DynamicAppManifestRecord?> FindAsync(DynamicModVersionKey key, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (_syncRoot)
        {
            return Task.FromResult(_records
                .Where(item => item.Manifest.Id == key.DynamicAppId
                    && item.Manifest.Mods.Any(mod => mod.Id == key.ModId && mod.Version == key.Version))
                .OrderByDescending(item => item.SavedAtUtc)
                .FirstOrDefault());
        }
    }

    private void Load()
    {
        if (!File.Exists(_filePath))
        {
            return;
        }

        var snapshot = JsonSerializer.Deserialize<ManifestHistorySnapshot>(File.ReadAllText(_filePath), JsonOptions);
        if (snapshot?.Records == null)
        {
            return;
        }

        _records.AddRange(snapshot.Records);
    }

    private void TrimUnsafe(DynamicAppId dynamicAppId)
    {
        var overflow = _records
            .Where(item => item.Manifest.Id == dynamicAppId)
            .OrderByDescending(item => item.SavedAtUtc)
            .Skip(_maxRecordsPerApp)
            .ToHashSet();

        if (overflow.Count > 0)
        {
            _records.RemoveAll(overflow.Contains);
        }
    }

    private void SaveUnsafe()
    {
        var directory = Path.GetDirectoryName(_filePath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        File.WriteAllText(_filePath, JsonSerializer.Serialize(new ManifestHistorySnapshot
        {
            Records = _records.OrderByDescending(item => item.SavedAtUtc).ToList()
        }, JsonOptions));
    }

    private sealed class ManifestHistorySnapshot
    {
        public List<DynamicAppManifestRecord> Records { get; set; } = new();
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
