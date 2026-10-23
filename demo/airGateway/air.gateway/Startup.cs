/*
 * Copyright (c) 2024-2030 星曳数据
 *
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at https://mozilla.org/MPL/2.0/.
 *
 * This file is provided under the Mozilla Public License Version 2.0,
 * and the "NO WARRANTY" clause of the MPL is hereby expressly
 * acknowledged.
 */
using air.gateway.Modules.TraceLogModules;
using air.gateway.Options;
using air.gateway.Configuration;

using air.cloud.gateway.Plugins.Extensions;
using air.cloud.gateway.Plugins.Runtime;

using air.gateway.Middleware;
using air.gateway.Manage;
using air.gateway.Jobs;
using air.gateway.Modules.WhiteRouteLoader;

using Air.Cloud.Capabilities.ControlPlaneAuth.Extensions;
using Air.Cloud.Core;
using Air.Cloud.Core.App;
using Air.Cloud.Core.App.Startups;
using Air.Cloud.Core.Attributes;
using Air.Cloud.Core.Plugins.LogFiltering;
using Air.Cloud.Core.Standard.TraceLog;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.Server.Kestrel.Https;

using Ocelot.Cache.CacheManager;
using Ocelot.DependencyInjection;
using Ocelot.Middleware;
using Ocelot.Provider.Consul;
using Ocelot.Provider.Polly;
using Quartz;
namespace air.gateway
{

    [AppStartup(Order=3000)]
    public class Startup : AppStartup
    {
        public override void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.UseCors("CorsPolicy");
            app.UseWebSockets();
            app.UseRouting();
            var controlPlaneAuth = AppConfigurationLoader.InnerConfiguration.GetSection("ControlPlaneAuth");
            app.UseMiddleware<ControlPlaneManagementMiddleware>();
            if (controlPlaneAuth.Exists()
                && controlPlaneAuth.GetValue("Enabled", true)
                && controlPlaneAuth.GetSection("Management").GetValue("Enabled", true))
            {
                app.UseControlPlaneAuth();
                app.UseEndpoints(endpoints =>
                {
                    var managementPort = controlPlaneAuth.GetValue("Management:Port", 0);
                    var authPath = controlPlaneAuth.GetValue<string>("AuthPath") ?? "/_control-plane/auth";
                    endpoints.MapGet("/", (HttpContext context) =>
                        managementPort > 0 && context.Connection.LocalPort != managementPort
                            ? Results.NotFound()
                            : Results.Redirect(authPath))
                        .WithDisplayName("AirGateway control-plane root redirect")
                        .WithOrder(-100);
                    endpoints.MapControlPlaneAuth();
                    endpoints.MapGatewayConfigurationPage();
                });
            }
            //app.UseTaxinServer<TaxinServerDependency>();
            app.UseMiddleware<GatewayPluginHostMiddleware>();
            app.UseOcelot().Wait();
        }

        /// <summary>
        /// <para>zh-cn:注册网关运行所需服务。Ocelot 配置源由 `GatewaySettings:OcelotConfigurationSource` 控制，默认继续使用 Consul；`AppLogFilter` 仍额外叠加本地 appsettings 配置，便于部署节点独立控制日志降噪。</para>
        /// <para>en-us:Registers services required by the gateway runtime. The Ocelot configuration source is controlled by `GatewaySettings:OcelotConfigurationSource` and defaults to Consul; `AppLogFilter` still overlays local appsettings configuration so each deployment node can control log filtering independently.</para>
        /// </summary>
        /// <param name="services">
        /// <para>zh-cn:ASP.NET Core 依赖注入服务集合。</para>
        /// <para>en-us:The ASP.NET Core dependency injection service collection.</para>
        /// </param>
        public override void ConfigureServices(IServiceCollection services)
        {
            var configurationManager = GatewayOcelotConfigurationLoader.Load(AppConfigurationLoader.InnerConfiguration);
            var controlPlaneAuth = AppConfigurationLoader.InnerConfiguration.GetSection("ControlPlaneAuth");
            if (controlPlaneAuth.Exists() && controlPlaneAuth.GetValue("Enabled", true))
            {
                services.AddControlPlaneAuth(controlPlaneAuth);
            }
            // Plugin descriptors belong to the host configuration. The Consul-only Ocelot configuration
            // deliberately excludes local product capabilities such as the runtime plugin chain.
            services.AddGatewayPluginRuntime(AppConfigurationLoader.InnerConfiguration.GetSection("GatewayPluginSettings"));
            services.Configure<GatewaySettingsOptions>(AppConfigurationLoader.InnerConfiguration.GetSection("GatewaySettings"));
            services.Configure<AppLogFilterOptions>(configurationManager.GetSection("AppLogFilter"));
            services.Configure<AppLogFilterOptions>(AppConfigurationLoader.InnerConfiguration.GetSection("AppLogFilter"));
            services.AddHttpContextAccessor();
            services.AddTransient<OcelotRequestContentRestorationDelegatingHandler>();
            services.AddSingleton<AirWhiteRouteLoader>();
            var whiteListRefreshIntervalSeconds = AppConfigurationLoader.InnerConfiguration
                .GetValue("WhiteListRouteRefresh:IntervalSeconds", 30);
            if (whiteListRefreshIntervalSeconds <= 0)
            {
                throw new InvalidOperationException("WhiteListRouteRefresh:IntervalSeconds must be greater than zero.");
            }

            services.AddQuartz(quartz =>
            {
                var jobKey = new JobKey(nameof(WhiteListRouteRefreshJob));
                quartz.AddJob<WhiteListRouteRefreshJob>(options => options.WithIdentity(jobKey));
                quartz.AddTrigger(trigger => trigger
                    .ForJob(jobKey)
                    .WithIdentity($"{nameof(WhiteListRouteRefreshJob)}.trigger")
                    .StartNow()
                    .WithSimpleSchedule(schedule => schedule.WithIntervalInSeconds(whiteListRefreshIntervalSeconds).RepeatForever()));
            });
            services.AddQuartzHostedService(options => options.WaitForJobsToComplete = true);
            //注入网关配置文件
            services.AddOcelot(configurationManager)
                .AddDelegatingHandler<OcelotRequestContentRestorationDelegatingHandler>(true)
                .AddCacheManager(x =>
            {
                x.WithDictionaryHandle();
            })
                .AddConsul()
                .AddPolly();
            AppConfigurationLoader.Configurations.AddConfiguration(AppConfigurationLoader.InnerConfiguration);
            #region  跨域配置
            string AllowCors = AppConfigurationLoader.InnerConfiguration["AllowCors"];
            services.AddCors(options => options.AddPolicy("CorsPolicy",
                builde =>
                {
                    builde.AllowAnyMethod()
                    .WithOrigins(AllowCors.Split(","))
                    .AllowAnyHeader()
                    .AllowCredentials();
                }));
            #endregion
            services.AddOptions<TraceLogSettings>()
                .BindConfiguration("TraceLogSettings")
                .ValidateDataAnnotations()
                .PostConfigure(options =>
                {
                    _ = TraceLogSettings.SetDefaultSettings(options);
                });
            services.AddHostedService<TraceLogBackgroundWriterService>();
            //services.AddTaxinServer<TaxinServerDependency, TaxinStoreDependency>();
            services.AddSkyMirrorShieldServer();
            #region  表单上传配置
            services.Configure<FormOptions>(x =>
            {
                x.ValueLengthLimit = int.MaxValue;
                x.MultipartBodyLengthLimit = int.MaxValue; // if don't set default value is: 128 MB
                x.MultipartHeadersLengthLimit = int.MaxValue;
            });
            services.Configure<KestrelServerOptions>(options =>
            {
                options.Limits.MaxRequestBodySize = int.MaxValue;
                options.ConfigureHttpsDefaults(httpsOptions =>
                {
                    httpsOptions.ClientCertificateMode = ClientCertificateMode.AllowCertificate;
                    httpsOptions.ClientCertificateValidation = (_, _, _) => true;
                });
            });
            services.Configure<HttpsRedirectionOptions>(options =>
            {
                options.HttpsPort = -1;
            });
            #endregion
            AppRealization.SetDependency<ITraceLogStandard>(new TraceLogStandardDependency());


        }
    }
}
