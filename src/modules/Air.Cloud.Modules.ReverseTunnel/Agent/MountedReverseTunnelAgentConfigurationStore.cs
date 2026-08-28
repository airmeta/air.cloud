using Air.Cloud.Modules.ReverseTunnel.Common.Options;
using Air.Cloud.Modules.ReverseTunnel.Common.Runtime;

using Microsoft.Extensions.Options;

namespace Air.Cloud.Modules.ReverseTunnel.Agent;

/// <summary>
/// <para>zh-cn:基于本地挂载文件的 Agent 配置快照存储。写入使用临时文件替换，避免进程重启时读取半文件。</para>
/// <para>en-us:Mounted-file Agent configuration snapshot store. Writes use a temporary file replacement so restarts never read a partial snapshot.</para>
/// </summary>
public sealed class MountedReverseTunnelAgentConfigurationStore : IReverseTunnelAgentConfigurationStore
{
    private readonly IOptions<ReverseTunnelOptions> startupOptions;
    private long currentVersion;

    /// <summary><para>zh-cn:创建 Agent 配置快照存储。 en-us:Creates an Agent configuration snapshot store.</para></summary>
    public MountedReverseTunnelAgentConfigurationStore(IOptions<ReverseTunnelOptions> startupOptions)
    {
        this.startupOptions = startupOptions;
    }

    /// <inheritdoc />
    public long CurrentVersion => Interlocked.Read(ref currentVersion);

    /// <inheritdoc />
    public async Task<ReverseTunnelAgentConfigurationSnapshot?> LoadAsync(CancellationToken cancellationToken = default)
    {
        var path = ResolvePath(startupOptions.Value);
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
        {
            return null;
        }

        await using var stream = File.OpenRead(path);
        var snapshot = System.Text.Json.JsonSerializer.Deserialize<ReverseTunnelAgentConfigurationSnapshot>(stream);
        if (snapshot is not null &&
            snapshot.Version > 0 &&
            ReverseTunnelAgentConfigurationSnapshotSerializer.VerifyHash(snapshot) &&
            string.Equals(snapshot.AgentId, startupOptions.Value.AgentId, StringComparison.OrdinalIgnoreCase))
        {
            Interlocked.Exchange(ref currentVersion, snapshot.Version);
            return snapshot;
        }

        return null;
    }

    /// <inheritdoc />
    public async Task SaveAsync(ReverseTunnelAgentConfigurationSnapshot snapshot, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        var path = ResolvePath(startupOptions.Value);
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new InvalidOperationException("ReverseTunnelSettings:AgentCertificate:ConfigurationSnapshotPath must resolve to a writable file.");
        }

        if (snapshot.Version <= 0 || !ReverseTunnelAgentConfigurationSnapshotSerializer.VerifyHash(snapshot))
        {
            throw new InvalidOperationException("Agent configuration snapshot must have a positive version and a valid SHA-256 hash.");
        }

        var directory = Path.GetDirectoryName(path);
        if (string.IsNullOrWhiteSpace(directory))
        {
            throw new InvalidOperationException("Agent configuration snapshot path must have a directory.");
        }

        Directory.CreateDirectory(directory);
        var temp = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        await using (var stream = File.Create(temp))
        {
            await System.Text.Json.JsonSerializer.SerializeAsync(stream, snapshot, cancellationToken: cancellationToken);
            await stream.FlushAsync(cancellationToken);
        }

        File.Move(temp, path, true);
        Interlocked.Exchange(ref currentVersion, snapshot.Version);
    }

    private static string ResolvePath(ReverseTunnelOptions options)
    {
        var configured = options.AgentCertificate.ConfigurationSnapshotPath;
        if (!string.IsNullOrWhiteSpace(configured))
        {
            return Path.GetFullPath(configured);
        }

        if (string.IsNullOrWhiteSpace(options.AgentCertificate.CertificatePath))
        {
            return string.Empty;
        }

        var directory = Path.GetDirectoryName(Path.GetFullPath(options.AgentCertificate.CertificatePath));
        return string.IsNullOrWhiteSpace(directory) ? string.Empty : Path.Combine(directory, "airtunnel.agent.snapshot.json");
    }
}
