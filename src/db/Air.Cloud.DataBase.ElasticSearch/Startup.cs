using Air.Cloud.Core.App.Startups;
using Air.Cloud.Core.Standard.DataBase.Options;
using Air.Cloud.DataBase.ElasticSearch.Extensions;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace Air.Cloud.DataBase.ElasticSearch
{
    public class Startup : AppStartup
    {
        public override void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            

        }

        public override void ConfigureServices(IServiceCollection services)
        {
            services.AddOptions<DataBaseOptions>()
               .BindConfiguration("DataBaseSettings")
               .ValidateDataAnnotations()
               .PostConfigure(options =>
               { });
            services.AddElasticSearch();
        }
    }
}
