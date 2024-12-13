
/*
 * Copyright (c) 2024 星曳数据
 *
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at https://mozilla.org/MPL/2.0/.
 *
 * This file is provided under the Mozilla Public License Version 2.0,
 * and the "NO WARRANTY" clause of the MPL is hereby expressly
 * acknowledged.
 */
using Air.Cloud.Core.App.Startups;
using Air.Cloud.Core.Attributes;
using Air.Cloud.Core.Standard.KVCenter;
using Air.Cloud.Core.Standard.ServerCenter;
using Air.Cloud.Modules.Consul.Service;
using Air.Cloud.Plugins.Jwt.Extensions;
using Air.Cloud.WebApp.Extensions;

using unit.webapp.common.Filters;
using unit.webapp.common.JwtHandler;
namespace unit.webapp.entry
{
    [AppStartup(Order = int.MinValue)]
    public class Startup :AppStartup
    {
        public override void ConfigureServices(IServiceCollection services)
        {
            //services.AddTaxinClient<TaxinClientDependency>();
            services.WebJwtHandlerInject<AppJwtHandler>(enableGlobalAuthorize: false);
            services.AddTransient<IServerCenterStandard, ConsulServerCenterDependency>();
            services.AddTransient<IKVCenterStandard, ConsulKVCenterDependency>();

            //注入
            services.AddControllers(a =>
            {
                a.Filters.Add<ActionLogFilter>();
            }).AddInjectWithUnifyResult();

        }
        public override void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
           
        }
    }
}
