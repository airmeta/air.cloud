using Air.Cloud.Modules.ReverseTunnel.Common.Runtime;
using Air.Cloud.Modules.ReverseTunnel.Common.Options;

using Microsoft.Extensions.Hosting;

using System.Text.Json;

namespace Air.Cloud.Modules.ReverseTunnel.Server;

/// <summary>
/// <para>zh-cn:基于配置目录的 Agent 目标配置存储。写入采用临时文件替换，避免 Server 重启或异常时读取半文件。</para>
/// <para>en-us:File-backed desired Agent configuration store. Writes use temporary-file replacement so restarts never read a partial file.</para>
/// </summary>
public sealed class MountedReverseTunnelDesiredAgentConfigurationStore : IReverseTunnelDesiredAgentConfigurationStore
{
    private readonly IReverseTunnelRuntimeOptionsAccessor runtimeOptions;
    private readonly IHostEnvironment? hostEnvironment;
    private readonly SemaphoreSlim gate = new(1, 1);
    private readonly Dictionary<string, ReverseTunnelAgentConfigurationSnapshot> snapshots = new(StringComparer.OrdinalIgnoreCase);
    private bool loaded;

    /// <summary>
    /// <para>zh-cn:创建 Server 端 Agent 目标配置存储。</para>
    /// <para>en-us:Creates the Server-side desired Agent configuration store.</para>
    /// </summary>
    public MountedReverseTunnelDesiredAgentConfigurationStore(
        IReverseTunnelRuntimeOptionsAccessor runtimeOptions,
        IHostEnvironment? hostEnvironment = null)
    {
        this.runtimeOptions = runtimeOptions;
        this.hostEnvironment = hostEnvironment;
    }

    /// <inheritdoc />
    public async Task<ReverseTunnelAgentConfigurationSnapshot?> GetAsync(
        string agentId,
        CancellationToken cancellationToken = default)
    {
        var key = NormalizeAgentId(agentId);
        await gate.WaitAsync(cancellationToken);
        try
        {
            await EnsureLoadedAsync(cancellationToken);
            return snapshots.TryGetValue(key, out var snapshot) ? Clone(snapshot) : null;
        }
        finally
        {
            gate.Release();
        }
    }

    /// <inheritdoc />
    public async Task SaveAsync(
        ReverseTunnelAgentConfigurationSnapshot snapshot,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        var key = NormalizeAgentId(snapshot.AgentId);
        if (snapshot.Version <= 0 || !ReverseTunnelAgentConfigurationSnapshotSerializer.VerifyHash(snapshot))
        {
            throw new InvalidOperationException("Desired Agent configuration must have a positive version and a valid SHA-256 hash.");
        }

        await gate.WaitAsync(cancellationToken);
        try
        {
            await EnsureLoadedAsync(cancellationToken);
            snapshots[key] = Clone(snapshot);
            await PersistAsync(cancellationToken);
        }
        finally
        {
            gate.Release();
        }
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ReverseTunnelAgentConfigurationSnapshot>> ListAsync(CancellationToken cancellationToken = default)
    {
        await gate.WaitAsync(cancellationToken);
        try
        {
            await EnsureLoadedAsync(cancellationToken);
            return snapshots.Values
                .OrderBy(item => item.AgentId, StringComparer.OrdinalIgnoreCase)
                .Select(Clone)
                .ToArray();
        }
        finally
        {
            gate.Release();
        }
    }

    /// <summary>
    /// <para>zh-cn:在同一存储锁内执行完整配置的版本比较、跨 Agent 冲突校验和原子落盘，避免并发管理请求互相覆盖。</para>
    /// <para>en-us:Performs version comparison, cross-Agent conflict validation, and atomic persistence for a complete configuration under one store lock, preventing concurrent management requests from overwriting one another.</para>
    /// </summary>
    public async Task<ReverseTunnelDesiredAgentConfigurationSaveResult> TrySaveAsync(
        string agentId,
        ReverseTunnelOptions configuration,
        long expectedVersion,
        Func<IReadOnlyList<ReverseTunnelAgentConfigurationSnapshot>, IReadOnlyList<string>>? validate = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        var key = NormalizeAgentId(agentId);

        await gate.WaitAsync(cancellationToken);
        try
        {
            await EnsureLoadedAsync(cancellationToken);
            var current = snapshots.TryGetValue(key, out var stored) ? stored : null;
            var currentVersion = current?.Version ?? 0;
            if (currentVersion != expectedVersion)
            {
                return ReverseTunnelDesiredAgentConfigurationSaveResult.Conflict;
            }

            var existing = snapshots.Values.Select(Clone).ToArray();
            var conflicts = validate?.Invoke(existing) ?? [];
            if (conflicts.Count > 0)
            {
                return ReverseTunnelDesiredAgentConfigurationSaveResult.ValidationConflict(conflicts);
            }

            var version = Math.Max(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(), currentVersion + 1);
            var snapshot = ReverseTunnelAgentConfigurationSnapshotSerializer.Create(key, configuration, version);
            snapshots[key] = Clone(snapshot);
            await PersistAsync(cancellationToken);
            return ReverseTunnelDesiredAgentConfigurationSaveResult.Success(Clone(snapshot));
        }
        finally
        {
            gate.Release();
        }
    }

    private async Task EnsureLoadedAsync(CancellationToken cancellationToken)
    {
        if (loaded)
        {
            return;
        }

        loaded = true;
        var path = ResolvePath();
        if (!File.Exists(path))
        {
            return;
        }

        await using var stream = File.OpenRead(path);
        var items = await JsonSerializer.DeserializeAsync<List<ReverseTunnelAgentConfigurationSnapshot>>(stream, cancellationToken: cancellationToken)
            ?? [];
        foreach (var item in items)
        {
            if (item.Version > 0 &&
                !string.IsNullOrWhiteSpace(item.AgentId) &&
                ReverseTunnelAgentConfigurationSnapshotSerializer.VerifyHash(item))
            {
                snapshots[NormalizeAgentId(item.AgentId)] = Clone(item);
            }
        }
    }

    private async Task PersistAsync(CancellationToken cancellationToken)
    {
        var path = ResolvePath();
        var directory = Path.GetDirectoryName(path) ?? throw new InvalidOperationException("Desired Agent configuration path has no directory.");
        Directory.CreateDirectory(directory);
        var temporaryPath = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        await using (var stream = File.Create(temporaryPath))
        {
            await JsonSerializer.SerializeAsync(stream, snapshots.Values.OrderBy(item => item.AgentId).ToArray(), cancellationToken: cancellationToken);
            await stream.FlushAsync(cancellationToken);
        }

        File.Move(temporaryPath, path, true);
    }

    private string ResolvePath()
    {
        var directory = runtimeOptions.Current.Management.ConfigDirectory;
        var fullDirectory = Path.IsPathRooted(directory)
            ? Path.GetFullPath(directory)
            : Path.GetFullPath(Path.Combine(hostEnvironment?.ContentRootPath ?? AppContext.BaseDirectory, directory));
        return Path.Combine(fullDirectory, "airtunnel.agent-configurations.json");
    }

    private static string NormalizeAgentId(string agentId)
    {
        if (string.IsNullOrWhiteSpace(agentId))
        {
            throw new ArgumentException("AgentId is required.", nameof(agentId));
        }

        return agentId.Trim();
    }

    private static ReverseTunnelAgentConfigurationSnapshot Clone(ReverseTunnelAgentConfigurationSnapshot source)
        => new()
        {
            Version = source.Version,
            AgentId = source.AgentId,
            GeneratedAtUtc = source.GeneratedAtUtc,
            Sha256 = source.Sha256,
            Configuration = ReverseTunnelOptionsCloner.CloneAndNormalize(source.Configuration)
        };
}
