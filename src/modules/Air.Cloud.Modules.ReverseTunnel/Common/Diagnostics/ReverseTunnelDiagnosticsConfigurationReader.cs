using Air.Cloud.Core.App;
using Air.Cloud.Modules.ReverseTunnel.Common.Options;

using Microsoft.Extensions.Configuration;

namespace Air.Cloud.Modules.ReverseTunnel.Common.Diagnostics;

internal static class ReverseTunnelDiagnosticsConfigurationReader
{
    public static ReverseTunnelDiagnosticsOptions Read(ReverseTunnelOptions fallback)
    {
        var diagnostics = fallback.Diagnostics ?? new ReverseTunnelDiagnosticsOptions();
        var section = AppCore.Configuration?.GetSection("ReverseTunnelSettings:Diagnostics");
        if (section is not null && (section.Value is not null || section.GetChildren().Any()))
        {
            diagnostics = section.Get<ReverseTunnelDiagnosticsOptions>() ?? diagnostics;
        }

        diagnostics.Normalize();
        return diagnostics;
    }
}
