using Air.Cloud.Capabilities.ControlPlaneAuth.Options;
using Air.Cloud.Capabilities.ControlPlaneAuth.Services;
using Air.Cloud.Modules.ReverseTunnel.Common.Options;
using Air.Cloud.Modules.ReverseTunnel.Management;

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

internal sealed class ControlPlaneAuthReverseTunnelManagementAuthorizer : IReverseTunnelManagementAuthorizer
{
    private readonly IControlPlaneAuthService authService;
    private readonly IOptionsMonitor<ControlPlaneAuthOptions> options;

    public ControlPlaneAuthReverseTunnelManagementAuthorizer(IControlPlaneAuthService authService, IOptionsMonitor<ControlPlaneAuthOptions> options)
    {
        this.authService = authService;
        this.options = options;
    }

    public async Task<IResult?> AuthorizeAsync(HttpContext context, ReverseTunnelManagementOptions management, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(management);

        if (!IsManagementPort(context, management))
        {
            return Results.NotFound();
        }

        if (!options.CurrentValue.Enabled)
        {
            return Results.Problem("ControlPlaneAuth is disabled for AirTunnel management.", statusCode: StatusCodes.Status503ServiceUnavailable);
        }

        var result = await authService.AuthorizeAsync(context, cancellationToken).ConfigureAwait(false);
        return result.Succeeded ? null : Results.Unauthorized();
    }

    private static bool IsManagementPort(HttpContext context, ReverseTunnelManagementOptions management)
    {
        if (!Uri.TryCreate(management.BindUrl, UriKind.Absolute, out var uri) || uri.Port <= 0)
        {
            return true;
        }

        return context.Connection.LocalPort == uri.Port;
    }
}
