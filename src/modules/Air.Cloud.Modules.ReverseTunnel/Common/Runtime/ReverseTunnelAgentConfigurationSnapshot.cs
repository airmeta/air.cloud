using Air.Cloud.Modules.ReverseTunnel.Common.Options;

namespace Air.Cloud.Modules.ReverseTunnel.Common.Runtime;

/// <summary>
/// <para>zh-cn:Server 下发给 Agent 的运行期配置快照。快照只包含可运行的 AirTunnel 选项，不包含证书私钥、CA 密码或一次性 Enrollment token。</para>
/// <para>en-us:Runtime configuration snapshot sent from Server to Agent. The snapshot contains runnable AirTunnel options but never certificate private keys, CA passwords, or one-time enrollment tokens.</para>
/// </summary>
public sealed class ReverseTunnelAgentConfigurationSnapshot
{
    /// <summary><para>zh-cn:远端配置版本号，必须单调递增。 en-us:Remote configuration version; it must increase monotonically.</para></summary>
    public long Version { get; set; }

    /// <summary><para>zh-cn:目标 AgentId。 en-us:Target AgentId.</para></summary>
    public string AgentId { get; set; } = string.Empty;

    /// <summary><para>zh-cn:快照生成时间。 en-us:Snapshot generation time.</para></summary>
    public DateTimeOffset GeneratedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    /// <summary><para>zh-cn:配置内容 SHA-256。 en-us:SHA-256 hash of the configuration content.</para></summary>
    public string Sha256 { get; set; } = string.Empty;

    /// <summary><para>zh-cn:AirTunnel 配置。 en-us:AirTunnel configuration.</para></summary>
    public ReverseTunnelOptions Configuration { get; set; } = new();
}

/// <summary>
/// <para>zh-cn:Agent 配置下发的处理结果。 en-us:Result returned after processing an Agent configuration offer.</para>
/// </summary>
public sealed class ReverseTunnelAgentConfigurationResult
{
    /// <summary><para>zh-cn:配置版本号。 en-us:Configuration version.</para></summary>
    public long Version { get; set; }

    /// <summary><para>zh-cn:AgentId。 en-us:AgentId.</para></summary>
    public string AgentId { get; set; } = string.Empty;

    /// <summary><para>zh-cn:是否成功。 en-us:Whether the configuration was accepted.</para></summary>
    public bool Accepted { get; set; }

    /// <summary><para>zh-cn:失败原因。 en-us:Failure reason.</para></summary>
    public string? FailureReason { get; set; }
}

/// <summary>
/// <para>zh-cn:Agent 运行期配置快照序列化工具。 en-us:Serializer for Agent runtime configuration snapshots.</para>
/// </summary>
public static class ReverseTunnelAgentConfigurationSnapshotSerializer
{
    private static readonly System.Text.Json.JsonSerializerOptions SerializerOptions = new(System.Text.Json.JsonSerializerDefaults.Web)
    {
        WriteIndented = false,
        Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
    };

    /// <summary>
    /// <para>zh-cn:创建并计算哈希的配置快照。 en-us:Creates a configuration snapshot and computes its hash.</para>
    /// </summary>
    public static ReverseTunnelAgentConfigurationSnapshot Create(string agentId, ReverseTunnelOptions configuration, long version)
    {
        var snapshot = new ReverseTunnelAgentConfigurationSnapshot
        {
            Version = version,
            AgentId = agentId?.Trim() ?? string.Empty,
            GeneratedAtUtc = DateTimeOffset.UtcNow,
            Configuration = ReverseTunnelOptionsCloner.CloneAndNormalize(configuration)
        };
        snapshot.Sha256 = ComputeHash(snapshot.Configuration);
        return snapshot;
    }

    /// <summary>
    /// <para>zh-cn:序列化配置快照。 en-us:Serializes a configuration snapshot.</para>
    /// </summary>
    public static string Serialize(ReverseTunnelAgentConfigurationSnapshot snapshot)
        => System.Text.Json.JsonSerializer.Serialize(snapshot, SerializerOptions);

    /// <summary>
    /// <para>zh-cn:反序列化配置快照。 en-us:Deserializes a configuration snapshot.</para>
    /// </summary>
    public static ReverseTunnelAgentConfigurationSnapshot Deserialize(ReadOnlySpan<byte> payload)
        => System.Text.Json.JsonSerializer.Deserialize<ReverseTunnelAgentConfigurationSnapshot>(payload, SerializerOptions)
            ?? throw new InvalidOperationException("Reverse tunnel configuration snapshot payload is empty.");

    /// <summary>
    /// <para>zh-cn:校验快照哈希。 en-us:Validates the snapshot hash.</para>
    /// </summary>
    public static bool VerifyHash(ReverseTunnelAgentConfigurationSnapshot snapshot)
        => snapshot is not null &&
           !string.IsNullOrWhiteSpace(snapshot.Sha256) &&
           string.Equals(snapshot.Sha256, ComputeHash(snapshot.Configuration), StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// <para>zh-cn:根据当前配置内容重新计算快照哈希。Agent 合并本地安全配置后必须调用此方法再持久化。</para>
    /// <para>en-us:Recomputes the snapshot hash from the current configuration. The Agent must call this after merging local security settings before persistence.</para>
    /// </summary>
    /// <param name="snapshot">
    /// <para>zh-cn:需要刷新哈希的配置快照。</para>
    /// <para>en-us:Snapshot whose hash should be refreshed.</para>
    /// </param>
    public static void RefreshHash(ReverseTunnelAgentConfigurationSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        snapshot.Sha256 = ComputeHash(snapshot.Configuration);
    }

    private static string ComputeHash(ReverseTunnelOptions options)
    {
        var json = System.Text.Json.JsonSerializer.Serialize(options, SerializerOptions);
        var bytes = System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(json));
        return Convert.ToHexString(bytes);
    }
}
