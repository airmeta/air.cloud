
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
using Air.Cloud.Core;
using Air.Cloud.Core.App.Startups;
using Air.Cloud.Core.Standard.TraceLog;
using Air.Cloud.WebApp.DynamicApiController.Extensions;
using Air.Cloud.WebApp.UnifyResult.Extensions;

using unit.taxinclient.entry.TraceLogDependency;
namespace unit.taxinclient.entry
{
    public class Startup : AppStartup
    {
        public override void ConfigureServices(IServiceCollection services)
        {
            //services.AddTaxinClient<TaxinClientDependency,TaxinStoreDependency>();
            ////注入
            services.AddControllers(a =>
            {
              
            });
            AppRealization.SetDependency<ITraceLogStandard>(new TraceLogStandardDependency());

            services.AddDynamicApiControllers().AddUnifyResult();
        }
        public override void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.UseMiddleware<TraceLogMiddleware>();
        }
    }
}
