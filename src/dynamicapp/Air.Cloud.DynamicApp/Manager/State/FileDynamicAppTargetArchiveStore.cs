using System.Text.Json;
using System.Text.Json.Serialization;

using Air.Cloud.DynamicApp.Contracts.Identifiers;
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
            using var fileLock = _options.SaveOnChange ? DynamicAppFileSnapshotLock.Acquire(_filePath) : null;
            if (_options.SaveOnChange) Load();
            var record = new DynamicAppArchivedTarget
            {
                Key = key,
                Reason = string.IsNullOrWhiteSpace(reason) ? "Archived by DynamicApp Manager governance." : reason.Trim(),
                ArchivedAtUtc = DateTimeOffset.UtcNow
            };
            _archives[BuildArchiveKey(key, string.Empty)] = record;
            SaveIfNeeded();
            return Clone(record);
        }
    }

    /// <summary>
    /// <para>zh-cn:保存运行实例销毁归档，并持久化实例配置、自愈状态和归档目录元数据。</para>
    /// <para>en-us:Stores a destroyed runtime-instance archive and persists its configuration, self-healing state, and archived-directory metadata.</para>
    /// </summary>
    public DynamicAppArchivedTarget ArchiveDeployment(
        DynamicModVersionKey key,
        string deploymentId,
        DynamicAppRouteTarget target,
        DynamicAppSelfHealingState? selfHealing,
        string? archiveDirectory,
        string? reason)
    {
        ArgumentNullException.ThrowIfNull(target);
        if (string.IsNullOrWhiteSpace(deploymentId))
        {
            throw new ArgumentException("Deployment identifier is required.", nameof(deploymentId));
        }

        lock (_syncRoot)
        {
            using var fileLock = _options.SaveOnChange ? DynamicAppFileSnapshotLock.Acquire(_filePath) : null;
            if (_options.SaveOnChange) Load();
            var record = new DynamicAppArchivedTarget
            {
                Key = key,
                DeploymentId = deploymentId.Trim(),
                RuntimeTarget = Clone(target),
                SelfHealing = selfHealing == null ? null : Clone(selfHealing),
                ArchiveDirectory = archiveDirectory ?? string.Empty,
                Reason = string.IsNullOrWhiteSpace(reason) ? "Destroyed by DynamicApp Manager." : reason.Trim(),
                ArchivedAtUtc = DateTimeOffset.UtcNow
            };
            _archives[BuildArchiveKey(key, record.DeploymentId)] = record;
            SaveIfNeeded();
            return Clone(record);
        }
    }

    /// <summary>
    /// <para>zh-cn:查找指定运行实例的归档记录。</para>
    /// <para>en-us:Finds the archive record for the specified runtime instance.</para>
    /// </summary>
    public DynamicAppArchivedTarget? FindDeployment(DynamicAppRuntimeKey key)
    {
        lock (_syncRoot)
        {
            using var fileLock = _options.SaveOnChange ? DynamicAppFileSnapshotLock.Acquire(_filePath) : null;
            if (_options.SaveOnChange) Load();
            return _archives.TryGetValue(BuildArchiveKey(key.PackageKey, key.DeploymentId), out var record)
                ? Clone(record)
                : null;
        }
    }

    /// <summary>
    /// <para>zh-cn:列出实例级归档记录。</para>
    /// <para>en-us:Lists runtime-instance archive records.</para>
    /// </summary>
    public IReadOnlyCollection<DynamicAppArchivedTarget> ListDeploymentArchives()
    {
        lock (_syncRoot)
        {
            using var fileLock = _options.SaveOnChange ? DynamicAppFileSnapshotLock.Acquire(_filePath) : null;
            if (_options.SaveOnChange) Load();
            return _archives.Values
                .Where(item => !string.IsNullOrWhiteSpace(item.DeploymentId))
                .Select(Clone)
                .ToList();
        }
    }

    /// <summary>
    /// <para>zh-cn:删除指定运行实例的归档记录。</para>
    /// <para>en-us:Deletes the archive record for the specified runtime instance.</para>
    /// </summary>
    public bool RemoveDeployment(DynamicAppRuntimeKey key)
    {
        lock (_syncRoot)
        {
            using var fileLock = _options.SaveOnChange ? DynamicAppFileSnapshotLock.Acquire(_filePath) : null;
            if (_options.SaveOnChange) Load();
            var removed = _archives.Remove(BuildArchiveKey(key.PackageKey, key.DeploymentId));
            if (removed)
            {
                SaveIfNeeded();
            }

            return removed;
        }
    }

    /// <summary>
    /// <para>zh-cn:判断指定服务是否存在实例级或版本级归档记录。</para>
    /// <para>en-us:Determines whether a service has any runtime-instance or version-level archive record.</para>
    /// </summary>
    public bool HasArchives(DynamicAppId dynamicAppId, DynamicModId modId)
    {
        lock (_syncRoot)
        {
            using var fileLock = _options.SaveOnChange ? DynamicAppFileSnapshotLock.Acquire(_filePath) : null;
            if (_options.SaveOnChange) Load();
            return _archives.Values.Any(item =>
                item.Key.DynamicAppId == dynamicAppId
                && item.Key.ModId == modId);
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
            using var fileLock = _options.SaveOnChange ? DynamicAppFileSnapshotLock.Acquire(_filePath) : null;
            if (_options.SaveOnChange) Load();
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
            using var fileLock = _options.SaveOnChange ? DynamicAppFileSnapshotLock.Acquire(_filePath) : null;
            if (_options.SaveOnChange) Load();
            return _archives.Values.Any(item =>
                item.Key == key
                && string.IsNullOrWhiteSpace(item.DeploymentId));
        }
    }

    /// <summary>
    /// <para>zh-cn:按运行实例判断目标是否被版本级或实例级归档；版本级归档阻断该版本的所有实例，实例级归档只阻断指定实例。</para>
    /// <para>en-us:Determines whether a runtime target is blocked by a version-level or instance-level archive; a version archive blocks all deployments while an instance archive blocks only the specified deployment.</para>
    /// </summary>
    public bool IsArchived(DynamicAppRuntimeKey key)
    {
        lock (_syncRoot)
        {
            using var fileLock = _options.SaveOnChange ? DynamicAppFileSnapshotLock.Acquire(_filePath) : null;
            if (_options.SaveOnChange) Load();
            return _archives.Values.Any(item =>
                item.Key == key.PackageKey
                && (string.IsNullOrWhiteSpace(item.DeploymentId)
                    || string.Equals(item.DeploymentId, key.DeploymentId, StringComparison.OrdinalIgnoreCase)));
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
            using var fileLock = _options.SaveOnChange ? DynamicAppFileSnapshotLock.Acquire(_filePath) : null;
            if (_options.SaveOnChange) Load();
            return _archives.Values.Select(Clone).ToList();
        }
    }

    private void Load()
    {
        _archives.Clear();
        if (!File.Exists(_filePath))
        {
            return;
        }

        ArchiveSnapshot? snapshot;
        try
        {
            snapshot = JsonSerializer.Deserialize<ArchiveSnapshot>(File.ReadAllText(_filePath), JsonOptions);
        }
        catch (JsonException)
        {
            return;
        }
        catch (IOException)
        {
            return;
        }

        if (snapshot?.Archives == null)
        {
            return;
        }

        foreach (var archive in snapshot.Archives)
        {
            _archives[BuildArchiveKey(archive.Key, archive.DeploymentId)] = Clone(archive);
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
        DynamicAppAtomicJsonFile.Write(_filePath, JsonSerializer.Serialize(snapshot, JsonOptions));
    }

    private static DynamicAppArchivedTarget Clone(DynamicAppArchivedTarget archive) => new()
    {
        Key = archive.Key,
        DeploymentId = archive.DeploymentId,
        RuntimeTarget = archive.RuntimeTarget == null ? null : Clone(archive.RuntimeTarget),
        SelfHealing = archive.SelfHealing == null ? null : Clone(archive.SelfHealing),
        ArchiveDirectory = archive.ArchiveDirectory,
        Reason = archive.Reason,
        ArchivedAtUtc = archive.ArchivedAtUtc
    };

    private static DynamicAppRouteTarget Clone(DynamicAppRouteTarget target) => new()
    {
        Key = target.Key,
        DeploymentId = string.IsNullOrWhiteSpace(target.DeploymentId) ? "default" : target.DeploymentId.Trim(),
        DeploymentDirectory = target.DeploymentDirectory,
        DeploymentEntryFilePath = target.DeploymentEntryFilePath,
        BaseAddress = target.BaseAddress,
        HealthPath = target.HealthPath,
        HealthProbeTimeoutSeconds = target.HealthProbeTimeoutSeconds,
        State = target.State,
        HealthStatus = target.HealthStatus,
        InstanceId = target.InstanceId,
        LastStatusMessage = target.LastStatusMessage,
        CreatedAtUtc = target.CreatedAtUtc,
        UpdatedAtUtc = target.UpdatedAtUtc,
        DrainingSinceUtc = target.DrainingSinceUtc
    };

    private static DynamicAppSelfHealingState Clone(DynamicAppSelfHealingState state) => new()
    {
        Key = state.Key,
        DeploymentId = string.IsNullOrWhiteSpace(state.DeploymentId) ? "default" : state.DeploymentId.Trim(),
        AttemptCount = state.AttemptCount,
        MaxAttempts = state.MaxAttempts,
        FirstAttemptUtc = state.FirstAttemptUtc,
        LastAttemptUtc = state.LastAttemptUtc,
        NextRetryAtUtc = state.NextRetryAtUtc,
        WindowResetAtUtc = state.WindowResetAtUtc,
        IsPaused = state.IsPaused,
        LastFailureStage = state.LastFailureStage,
        LastFailureMessage = state.LastFailureMessage,
        UpdatedAtUtc = state.UpdatedAtUtc
    };

    private static string BuildArchiveKey(DynamicModVersionKey key, string? deploymentId)
        => string.IsNullOrWhiteSpace(deploymentId)
            ? key.ToString()
            : $"{key}/{deploymentId.Trim()}";

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
