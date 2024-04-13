using Air.Cloud.Core.App.Startups;
using Air.Cloud.Plugins.SpecificationDocument.Extensions;
using Air.Cloud.Plugins.SpecificationDocument.Extensions.Options;
using Air.Cloud.Plugins.SpecificationDocument.Options;

using Microsoft.AspNetCore.Hosting;

namespace Air.Cloud.Plugins.SpecificationDocument
{
    public class Startup : AppStartup
    {
        public override void ConfigureServices(IServiceCollection services)
        {
            services.AddSpecificationDocuments();
            services.WebSpecificationDocumentInject();
        }

        public override void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.UseSwaggerDocumentPlugin();
        }
    }
}