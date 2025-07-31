using Air.Cloud.Core.App.Startups;
using Air.Cloud.Core.Standard.Taxin;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace Air.Cloud.Modules.Taxin
{
    public class Startup : AppStartup
    {
        public override void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {


        }

        public override void ConfigureServices(IServiceCollection services)
        {
            services.AddOptions<TaxinOptions>()
               .BindConfiguration("TaxinSettings")
               .ValidateDataAnnotations()
               .PostConfigure(options =>
               { });
        }
    }
}
