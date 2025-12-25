
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
global using Air.Cloud.Core.Modules.AppPrint;
using Air.Cloud.Core;
using Air.Cloud.Core.App.Startups;
using Air.Cloud.Core.Attributes;
using Air.Cloud.Core.Standard.KVCenter;
using Air.Cloud.Core.Standard.ServerCenter;
using Air.Cloud.Core.Standard.TraceLog;
using Air.Cloud.GateWay.Middleware;
using Air.Cloud.Modules.Consul.Service;
using Air.Cloud.Modules.Taxin.Client;
using Air.Cloud.Modules.Taxin.Extensions;
using Air.Cloud.Modules.Taxin.Store;
using Air.Cloud.Plugins.Jwt.Extensions;
using Air.Cloud.WebApp.Extensions;

using Microsoft.OpenApi.Models;

using unit.webapp.common.Filters;
using unit.webapp.common.JwtHandler;
using unit.webapp.entry.TraceLogDependency;
namespace unit.webapp.entry
{
    [AppStartup]
    public class Startup :AppStartup
    {
        public override void ConfigureServices(IServiceCollection services)
        {
           
            AppRealization.SetDependency<ITraceLogStandard>(new TraceLogStandardDependency());
            services.AddTaxinClient<TaxinClientDependency,TaxinStoreDependency>();
            //services.WebJwtHandlerInject<AppJwtHandler>(enableGlobalAuthorize: false);
            services.AddTransient<IServerCenterStandard, ConsulServerCenterDependency>();
            services.AddTransient<IKVCenterStandard, ConsulKVCenterDependency>();
            //注入
            services.AddSkyMirrorShieldClient().AddAppControllers(a =>
            {
                a.Filters.Add<ActionLogFilter>();
            }).AddInjectWithUnifyResult();
        }
        public override void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
           
        }
    }

    [AppStartup(Order =3000)]
    public class Startup1 : AppStartup
    {
        public override void ConfigureServices(IServiceCollection services)
        {
          
        }
        public override void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.UseMiddleware<TraceLogMiddleware>();
        }
    }

}
