using Air.Cloud.Core.App.Startups;
using Air.Cloud.Plugins.APICatalog.Extensions;

using Microsoft.AspNetCore.Hosting;

namespace Air.Cloud.Plugins.APICatalog;

[AppStartup(Order = 9890)]
public class Startup : AppStartup
{
    public override void ConfigureServices(IServiceCollection services)
    {
        services.AddAPIProbe();
    }

    public override void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        app.UseAPIProbePlugin();
    }
}
