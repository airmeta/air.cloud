using Air.Cloud.Core.App.Startups;
using Air.Cloud.Core.Attributes;
using Air.Cloud.Modules.Quartz.Extensions;
using Air.Cloud.Modules.Quartz.Options;

namespace unit.webapp.entry
{
    /// <summary>
    /// <para>zh-cn:启动项</para>
    /// <para>en-us:Startup</para>
    /// </summary>
    [AppStartup(Order = 500)]
    public class QuartStartup : AppStartup
    {
        public static CancellationTokenSource cts = new CancellationTokenSource();

            public override void Configure(IApplicationBuilder app, IWebHostEnvironment env)
            {
                app.UseQuartzServices<QuartzSchedulerStandardOptions>();
            }
            public override void ConfigureServices(IServiceCollection services)
            {
                services.AddQuartzService<QuartzSchedulerStandardOptions>();
            }
        
    }
}
