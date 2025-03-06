using Air.Cloud.Core.App.Startups;
using Air.Cloud.Modules.RedisCache.Dependencies;
using Air.Cloud.Modules.RedisCache.Extensions;
using Air.Cloud.Modules.RedisCache.Options;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace Air.Cloud.Modules.RedisCache
{
    public class Startup : AppStartup
    {
        public override void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            

        }

        public override void ConfigureServices(IServiceCollection services)
        {
            services.AddOptions<RedisSettingsOptions>()
               .BindConfiguration("RedisSettings")
               .ValidateDataAnnotations()
               .PostConfigure(options =>
               {});
            services.AddRedisCacheService<RedisCacheDependency>();
        }
    }
}
