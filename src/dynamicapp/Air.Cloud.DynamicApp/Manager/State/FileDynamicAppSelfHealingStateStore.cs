using System.Text.Json;
using System.Text.Json.Serialization;

using Air.Cloud.DynamicApp.Contracts.Runtime;

namespace Air.Cloud.DynamicApp.Manager;

/// <summary>
/// <para>zh-cn:文件型 DynamicApp 自愈状态存储，适合单节点 Compact Host 在重启后保留 Process 自愈重试窗口。</para>
/// <para>en-us:File-based DynamicApp self-healing state store for single-node Compact Host deployments that need Process retry windows to survive restarts.</para>
/// </summary>
public sealed class FileDynamicAppSelfHealingStateStore : IDynamicAppSelfHealingStateStore
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
    private readonly FileDynamicAppSelfHealingStateStoreOptions _options;
    private readonly object _syncRoot = new();
    private readonly Dictionary<string, DynamicAppSelfHealingState> _states = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// <para>zh-cn:创建文件型自愈状态存储，并从磁盘恢复已有重试计数。</para>
    /// <para>en-us:Creates the file-based self-healing state store and restores existing retry counters from disk.</para>
    /// </summary>
    /// <param name="options">
    /// <para>zh-cn:文件型自愈状态存储选项。</para>
    /// <para>en-us:The file-based self-healing state store options.</para>
    /// </param>
    public FileDynamicAppSelfHealingStateStore(FileDynamicAppSelfHealingStateStoreOptions options)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _filePath = ResolveFilePath(options.FilePath, "dynamicapp-self-healing.json");
        Load();
    }

    /// <summary>
    /// <para>zh-cn:按目标键读取自愈状态副本；不存在时返回 null。</para>
    /// <para>en-us:Reads a copy of the self-healing state by target key; returns null when it does not exist.</para>
    /// </summary>
    /// <param name="key">
    /// <para>zh-cn:目标 Mod 版本键。</para>
    /// <para>en-us:The target Mod version key.</para>
    /// </param>
    /// <returns>
    /// <para>zh-cn:自愈状态副本，或 null。</para>
    /// <para>en-us:A copy of the self-healing state, or null.</para>
    /// </returns>
    public DynamicAppSelfHealingState? Get(DynamicModVersionKey key)
    {
        lock (_syncRoot)
        {
            return _states.TryGetValue(key.ToString(), out var state) ? Clone(state) : null;
        }
    }

    /// <summary>
    /// <para>zh-cn:新增或更新自愈状态，并按配置保存到文件。</para>
    /// <para>en-us:Adds or updates self-healing state and saves it to the file according to options.</para>
    /// </summary>
    /// <param name="state">
    /// <para>zh-cn:需要保存的自愈状态。</para>
    /// <para>en-us:The self-healing state to save.</para>
    /// </param>
    public void Upsert(DynamicAppSelfHealingState state)
    {
        ArgumentNullException.ThrowIfNull(state);
        lock (_syncRoot)
        {
            var copy = Clone(state);
            copy.UpdatedAtUtc = copy.UpdatedAtUtc == default ? DateTimeOffset.UtcNow : copy.UpdatedAtUtc;
            _states[copy.Key.ToString()] = copy;
            SaveIfNeeded();
        }
    }

    /// <summary>
    /// <para>zh-cn:移除指定目标的自愈状态，并按配置保存到文件。</para>
    /// <para>en-us:Removes the self-healing state for the specified target and saves the file according to options.</para>
    /// </summary>
    /// <param name="key">
    /// <para>zh-cn:目标 Mod 版本键。</para>
    /// <para>en-us:The target Mod version key.</para>
    /// </param>
    /// <returns>
    /// <para>zh-cn:是否实际移除了状态。</para>
    /// <para>en-us:Whether a state entry was actually removed.</para>
    /// </returns>
    public bool Remove(DynamicModVersionKey key)
    {
        lock (_syncRoot)
        {
            var removed = _states.Remove(key.ToString());
            if (removed)
            {
                SaveIfNeeded();
            }

            return removed;
        }
    }

    /// <summary>
    /// <para>zh-cn:列出全部自愈状态副本。</para>
    /// <para>en-us:Lists copies of all self-healing states.</para>
    /// </summary>
    /// <returns>
    /// <para>zh-cn:自愈状态集合。</para>
    /// <para>en-us:The self-healing state collection.</para>
    /// </returns>
    public IReadOnlyCollection<DynamicAppSelfHealingState> List()
    {
        lock (_syncRoot)
        {
            return _states.Values.Select(Clone).ToList();
        }
    }

    private void Load()
    {
        if (!File.Exists(_filePath))
        {
            return;
        }

        var snapshot = JsonSerializer.Deserialize<StateSnapshot>(File.ReadAllText(_filePath), JsonOptions);
        if (snapshot?.States == null)
        {
            return;
        }

        foreach (var state in snapshot.States)
        {
            _states[state.Key.ToString()] = Clone(state);
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

        var snapshot = new StateSnapshot
        {
            States = _states.Values.Select(Clone).ToList()
        };
        File.WriteAllText(_filePath, JsonSerializer.Serialize(snapshot, JsonOptions));
    }

    private static DynamicAppSelfHealingState Clone(DynamicAppSelfHealingState state) => new()
    {
        Key = state.Key,
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

    private sealed class StateSnapshot
    {
        public List<DynamicAppSelfHealingState> States { get; set; } = new();
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
