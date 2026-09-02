using Air.Cloud.Capabilities.ControlPlaneAuth.Options;
using Air.Cloud.Capabilities.ControlPlaneAuth.Services;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Air.Cloud.Capabilities.ControlPlaneAuth.Extensions;

/// <summary>
/// <para>zh-cn:控制面认证能力 endpoint 映射扩展。</para>
/// <para>en-us:Endpoint-mapping extensions for the control-plane authentication capability.</para>
/// </summary>
public static class ControlPlaneAuthEndpointRouteBuilderExtensions
{
    /// <summary>
    /// <para>zh-cn:映射控制面初始化页、登录页、状态 API、登录 API、注销 API 和服务票据管理 API。该方法只映射 ControlPlaneAuth 自身接口，不会接管宿主业务 API。</para>
    /// <para>en-us:Maps the control-plane initialization page, login page, status API, login API, logout API, and service-ticket management APIs. This method maps only ControlPlaneAuth endpoints and does not take over host business APIs.</para>
    /// </summary>
    /// <param name="endpoints"><para>zh-cn:endpoint 路由构建器。</para><para>en-us:Endpoint route builder.</para></param>
    /// <returns><para>zh-cn:原 endpoint 路由构建器，便于链式调用。</para><para>en-us:The original endpoint route builder for chaining.</para></returns>
    public static IEndpointRouteBuilder MapControlPlaneAuth(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        var options = endpoints.ServiceProvider.GetRequiredService<IOptionsMonitor<ControlPlaneAuthOptions>>().CurrentValue;
        options.Normalize();
        var apiPrefix = options.BasePath;

        endpoints.MapGet(options.AuthPath, CreateAuthPageAsync).WithDisplayName("ControlPlaneAuth auth page");
        if (!options.PlanePath.Equals(options.AuthPath, StringComparison.OrdinalIgnoreCase))
        {
            endpoints.MapGet(options.PlanePath, CreatePlanePageAsync).WithDisplayName("ControlPlaneAuth shell page");
        }

        endpoints.MapGet($"{apiPrefix}/api/status", ReadStatusAsync).WithDisplayName("ControlPlaneAuth status");
        endpoints.MapPost($"{apiPrefix}/api/initialize", InitializeAsync).WithDisplayName("ControlPlaneAuth initialize");
        endpoints.MapPost($"{apiPrefix}/api/login", LoginAsync).WithDisplayName("ControlPlaneAuth login");
        endpoints.MapPost($"{apiPrefix}/api/mfa/setup", BeginMfaSetupAsync).WithDisplayName("ControlPlaneAuth MFA setup");
        endpoints.MapPost($"{apiPrefix}/api/mfa/setup/confirm", CompleteMfaSetupAsync).WithDisplayName("ControlPlaneAuth MFA setup confirm");
        endpoints.MapPost($"{apiPrefix}/api/logout", LogoutAsync).WithDisplayName("ControlPlaneAuth logout");
        endpoints.MapGet($"{apiPrefix}/api/me", ReadMeAsync).WithDisplayName("ControlPlaneAuth current user");
        endpoints.MapGet($"{apiPrefix}/api/service-tickets", ListServiceTicketsAsync).WithDisplayName("ControlPlaneAuth service-ticket list");
        endpoints.MapPost($"{apiPrefix}/api/service-tickets", CreateServiceTicketAsync).WithDisplayName("ControlPlaneAuth service-ticket create");
        endpoints.MapPost($"{apiPrefix}/api/service-tickets/{{id}}/rotate", RotateServiceTicketAsync).WithDisplayName("ControlPlaneAuth service-ticket rotate");
        endpoints.MapPost($"{apiPrefix}/api/service-tickets/{{id}}/enable", EnableServiceTicketAsync).WithDisplayName("ControlPlaneAuth service-ticket enable");
        endpoints.MapPost($"{apiPrefix}/api/service-tickets/{{id}}/disable", DisableServiceTicketAsync).WithDisplayName("ControlPlaneAuth service-ticket disable");
        return endpoints;
    }

    private static async Task<IResult> CreateAuthPageAsync(
        HttpContext context,
        IControlPlaneAuthService authService,
        IOptionsMonitor<ControlPlaneAuthOptions> options,
        CancellationToken cancellationToken)
    {
        await authService.EnsureBootstrapTicketAsync(cancellationToken).ConfigureAwait(false);
        var current = options.CurrentValue;
        current.Normalize();
        var authorized = await authService.AuthorizeAsync(context, cancellationToken).ConfigureAwait(false);
        if (authorized.Succeeded)
        {
            return Results.Redirect(ResolveReturnUrl(context, current, current.PlanePath));
        }

        return Results.Content(ControlPlaneAuthPageRenderer.CreateAuthPage(current), "text/html; charset=utf-8");
    }

    private static async Task<IResult> CreatePlanePageAsync(
        HttpContext context,
        IControlPlaneAuthService authService,
        IOptionsMonitor<ControlPlaneAuthOptions> options,
        CancellationToken cancellationToken)
    {
        var current = options.CurrentValue;
        current.Normalize();
        var authorized = await authService.AuthorizeAsync(context, cancellationToken).ConfigureAwait(false);
        if (!authorized.Succeeded)
        {
            return Results.Redirect(BuildAuthRedirectUrl(context, current));
        }

        return Results.Content(ControlPlaneAuthPageRenderer.CreateShellPage(current), "text/html; charset=utf-8");
    }

    private static async Task<IResult> ReadStatusAsync(IControlPlaneAuthService authService, CancellationToken cancellationToken)
    {
        return Results.Ok(await authService.ReadStatusAsync(cancellationToken).ConfigureAwait(false));
    }

    private static async Task<IResult> InitializeAsync(InitializeRequest request, IControlPlaneAuthService authService, CancellationToken cancellationToken)
    {
        var result = await authService.InitializeAsync(request.BootstrapTicket, request.UserName, request.Password, cancellationToken).ConfigureAwait(false);
        return result.Succeeded ? Results.Ok(new { initialized = true }) : Results.BadRequest(result);
    }

    private static async Task<IResult> LoginAsync(
        LoginRequest request,
        HttpContext context,
        IControlPlaneAuthService authService,
        IOptionsMonitor<ControlPlaneAuthOptions> options,
        CancellationToken cancellationToken)
    {
        var result = await authService.LoginAsync(request.UserName, request.Password, request.MfaCode, cancellationToken).ConfigureAwait(false);
        if (!result.Succeeded || string.IsNullOrWhiteSpace(result.Ticket))
        {
            return Results.Json(result, statusCode: StatusCodes.Status401Unauthorized);
        }

        var current = options.CurrentValue;
        current.Normalize();
        context.Response.Cookies.Append(current.Session.CookieName, result.Ticket, new CookieOptions
        {
            HttpOnly = true,
            Secure = context.Request.IsHttps,
            SameSite = SameSiteMode.Strict,
            Expires = result.ExpiresAtUtc
        });
        return Results.Ok(new { ticket = result.Ticket, result.ExpiresAtUtc });
    }

    private static async Task<IResult> BeginMfaSetupAsync(
        MfaSetupRequest request,
        IControlPlaneAuthService authService,
        CancellationToken cancellationToken)
    {
        var result = await authService.BeginMfaSetupAsync(request.UserName, request.Password, cancellationToken).ConfigureAwait(false);
        return result.Succeeded
            ? Results.Ok(new { result.SetupToken, result.ProvisioningUri, result.ExpiresAtUtc })
            : Results.Json(result, statusCode: StatusCodes.Status400BadRequest);
    }

    private static async Task<IResult> CompleteMfaSetupAsync(
        MfaSetupConfirmRequest request,
        IControlPlaneAuthService authService,
        CancellationToken cancellationToken)
    {
        var result = await authService.CompleteMfaSetupAsync(request.SetupToken, request.Code, cancellationToken).ConfigureAwait(false);
        return result.Succeeded
            ? Results.Ok(new { mfaConfigured = true })
            : Results.Json(result, statusCode: StatusCodes.Status400BadRequest);
    }

    private static async Task<IResult> LogoutAsync(HttpContext context, IControlPlaneAuthService authService, IOptionsMonitor<ControlPlaneAuthOptions> options, CancellationToken cancellationToken)
    {
        await authService.LogoutAsync(context, cancellationToken).ConfigureAwait(false);
        var current = options.CurrentValue;
        current.Normalize();
        context.Response.Cookies.Delete(current.Session.CookieName);
        return Results.Ok(new { loggedOut = true });
    }

    private static async Task<IResult> ReadMeAsync(
        HttpContext context,
        IControlPlaneAuthService authService,
        IOptionsMonitor<ControlPlaneAuthOptions> options,
        CancellationToken cancellationToken)
    {
        var authorized = await authService.AuthorizeAsync(context, cancellationToken).ConfigureAwait(false);
        var current = options.CurrentValue;
        current.Normalize();
        return authorized.Succeeded
            ? Results.Ok(new { authorized.User!.Id, authorized.User.UserName, authorized.Session!.ExpiresAtUtc, SessionTicket = ReadRequestTicket(context, current) })
            : Results.Unauthorized();
    }

    private static async Task<IResult> ListServiceTicketsAsync(HttpContext context, IControlPlaneAuthService authService, CancellationToken cancellationToken)
    {
        var authorized = await authService.AuthorizeAsync(context, cancellationToken).ConfigureAwait(false);
        if (!authorized.Succeeded)
        {
            return Results.Unauthorized();
        }

        var tickets = await authService.ReadServiceTicketsAsync(cancellationToken).ConfigureAwait(false);
        return Results.Ok(tickets.Select(item => new
        {
            item.Id,
            item.Type,
            item.Name,
            item.Enabled,
            item.CreatedAtUtc,
            item.ExpiresAtUtc,
            item.LastUsedAtUtc
        }));
    }

    private static async Task<IResult> CreateServiceTicketAsync(CreateServiceTicketRequest request, HttpContext context, IControlPlaneAuthService authService, CancellationToken cancellationToken)
    {
        var authorized = await authService.AuthorizeAsync(context, cancellationToken).ConfigureAwait(false);
        if (!authorized.Succeeded)
        {
            return Results.Unauthorized();
        }

        var result = await authService.CreateServiceTicketAsync(request.Type, request.Name, request.ExpiresAtUtc, cancellationToken).ConfigureAwait(false);
        return Results.Ok(new { result.Ticket.Id, result.Ticket.Type, result.Ticket.Name, result.Ticket.ExpiresAtUtc, plainTextTicket = result.PlainTextTicket });
    }

    private static async Task<IResult> RotateServiceTicketAsync(string id, HttpContext context, IControlPlaneAuthService authService, CancellationToken cancellationToken)
    {
        var authorized = await authService.AuthorizeAsync(context, cancellationToken).ConfigureAwait(false);
        if (!authorized.Succeeded)
        {
            return Results.Unauthorized();
        }

        var result = await authService.RotateServiceTicketAsync(id, cancellationToken).ConfigureAwait(false);
        return result is null
            ? Results.NotFound()
            : Results.Ok(new { result.Ticket.Id, result.Ticket.Type, result.Ticket.Name, plainTextTicket = result.PlainTextTicket });
    }

    private static Task<IResult> EnableServiceTicketAsync(string id, HttpContext context, IControlPlaneAuthService authService, CancellationToken cancellationToken) =>
        SetServiceTicketEnabledAsync(id, true, context, authService, cancellationToken);

    private static Task<IResult> DisableServiceTicketAsync(string id, HttpContext context, IControlPlaneAuthService authService, CancellationToken cancellationToken) =>
        SetServiceTicketEnabledAsync(id, false, context, authService, cancellationToken);

    private static async Task<IResult> SetServiceTicketEnabledAsync(string id, bool enabled, HttpContext context, IControlPlaneAuthService authService, CancellationToken cancellationToken)
    {
        var authorized = await authService.AuthorizeAsync(context, cancellationToken).ConfigureAwait(false);
        if (!authorized.Succeeded)
        {
            return Results.Unauthorized();
        }

        return await authService.SetServiceTicketEnabledAsync(id, enabled, cancellationToken).ConfigureAwait(false)
            ? Results.Ok(new { id, enabled })
            : Results.NotFound();
    }

    private static string BuildAuthRedirectUrl(HttpContext context, ControlPlaneAuthOptions options)
    {
        var returnUrl = (context.Request.PathBase + context.Request.Path + context.Request.QueryString).ToString();
        return options.AuthPath + "?returnUrl=" + Uri.EscapeDataString(NormalizeReturnUrl(returnUrl) ?? options.PlanePath);
    }

    private static string ResolveReturnUrl(HttpContext context, ControlPlaneAuthOptions options, string fallback)
    {
        var requested = NormalizeReturnUrl(context.Request.Query["returnUrl"].FirstOrDefault());
        if (!string.IsNullOrWhiteSpace(requested))
        {
            return requested;
        }

        return !string.IsNullOrWhiteSpace(options.DefaultReturnUrl) ? options.DefaultReturnUrl : fallback;
    }

    private static string? NormalizeReturnUrl(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var normalized = value.Trim();
        return normalized.StartsWith("/", StringComparison.Ordinal)
            && !normalized.StartsWith("//", StringComparison.Ordinal)
            && !normalized.Contains('\\', StringComparison.Ordinal)
            ? normalized
            : null;
    }

    private static string? ReadRequestTicket(HttpContext context, ControlPlaneAuthOptions options)
    {
        if (context.Request.Headers.TryGetValue(options.Session.HeaderName, out var header) && header.Count == 1 && !string.IsNullOrWhiteSpace(header[0]))
        {
            return header[0];
        }

        if (context.Request.Cookies.TryGetValue(options.Session.CookieName, out var cookieTicket))
        {
            return cookieTicket;
        }

        if (!string.IsNullOrWhiteSpace(options.Session.QueryName)
            && context.Request.Query.TryGetValue(options.Session.QueryName, out var query)
            && query.Count == 1
            && !string.IsNullOrWhiteSpace(query[0]))
        {
            return query[0];
        }

        var authorization = context.Request.Headers.Authorization.FirstOrDefault();
        const string bearerPrefix = "Bearer ";
        return authorization?.StartsWith(bearerPrefix, StringComparison.OrdinalIgnoreCase) == true
            ? authorization[bearerPrefix.Length..]
            : null;
    }

    private sealed record InitializeRequest(string BootstrapTicket, string UserName, string Password);

    private sealed record LoginRequest(string UserName, string Password, string? MfaCode);

    private sealed record MfaSetupRequest(string UserName, string Password);

    private sealed record MfaSetupConfirmRequest(string SetupToken, string Code);

    private sealed record CreateServiceTicketRequest(string Type, string Name, DateTimeOffset? ExpiresAtUtc);
}
