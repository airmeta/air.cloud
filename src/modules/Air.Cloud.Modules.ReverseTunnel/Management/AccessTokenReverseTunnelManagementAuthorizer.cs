using Air.Cloud.Modules.ReverseTunnel.Common.Options;

using Microsoft.AspNetCore.Http;

using System.Net;

namespace Air.Cloud.Modules.ReverseTunnel.Management;

/// <summary>
/// <para>zh-cn:AirTunnel 管理端默认 Token 授权器。该实现保留旧版 `X-AirTunnel-Admin-Token` 与 Bearer Token 行为，便于未接入统一控制面认证的宿主继续运行。</para>
/// <para>en-us:Default token authorizer for AirTunnel management endpoints. This implementation preserves the legacy `X-AirTunnel-Admin-Token` and Bearer Token behavior so hosts that have not adopted unified control-plane authentication continue to work.</para>
/// </summary>
public sealed class AccessTokenReverseTunnelManagementAuthorizer : IReverseTunnelManagementAuthorizer
{
    /// <inheritdoc />
    public Task<IResult?> AuthorizeAsync(HttpContext context, ReverseTunnelManagementOptions management, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(management);

        if (!IsManagementPort(context, management))
        {
            return Task.FromResult<IResult?>(Results.NotFound());
        }

        var expected = string.IsNullOrWhiteSpace(management.AccessTokenEnvironmentVariable)
            ? null
            : Environment.GetEnvironmentVariable(management.AccessTokenEnvironmentVariable);
        if (string.IsNullOrEmpty(expected))
        {
            return Task.FromResult<IResult?>(management.AllowAnonymousLoopback && IsLoopback(context)
                ? null
                : Results.Problem("AirTunnel management access token is not configured.", statusCode: StatusCodes.Status503ServiceUnavailable));
        }

        var actual = ReadManagementToken(context);
        return Task.FromResult<IResult?>(string.Equals(expected, actual, StringComparison.Ordinal)
            ? null
            : Results.Unauthorized());
    }

    private static bool IsManagementPort(HttpContext context, ReverseTunnelManagementOptions management)
    {
        if (!Uri.TryCreate(management.BindUrl, UriKind.Absolute, out var uri) || uri.Port <= 0)
        {
            return true;
        }

        return context.Connection.LocalPort == uri.Port;
    }

    private static bool IsLoopback(HttpContext context)
    {
        var remote = context.Connection.RemoteIpAddress;
        return remote is null || IPAddress.IsLoopback(remote);
    }

    private static string? ReadManagementToken(HttpContext context)
    {
        if (context.Request.Headers.TryGetValue("X-AirTunnel-Admin-Token", out var header) && header.Count == 1)
        {
            return header[0];
        }

        var authorization = context.Request.Headers.Authorization.FirstOrDefault();
        const string bearerPrefix = "Bearer ";
        return authorization?.StartsWith(bearerPrefix, StringComparison.OrdinalIgnoreCase) == true
            ? authorization[bearerPrefix.Length..]
            : null;
    }
}
