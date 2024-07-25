using Air.Cloud.Core.App.Startups;
using Air.Cloud.Modules.Kafka.Config;
using Air.Cloud.Modules.Kafka.Extensions;
using Air.Cloud.Modules.Kafka.Helper;

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
            services.AddOptions<KafkaSettingsOptions>()
               .BindConfiguration("KafkaSettings")
               .ValidateDataAnnotations()
               .PostConfigure(options =>
               {});
            services.AddKafkaService();
        }
    }
}
