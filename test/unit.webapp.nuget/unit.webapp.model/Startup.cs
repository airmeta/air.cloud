using Air.Cloud.Core.App;
using Air.Cloud.Core.App.Startups;
using Air.Cloud.DataBase.Extensions;
using Air.Cloud.DataBase.Extensions.DatabaseProvider;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using unit.webapp.repository;
using unit.webapp.repository.DbContexts;

using DbContextSaveChangesInterceptor = unit.webapp.repository.DbContexts.DbContextSaveChangesInterceptor;

namespace unit.webapp.model
{
    public class Startup : AppStartup
    {
        public override void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {

        }

        public override void ConfigureServices(IServiceCollection services)
        {
            services.AddDatabaseAccessor(options =>
            {
                options.AddDbPool<DefaultDbContext>(providerName: default,
                       (services, opt) =>
                       {
                           var conn = AppCore.Configuration["ConnectionStrings:OracleConnectionString"];
                           //设置oracle使用的版本
                           opt.EnableSensitiveDataLogging().UseOracle(conn, b =>
                           {
                               b.UseOracleSQLCompatibility("11");
                           });
                       },
                       //注册拦截器
                       interceptors: new Microsoft.EntityFrameworkCore.Diagnostics.IInterceptor[]
                       {
                        new DbContextSaveChangesInterceptor(),
                        new SqlCommandAuditInterceptor()
                       });
            }, "Air.Database.Migrations");

            //使用EF的分析工具
            HibernatingRhinos.Profiler.Appender.EntityFramework.EntityFrameworkProfiler.Initialize();
        }
    }
}