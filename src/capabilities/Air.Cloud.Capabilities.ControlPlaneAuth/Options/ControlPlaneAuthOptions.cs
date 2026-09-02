using Air.Cloud.Core.App.Options;
using Air.Cloud.Core.Attributes;

using Microsoft.Extensions.Configuration;

namespace Air.Cloud.Capabilities.ControlPlaneAuth.Options;

/// <summary>
/// <para>zh-cn:控制面认证能力配置。该配置只描述管理面初始化、登录、会话和服务票据策略，运行期用户、会话、票据 hash 与审计数据应写入 StorageDirectory。</para>
/// <para>en-us:Options for the control-plane authentication capability. These settings describe initialization, login, session, and service-ticket policies only; runtime users, sessions, ticket hashes, and audit data should be written to StorageDirectory.</para>
/// </summary>
[ConfigurationInfo("ControlPlaneAuth")]
public sealed class ControlPlaneAuthOptions : IConfigurableOptions<ControlPlaneAuthOptions>
{
    /// <summary><para>zh-cn:是否启用控制面认证能力。</para><para>en-us:Whether the control-plane authentication capability is enabled.</para></summary>
    public bool Enabled { get; set; } = true;

    /// <summary><para>zh-cn:是否强制所有控制面用户使用基于标准 TOTP 协议的多因素认证。启用后，未绑定 MFA 的用户必须先完成 Authenticator 绑定。</para><para>en-us:Whether all control-plane users must use standards-based TOTP multi-factor authentication. When enabled, users without an enrolled authenticator must complete enrollment before receiving a session ticket.</para></summary>
    public bool EnableMFA { get; set; }

    /// <summary><para>zh-cn:宿主产品编码，用于审计和多产品识别。</para><para>en-us:Host product code used for auditing and multi-product identification.</para></summary>
    public string ProductCode { get; set; } = "air-cloud";

    /// <summary><para>zh-cn:宿主产品名称，用于内置页面展示。</para><para>en-us:Host product name displayed by the built-in pages.</para></summary>
    public string ProductName { get; set; } = "Air.Cloud";

    /// <summary><para>zh-cn:Authenticator 二维码中的发行方标识；未配置时使用 airForge。</para><para>en-us:Issuer label embedded in the Authenticator QR code; airForge is used when it is not configured.</para></summary>
    public string MfaIssuer { get; set; } = "airForge";

    /// <summary><para>zh-cn:控制面页面和 API 的基础路径。</para><para>en-us:Base path for control-plane pages and APIs.</para></summary>
    public string BasePath { get; set; } = "/_control-plane";

    /// <summary><para>zh-cn:初始化和登录页路径。为空时使用 BasePath + /auth。</para><para>en-us:Initialization and sign-in page path. BasePath + /auth is used when empty.</para></summary>
    public string AuthPath { get; set; } = string.Empty;

    /// <summary><para>zh-cn:H5 控制面容器页路径。为空时使用 BasePath。</para><para>en-us:H5 control-plane shell page path. BasePath is used when empty.</para></summary>
    public string PlanePath { get; set; } = string.Empty;

    /// <summary><para>zh-cn:登录成功后的默认跳转路径。该值仅允许同站相对路径，并且在请求 query 不含 returnUrl 时生效。</para><para>en-us:Default redirect path after a successful sign-in. Only same-site relative paths are allowed, and the value is used only when the request query does not contain returnUrl.</para></summary>
    public string DefaultReturnUrl { get; set; } = string.Empty;

    /// <summary><para>zh-cn:控制面认证运行数据目录。相对路径由宿主 ContentRootPath 解析。</para><para>en-us:Runtime data directory for control-plane authentication. Relative paths are resolved from the host ContentRootPath.</para></summary>
    public string StorageDirectory { get; set; } = "data/control-plane-auth";

    /// <summary><para>zh-cn:首次初始化 Ticket 配置。</para><para>en-us:Bootstrap-ticket options for first initialization.</para></summary>
    public ControlPlaneBootstrapOptions Bootstrap { get; set; } = new();

    /// <summary><para>zh-cn:管理会话配置。</para><para>en-us:Management-session options.</para></summary>
    public ControlPlaneSessionOptions Session { get; set; } = new();

    /// <summary><para>zh-cn:管理员密码策略。</para><para>en-us:Administrator password policy.</para></summary>
    public ControlPlanePasswordPolicyOptions PasswordPolicy { get; set; } = new();

    /// <summary><para>zh-cn:登录失败和锁定策略。</para><para>en-us:Login failure and lockout policy.</para></summary>
    public ControlPlaneLoginPolicyOptions LoginPolicy { get; set; } = new();

    /// <summary><para>zh-cn:服务间票据管理策略。</para><para>en-us:Service-to-service ticket management policy.</para></summary>
    public ControlPlaneServiceTicketOptions ServiceTickets { get; set; } = new();

    /// <summary><para>zh-cn:审计配置。</para><para>en-us:Audit options.</para></summary>
    public ControlPlaneAuditOptions Audit { get; set; } = new();

    /// <summary><para>zh-cn:H5 控制台容器配置，用于承载当前认证能力页面和宿主业务管理页。</para><para>en-us:H5 console-shell options used to host the authentication capability pages and host-product management pages.</para></summary>
    public ControlPlaneShellOptions Shell { get; set; } = new();

    /// <summary><para>zh-cn:控制面管理监听配置。Port 大于零时，宿主应额外监听该端口；端口为零时继续使用宿主主监听端口。</para><para>en-us:Control-plane management listener options. When Port is greater than zero, the host should add a listener on that port; zero keeps the primary host listener.</para></summary>
    public ControlPlaneManagementOptions Management { get; set; } = new();

    /// <inheritdoc />
    public void PostConfigure(ControlPlaneAuthOptions options, IConfiguration configuration)
    {
        Enabled = options.Enabled;
        ProductCode = NormalizeToken(options.ProductCode, "air-cloud");
        ProductName = string.IsNullOrWhiteSpace(options.ProductName) ? "Air.Cloud" : options.ProductName.Trim();
        MfaIssuer = NormalizeToken(options.MfaIssuer, "airForge");
        BasePath = NormalizePath(options.BasePath, "/_control-plane");
        AuthPath = NormalizePath(options.AuthPath, BasePath + "/auth");
        PlanePath = NormalizePath(options.PlanePath, BasePath);
        DefaultReturnUrl = NormalizeOptionalReturnPath(options.DefaultReturnUrl);
        StorageDirectory = string.IsNullOrWhiteSpace(options.StorageDirectory) ? "data/control-plane-auth" : options.StorageDirectory.Trim();
        Bootstrap = options.Bootstrap ?? new();
        Bootstrap.Normalize();
        Session = options.Session ?? new();
        Session.Normalize();
        PasswordPolicy = options.PasswordPolicy ?? new();
        PasswordPolicy.Normalize();
        LoginPolicy = options.LoginPolicy ?? new();
        LoginPolicy.Normalize();
        ServiceTickets = options.ServiceTickets ?? new();
        ServiceTickets.Normalize();
        Audit = options.Audit ?? new();
        Audit.Normalize();
        Shell = options.Shell ?? new();
        Shell.Normalize(DefaultReturnUrl);
        Management = options.Management ?? new();
        Management.Normalize();
    }

    internal void Normalize()
    {
        PostConfigure(this, new ConfigurationBuilder().Build());
    }

    private static string NormalizePath(string? value, string fallback)
    {
        var normalized = string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
        normalized = "/" + normalized.Trim('/');
        return normalized == "/" ? fallback : normalized;
    }

    private static string NormalizeToken(string? value, string fallback)
    {
        return string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
    }

    private static string NormalizeOptionalReturnPath(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var normalized = value.Trim();
        if (!normalized.StartsWith("/", StringComparison.Ordinal)
            || normalized.StartsWith("//", StringComparison.Ordinal)
            || normalized.Contains('\\', StringComparison.Ordinal))
        {
            return string.Empty;
        }

        return normalized;
    }
}

/// <summary>
/// <para>zh-cn:控制面管理监听配置。</para>
/// <para>en-us:Control-plane management listener configuration.</para>
/// </summary>
public sealed class ControlPlaneManagementOptions
{
    /// <summary><para>zh-cn:是否启用控制面管理页面和 API。</para><para>en-us:Whether to enable control-plane management pages and APIs.</para></summary>
    public bool Enabled { get; set; } = true;

    /// <summary><para>zh-cn:独立管理监听端口；大于零时由宿主额外监听，零表示不增加监听器。</para><para>en-us:Dedicated management listener port; a positive value adds a listener, while zero adds no listener.</para></summary>
    public int Port { get; set; }

    /// <summary><para>zh-cn:独立管理监听绑定地址，默认监听所有 IPv4 地址。</para><para>en-us:Bind address for the dedicated management listener; all IPv4 addresses are used by default.</para></summary>
    public string BindAddress { get; set; } = "0.0.0.0";

    /// <summary><para>zh-cn:是否只允许在 Port 对应监听器访问控制面路径；启用独立端口时默认开启。</para><para>en-us:Whether control-plane paths are allowed only on the listener identified by Port; enabled by default when a dedicated port is configured.</para></summary>
    public bool RestrictToPort { get; set; } = true;

    internal void Normalize()
    {
        if (Port is < 0 or > 65535)
        {
            Port = 0;
        }

        BindAddress = string.IsNullOrWhiteSpace(BindAddress) ? "0.0.0.0" : BindAddress.Trim();
    }
}

/// <summary>
/// <para>zh-cn:控制面 H5 容器配置。</para>
/// <para>en-us:Control-plane H5 shell options.</para>
/// </summary>
public sealed class ControlPlaneShellOptions
{
    /// <summary><para>zh-cn:是否启用 H5 容器布局；关闭后仍显示内置登录和票据管理内容。</para><para>en-us:Whether the H5 shell layout is enabled; when disabled, built-in sign-in and ticket management content is still shown.</para></summary>
    public bool Enabled { get; set; } = true;

    /// <summary><para>zh-cn:容器标题；为空时使用 ProductName。</para><para>en-us:Shell title; ProductName is used when empty.</para></summary>
    public string Title { get; set; } = string.Empty;

    /// <summary><para>zh-cn:默认打开的菜单键。</para><para>en-us:Default menu key to open.</para></summary>
    public string DefaultMenuKey { get; set; } = "overview";

    /// <summary><para>zh-cn:是否在菜单中显示服务票据管理页。</para><para>en-us:Whether to show the service-ticket management page in the menu.</para></summary>
    public bool ShowServiceTickets { get; set; } = true;

    /// <summary><para>zh-cn:由控制面容器承载的 iframe 应用列表。</para><para>en-us:List of iframe applications hosted by the control-plane shell.</para></summary>
    public List<ControlPlaneShellFrameOptions> Frames { get; set; } = new();

    internal void Normalize(string defaultReturnUrl)
    {
        Title = string.IsNullOrWhiteSpace(Title) ? string.Empty : Title.Trim();
        DefaultMenuKey = string.IsNullOrWhiteSpace(DefaultMenuKey) ? "overview" : DefaultMenuKey.Trim();
        Frames ??= new();
        foreach (var frame in Frames)
        {
            frame.Normalize();
        }

        if (Frames.Count == 0 && !string.IsNullOrWhiteSpace(defaultReturnUrl))
        {
            Frames.Add(new ControlPlaneShellFrameOptions
            {
                Key = "host-manager",
                Title = "宿主管理",
                Url = defaultReturnUrl,
                Embedded = true
            });
        }
    }
}

/// <summary>
/// <para>zh-cn:控制面 H5 容器 iframe 菜单项配置。</para>
/// <para>en-us:Control-plane H5 shell iframe menu-item options.</para>
/// </summary>
public sealed class ControlPlaneShellFrameOptions
{
    /// <summary><para>zh-cn:菜单唯一键。</para><para>en-us:Unique menu key.</para></summary>
    public string Key { get; set; } = string.Empty;

    /// <summary><para>zh-cn:菜单标题。</para><para>en-us:Menu title.</para></summary>
    public string Title { get; set; } = string.Empty;

    /// <summary><para>zh-cn:iframe 地址；仅允许同站相对路径。</para><para>en-us:Iframe URL; only same-site relative paths are allowed.</para></summary>
    public string Url { get; set; } = string.Empty;

    /// <summary><para>zh-cn:是否追加 embedded=1，供被承载页面进入嵌入模式。</para><para>en-us:Whether to append embedded=1 so the hosted page can enter embedded mode.</para></summary>
    public bool Embedded { get; set; } = true;

    internal void Normalize()
    {
        Key = string.IsNullOrWhiteSpace(Key) ? "frame" : Key.Trim();
        Title = string.IsNullOrWhiteSpace(Title) ? Key : Title.Trim();
        Url = NormalizeShellFrameUrl(Url);
    }

    private static string NormalizeShellFrameUrl(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var normalized = value.Trim();
        if (!normalized.StartsWith("/", StringComparison.Ordinal)
            || normalized.StartsWith("//", StringComparison.Ordinal)
            || normalized.Contains('\\', StringComparison.Ordinal))
        {
            return string.Empty;
        }

        return normalized;
    }
}

/// <summary>
/// <para>zh-cn:控制面首次初始化 Ticket 配置。</para>
/// <para>en-us:Control-plane bootstrap-ticket options.</para>
/// </summary>
public sealed class ControlPlaneBootstrapOptions
{
    /// <summary><para>zh-cn:是否启用首次初始化 Ticket。</para><para>en-us:Whether the first-initialization ticket is enabled.</para></summary>
    public bool Enabled { get; set; } = true;

    /// <summary><para>zh-cn:初始化 Ticket 过期分钟数。</para><para>en-us:Bootstrap-ticket expiration in minutes.</para></summary>
    public int TicketExpiresMinutes { get; set; } = 30;

    /// <summary><para>zh-cn:是否把初始化 Ticket 输出到控制台。</para><para>en-us:Whether the bootstrap ticket is printed to the console.</para></summary>
    public bool PrintTicketToConsole { get; set; } = true;

    /// <summary><para>zh-cn:是否把初始化 Ticket 写入一次性文件。</para><para>en-us:Whether the bootstrap ticket is written to a one-time file.</para></summary>
    public bool WriteTicketFile { get; set; } = true;

    /// <summary><para>zh-cn:初始化 Ticket 文件名。</para><para>en-us:Bootstrap-ticket file name.</para></summary>
    public string TicketFileName { get; set; } = "initial-admin-ticket";

    internal void Normalize()
    {
        TicketExpiresMinutes = Math.Clamp(TicketExpiresMinutes, 1, 24 * 60);
        TicketFileName = string.IsNullOrWhiteSpace(TicketFileName) ? "initial-admin-ticket" : TicketFileName.Trim();
    }
}

/// <summary>
/// <para>zh-cn:控制面管理会话配置。</para>
/// <para>en-us:Control-plane management-session options.</para>
/// </summary>
public sealed class ControlPlaneSessionOptions
{
    /// <summary><para>zh-cn:会话 Ticket 请求头名称。</para><para>en-us:Session-ticket request header name.</para></summary>
    public string HeaderName { get; set; } = "X-Air-ControlPlane-Ticket";

    /// <summary><para>zh-cn:会话 Cookie 名称。</para><para>en-us:Session cookie name.</para></summary>
    public string CookieName { get; set; } = "Air.ControlPlane.Ticket";

    /// <summary><para>zh-cn:会话 Ticket 的同站 URL 查询参数名，用于控制面 iframe 承载场景。为空时禁用查询参数读取。</para><para>en-us:Same-site URL query parameter name for the session ticket, used by control-plane iframe hosting. Query-string reading is disabled when empty.</para></summary>
    public string QueryName { get; set; } = "airControlPlaneTicket";

    /// <summary><para>zh-cn:会话过期分钟数。</para><para>en-us:Session expiration in minutes.</para></summary>
    public int ExpiresMinutes { get; set; } = 120;

    /// <summary><para>zh-cn:是否启用滑动过期。</para><para>en-us:Whether sliding expiration is enabled.</para></summary>
    public bool SlidingExpiration { get; set; } = true;

    internal void Normalize()
    {
        HeaderName = string.IsNullOrWhiteSpace(HeaderName) ? "X-Air-ControlPlane-Ticket" : HeaderName.Trim();
        CookieName = string.IsNullOrWhiteSpace(CookieName) ? "Air.ControlPlane.Ticket" : CookieName.Trim();
        QueryName = string.IsNullOrWhiteSpace(QueryName) ? "airControlPlaneTicket" : QueryName.Trim();
        ExpiresMinutes = Math.Clamp(ExpiresMinutes, 5, 24 * 60);
    }
}

/// <summary>
/// <para>zh-cn:控制面管理员密码策略配置。</para>
/// <para>en-us:Control-plane administrator password-policy options.</para>
/// </summary>
public sealed class ControlPlanePasswordPolicyOptions
{
    /// <summary><para>zh-cn:最小密码长度。</para><para>en-us:Minimum password length.</para></summary>
    public int MinLength { get; set; } = 8;

    /// <summary><para>zh-cn:是否要求数字。</para><para>en-us:Whether a digit is required.</para></summary>
    public bool RequireDigit { get; set; } = true;

    /// <summary><para>zh-cn:是否要求小写字母。</para><para>en-us:Whether a lowercase letter is required.</para></summary>
    public bool RequireLowercase { get; set; } = true;

    /// <summary><para>zh-cn:是否要求大写字母。</para><para>en-us:Whether an uppercase letter is required.</para></summary>
    public bool RequireUppercase { get; set; }

    /// <summary><para>zh-cn:是否要求特殊字符。</para><para>en-us:Whether a special character is required.</para></summary>
    public bool RequireSpecialCharacter { get; set; }

    internal void Normalize()
    {
        MinLength = Math.Clamp(MinLength, 6, 128);
    }
}

/// <summary>
/// <para>zh-cn:控制面登录策略配置。</para>
/// <para>en-us:Control-plane login-policy options.</para>
/// </summary>
public sealed class ControlPlaneLoginPolicyOptions
{
    /// <summary><para>zh-cn:锁定前最大失败次数。</para><para>en-us:Maximum failed attempts before lockout.</para></summary>
    public int MaxFailedAttempts { get; set; } = 5;

    /// <summary><para>zh-cn:锁定分钟数。</para><para>en-us:Lockout duration in minutes.</para></summary>
    public int LockoutMinutes { get; set; } = 15;

    internal void Normalize()
    {
        MaxFailedAttempts = Math.Clamp(MaxFailedAttempts, 1, 100);
        LockoutMinutes = Math.Clamp(LockoutMinutes, 1, 24 * 60);
    }
}

/// <summary>
/// <para>zh-cn:控制面服务票据策略配置。</para>
/// <para>en-us:Control-plane service-ticket policy options.</para>
/// </summary>
public sealed class ControlPlaneServiceTicketOptions
{
    /// <summary><para>zh-cn:服务票据默认过期天数。</para><para>en-us:Default service-ticket expiration in days.</para></summary>
    public int DefaultExpiresDays { get; set; } = 180;

    /// <summary><para>zh-cn:是否允许同一类型存在多个启用票据。</para><para>en-us:Whether multiple active tickets are allowed for the same type.</para></summary>
    public bool AllowMultipleActiveTickets { get; set; } = true;

    internal void Normalize()
    {
        DefaultExpiresDays = Math.Clamp(DefaultExpiresDays, 1, 3650);
    }
}

/// <summary>
/// <para>zh-cn:控制面审计配置。</para>
/// <para>en-us:Control-plane audit options.</para>
/// </summary>
public sealed class ControlPlaneAuditOptions
{
    /// <summary><para>zh-cn:是否启用审计文件。</para><para>en-us:Whether audit-file writing is enabled.</para></summary>
    public bool Enabled { get; set; } = true;

    /// <summary><para>zh-cn:审计文件名。</para><para>en-us:Audit file name.</para></summary>
    public string FileName { get; set; } = "audit.log";

    internal void Normalize()
    {
        FileName = string.IsNullOrWhiteSpace(FileName) ? "audit.log" : FileName.Trim();
    }
}
