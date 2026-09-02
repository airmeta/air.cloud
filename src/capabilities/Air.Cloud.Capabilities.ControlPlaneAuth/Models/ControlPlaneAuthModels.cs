namespace Air.Cloud.Capabilities.ControlPlaneAuth.Models;

/// <summary>
/// <para>zh-cn:控制面初始化状态。</para>
/// <para>en-us:Control-plane initialization status.</para>
/// </summary>
public sealed class ControlPlaneAuthState
{
    /// <summary><para>zh-cn:是否已完成管理员初始化。</para><para>en-us:Whether administrator initialization has been completed.</para></summary>
    public bool Initialized { get; set; }

    /// <summary><para>zh-cn:初始化 Ticket 的 hash 值。</para><para>en-us:Hash value of the bootstrap ticket.</para></summary>
    public string BootstrapTicketHash { get; set; } = string.Empty;

    /// <summary><para>zh-cn:初始化 Ticket 过期时间。</para><para>en-us:Bootstrap-ticket expiration time.</para></summary>
    public DateTimeOffset? BootstrapTicketExpiresAtUtc { get; set; }
}

/// <summary>
/// <para>zh-cn:控制面管理员用户。</para>
/// <para>en-us:Control-plane administrator user.</para>
/// </summary>
public sealed class ControlPlaneUser
{
    /// <summary><para>zh-cn:用户唯一标识。</para><para>en-us:Unique user identifier.</para></summary>
    public string Id { get; set; } = string.Empty;

    /// <summary><para>zh-cn:用户名。</para><para>en-us:User name.</para></summary>
    public string UserName { get; set; } = string.Empty;

    /// <summary><para>zh-cn:密码 hash。</para><para>en-us:Password hash.</para></summary>
    public string PasswordHash { get; set; } = string.Empty;

    /// <summary><para>zh-cn:是否启用。</para><para>en-us:Whether the user is enabled.</para></summary>
    public bool Enabled { get; set; } = true;

    /// <summary><para>zh-cn:连续登录失败次数。</para><para>en-us:Consecutive failed login attempts.</para></summary>
    public int FailedAttempts { get; set; }

    /// <summary><para>zh-cn:锁定截止时间。</para><para>en-us:Lockout end time.</para></summary>
    public DateTimeOffset? LockedUntilUtc { get; set; }

    /// <summary><para>zh-cn:创建时间。</para><para>en-us:Creation time.</para></summary>
    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    /// <summary><para>zh-cn:使用 ASP.NET Data Protection 加密后的 TOTP 密钥。明文密钥不会写入运行数据文件。</para><para>en-us:The TOTP secret protected with ASP.NET Data Protection. The clear-text secret is never written to the runtime data file.</para></summary>
    public string MfaSecretProtected { get; set; } = string.Empty;

    /// <summary><para>zh-cn:最近一次成功使用的 TOTP 时间片，用于阻止同一验证码重复使用。</para><para>en-us:The most recently accepted TOTP time-step, used to prevent reuse of the same code.</para></summary>
    public long? MfaLastUsedTimeStep { get; set; }
}

/// <summary>
/// <para>zh-cn:控制面管理会话。</para>
/// <para>en-us:Control-plane management session.</para>
/// </summary>
public sealed class ControlPlaneSession
{
    /// <summary><para>zh-cn:会话唯一标识。</para><para>en-us:Unique session identifier.</para></summary>
    public string Id { get; set; } = string.Empty;

    /// <summary><para>zh-cn:会话 Ticket hash。</para><para>en-us:Session-ticket hash.</para></summary>
    public string TicketHash { get; set; } = string.Empty;

    /// <summary><para>zh-cn:用户唯一标识。</para><para>en-us:Unique user identifier.</para></summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary><para>zh-cn:用户名快照。</para><para>en-us:User-name snapshot.</para></summary>
    public string UserName { get; set; } = string.Empty;

    /// <summary><para>zh-cn:过期时间。</para><para>en-us:Expiration time.</para></summary>
    public DateTimeOffset ExpiresAtUtc { get; set; }

    /// <summary><para>zh-cn:是否已吊销。</para><para>en-us:Whether the session is revoked.</para></summary>
    public bool Revoked { get; set; }

    /// <summary><para>zh-cn:创建时间。</para><para>en-us:Creation time.</para></summary>
    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
}

/// <summary>
/// <para>zh-cn:控制面服务间票据。</para>
/// <para>en-us:Control-plane service-to-service ticket.</para>
/// </summary>
public sealed class ControlPlaneServiceTicket
{
    /// <summary><para>zh-cn:票据唯一标识。</para><para>en-us:Unique ticket identifier.</para></summary>
    public string Id { get; set; } = string.Empty;

    /// <summary><para>zh-cn:票据类型，例如 airtunnel-gateway。</para><para>en-us:Ticket type, such as airtunnel-gateway.</para></summary>
    public string Type { get; set; } = string.Empty;

    /// <summary><para>zh-cn:票据名称。</para><para>en-us:Ticket name.</para></summary>
    public string Name { get; set; } = string.Empty;

    /// <summary><para>zh-cn:票据 hash。</para><para>en-us:Ticket hash.</para></summary>
    public string TicketHash { get; set; } = string.Empty;

    /// <summary><para>zh-cn:是否启用。</para><para>en-us:Whether the ticket is enabled.</para></summary>
    public bool Enabled { get; set; } = true;

    /// <summary><para>zh-cn:创建时间。</para><para>en-us:Creation time.</para></summary>
    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    /// <summary><para>zh-cn:过期时间。</para><para>en-us:Expiration time.</para></summary>
    public DateTimeOffset? ExpiresAtUtc { get; set; }

    /// <summary><para>zh-cn:最后使用时间。</para><para>en-us:Last-used time.</para></summary>
    public DateTimeOffset? LastUsedAtUtc { get; set; }
}

/// <summary>
/// <para>zh-cn:创建服务票据后的结果。明文 Ticket 只在创建或轮换时返回一次。</para>
/// <para>en-us:Result returned after creating a service ticket. The clear-text ticket is returned only once during creation or rotation.</para>
/// </summary>
public sealed record ControlPlaneServiceTicketCreatedResult(ControlPlaneServiceTicket Ticket, string PlainTextTicket);
