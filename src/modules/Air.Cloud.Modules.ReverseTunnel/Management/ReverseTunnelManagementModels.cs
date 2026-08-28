using Air.Cloud.Modules.ReverseTunnel.Common.Diagnostics;
using Air.Cloud.Modules.ReverseTunnel.Common.Options;

namespace Air.Cloud.Modules.ReverseTunnel.Management;

/// <summary>
/// <para>zh-cn:AirTunnel Server 管理端总览响应。</para>
/// <para>en-us:AirTunnel Server management overview response.</para>
/// </summary>
public sealed class ReverseTunnelManagementOverview
{
    /// <summary><para>zh-cn:控制端口地址。</para><para>en-us:Tunnel control endpoint URL.</para></summary>
    public string ControlUrl { get; init; } = string.Empty;

    /// <summary><para>zh-cn:管理端地址。</para><para>en-us:Management endpoint URL.</para></summary>
    public string ManagementUrl { get; init; } = string.Empty;

    /// <summary><para>zh-cn:TCP listener 配置。</para><para>en-us:TCP listener configuration.</para></summary>
    public IReadOnlyList<ReverseTunnelTcpListenerOptions> TcpListeners { get; init; } = [];

    /// <summary><para>zh-cn:UDP listener 配置。</para><para>en-us:UDP listener configuration.</para></summary>
    public IReadOnlyList<ReverseTunnelUdpListenerOptions> UdpListeners { get; init; } = [];

    /// <summary><para>zh-cn:在线 Agent 会话。</para><para>en-us:Online Agent sessions.</para></summary>
    public IReadOnlyList<ReverseTunnelSessionDiagnosticsSnapshot> Sessions { get; init; } = [];

    /// <summary><para>zh-cn:运行期指标。</para><para>en-us:Runtime metrics.</para></summary>
    public ReverseTunnelMetricsSnapshot? Metrics { get; init; }

    /// <summary><para>zh-cn:当前运行期配置版本。</para><para>en-us:Current runtime options version.</para></summary>
    public long Version { get; init; }

    /// <summary><para>zh-cn:已保存的 Agent 目标配置状态。</para><para>en-us:Stored desired configuration status for each Agent.</para></summary>
    public IReadOnlyList<ReverseTunnelAgentConfigurationStatus> AgentConfigurations { get; init; } = [];

    /// <summary><para>zh-cn:由 Agent 本地服务配置自动汇总出的 HTTP 隧道路由。</para><para>en-us:HTTP tunnel routes automatically aggregated from Agent local-service configurations.</para></summary>
    public IReadOnlyList<ReverseTunnelRouteOptions> DerivedRoutes { get; init; } = [];

    /// <summary><para>zh-cn:由 Agent TCP 本地服务配置自动汇总出的 Server 监听器。</para><para>en-us:Server TCP listeners automatically aggregated from Agent local-service configuration.</para></summary>
    public IReadOnlyList<ReverseTunnelTcpListenerOptions> DerivedTcpListeners { get; init; } = [];

    /// <summary><para>zh-cn:由 Agent UDP 本地服务配置自动汇总出的 Server 监听器。</para><para>en-us:Server UDP listeners automatically aggregated from Agent local-service configuration.</para></summary>
    public IReadOnlyList<ReverseTunnelUdpListenerOptions> DerivedUdpListeners { get; init; } = [];
}

/// <summary>
/// <para>zh-cn:Agent 目标配置状态。</para>
/// <para>en-us:Desired configuration status for an Agent.</para>
/// </summary>
public sealed class ReverseTunnelAgentConfigurationStatus
{
    /// <summary><para>zh-cn:Agent 标识。</para><para>en-us:Agent identifier.</para></summary>
    public string AgentId { get; init; } = string.Empty;
    /// <summary><para>zh-cn:期望配置版本。</para><para>en-us:Desired configuration version.</para></summary>
    public long DesiredVersion { get; init; }
    /// <summary><para>zh-cn:快照生成时间。</para><para>en-us:Snapshot generation time.</para></summary>
    public DateTimeOffset GeneratedAtUtc { get; init; }
    /// <summary><para>zh-cn:配置哈希。</para><para>en-us:Configuration hash.</para></summary>
    public string Sha256 { get; init; } = string.Empty;
}

/// <summary>
/// <para>zh-cn:AirTunnel 拆分配置视图。</para>
/// <para>en-us:AirTunnel split-configuration view.</para>
/// </summary>
public sealed class ReverseTunnelConfigurationView
{
    /// <summary><para>zh-cn:完整运行期配置。</para><para>en-us:Complete runtime options.</para></summary>
    public ReverseTunnelOptions Options { get; init; } = new();

    /// <summary><para>zh-cn:拆分配置文件路径。</para><para>en-us:Split configuration file paths.</para></summary>
    public IReadOnlyList<string> Files { get; init; } = [];
}

/// <summary>
/// <para>zh-cn:AirTunnel 配置保存请求。配置和变更原因都通过 JSON 请求体传输，支持完整 Unicode 文本。</para>
/// <para>en-us:AirTunnel configuration save request. The options and change reason are carried in the JSON body so full Unicode text is supported.</para>
/// </summary>
public sealed class ReverseTunnelConfigurationSaveRequest
{
    /// <summary><para>zh-cn:待校验和保存的完整配置。</para><para>en-us:Complete configuration to validate and save.</para></summary>
    public ReverseTunnelOptions Options { get; init; } = new();

    /// <summary><para>zh-cn:本次配置变更原因，不能为空。</para><para>en-us:Reason for this configuration change; must not be empty.</para></summary>
    public string ChangeReason { get; init; } = string.Empty;
}

/// <summary>
/// <para>zh-cn:AirTunnel 配置备份恢复请求。恢复原因通过 JSON 请求体传输，支持完整 Unicode 文本。</para>
/// <para>en-us:AirTunnel configuration-backup restore request. The restore reason is carried in the JSON body so full Unicode text is supported.</para>
/// </summary>
public sealed class ReverseTunnelConfigurationRestoreRequest
{
    /// <summary><para>zh-cn:本次恢复变更原因，不能为空。</para><para>en-us:Reason for this restore operation; must not be empty.</para></summary>
    public string ChangeReason { get; init; } = string.Empty;
}

/// <summary>
/// <para>zh-cn:AirTunnel 配置校验结果。</para>
/// <para>en-us:AirTunnel configuration validation result.</para>
/// </summary>
public sealed class ReverseTunnelConfigurationValidationResult
{
    /// <summary><para>zh-cn:是否通过校验。</para><para>en-us:Whether validation succeeded.</para></summary>
    public bool Succeeded { get; init; }

    /// <summary><para>zh-cn:校验失败明细。</para><para>en-us:Validation failure details.</para></summary>
    public IReadOnlyList<string> Failures { get; init; } = [];

    /// <summary><para>zh-cn:标准化后的候选配置。</para><para>en-us:Normalized candidate options.</para></summary>
    public ReverseTunnelOptions Options { get; init; } = new();
}

/// <summary>
/// <para>zh-cn:AirTunnel 配置保存结果。</para>
/// <para>en-us:AirTunnel configuration save result.</para>
/// </summary>
public sealed class ReverseTunnelConfigurationSaveResult
{
    /// <summary><para>zh-cn:保存是否成功。</para><para>en-us:Whether the save succeeded.</para></summary>
    public bool Succeeded { get; init; }

    /// <summary><para>zh-cn:失败原因。</para><para>en-us:Failure reason.</para></summary>
    public IReadOnlyList<string> Failures { get; init; } = [];

    /// <summary><para>zh-cn:保存后的配置。</para><para>en-us:Saved options.</para></summary>
    public ReverseTunnelOptions Options { get; init; } = new();

    /// <summary><para>zh-cn:备份目录。</para><para>en-us:Backup directory.</para></summary>
    public string? BackupDirectory { get; init; }

    /// <summary><para>zh-cn:保存后配置版本。</para><para>en-us:Configuration version after the save.</para></summary>
    public long Version { get; init; }
}

/// <summary>
/// <para>zh-cn:AirTunnel 配置备份信息。</para>
/// <para>en-us:AirTunnel configuration backup information.</para>
/// </summary>
public sealed class ReverseTunnelConfigurationBackupInfo
{
    /// <summary><para>zh-cn:备份目录名称。</para><para>en-us:Backup directory name.</para></summary>
    public string Name { get; init; } = string.Empty;

    /// <summary><para>zh-cn:备份目录完整路径。</para><para>en-us:Full backup directory path.</para></summary>
    public string Path { get; init; } = string.Empty;

    /// <summary><para>zh-cn:备份创建时间。</para><para>en-us:Backup creation time.</para></summary>
    public DateTimeOffset CreatedAtUtc { get; init; }

    /// <summary><para>zh-cn:备份对应的配置版本。</para><para>en-us:Configuration version represented by the backup.</para></summary>
    public long Version { get; init; }

    /// <summary><para>zh-cn:执行变更的操作者。</para><para>en-us:Actor who performed the change.</para></summary>
    public string Actor { get; init; } = string.Empty;

    /// <summary><para>zh-cn:变更原因。</para><para>en-us:Reason for the change.</para></summary>
    public string Reason { get; init; } = string.Empty;
}

/// <summary>
/// <para>zh-cn:AirTunnel 配置审计记录。</para>
/// <para>en-us:AirTunnel configuration audit record.</para>
/// </summary>
public sealed class ReverseTunnelConfigurationAuditInfo
{
    /// <summary><para>zh-cn:配置版本。</para><para>en-us:Configuration version.</para></summary>
    public long Version { get; init; }
    /// <summary><para>zh-cn:变更时间。</para><para>en-us:Change time.</para></summary>
    public DateTimeOffset ChangedAtUtc { get; init; }
    /// <summary><para>zh-cn:操作者。</para><para>en-us:Actor.</para></summary>
    public string Actor { get; init; } = string.Empty;
    /// <summary><para>zh-cn:变更原因。</para><para>en-us:Change reason.</para></summary>
    public string Reason { get; init; } = string.Empty;
    /// <summary><para>zh-cn:关联的备份目录。</para><para>en-us:Related backup directory.</para></summary>
    public string? BackupDirectory { get; init; }
    /// <summary><para>zh-cn:差异摘要。</para><para>en-us:Difference summary.</para></summary>
    public string DifferenceSummary { get; init; } = string.Empty;
}

/// <summary>
/// <para>zh-cn:生成 Enrollment token 请求。</para>
/// <para>en-us:Enrollment-token generation request.</para>
/// </summary>
public sealed class ReverseTunnelEnrollmentTokenRequest
{
    /// <summary><para>zh-cn:要生成一次性凭据的 AgentId。</para><para>en-us:AgentId for which the one-time credential is generated.</para></summary>
    public string AgentId { get; set; } = string.Empty;

    /// <summary>
    /// <para>zh-cn:可选的 Agent 连接地址。写入引导 Token 后，客户端可以使用该地址完成首次连接。</para>
    /// <para>en-us:Optional Agent connection address. When included, the Agent can use it for the first connection.</para>
    /// </summary>
    public string ServerAddress { get; set; } = string.Empty;

    /// <summary>
    /// <para>zh-cn:Token 有效期，单位为小时，默认 24 小时。</para>
    /// <para>en-us:Token lifetime in hours; the default is 24 hours.</para>
    /// </summary>
    public int ExpiresInHours { get; set; } = 24;
}

/// <summary>
/// <para>zh-cn:生成 Enrollment token 响应。Token 只在该响应中返回一次。</para>
/// <para>en-us:Enrollment-token generation response. The token is returned only once in this response.</para>
/// </summary>
public sealed class ReverseTunnelEnrollmentTokenResponse
{
    /// <summary><para>zh-cn:AgentId。</para><para>en-us:AgentId.</para></summary>
    public string AgentId { get; init; } = string.Empty;

    /// <summary><para>zh-cn:一次性 token 明文。</para><para>en-us:Plain one-time token.</para></summary>
    public string Token { get; init; } = string.Empty;

    /// <summary><para>zh-cn:写入的挂载文件路径。</para><para>en-us:Mounted file path that was written.</para></summary>
    public string Path { get; init; } = string.Empty;
}

/// <summary>
/// <para>zh-cn:证书吊销请求。</para>
/// <para>en-us:Certificate revocation request.</para>
/// </summary>
public sealed class ReverseTunnelCertificateRevocationRequest
{
    /// <summary><para>zh-cn:签发者证书指纹。</para><para>en-us:Issuer certificate thumbprint.</para></summary>
    public string IssuerThumbprint { get; set; } = string.Empty;

    /// <summary><para>zh-cn:证书序列号。</para><para>en-us:Certificate serial number.</para></summary>
    public string SerialNumber { get; set; } = string.Empty;
}

/// <summary>
/// <para>zh-cn:远程客户端配置下发请求。当前版本只定义 Server API 边界，客户端执行协议后续单独扩展。</para>
/// <para>en-us:Remote client-configuration push request. The current version defines the Server API boundary; client-side execution protocol is extended separately.</para>
/// </summary>
public sealed class ReverseTunnelClientConfigurationPushRequest
{
    /// <summary><para>zh-cn:目标 sessionId。</para><para>en-us:Target sessionId.</para></summary>
    public string? SessionId { get; set; }

    /// <summary>
    /// <para>zh-cn:目标 AgentId。SessionId 不在线时使用该字段将配置排队保存。</para>
    /// <para>en-us:Target AgentId. When SessionId is offline, this field queues the desired configuration for replay.</para>
    /// </summary>
    public string? AgentId { get; set; }

    /// <summary><para>zh-cn:管理端读取配置时取得的期望版本；版本不一致时拒绝保存。</para><para>en-us:Expected version read by management; the save is rejected when it differs from the current version.</para></summary>
    public long ExpectedVersion { get; set; }

    /// <summary><para>zh-cn:要下发的客户端配置片段。</para><para>en-us:Client configuration fragment to push.</para></summary>
    public object? Configuration { get; set; }
}
