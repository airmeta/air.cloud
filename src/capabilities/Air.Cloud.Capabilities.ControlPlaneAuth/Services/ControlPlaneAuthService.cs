using Air.Cloud.Capabilities.ControlPlaneAuth.Models;
using Air.Cloud.Capabilities.ControlPlaneAuth.Options;
using Air.Cloud.Capabilities.ControlPlaneAuth.Security;
using Air.Cloud.Capabilities.ControlPlaneAuth.Stores;

using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using System.Security.Cryptography;
using System.Text.Json;

namespace Air.Cloud.Capabilities.ControlPlaneAuth.Services;

/// <summary>
/// <para>zh-cn:默认控制面认证服务，负责首次初始化、管理员登录、会话校验和服务票据管理。</para>
/// <para>en-us:Default control-plane authentication service that handles first initialization, administrator login, session validation, and service-ticket management.</para>
/// </summary>
public sealed class ControlPlaneAuthService : IControlPlaneAuthService
{
    private readonly IControlPlaneAuthStore store;
    private readonly IOptionsMonitor<ControlPlaneAuthOptions> options;
    private readonly ControlPlaneMfaSecretProtector mfaProtector;
    private readonly ILogger<ControlPlaneAuthService> logger;
    private readonly SemaphoreSlim operationLock = new(1, 1);

    /// <summary>
    /// <para>zh-cn:创建默认控制面认证服务。</para>
    /// <para>en-us:Creates the default control-plane authentication service.</para>
    /// </summary>
    /// <param name="store"><para>zh-cn:控制面认证运行数据存储。</para><para>en-us:Control-plane authentication runtime data store.</para></param>
    /// <param name="options"><para>zh-cn:控制面认证配置监视器。</para><para>en-us:Control-plane authentication options monitor.</para></param>
    /// <param name="mfaProtector"><para>zh-cn:MFA 密钥和绑定凭证保护器。</para><para>en-us:MFA secret and enrollment-token protector.</para></param>
    /// <param name="logger"><para>zh-cn:日志记录器。</para><para>en-us:Logger.</para></param>
    public ControlPlaneAuthService(
        IControlPlaneAuthStore store,
        IOptionsMonitor<ControlPlaneAuthOptions> options,
        ControlPlaneMfaSecretProtector mfaProtector,
        ILogger<ControlPlaneAuthService> logger)
    {
        this.store = store;
        this.options = options;
        this.mfaProtector = mfaProtector;
        this.logger = logger;
    }

    /// <summary>
    /// <para>zh-cn:创建使用临时数据保护密钥的控制面认证服务，供无宿主依赖的测试或内存场景使用。</para>
    /// <para>en-us:Creates a control-plane authentication service with ephemeral Data Protection keys for tests or host-independent scenarios.</para>
    /// </summary>
    /// <param name="store"><para>zh-cn:控制面认证运行数据存储。</para><para>en-us:Control-plane authentication runtime data store.</para></param>
    /// <param name="options"><para>zh-cn:控制面认证配置监视器。</para><para>en-us:Control-plane authentication options monitor.</para></param>
    /// <param name="logger"><para>zh-cn:日志记录器。</para><para>en-us:Logger.</para></param>
    public ControlPlaneAuthService(
        IControlPlaneAuthStore store,
        IOptionsMonitor<ControlPlaneAuthOptions> options,
        ILogger<ControlPlaneAuthService> logger)
        : this(store, options, new ControlPlaneMfaSecretProtector(new EphemeralDataProtectionProvider()), logger)
    {
    }

    /// <inheritdoc />
    public async Task EnsureBootstrapTicketAsync(CancellationToken cancellationToken = default)
    {
        var current = CurrentOptions();
        if (!current.Enabled || !current.Bootstrap.Enabled)
        {
            return;
        }

        await operationLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var users = await store.ReadUsersAsync(cancellationToken).ConfigureAwait(false);
            var state = await store.ReadStateAsync(cancellationToken).ConfigureAwait(false);
            if (users.Count > 0 || state.Initialized)
            {
                state.Initialized = true;
                state.BootstrapTicketHash = string.Empty;
                state.BootstrapTicketExpiresAtUtc = null;
                await store.WriteStateAsync(state, cancellationToken).ConfigureAwait(false);
                await store.DeleteBootstrapTicketFileAsync(cancellationToken).ConfigureAwait(false);
                return;
            }

            if (!string.IsNullOrWhiteSpace(state.BootstrapTicketHash) &&
                state.BootstrapTicketExpiresAtUtc is { } expiresAt &&
                expiresAt > DateTimeOffset.UtcNow)
            {
                return;
            }

            var ticket = ControlPlaneTicketGenerator.Create();
            var expires = DateTimeOffset.UtcNow.AddMinutes(current.Bootstrap.TicketExpiresMinutes);
            state.Initialized = false;
            state.BootstrapTicketHash = ControlPlanePasswordHasher.Hash(ticket);
            state.BootstrapTicketExpiresAtUtc = expires;
            await store.WriteStateAsync(state, cancellationToken).ConfigureAwait(false);
            await store.WriteBootstrapTicketFileAsync(ticket, expires, cancellationToken).ConfigureAwait(false);
            if (current.Bootstrap.PrintTicketToConsole)
            {
                logger.LogWarning("Air.Cloud ControlPlaneAuth bootstrap ticket for {ProductName}: {BootstrapTicket}. It expires at {ExpiresAtUtc}.", current.ProductName, ticket, expires);
            }
        }
        finally
        {
            operationLock.Release();
        }
    }

    /// <inheritdoc />
    public async Task<object> ReadStatusAsync(CancellationToken cancellationToken = default)
    {
        var current = CurrentOptions();
        var state = await store.ReadStateAsync(cancellationToken).ConfigureAwait(false);
        var users = await store.ReadUsersAsync(cancellationToken).ConfigureAwait(false);
        return new
        {
            current.Enabled,
            current.EnableMFA,
            current.ProductCode,
            current.ProductName,
            current.BasePath,
            initialized = state.Initialized || users.Count > 0,
            bootstrapRequired = current.Bootstrap.Enabled && users.Count == 0,
            bootstrapTicketExpiresAtUtc = users.Count == 0 ? state.BootstrapTicketExpiresAtUtc : null
        };
    }

    /// <inheritdoc />
    public async Task<ControlPlaneOperationResult> InitializeAsync(string bootstrapTicket, string userName, string password, CancellationToken cancellationToken = default)
    {
        var current = CurrentOptions();
        if (!current.Enabled)
        {
            return new(false, "disabled", "ControlPlaneAuth is disabled.");
        }

        var passwordError = ValidatePassword(password, current.PasswordPolicy);
        if (passwordError is not null)
        {
            return new(false, "invalid_password", passwordError);
        }

        await operationLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var users = (await store.ReadUsersAsync(cancellationToken).ConfigureAwait(false)).ToList();
            if (users.Count > 0)
            {
                return new(false, "already_initialized", "Control plane has already been initialized.");
            }

            var state = await store.ReadStateAsync(cancellationToken).ConfigureAwait(false);
            if (string.IsNullOrWhiteSpace(state.BootstrapTicketHash) ||
                state.BootstrapTicketExpiresAtUtc is null ||
                state.BootstrapTicketExpiresAtUtc <= DateTimeOffset.UtcNow ||
                !ControlPlanePasswordHasher.Verify(bootstrapTicket, state.BootstrapTicketHash))
            {
                return new(false, "invalid_bootstrap_ticket", "Bootstrap ticket is invalid or expired.");
            }

            var normalizedUserName = NormalizeRequired(userName);
            users.Add(new ControlPlaneUser
            {
                Id = Guid.NewGuid().ToString("N"),
                UserName = normalizedUserName,
                PasswordHash = ControlPlanePasswordHasher.Hash(password),
                Enabled = true,
                CreatedAtUtc = DateTimeOffset.UtcNow
            });
            await store.WriteUsersAsync(users, cancellationToken).ConfigureAwait(false);
            state.Initialized = true;
            state.BootstrapTicketHash = string.Empty;
            state.BootstrapTicketExpiresAtUtc = null;
            await store.WriteStateAsync(state, cancellationToken).ConfigureAwait(false);
            await store.DeleteBootstrapTicketFileAsync(cancellationToken).ConfigureAwait(false);
            await store.AppendAuditAsync("initialize", normalizedUserName, "Control plane administrator initialized.", cancellationToken).ConfigureAwait(false);
            return new(true);
        }
        finally
        {
            operationLock.Release();
        }
    }

    /// <inheritdoc />
    public async Task<ControlPlaneLoginResult> LoginAsync(string userName, string password, string? mfaCode = null, CancellationToken cancellationToken = default)
    {
        var current = CurrentOptions();
        var normalizedUserName = NormalizeRequired(userName);

        await operationLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var users = (await store.ReadUsersAsync(cancellationToken).ConfigureAwait(false)).ToList();
            var user = users.FirstOrDefault(item => string.Equals(item.UserName, normalizedUserName, StringComparison.OrdinalIgnoreCase));
            if (user is null || !user.Enabled)
            {
                return new(false, ErrorCode: "invalid_credentials", ErrorMessage: "User name or password is invalid.");
            }

            if (user.LockedUntilUtc is { } lockedUntil && lockedUntil > DateTimeOffset.UtcNow)
            {
                return new(false, ErrorCode: "locked", ErrorMessage: "User is temporarily locked.");
            }

            if (!ControlPlanePasswordHasher.Verify(password, user.PasswordHash))
            {
                user.FailedAttempts++;
                if (user.FailedAttempts >= current.LoginPolicy.MaxFailedAttempts)
                {
                    user.LockedUntilUtc = DateTimeOffset.UtcNow.AddMinutes(current.LoginPolicy.LockoutMinutes);
                }

                await store.WriteUsersAsync(users, cancellationToken).ConfigureAwait(false);
                await store.AppendAuditAsync("login_failed", normalizedUserName, "Control plane login failed.", cancellationToken).ConfigureAwait(false);
                return new(false, ErrorCode: "invalid_credentials", ErrorMessage: "User name or password is invalid.");
            }

            if (current.EnableMFA)
            {
                if (string.IsNullOrWhiteSpace(user.MfaSecretProtected))
                {
                    await store.AppendAuditAsync("mfa_setup_required", normalizedUserName, "Control plane login requires Authenticator enrollment.", cancellationToken).ConfigureAwait(false);
                    return new(
                        false,
                        ErrorCode: "mfa_setup_required",
                        ErrorMessage: "Authenticator enrollment is required.",
                        RequiresMfa: true,
                        MfaSetupRequired: true);
                }

                string secret;
                try
                {
                    secret = mfaProtector.UnprotectSecret(user.MfaSecretProtected);
                }
                catch (Exception exception) when (exception is CryptographicException or InvalidOperationException)
                {
                    logger.LogError(exception, "Unable to unprotect the ControlPlaneAuth MFA secret for {UserName}.", normalizedUserName);
                    return new(false, ErrorCode: "mfa_unavailable", ErrorMessage: "MFA configuration is unavailable.", RequiresMfa: true);
                }

                if (string.IsNullOrWhiteSpace(mfaCode))
                {
                    await store.AppendAuditAsync("mfa_required", normalizedUserName, "Control plane MFA code was not provided.", cancellationToken).ConfigureAwait(false);
                    return new(false, ErrorCode: "mfa_required", ErrorMessage: "Authenticator code is required.", RequiresMfa: true);
                }

                if (!ControlPlaneTotp.VerifyCode(secret, mfaCode, DateTimeOffset.UtcNow, user.MfaLastUsedTimeStep, out var acceptedTimeStep))
                {
                    await store.AppendAuditAsync("mfa_failed", normalizedUserName, "Control plane MFA verification failed.", cancellationToken).ConfigureAwait(false);
                    return new(false, ErrorCode: "invalid_mfa", ErrorMessage: "Authenticator code is invalid or expired.", RequiresMfa: true);
                }

                user.MfaLastUsedTimeStep = acceptedTimeStep;
            }

            user.FailedAttempts = 0;
            user.LockedUntilUtc = null;
            await store.WriteUsersAsync(users, cancellationToken).ConfigureAwait(false);

            var plainTicket = ControlPlaneTicketGenerator.Create();
            var expiresAt = DateTimeOffset.UtcNow.AddMinutes(current.Session.ExpiresMinutes);
            var sessions = (await store.ReadSessionsAsync(cancellationToken).ConfigureAwait(false))
                .Where(item => !item.Revoked && item.ExpiresAtUtc > DateTimeOffset.UtcNow)
                .ToList();
            sessions.Add(new ControlPlaneSession
            {
                Id = Guid.NewGuid().ToString("N"),
                TicketHash = ControlPlanePasswordHasher.Hash(plainTicket),
                UserId = user.Id,
                UserName = user.UserName,
                CreatedAtUtc = DateTimeOffset.UtcNow,
                ExpiresAtUtc = expiresAt
            });
            await store.WriteSessionsAsync(sessions, cancellationToken).ConfigureAwait(false);
            await store.AppendAuditAsync("login", normalizedUserName, "Control plane login succeeded.", cancellationToken).ConfigureAwait(false);
            return new(true, plainTicket, expiresAt);
        }
        finally
        {
            operationLock.Release();
        }
    }

    /// <inheritdoc />
    public async Task<ControlPlaneMfaSetupResult> BeginMfaSetupAsync(string userName, string password, CancellationToken cancellationToken = default)
    {
        var current = CurrentOptions();
        if (!current.Enabled || !current.EnableMFA)
        {
            return new(false, ErrorCode: "disabled", ErrorMessage: "ControlPlaneAuth MFA is disabled.");
        }

        var normalizedUserName = NormalizeRequired(userName);
        await operationLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var users = (await store.ReadUsersAsync(cancellationToken).ConfigureAwait(false)).ToList();
            var user = users.FirstOrDefault(item => string.Equals(item.UserName, normalizedUserName, StringComparison.OrdinalIgnoreCase));
            if (user is null || !user.Enabled || !ControlPlanePasswordHasher.Verify(password, user.PasswordHash))
            {
                return new(false, ErrorCode: "invalid_credentials", ErrorMessage: "User name or password is invalid.");
            }

            if (!string.IsNullOrWhiteSpace(user.MfaSecretProtected))
            {
                return new(false, ErrorCode: "mfa_already_configured", ErrorMessage: "MFA is already configured for this user.");
            }

            var secret = ControlPlaneTotp.CreateSecret();
            var expiresAt = DateTimeOffset.UtcNow.AddMinutes(10);
            var setupToken = mfaProtector.ProtectSetupToken(JsonSerializer.Serialize(new MfaSetupTokenPayload(
                user.Id,
                user.UserName,
                secret,
                expiresAt)));
            var provisioningUri = ControlPlaneTotp.CreateProvisioningUri(secret, current.MfaIssuer, user.UserName);
            await store.AppendAuditAsync("mfa_setup_started", normalizedUserName, "Control plane MFA enrollment started.", cancellationToken).ConfigureAwait(false);
            return new(true, setupToken, secret, provisioningUri, expiresAt);
        }
        finally
        {
            operationLock.Release();
        }
    }

    /// <inheritdoc />
    public async Task<ControlPlaneOperationResult> CompleteMfaSetupAsync(string setupToken, string code, CancellationToken cancellationToken = default)
    {
        var current = CurrentOptions();
        if (!current.Enabled || !current.EnableMFA)
        {
            return new(false, "disabled", "ControlPlaneAuth MFA is disabled.");
        }

        MfaSetupTokenPayload payload;
        try
        {
            payload = JsonSerializer.Deserialize<MfaSetupTokenPayload>(mfaProtector.UnprotectSetupToken(setupToken))
                ?? throw new InvalidOperationException("MFA setup token is empty.");
        }
        catch (Exception exception) when (exception is CryptographicException or InvalidOperationException or JsonException)
        {
            return new(false, "invalid_mfa_setup_token", "MFA setup token is invalid or expired.");
        }

        if (payload.ExpiresAtUtc <= DateTimeOffset.UtcNow)
        {
            return new(false, "invalid_mfa_setup_token", "MFA setup token is invalid or expired.");
        }

        if (!ControlPlaneTotp.VerifyCode(payload.Secret, code, DateTimeOffset.UtcNow, null, out _))
        {
            return new(false, "invalid_mfa", "Authenticator code is invalid or expired.");
        }

        await operationLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var users = (await store.ReadUsersAsync(cancellationToken).ConfigureAwait(false)).ToList();
            var user = users.FirstOrDefault(item => item.Id == payload.UserId);
            if (user is null || !user.Enabled || !string.Equals(user.UserName, payload.UserName, StringComparison.OrdinalIgnoreCase))
            {
                return new(false, "invalid_mfa_setup_token", "MFA setup token is invalid or expired.");
            }

            if (!string.IsNullOrWhiteSpace(user.MfaSecretProtected))
            {
                return new(false, "mfa_already_configured", "MFA is already configured for this user.");
            }

            user.MfaSecretProtected = mfaProtector.ProtectSecret(payload.Secret);
            user.MfaLastUsedTimeStep = null;
            await store.WriteUsersAsync(users, cancellationToken).ConfigureAwait(false);
            await store.AppendAuditAsync("mfa_setup_completed", user.UserName, "Control plane MFA enrollment completed.", cancellationToken).ConfigureAwait(false);
            return new(true);
        }
        finally
        {
            operationLock.Release();
        }
    }

    /// <inheritdoc />
    public async Task<ControlPlaneOperationResult> ResetMfaAsync(string userName, CancellationToken cancellationToken = default)
    {
        var normalizedUserName = NormalizeRequired(userName);
        await operationLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var users = (await store.ReadUsersAsync(cancellationToken).ConfigureAwait(false)).ToList();
            var user = users.FirstOrDefault(item => string.Equals(item.UserName, normalizedUserName, StringComparison.OrdinalIgnoreCase));
            if (user is null)
            {
                return new(false, "not_found", "Control-plane user was not found.");
            }

            user.MfaSecretProtected = string.Empty;
            user.MfaLastUsedTimeStep = null;
            await store.WriteUsersAsync(users, cancellationToken).ConfigureAwait(false);
            await store.AppendAuditAsync("mfa_reset", user.UserName, "Control plane MFA enrollment was reset by a server-side emergency operation.", cancellationToken).ConfigureAwait(false);
            return new(true);
        }
        finally
        {
            operationLock.Release();
        }
    }

    /// <inheritdoc />
    public async Task<ControlPlaneAuthorizationResult> AuthorizeAsync(HttpContext context, CancellationToken cancellationToken = default)
    {
        var tickets = ReadTickets(context, CurrentOptions()).ToArray();
        if (tickets.Length == 0)
        {
            return new(false);
        }

        var sessions = (await store.ReadSessionsAsync(cancellationToken).ConfigureAwait(false)).ToList();
        var session = sessions.FirstOrDefault(item =>
            !item.Revoked &&
            item.ExpiresAtUtc > DateTimeOffset.UtcNow &&
            tickets.Any(ticket => ControlPlanePasswordHasher.Verify(ticket, item.TicketHash)));
        if (session is null)
        {
            return new(false);
        }

        var users = await store.ReadUsersAsync(cancellationToken).ConfigureAwait(false);
        var user = users.FirstOrDefault(item => item.Id == session.UserId && item.Enabled);
        if (user is null)
        {
            return new(false);
        }

        var current = CurrentOptions();
        if (current.Session.SlidingExpiration)
        {
            session.ExpiresAtUtc = DateTimeOffset.UtcNow.AddMinutes(current.Session.ExpiresMinutes);
            await store.WriteSessionsAsync(sessions, cancellationToken).ConfigureAwait(false);
        }

        return new(true, session, user);
    }

    /// <inheritdoc />
    public async Task LogoutAsync(HttpContext context, CancellationToken cancellationToken = default)
    {
        var tickets = ReadTickets(context, CurrentOptions()).ToArray();
        if (tickets.Length == 0)
        {
            return;
        }

        var sessions = (await store.ReadSessionsAsync(cancellationToken).ConfigureAwait(false)).ToList();
        var revoked = false;
        foreach (var session in sessions.Where(item => tickets.Any(ticket => ControlPlanePasswordHasher.Verify(ticket, item.TicketHash))))
        {
            session.Revoked = true;
            await store.AppendAuditAsync("logout", session.UserName, "Control plane session revoked.", cancellationToken).ConfigureAwait(false);
            revoked = true;
        }

        if (revoked)
        {
            await store.WriteSessionsAsync(sessions, cancellationToken).ConfigureAwait(false);
        }
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<ControlPlaneServiceTicket>> ReadServiceTicketsAsync(CancellationToken cancellationToken = default) =>
        store.ReadServiceTicketsAsync(cancellationToken);

    /// <inheritdoc />
    public async Task<ControlPlaneServiceTicketCreatedResult> CreateServiceTicketAsync(string type, string name, DateTimeOffset? expiresAtUtc = null, CancellationToken cancellationToken = default)
    {
        var current = CurrentOptions();
        var normalizedType = NormalizeRequired(type);
        var normalizedName = NormalizeRequired(name);
        var plainTicket = ControlPlaneTicketGenerator.Create();

        await operationLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var tickets = (await store.ReadServiceTicketsAsync(cancellationToken).ConfigureAwait(false)).ToList();
            if (!current.ServiceTickets.AllowMultipleActiveTickets)
            {
                foreach (var ticket in tickets.Where(item => item.Enabled && string.Equals(item.Type, normalizedType, StringComparison.OrdinalIgnoreCase)))
                {
                    ticket.Enabled = false;
                }
            }

            var entity = new ControlPlaneServiceTicket
            {
                Id = Guid.NewGuid().ToString("N"),
                Type = normalizedType,
                Name = normalizedName,
                TicketHash = ControlPlanePasswordHasher.Hash(plainTicket),
                CreatedAtUtc = DateTimeOffset.UtcNow,
                ExpiresAtUtc = expiresAtUtc ?? DateTimeOffset.UtcNow.AddDays(current.ServiceTickets.DefaultExpiresDays),
                Enabled = true
            };
            tickets.Add(entity);
            await store.WriteServiceTicketsAsync(tickets, cancellationToken).ConfigureAwait(false);
            await store.AppendAuditAsync("service_ticket_create", null, $"Created service ticket {entity.Id} of type {entity.Type}.", cancellationToken).ConfigureAwait(false);
            return new(entity, plainTicket);
        }
        finally
        {
            operationLock.Release();
        }
    }

    /// <inheritdoc />
    public async Task<ControlPlaneServiceTicketCreatedResult?> RotateServiceTicketAsync(string id, CancellationToken cancellationToken = default)
    {
        await operationLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var tickets = (await store.ReadServiceTicketsAsync(cancellationToken).ConfigureAwait(false)).ToList();
            var entity = tickets.FirstOrDefault(item => item.Id == id);
            if (entity is null)
            {
                return null;
            }

            var plainTicket = ControlPlaneTicketGenerator.Create();
            entity.TicketHash = ControlPlanePasswordHasher.Hash(plainTicket);
            entity.Enabled = true;
            entity.LastUsedAtUtc = null;
            await store.WriteServiceTicketsAsync(tickets, cancellationToken).ConfigureAwait(false);
            await store.AppendAuditAsync("service_ticket_rotate", null, $"Rotated service ticket {entity.Id} of type {entity.Type}.", cancellationToken).ConfigureAwait(false);
            return new(entity, plainTicket);
        }
        finally
        {
            operationLock.Release();
        }
    }

    /// <inheritdoc />
    public async Task<bool> SetServiceTicketEnabledAsync(string id, bool enabled, CancellationToken cancellationToken = default)
    {
        await operationLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var tickets = (await store.ReadServiceTicketsAsync(cancellationToken).ConfigureAwait(false)).ToList();
            var entity = tickets.FirstOrDefault(item => item.Id == id);
            if (entity is null)
            {
                return false;
            }

            entity.Enabled = enabled;
            await store.WriteServiceTicketsAsync(tickets, cancellationToken).ConfigureAwait(false);
            await store.AppendAuditAsync(enabled ? "service_ticket_enable" : "service_ticket_disable", null, $"Changed service ticket {entity.Id} enabled state to {enabled}.", cancellationToken).ConfigureAwait(false);
            return true;
        }
        finally
        {
            operationLock.Release();
        }
    }

    private ControlPlaneAuthOptions CurrentOptions()
    {
        var current = options.CurrentValue;
        current.Normalize();
        return current;
    }

    private static string NormalizeRequired(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Value is required.", nameof(value));
        }

        return value.Trim();
    }

    private static string? ValidatePassword(string password, ControlPlanePasswordPolicyOptions policy)
    {
        if (string.IsNullOrEmpty(password) || password.Length < policy.MinLength)
        {
            return $"Password length must be at least {policy.MinLength}.";
        }

        if (policy.RequireDigit && !password.Any(char.IsDigit))
        {
            return "Password must contain a digit.";
        }

        if (policy.RequireLowercase && !password.Any(char.IsLower))
        {
            return "Password must contain a lowercase letter.";
        }

        if (policy.RequireUppercase && !password.Any(char.IsUpper))
        {
            return "Password must contain an uppercase letter.";
        }

        if (policy.RequireSpecialCharacter && password.All(char.IsLetterOrDigit))
        {
            return "Password must contain a special character.";
        }

        return null;
    }

    private static string? ReadTicket(HttpContext context, ControlPlaneAuthOptions options)
    {
        return ReadTickets(context, options).FirstOrDefault();
    }

    private static IEnumerable<string> ReadTickets(HttpContext context, ControlPlaneAuthOptions options)
    {
        if (context.Request.Headers.TryGetValue(options.Session.HeaderName, out var header) && header.Count == 1 && !string.IsNullOrWhiteSpace(header[0]))
        {
            yield return header[0]!;
        }

        if (context.Request.Cookies.TryGetValue(options.Session.CookieName, out var cookieTicket))
        {
            yield return cookieTicket;
        }

        if (!string.IsNullOrWhiteSpace(options.Session.QueryName)
            && context.Request.Query.TryGetValue(options.Session.QueryName, out var query)
            && query.Count == 1
            && !string.IsNullOrWhiteSpace(query[0]))
        {
            yield return query[0]!;
        }

        var authorization = context.Request.Headers.Authorization.FirstOrDefault();
        const string bearerPrefix = "Bearer ";
        if (authorization?.StartsWith(bearerPrefix, StringComparison.OrdinalIgnoreCase) == true)
        {
            var bearer = authorization[bearerPrefix.Length..];
            if (!string.IsNullOrWhiteSpace(bearer))
            {
                yield return bearer;
            }
        }
    }

    private sealed record MfaSetupTokenPayload(
        string UserId,
        string UserName,
        string Secret,
        DateTimeOffset ExpiresAtUtc);
}
