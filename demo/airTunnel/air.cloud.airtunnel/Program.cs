using Air.Cloud.Capabilities.ControlPlaneAuth.Extensions;
using Air.Cloud.Core.Extensions;
using Air.Cloud.Modules.ReverseTunnel.Common.Options;
using Air.Cloud.Modules.ReverseTunnel.Extensions;
using Air.Cloud.Modules.ReverseTunnel.Management;

using Microsoft.AspNetCore.Server.Kestrel.Https;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Server.Kestrel.Core;

AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);

var builder = WebApplication.CreateBuilder(args);
builder.Configuration
    .AddAirCloudConfigDirectory("config", builder.Environment.EnvironmentName, optional: true, reloadOnChange: true)
    .AddJsonFile("configs/airtunnel.server.json", optional: true, reloadOnChange: true)
    .AddJsonFile("configs/airtunnel.listeners.json", optional: true, reloadOnChange: true)
    .AddJsonFile("configs/airtunnel.authorizations.json", optional: true, reloadOnChange: true)
    .AddJsonFile("configs/airtunnel.limits.json", optional: true, reloadOnChange: true)
    .AddJsonFile("configs/airtunnel.diagnostics.json", optional: true, reloadOnChange: true)
    .AddJsonFile("configs/airtunnel.management.json", optional: true, reloadOnChange: true);

builder.Services.AddControlPlaneAuth(builder.Configuration.GetSection("ControlPlaneAuth"));
builder.Services.AddSingleton<IReverseTunnelManagementAuthorizer, ControlPlaneAuthReverseTunnelManagementAuthorizer>();
builder.Services.AddReverseTunnel();
builder.Services.Configure<ReverseTunnelOptions>(builder.Configuration.GetSection("ReverseTunnelSettings"));

builder.WebHost.ConfigureKestrel((context, options) =>
{
    var kestrelConfiguration = options.Configure(context.Configuration.GetSection("Kestrel"));
    kestrelConfiguration.Endpoint("AirTunnel", endpointOptions =>
    {
        endpointOptions.HttpsOptions.ClientCertificateMode = ClientCertificateMode.AllowCertificate;
        endpointOptions.HttpsOptions.ClientCertificateValidation = (_, _, _) => true;
    });
    kestrelConfiguration.Endpoint("AirTunnelManagement", endpointOptions =>
    {
        endpointOptions.HttpsOptions.ClientCertificateMode = ClientCertificateMode.NoCertificate;
    });
    options.ConfigureHttpsDefaults(httpsOptions =>
    {
        httpsOptions.ClientCertificateMode = ClientCertificateMode.NoCertificate;
        httpsOptions.ClientCertificateValidation = (_, _, _) => true;
    });

    var managementUrl = context.Configuration["ReverseTunnelSettings:Management:BindUrl"];
    if (context.Configuration.GetValue<bool>("ReverseTunnelSettings:Management:Enabled") &&
        Uri.TryCreate(managementUrl, UriKind.Absolute, out var managementUri) &&
        managementUri.Port > 0 &&
        !context.Configuration.GetSection("Kestrel:Endpoints:AirTunnelManagement").Exists())
    {
        options.ListenAnyIP(managementUri.Port, listenOptions =>
        {
            listenOptions.Protocols = HttpProtocols.Http1AndHttp2;
            if (managementUri.Scheme == Uri.UriSchemeHttps)
            {
                listenOptions.UseHttps(httpsOptions =>
                {
                    httpsOptions.ClientCertificateMode = ClientCertificateMode.NoCertificate;
                });
            }
        });
    }
});

var app = builder.Build();
var reverseTunnelOptions = app.Services.GetRequiredService<IOptions<ReverseTunnelOptions>>().Value;

app.UseRouting();
app.UseControlPlaneAuth();
app.MapControlPlaneAuth();

if (reverseTunnelOptions.Enabled && reverseTunnelOptions.TunnelMode == ReverseTunnelMode.Server)
{
    app.MapReverseTunnelServer();
}
else
{
    // Keep diagnostics discoverable for Agent-only and disabled hosts.
    // The endpoint checks the runtime switch and returns 404 while disabled.
    app.MapReverseTunnelDiagnostics();
}

await app.RunAsync();
