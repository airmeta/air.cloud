using Air.Cloud.Core.App.Startups;
using Air.Cloud.Plugins.Jwt.Extensions;

using unit.webapp.common.JwtHandler;
namespace unit.webapp.entry
{
    public class Startup : AppStartup
    {
        public override void ConfigureServices(IServiceCollection services)
        {
            //注入jwt
            services.WebJwtHandlerInject<AppJwtHandler>(enableGlobalAuthorize: false);
        }
        public override void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
        }
    }
}
