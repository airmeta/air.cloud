using Air.Cloud.Capabilities.ControlPlaneAuth.Models;

using Microsoft.AspNetCore.Http;

namespace Air.Cloud.Capabilities.ControlPlaneAuth.Services;

/// <summary>
/// <para>zh-cn:控制面认证服务接口。</para>
/// <para>en-us:Control-plane authentication service contract.</para>
/// </summary>
public interface IControlPlaneAuthService
{
    /// <summary>
    /// <para>zh-cn:确保首次初始化 Ticket 已按当前状态生成。只有在未初始化且没有有效 Bootstrap Ticket 时才会生成新 Ticket。</para>
    /// <para>en-us:Ensures that the first-initialization ticket exists for the current state. A new ticket is generated only when the control plane is not initialized and no valid bootstrap ticket exists.</para>
    /// </summary>
    /// <param name="cancellationToken"><para>zh-cn:取消令牌。</para><para>en-us:Cancellation token.</para></param>
    Task EnsureBootstrapTicketAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// <para>zh-cn:读取控制面状态视图。</para>
    /// <para>en-us:Reads the control-plane status view.</para>
    /// </summary>
    /// <param name="cancellationToken"><para>zh-cn:取消令牌。</para><para>en-us:Cancellation token.</para></param>
    /// <returns><para>zh-cn:状态视图。</para><para>en-us:Status view.</para></returns>
    Task<object> ReadStatusAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// <para>zh-cn:使用 Bootstrap Ticket 创建首个管理员。</para>
    /// <para>en-us:Creates the first administrator with a bootstrap ticket.</para>
    /// </summary>
    /// <param name="bootstrapTicket"><para>zh-cn:初始化 Ticket 明文。</para><para>en-us:Bootstrap ticket in clear text.</para></param>
    /// <param name="userName"><para>zh-cn:管理员用户名。</para><para>en-us:Administrator user name.</para></param>
    /// <param name="password"><para>zh-cn:管理员密码。</para><para>en-us:Administrator password.</para></param>
    /// <param name="cancellationToken"><para>zh-cn:取消令牌。</para><para>en-us:Cancellation token.</para></param>
    /// <returns><para>zh-cn:初始化结果。</para><para>en-us:Initialization result.</para></returns>
    Task<ControlPlaneOperationResult> InitializeAsync(string bootstrapTicket, string userName, string password, CancellationToken cancellationToken = default);

    /// <summary>
    /// <para>zh-cn:使用用户名和密码登录控制面。</para>
    /// <para>en-us:Logs in to the control plane with user name and password.</para>
    /// </summary>
    /// <param name="userName"><para>zh-cn:用户名。</para><para>en-us:User name.</para></param>
    /// <param name="password"><para>zh-cn:密码。</para><para>en-us:Password.</para></param>
    /// <param name="mfaCode"><para>zh-cn:Authenticator 当前验证码；未启用 MFA 时可为空。</para><para>en-us:Current Authenticator code; optional when MFA is disabled.</para></param>
    /// <param name="cancellationToken"><para>zh-cn:取消令牌。</para><para>en-us:Cancellation token.</para></param>
    /// <returns><para>zh-cn:登录结果。</para><para>en-us:Login result.</para></returns>
    Task<ControlPlaneLoginResult> LoginAsync(string userName, string password, string? mfaCode = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// <para>zh-cn:使用用户名和密码开始 Authenticator 绑定，返回一次性受保护的绑定凭证和标准 otpauth URI。</para>
    /// <para>en-us:Starts Authenticator enrollment with a user name and password and returns a one-time protected enrollment token plus a standard otpauth URI.</para>
    /// </summary>
    /// <param name="userName"><para>zh-cn:管理员用户名。</para><para>en-us:Administrator user name.</para></param>
    /// <param name="password"><para>zh-cn:管理员密码。</para><para>en-us:Administrator password.</para></param>
    /// <param name="cancellationToken"><para>zh-cn:取消令牌。</para><para>en-us:Cancellation token.</para></param>
    /// <returns><para>zh-cn:MFA 绑定信息。</para><para>en-us:MFA enrollment information.</para></returns>
    Task<ControlPlaneMfaSetupResult> BeginMfaSetupAsync(string userName, string password, CancellationToken cancellationToken = default);

    /// <summary>
    /// <para>zh-cn:使用 Authenticator 当前验证码完成 MFA 绑定。</para>
    /// <para>en-us:Completes MFA enrollment with the current Authenticator code.</para>
    /// </summary>
    /// <param name="setupToken"><para>zh-cn:一次性绑定凭证。</para><para>en-us:One-time enrollment token.</para></param>
    /// <param name="code"><para>zh-cn:Authenticator 当前验证码。</para><para>en-us:Current Authenticator code.</para></param>
    /// <param name="cancellationToken"><para>zh-cn:取消令牌。</para><para>en-us:Cancellation token.</para></param>
    /// <returns><para>zh-cn:绑定结果。</para><para>en-us:Enrollment result.</para></returns>
    Task<ControlPlaneOperationResult> CompleteMfaSetupAsync(string setupToken, string code, CancellationToken cancellationToken = default);

    /// <summary>
    /// <para>zh-cn:服务器侧应急清除指定用户的 MFA 绑定。该操作不映射为公网 endpoint，宿主应通过本机运维代码调用。</para>
    /// <para>en-us:Clears MFA enrollment for a user as a server-side emergency operation. This operation is not mapped to a public endpoint and should be invoked by host-side operational code.</para>
    /// </summary>
    /// <param name="userName"><para>zh-cn:管理员用户名。</para><para>en-us:Administrator user name.</para></param>
    /// <param name="cancellationToken"><para>zh-cn:取消令牌。</para><para>en-us:Cancellation token.</para></param>
    /// <returns><para>zh-cn:清除结果。</para><para>en-us:Reset result.</para></returns>
    Task<ControlPlaneOperationResult> ResetMfaAsync(string userName, CancellationToken cancellationToken = default);

    /// <summary>
    /// <para>zh-cn:从 HTTP 请求中读取并校验管理会话。</para>
    /// <para>en-us:Reads and validates a management session from an HTTP request.</para>
    /// </summary>
    /// <param name="context"><para>zh-cn:HTTP 上下文。</para><para>en-us:HTTP context.</para></param>
    /// <param name="cancellationToken"><para>zh-cn:取消令牌。</para><para>en-us:Cancellation token.</para></param>
    /// <returns><para>zh-cn:授权结果。</para><para>en-us:Authorization result.</para></returns>
    Task<ControlPlaneAuthorizationResult> AuthorizeAsync(HttpContext context, CancellationToken cancellationToken = default);

    /// <summary>
    /// <para>zh-cn:注销当前管理会话。</para>
    /// <para>en-us:Logs out the current management session.</para>
    /// </summary>
    /// <param name="context"><para>zh-cn:HTTP 上下文。</para><para>en-us:HTTP context.</para></param>
    /// <param name="cancellationToken"><para>zh-cn:取消令牌。</para><para>en-us:Cancellation token.</para></param>
    Task LogoutAsync(HttpContext context, CancellationToken cancellationToken = default);

    /// <summary>
    /// <para>zh-cn:读取服务间票据列表，不返回明文。</para>
    /// <para>en-us:Reads service-to-service tickets without clear-text values.</para>
    /// </summary>
    /// <param name="cancellationToken"><para>zh-cn:取消令牌。</para><para>en-us:Cancellation token.</para></param>
    /// <returns><para>zh-cn:服务票据列表。</para><para>en-us:Service-ticket list.</para></returns>
    Task<IReadOnlyList<ControlPlaneServiceTicket>> ReadServiceTicketsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// <para>zh-cn:创建服务间票据，明文 Ticket 只在返回值中出现一次。</para>
    /// <para>en-us:Creates a service-to-service ticket; the clear-text ticket appears only once in the returned value.</para>
    /// </summary>
    /// <param name="type"><para>zh-cn:票据类型。</para><para>en-us:Ticket type.</para></param>
    /// <param name="name"><para>zh-cn:票据名称。</para><para>en-us:Ticket name.</para></param>
    /// <param name="expiresAtUtc"><para>zh-cn:可选过期时间。</para><para>en-us:Optional expiration time.</para></param>
    /// <param name="cancellationToken"><para>zh-cn:取消令牌。</para><para>en-us:Cancellation token.</para></param>
    /// <returns><para>zh-cn:创建结果。</para><para>en-us:Creation result.</para></returns>
    Task<ControlPlaneServiceTicketCreatedResult> CreateServiceTicketAsync(string type, string name, DateTimeOffset? expiresAtUtc = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// <para>zh-cn:轮换服务间票据，旧 hash 会被新 hash 替换，明文 Ticket 只在返回值中出现一次。</para>
    /// <para>en-us:Rotates a service-to-service ticket by replacing the old hash with a new hash; the clear-text ticket appears only once in the returned value.</para>
    /// </summary>
    /// <param name="id"><para>zh-cn:票据唯一标识。</para><para>en-us:Unique ticket identifier.</para></param>
    /// <param name="cancellationToken"><para>zh-cn:取消令牌。</para><para>en-us:Cancellation token.</para></param>
    /// <returns><para>zh-cn:轮换结果。</para><para>en-us:Rotation result.</para></returns>
    Task<ControlPlaneServiceTicketCreatedResult?> RotateServiceTicketAsync(string id, CancellationToken cancellationToken = default);

    /// <summary>
    /// <para>zh-cn:设置服务间票据启用状态。</para>
    /// <para>en-us:Sets the enabled state of a service-to-service ticket.</para>
    /// </summary>
    /// <param name="id"><para>zh-cn:票据唯一标识。</para><para>en-us:Unique ticket identifier.</para></param>
    /// <param name="enabled"><para>zh-cn:是否启用。</para><para>en-us:Whether the ticket is enabled.</para></param>
    /// <param name="cancellationToken"><para>zh-cn:取消令牌。</para><para>en-us:Cancellation token.</para></param>
    /// <returns><para>zh-cn:找到并更新时返回 true。</para><para>en-us:True when the ticket is found and updated.</para></returns>
    Task<bool> SetServiceTicketEnabledAsync(string id, bool enabled, CancellationToken cancellationToken = default);
}

/// <summary>
/// <para>zh-cn:控制面通用操作结果。</para>
/// <para>en-us:Generic control-plane operation result.</para>
/// </summary>
public sealed record ControlPlaneOperationResult(bool Succeeded, string? ErrorCode = null, string? ErrorMessage = null);

/// <summary>
/// <para>zh-cn:控制面登录结果。</para>
/// <para>en-us:Control-plane login result.</para>
/// </summary>
public sealed record ControlPlaneLoginResult(
    bool Succeeded,
    string? Ticket = null,
    DateTimeOffset? ExpiresAtUtc = null,
    string? ErrorCode = null,
    string? ErrorMessage = null,
    bool RequiresMfa = false,
    bool MfaSetupRequired = false);

/// <summary>
/// <para>zh-cn:ControlPlaneAuth MFA 绑定结果。</para>
/// <para>en-us:ControlPlaneAuth MFA enrollment result.</para>
/// </summary>
public sealed record ControlPlaneMfaSetupResult(
    bool Succeeded,
    string? SetupToken = null,
    string? Secret = null,
    string? ProvisioningUri = null,
    DateTimeOffset? ExpiresAtUtc = null,
    string? ErrorCode = null,
    string? ErrorMessage = null);

/// <summary>
/// <para>zh-cn:控制面授权结果。</para>
/// <para>en-us:Control-plane authorization result.</para>
/// </summary>
public sealed record ControlPlaneAuthorizationResult(bool Succeeded, ControlPlaneSession? Session = null, ControlPlaneUser? User = null);
