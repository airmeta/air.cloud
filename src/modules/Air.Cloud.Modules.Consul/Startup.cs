using Air.Cloud.Core.App.Startups;
using Air.Cloud.Core.Standard.KVCenter;
using Air.Cloud.Core.Standard.ServerCenter;
using Air.Cloud.Modules.Consul.Service;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace Air.Cloud.Modules.Consul
{
    public  class Startup : AppStartup
    {
        public override void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {


        }

        public override void ConfigureServices(IServiceCollection services)
        {
            services.AddTransient<IServerCenterStandard, ConsulServerCenterDependency>();
            services.AddTransient<IKVCenterStandard, ConsulKVCenterDependency>();
        }
    }
}
