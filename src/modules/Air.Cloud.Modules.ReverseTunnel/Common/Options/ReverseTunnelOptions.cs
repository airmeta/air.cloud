using Air.Cloud.Core.App.Options;
using Air.Cloud.Core.Attributes;

using Microsoft.Extensions.Configuration;

namespace Air.Cloud.Modules.ReverseTunnel.Common.Options;

/// <summary>
/// <para>zh-cn:反向隧道模块配置。模块默认禁用；启用后由 TunnelMode 决定连接端角色，由 EnableTunnelProxy 独立决定是否接管动态 HTTP 路由。</para>
/// <para>en-us:Reverse tunnel module configuration. The module is disabled by default; when enabled, TunnelMode selects the connection endpoint role and EnableTunnelProxy independently controls dynamic HTTP route interception.</para>
/// </summary>
[ConfigurationInfo("ReverseTunnelSettings")]
public sealed class ReverseTunnelOptions : IConfigurableOptions<ReverseTunnelOptions, ReverseTunnelOptionsValidation>
{
    /// <summary><para>zh-cn:是否启用模块。默认 false。</para><para>en-us:Whether the module is enabled. The default is false.</para></summary>
    public bool Enabled { get; set; }

    /// <summary><para>zh-cn:当前进程的隧道连接模式。Server 接收 Agent，Agent 主动连接 Server；None 仅用于不承载连接端的管理或独立远程 Gateway 场景。</para><para>en-us:Connection mode for the current process. Server accepts Agents, Agent actively connects to Server, and None is reserved for management-only or standalone remote Gateway scenarios.</para></summary>
    public ReverseTunnelMode TunnelMode { get; set; }

    /// <summary><para>zh-cn:是否启用 Gateway 动态隧道代理。该开关只控制 HTTP 路由拦截，不改变 Server 或 Agent 的连接职责。</para><para>en-us:Whether to enable Gateway dynamic tunnel proxying. This switch controls only HTTP route interception and does not change the Server or Agent connection responsibility.</para></summary>
    public bool EnableTunnelProxy { get; set; }

    /// <summary><para>zh-cn:Agent 稳定身份。仅 Agent 模式需要配置。</para><para>en-us:Stable Agent identity. Only Agent mode requires this value.</para></summary>
    public string AgentId { get; set; } = string.Empty;

    /// <summary><para>zh-cn:Agent 主动连接的 Server 地址。仅 Agent 模式需要配置。</para><para>en-us:Server address to which the Agent actively connects. Only Agent mode requires this value.</para></summary>
    public string ServerAddress { get; set; } = string.Empty;

    /// <summary>
    /// <para>zh-cn:单个 Agent 进程向 Server 建立的物理隧道连接数量。默认值为 1；调高后仍使用同一个 AgentId 作为安全身份，但每条连接会注册为独立会话，用于在单 Gateway/Server 进程内分摊并发 HTTP 请求。</para>
    /// <para>en-us:Number of physical tunnel connections opened from one Agent process to the Server. The default is 1; higher values keep the same AgentId security identity, but each connection registers as an independent session so a single Gateway/Server process can spread concurrent HTTP requests.</para>
    /// </summary>
    public int AgentConnectionCount { get; set; } = 1;

    /// <summary><para>zh-cn:协议版本。当前仅支持版本 1。</para><para>en-us:Protocol version. Only version 1 is currently supported.</para></summary>
    public int ProtocolVersion { get; set; } = 1;

    /// <summary><para>zh-cn:连接心跳间隔秒数。默认 15 秒。</para><para>en-us:Connection heartbeat interval in seconds. The default is 15 seconds.</para></summary>
    public int HeartbeatIntervalSeconds { get; set; } = 15;

    /// <summary><para>zh-cn:无心跳会话的摘除阈值秒数。默认 45 秒。</para><para>en-us:Eviction threshold in seconds for a session without a heartbeat. The default is 45 seconds.</para></summary>
    public int SessionIdleTimeoutSeconds { get; set; } = 45;

    /// <summary><para>zh-cn:每个 Agent 会话允许的最大并发请求流。默认 128。</para><para>en-us:Maximum concurrent request streams allowed per Agent session. The default is 128.</para></summary>
    public int MaxConcurrentStreams { get; set; } = 128;

    /// <summary><para>zh-cn:单次 HTTP 请求体允许的最大字节数。默认 10 MiB。</para><para>en-us:Maximum byte size allowed for a single HTTP request body. The default is 10 MiB.</para></summary>
    public long MaxRequestBodyBytes { get; set; } = 10 * 1024 * 1024;

    /// <summary><para>zh-cn:单次 HTTP 响应体允许的最大字节数。默认 10 MiB；超过该值时 Agent 或 Gateway 会终止本次交换，避免把过大的下游响应聚合到内存。</para><para>en-us:Maximum byte size allowed for a single HTTP response body. The default is 10 MiB; Agent or Gateway terminates the exchange when the downstream response exceeds the limit to avoid aggregating oversized payloads in memory.</para></summary>
    public long MaxResponseBodyBytes { get; set; } = 10 * 1024 * 1024;

    /// <summary><para>zh-cn:单个隧道数据帧承载的最大 payload 字节数。默认 64 KiB；请求体和响应体会按该值拆分为多个帧。</para><para>en-us:Maximum payload bytes carried by one tunnel data frame. The default is 64 KiB; request and response bodies are split into multiple frames by this value.</para></summary>
    public int MaxFramePayloadBytes { get; set; } = 64 * 1024;

    /// <summary><para>zh-cn:每个 Agent 会话的出站帧队列容量。默认 1024；队列满时写入方等待，以形成基础背压。</para><para>en-us:Outbound frame queue capacity per Agent session. The default is 1024; writers wait when the queue is full to provide basic backpressure.</para></summary>
    public int OutboundFrameQueueCapacity { get; set; } = 1024;

    /// <summary>
    /// <para>zh-cn:同一个 ServiceKey 存在多个 Agent 会话时的选择策略。默认 `NewestActive`，保持已有“最近活跃会话优先”的行为；`RoundRobin` 仅在当前进程内轮询，不提供跨 Gateway 全局负载均衡。</para>
    /// <para>en-us:Selection strategy used when multiple Agent sessions declare the same ServiceKey. The default is `NewestActive`, preserving the existing most-recently-active behavior; `RoundRobin` rotates only within the current process and does not provide cross-Gateway global load balancing.</para>
    /// </summary>
    public ReverseTunnelSessionSelectionStrategy SessionSelectionStrategy { get; set; } = ReverseTunnelSessionSelectionStrategy.NewestActive;

    /// <summary><para>zh-cn:Agent 连接失败后的初始重连等待秒数。默认 5 秒。</para><para>en-us:Initial reconnect delay in seconds after an Agent connection failure. The default is 5 seconds.</para></summary>
    public int AgentReconnectInitialDelaySeconds { get; set; } = 5;

    /// <summary><para>zh-cn:Agent 指数退避重连的最大等待秒数。默认 60 秒。</para><para>en-us:Maximum reconnect delay in seconds for Agent exponential backoff. The default is 60 seconds.</para></summary>
    public int AgentReconnectMaxDelaySeconds { get; set; } = 60;

    /// <summary>
    /// <para>zh-cn:Agent 周期主动查询 Server 目标配置的设置。默认每 30 秒查询一次。</para>
    /// <para>en-us:Settings for the Agent's periodic active queries of Server desired configuration. The default interval is 30 seconds.</para>
    /// </summary>
    public ReverseTunnelConfigurationSyncOptions ConfigurationSync { get; set; } = new();

    /// <summary><para>zh-cn:已废弃的显式隧道路由集合。新 Gateway 只使用 Agent 配置目录派生路由，该集合不会被读取。</para><para>en-us:Deprecated explicit tunnel route collection. The new Gateway uses only routes derived from the Agent configuration catalog and never reads this collection.</para></summary>
    public List<ReverseTunnelRouteOptions> Routes { get; set; } = [];

    /// <summary><para>zh-cn:Agent 可代理的本地服务集合。</para><para>en-us:Collection of local services that an Agent can proxy.</para></summary>
    public List<ReverseTunnelLocalServiceOptions> LocalServices { get; set; } = [];

    /// <summary>
    /// <para>zh-cn:Gateway 侧远程调用配置。用于 Gateway 不承载 Server 角色时，把 Ocelot 命中的隧道请求转发给独立 AirTunnel Server 进程。</para>
    /// <para>en-us:Gateway-side remote invocation options. These settings are used when the Gateway process does not use Server mode and needs to forward Ocelot tunnel requests to a standalone AirTunnel Server process.</para>
    /// </summary>
    public ReverseTunnelGatewayOptions Gateway { get; set; } = new();

    /// <summary>
    /// <para>zh-cn:TCP 穿透配置。Server 角色按监听器绑定端口，Agent 角色按本地服务声明真实目标。</para>
    /// <para>en-us:TCP tunneling options. Server mode binds ports through listeners, while Agent mode declares real local targets through local services.</para>
    /// </summary>
    public ReverseTunnelTcpOptions Tcp { get; set; } = new();

    /// <summary>
    /// <para>zh-cn:UDP 穿透配置。Server 角色按监听器绑定端口并维护流表，Agent 角色按本地服务声明真实目标。</para>
    /// <para>en-us:UDP tunneling options. Server mode binds ports and maintains flow entries, while Agent mode declares real local targets through local services.</para>
    /// </summary>
    public ReverseTunnelUdpOptions Udp { get; set; } = new();

    /// <summary>
    /// <para>zh-cn:Server 侧 Agent 服务声明授权列表。为空时保持兼容行为，允许已通过 mTLS 认证的 Agent 声明任意 ServiceKey；配置后只允许匹配 AgentId 的白名单 ServiceKey。</para>
    /// <para>en-us:Server-side authorization list for Agent service declarations. When empty, backward compatibility is preserved and an mTLS-authenticated Agent may declare any ServiceKey; once configured, only whitelisted ServiceKeys for the matched AgentId are allowed.</para>
    /// </summary>
    public List<ReverseTunnelAgentAuthorizationOptions> AgentAuthorizations { get; set; } = [];

    /// <summary>
    /// <para>zh-cn:Server 内置 CA 的挂载文件配置。仅保存路径和环境变量名，不保存证书密码或私钥材料。</para>
    /// <para>en-us:Mounted-file configuration for the Server built-in CA. It stores only paths and environment-variable names, never certificate passwords or private-key material.</para>
    /// </summary>
    public ReverseTunnelCertificateAuthorityOptions CertificateAuthority { get; set; } = new();

    /// <summary>
    /// <para>zh-cn:Server TLS 证书的主题和 SAN 配置。该配置只用于描述可迁移的证书身份，证书生成和私钥保存仍由部署工具负责。</para>
    /// <para>en-us:Subject and SAN configuration for the Server TLS certificate. It describes the portable certificate identity only; deployment tooling remains responsible for certificate generation and private-key storage.</para>
    /// </summary>
    public ReverseTunnelServerCertificateOptions ServerCertificate { get; set; } = new();

    /// <summary>
    /// <para>zh-cn:Agent 首次注册或证书恢复使用的 Enrollment 配置。首次注册通过 CA 加密 GUID 和服务端 replay store 完成。</para>
    /// <para>en-us:Enrollment configuration used for initial Agent registration or certificate recovery. Initial registration uses a CA-encrypted GUID and the Server replay store.</para>
    /// </summary>
    public ReverseTunnelEnrollmentOptions Enrollment { get; set; } = new();

    /// <summary>
    /// <para>zh-cn:Agent 侧证书材料路径配置。该配置仅保存本地挂载路径，私钥、证书字节和一次性凭据内容均不写入配置、数据库或 Consul。</para>
    /// <para>en-us:Agent-side certificate material path configuration. It stores only local mounted paths; private keys, certificate bytes, and one-time credential values are not written to configuration, databases, or Consul.</para>
    /// </summary>
    public ReverseTunnelAgentCertificateOptions AgentCertificate { get; set; } = new();

    /// <summary>
    /// <para>zh-cn:诊断 HTTP API 配置。该配置只控制当前进程内的只读快照接口，不会启用外部观测平台或远程采集器。</para>
    /// <para>en-us:Diagnostic HTTP API options. These options only control read-only in-process snapshot APIs and do not enable an external observability platform or remote collector.</para>
    /// </summary>
    public ReverseTunnelDiagnosticsOptions Diagnostics { get; set; } = new();

    /// <summary>
    /// <para>zh-cn:Server 远程管理端配置。管理端应使用独立监听端口，并通过访问 token 控制配置写入和客户端运维动作。</para>
    /// <para>en-us:Server remote-management options. The management endpoint should use a dedicated listen port and protect configuration writes and client operations with an access token.</para>
    /// </summary>
    public ReverseTunnelManagementOptions Management { get; set; } = new();

    /// <summary>
    /// <para>zh-cn:标准化配置默认值和字符串键。该过程不自动启用模块，也不允许通过默认值创建可访问的网络入口。</para>
    /// <para>en-us:Normalizes configuration defaults and string keys. The process does not enable the module or create an accessible network entry through defaults.</para>
    /// </summary>
    /// <param name="options">
    /// <para>zh-cn:已绑定的选项实例。</para>
    /// <para>en-us:The bound options instance.</para>
    /// </param>
    /// <param name="configuration">
    /// <para>zh-cn:应用配置，当前阶段仅用于保持标准选项签名。</para>
    /// <para>en-us:Application configuration, retained only to satisfy the standard options signature in the current phase.</para>
    /// </param>
    public void PostConfigure(ReverseTunnelOptions options, IConfiguration configuration)
    {
        ProtocolVersion = ProtocolVersion <= 0 ? 1 : ProtocolVersion;
        AgentId = AgentId?.Trim() ?? string.Empty;
        ServerAddress = ServerAddress?.Trim().TrimEnd('/') ?? string.Empty;
        AgentConnectionCount = Math.Clamp(AgentConnectionCount, 1, 64);
        HeartbeatIntervalSeconds = Math.Clamp(HeartbeatIntervalSeconds, 5, 300);
        SessionIdleTimeoutSeconds = Math.Max(SessionIdleTimeoutSeconds, HeartbeatIntervalSeconds * 2);
        MaxConcurrentStreams = Math.Clamp(MaxConcurrentStreams, 1, 4_096);
        MaxRequestBodyBytes = Math.Clamp(MaxRequestBodyBytes, 1_024, 1_073_741_824);
        MaxResponseBodyBytes = Math.Clamp(MaxResponseBodyBytes, 1_024, 1_073_741_824);
        MaxFramePayloadBytes = Math.Clamp(MaxFramePayloadBytes, 1_024, 1_048_576);
        OutboundFrameQueueCapacity = Math.Clamp(OutboundFrameQueueCapacity, 16, 65_536);
        AgentReconnectInitialDelaySeconds = Math.Clamp(AgentReconnectInitialDelaySeconds, 1, 300);
        AgentReconnectMaxDelaySeconds = Math.Clamp(Math.Max(AgentReconnectMaxDelaySeconds, AgentReconnectInitialDelaySeconds), AgentReconnectInitialDelaySeconds, 3600);
        ConfigurationSync ??= new ReverseTunnelConfigurationSyncOptions();
        ConfigurationSync.Normalize();

        Routes ??= [];
        LocalServices ??= [];
        Gateway ??= new ReverseTunnelGatewayOptions();
        Gateway.Normalize();
        Tcp ??= new ReverseTunnelTcpOptions();
        Tcp.Normalize();
        Udp ??= new ReverseTunnelUdpOptions();
        Udp.Normalize();
        AgentAuthorizations ??= [];
        CertificateAuthority ??= new ReverseTunnelCertificateAuthorityOptions();
        CertificateAuthority.Normalize();
        ServerCertificate ??= new ReverseTunnelServerCertificateOptions();
        ServerCertificate.Normalize();
        Enrollment ??= new ReverseTunnelEnrollmentOptions();
        Enrollment.Normalize();
        AgentCertificate ??= new ReverseTunnelAgentCertificateOptions();
        AgentCertificate.Normalize();
        Diagnostics ??= new ReverseTunnelDiagnosticsOptions();
        Diagnostics.Normalize();
        Management ??= new ReverseTunnelManagementOptions();
        Management.Normalize();

        foreach (var route in Routes)
        {
            route?.Normalize();
        }

        foreach (var localService in LocalServices)
        {
            localService?.Normalize();
        }

        NormalizeAgentAuthorizations();
    }

    private void NormalizeAgentAuthorizations()
    {
        var merged = new Dictionary<string, ReverseTunnelAgentAuthorizationOptions>(StringComparer.OrdinalIgnoreCase);
        foreach (var authorization in AgentAuthorizations.Where(authorization => authorization is not null))
        {
            authorization.Normalize();
            if (string.IsNullOrWhiteSpace(authorization.AgentId))
            {
                merged[$"__invalid_{merged.Count}"] = authorization;
                continue;
            }

            if (!merged.TryGetValue(authorization.AgentId, out var existing))
            {
                merged[authorization.AgentId] = authorization;
                continue;
            }

            existing.AllowedServiceKeys = existing.AllowedServiceKeys
                .Concat(authorization.AllowedServiceKeys)
                .Where(key => !string.IsNullOrWhiteSpace(key))
                .Select(key => key.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        AgentAuthorizations = merged.Values.ToList();
    }
}
