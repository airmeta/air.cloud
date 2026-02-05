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

using Air.Cloud.Core;
using Air.Cloud.Core.App;
using Air.Cloud.Core.App.Startups;
using Air.Cloud.Core.Attributes;
using Air.Cloud.Core.Standard.TraceLog;
using Air.Cloud.Modules.Consul.Model;
using Air.Cloud.Modules.Consul.Util;
using Air.Cloud.Modules.SkyMirrorShield.Middleware;

using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core;

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
            app.UseSkyMirrorShieldServer();
            //app.UseTaxinServer<TaxinServerDependency>();
            app.UseMiddleware<TraceLogMiddleware>();
            app.UseMiddleware<SkyMirrorShieldMiddleware>();
            app.UseMiddleware<SignatureMiddleware>();
            app.UseMiddleware<WhiteListRequestMiddleware>();
            app.UseMiddleware<IPMiddleware>();
            app.UseOcelot().Wait();
        }

        public override void ConfigureServices(IServiceCollection services)
        {
            // services.AddGateWayPlugins();
            #region 加载配置信息
            var options = AppConfigurationLoader.InnerConfiguration.GetConfig<ConsulServiceOptions>();
            var Config = ConfigurationLoader.LoadRemoteConfiguration(options);
            ConfigurationManager configurationManager = new ConfigurationManager();
            configurationManager.AddConfiguration(Config.Item1);
            configurationManager.AddConfiguration(Config.Item2);
            #endregion
            //注入网关配置文件
            services.AddOcelot(configurationManager).AddCacheManager(x =>
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
            });
            #endregion
            AppRealization.SetDependency<ITraceLogStandard>(new TraceLogStandardDependency());


        }
    }
}
