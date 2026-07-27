using Air.Cloud.Core.App.Options;
using Air.Cloud.Core.Attributes;

using Microsoft.Extensions.Configuration;

namespace Air.Cloud.Modules.ReverseTunnel.Common.Options;

/// <summary>
/// <para>zh-cn:反向隧道模块配置。模块默认禁用，必须显式设置 Enabled 并选择运行角色后才会在后续阶段建立网络连接或接管 Ocelot 路由。</para>
/// <para>en-us:Reverse tunnel module configuration. The module is disabled by default; a later phase establishes network connections or handles Ocelot routes only after Enabled and runtime roles are explicitly configured.</para>
/// </summary>
[ConfigurationInfo("ReverseTunnelSettings")]
public sealed class ReverseTunnelOptions : IConfigurableOptions<ReverseTunnelOptions, ReverseTunnelOptionsValidation>
{
    /// <summary><para>zh-cn:是否启用模块。默认 false。</para><para>en-us:Whether the module is enabled. The default is false.</para></summary>
    public bool Enabled { get; set; }

    /// <summary><para>zh-cn:当前进程启用的运行角色。</para><para>en-us:Runtime roles enabled by the current process.</para></summary>
    public ReverseTunnelRole Roles { get; set; } = ReverseTunnelRole.None;

    /// <summary><para>zh-cn:Agent 稳定身份。仅 Agent 角色需要配置。</para><para>en-us:Stable Agent identity. Only the Agent role requires this value.</para></summary>
    public string AgentId { get; set; } = string.Empty;

    /// <summary><para>zh-cn:Agent 主动连接的 Server 地址。仅 Agent 角色需要配置。</para><para>en-us:Server address to which the Agent actively connects. Only the Agent role requires this value.</para></summary>
    public string ServerAddress { get; set; } = string.Empty;

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

    /// <summary><para>zh-cn:由 Ocelot 虚拟主机定位的隧道路由集合。</para><para>en-us:Tunnel route collection located by Ocelot virtual hosts.</para></summary>
    public List<ReverseTunnelRouteOptions> Routes { get; set; } = [];

    /// <summary><para>zh-cn:Agent 可代理的本地服务集合。</para><para>en-us:Collection of local services that an Agent can proxy.</para></summary>
    public List<ReverseTunnelLocalServiceOptions> LocalServices { get; set; } = [];

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
    /// <para>zh-cn:Agent 首次注册或证书恢复使用的 Enrollment 配置。配置中只保存挂载目录，不保存一次性凭据内容。</para>
    /// <para>en-us:Enrollment configuration used for initial Agent registration or certificate recovery. Configuration stores only the mounted directory and never stores one-time credential values.</para>
    /// </summary>
    public ReverseTunnelEnrollmentOptions Enrollment { get; set; } = new();

    /// <summary>
    /// <para>zh-cn:Agent 侧证书材料路径配置。该配置仅保存本地挂载路径，私钥、证书字节和一次性凭据内容均不写入配置、数据库或 Consul。</para>
    /// <para>en-us:Agent-side certificate material path configuration. It stores only local mounted paths; private keys, certificate bytes, and one-time credential values are not written to configuration, databases, or Consul.</para>
    /// </summary>
    public ReverseTunnelAgentCertificateOptions AgentCertificate { get; set; } = new();

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
        HeartbeatIntervalSeconds = Math.Clamp(HeartbeatIntervalSeconds, 5, 300);
        SessionIdleTimeoutSeconds = Math.Max(SessionIdleTimeoutSeconds, HeartbeatIntervalSeconds * 2);
        MaxConcurrentStreams = Math.Clamp(MaxConcurrentStreams, 1, 4_096);
        MaxRequestBodyBytes = Math.Clamp(MaxRequestBodyBytes, 1_024, 1_073_741_824);
        MaxResponseBodyBytes = Math.Clamp(MaxResponseBodyBytes, 1_024, 1_073_741_824);
        MaxFramePayloadBytes = Math.Clamp(MaxFramePayloadBytes, 1_024, 1_048_576);
        OutboundFrameQueueCapacity = Math.Clamp(OutboundFrameQueueCapacity, 16, 65_536);
        AgentReconnectInitialDelaySeconds = Math.Clamp(AgentReconnectInitialDelaySeconds, 1, 300);
        AgentReconnectMaxDelaySeconds = Math.Clamp(Math.Max(AgentReconnectMaxDelaySeconds, AgentReconnectInitialDelaySeconds), AgentReconnectInitialDelaySeconds, 3600);

        Routes ??= [];
        LocalServices ??= [];
        AgentAuthorizations ??= [];
        CertificateAuthority ??= new ReverseTunnelCertificateAuthorityOptions();
        CertificateAuthority.Normalize();
        Enrollment ??= new ReverseTunnelEnrollmentOptions();
        Enrollment.Normalize();
        AgentCertificate ??= new ReverseTunnelAgentCertificateOptions();
        AgentCertificate.Normalize();

        foreach (var route in Routes)
        {
            route?.Normalize();
        }

        foreach (var localService in LocalServices)
        {
            localService?.Normalize();
        }

        foreach (var authorization in AgentAuthorizations)
        {
            authorization?.Normalize();
        }
    }
}
