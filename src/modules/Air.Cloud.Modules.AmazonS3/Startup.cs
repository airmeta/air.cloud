using Air.Cloud.Core.App.Startups;
using Air.Cloud.Core.Standard.AmazonS3;
using Air.Cloud.Core.Standard.AmazonS3.Options;
using Air.Cloud.Modules.AmazonS3.ClientFactory;
using Air.Cloud.Modules.AmazonS3.Dependencies;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace Air.Cloud.Modules.AmazonS3
{
    public class Startup : AppStartup
    {
        public override void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
           
        }

        public override void ConfigureServices(IServiceCollection services)
        {
            services.AddOptions<AmazonS3Options>()
                .BindConfiguration("AmazonS3Settings")
                .ValidateDataAnnotations()
                   .PostConfigure(options =>
                   { }); ;
            services.AddSingleton<IAmazonS3ObjectStandard, AmazonS3ObjectDependency>();
            services.AddSingleton<IAmazonS3BucketStandard, AmazonS3BucketDependency>();
            services.AddSingleton<IAmazonS3ClientFactory, AmazonS3ClientFactory>();
            services.AddSingleton<IAmazonS3ClientStandard, AmazonS3ClientDependency>();
            services.AddSingleton<IAmazonS3Standard, AmazonS3Dependency>();
        }
    }
}
