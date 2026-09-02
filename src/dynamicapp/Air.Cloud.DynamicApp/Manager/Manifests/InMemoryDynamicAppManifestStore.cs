using System.Collections.Concurrent;

using Air.Cloud.DynamicApp.Contracts.Identifiers;
using Air.Cloud.DynamicApp.Contracts.Manifest;
using Air.Cloud.DynamicApp.Contracts.Runtime;

namespace Air.Cloud.DynamicApp.Manager;

/// <summary>
/// <para>zh-cn:进程内 Manifest 历史存储，适合默认控制面和单机调试。</para>
/// <para>en-us:In-process manifest history store suitable for the default control plane and single-node debugging.</para>
/// </summary>
public sealed class InMemoryDynamicAppManifestStore : IDynamicAppManifestStore
{
    private readonly ConcurrentDictionary<string, List<DynamicAppManifestRecord>> _records = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// <para>zh-cn:保存一次 Manifest 快照；快照按保存时间倒序供查询。</para>
    /// <para>en-us:Saves one manifest snapshot; snapshots are queried in descending saved-time order.</para>
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

        var record = new DynamicAppManifestRecord
        {
            Manifest = manifest,
            SavedAtUtc = DateTimeOffset.UtcNow
        };
        var list = _records.GetOrAdd(manifest.Id.Value, _ => new List<DynamicAppManifestRecord>());
        lock (list)
        {
            list.Add(record);
        }

        return Task.FromResult(record);
    }

    /// <summary>
    /// <para>zh-cn:列出所有 DynamicApp 的 Manifest 历史快照，结果按保存时间倒序返回，供控制面板构建全局应用清单。</para>
    /// <para>en-us:Lists manifest history snapshots for all DynamicApps in descending save order for building a global control-panel inventory.</para>
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
        var records = new List<DynamicAppManifestRecord>();
        foreach (var list in _records.Values)
        {
            lock (list)
            {
                records.AddRange(list);
            }
        }

        return Task.FromResult<IReadOnlyCollection<DynamicAppManifestRecord>>(records
            .OrderByDescending(item => item.SavedAtUtc)
            .ToList());
    }

    /// <summary>
    /// <para>zh-cn:按 DynamicApp 标识查询 Manifest 历史。</para>
    /// <para>en-us:Queries manifest history by DynamicApp identifier.</para>
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
        if (!_records.TryGetValue(dynamicAppId.Value, out var list))
        {
            return Task.FromResult<IReadOnlyCollection<DynamicAppManifestRecord>>(Array.Empty<DynamicAppManifestRecord>());
        }

        lock (list)
        {
            return Task.FromResult<IReadOnlyCollection<DynamicAppManifestRecord>>(list.OrderByDescending(item => item.SavedAtUtc).ToList());
        }
    }

    /// <summary>
    /// <para>zh-cn:查找最近一次包含指定 DynamicApp/Mod/Version 的 Manifest 快照，用于控制面板按目标版本执行启动或重启。</para>
    /// <para>en-us:Finds the latest manifest snapshot containing the specified DynamicApp/Mod/Version for control-panel start or restart operations.</para>
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
        if (!_records.TryGetValue(key.DynamicAppId.Value, out var list))
        {
            return Task.FromResult<DynamicAppManifestRecord?>(null);
        }

        lock (list)
        {
            return Task.FromResult(list
                .Where(item => item.Manifest.Mods.Any(mod => mod.Id == key.ModId && mod.Version == key.Version))
                .OrderByDescending(item => item.SavedAtUtc)
                .FirstOrDefault());
        }
    }

    /// <summary>
    /// <para>zh-cn:从内存中的 Manifest 历史删除指定服务，并移除删除后不再包含任何 Mod 的空记录。</para>
    /// <para>en-us:Removes a service from in-memory manifest history and drops records that no longer contain any Mod.</para>
    /// </summary>
    public Task<int> RemoveModAsync(DynamicAppId dynamicAppId, DynamicModId modId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!_records.TryGetValue(dynamicAppId.Value, out var list))
        {
            return Task.FromResult(0);
        }

        var changed = 0;
        lock (list)
        {
            for (var index = list.Count - 1; index >= 0; index--)
            {
                var record = list[index];
                var removed = record.Manifest.Mods.RemoveAll(item => item.Id == modId);
                if (removed > 0)
                {
                    changed += removed;
                }

                if (record.Manifest.Mods.Count == 0)
                {
                    list.RemoveAt(index);
                }
            }
        }

        if (list.Count == 0)
        {
            _records.TryRemove(dynamicAppId.Value, out _);
        }

        return Task.FromResult(changed);
    }

    /// <summary>
    /// <para>zh-cn:从内存 Manifest 历史中删除指定 Mod 版本。</para>
    /// <para>en-us:Removes one Mod version from in-memory manifest history.</para>
    /// </summary>
    public Task<int> RemoveVersionAsync(DynamicModVersionKey key, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!_records.TryGetValue(key.DynamicAppId.Value, out var list)) return Task.FromResult(0);
        var changed = 0;
        lock (list)
        {
            for (var index = list.Count - 1; index >= 0; index--)
            {
                var record = list[index];
                changed += record.Manifest.Mods.RemoveAll(item => item.Id == key.ModId && item.Version == key.Version);
                if (record.Manifest.Mods.Count == 0) list.RemoveAt(index);
            }
        }
        if (list.Count == 0) _records.TryRemove(key.DynamicAppId.Value, out _);
        return Task.FromResult(changed);
    }

    /// <summary>
    /// <para>zh-cn:从内存历史中删除指定的单条 Manifest 发布记录。</para>
    /// <para>en-us:Removes one exact manifest publish record from the in-memory history.</para>
    /// </summary>
    public Task<bool> RemoveRecordAsync(string recordId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (string.IsNullOrWhiteSpace(recordId)) return Task.FromResult(false);

        foreach (var pair in _records.ToArray())
        {
            lock (pair.Value)
            {
                var removed = pair.Value.RemoveAll(item => string.Equals(item.RecordId, recordId, StringComparison.OrdinalIgnoreCase)) > 0;
                if (!removed) continue;
                if (pair.Value.Count == 0) _records.TryRemove(pair.Key, out _);
                return Task.FromResult(true);
            }
        }

        return Task.FromResult(false);
    }
}
