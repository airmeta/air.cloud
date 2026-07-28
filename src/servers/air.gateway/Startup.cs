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
using air.gateway.Middleware;
using air.gateway.Modules.TraceLogModules;
using air.gateway.Options;
using air.gateway.Configuration;

using Air.Cloud.Core;
using Air.Cloud.Core.App;
using Air.Cloud.Core.App.Startups;
using Air.Cloud.Core.Attributes;
using Air.Cloud.Core.Plugins.LogFiltering;
using Air.Cloud.Core.Standard.TraceLog;
using Air.Cloud.Modules.ReverseTunnel.Common.Options;
using Air.Cloud.Modules.ReverseTunnel.Extensions;
using Air.Cloud.Modules.SkyMirrorShield.Middleware;

using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.Server.Kestrel.Https;
using Microsoft.Extensions.Options;

using Ocelot.Cache.CacheManager;
using Ocelot.DependencyInjection;
using Ocelot.Middleware;
using Ocelot.Provider.Consul;
using Ocelot.Provider.Polly;
namespace air.gateway
{

    [AppStartup(Order=3000)]
    public class Startup : AppStartup
    {
        public override void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.UseCors("CorsPolicy");
            app.UseWebSockets();
            var reverseTunnelOptions = app.ApplicationServices.GetRequiredService<IOptions<ReverseTunnelOptions>>().Value;
            if (reverseTunnelOptions.Enabled)
            {
                app.UseRouting();
                if (reverseTunnelOptions.Roles.HasFlag(ReverseTunnelRole.Server))
                {
                    app.UseReverseTunnelServer();
                }
                else
                {
                    app.UseReverseTunnelDiagnostics();
                }
            }
            bool enableSkyMirrorShield = bool.TryParse(
                AppConfigurationLoader.InnerConfiguration["SkyMirrorShieldSettings:EnableSkyMirrorShield"],
                out bool skyMirrorShieldEnabled) && skyMirrorShieldEnabled;
            if (enableSkyMirrorShield)
            {
                app.UseSkyMirrorShieldServer();
            }
            //app.UseTaxinServer<TaxinServerDependency>();
            app.UseMiddleware<TraceLogMiddleware>();
            if (enableSkyMirrorShield)
            {
                app.UseMiddleware<SkyMirrorShieldMiddleware>();
            }
            app.UseMiddleware<SignatureMiddleware>();
            app.UseMiddleware<WhiteListRequestMiddleware>();
            app.UseMiddleware<IPMiddleware>();
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
            // services.AddGateWayPlugins();
            var configurationManager = GatewayOcelotConfigurationLoader.Load(AppConfigurationLoader.InnerConfiguration);
            services.AddReverseTunnel();
            services.Configure<ReverseTunnelOptions>(configurationManager.GetSection("ReverseTunnelSettings"));
            services.Configure<GatewaySettingsOptions>(AppConfigurationLoader.InnerConfiguration.GetSection("GatewaySettings"));
            services.Configure<AppLogFilterOptions>(configurationManager.GetSection("AppLogFilter"));
            services.Configure<AppLogFilterOptions>(AppConfigurationLoader.InnerConfiguration.GetSection("AppLogFilter"));
            //注入网关配置文件
            services.AddOcelot(configurationManager)
                .AddReverseTunnelDelegatingHandler()
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
