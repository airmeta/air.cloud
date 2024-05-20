
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
using Air.Cloud.Modules.Taxin.Server;

using unit.webapp.common.Filters;
using Air.Cloud.Core.Standard.Taxin.Server;
using Air.Cloud.Modules.Taxin.Extensions;
namespace unit.webapp.entry
{
    [AppStartup(Order = 12000)]
    public class Startup :AppStartup
    {
        public override void ConfigureServices(IServiceCollection services)
        {
            services.AddTaxinServer<TaxinServerDependency>();
            //注入
            services.AddControllers(a =>
            {
                a.Filters.Add<ActionLogFilter>();
            });
        }
        public override void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.UseTaxinServer<TaxinServerDependency>();
            app.Use(next => context =>
            {
                context.Request.EnableBuffering();//启动倒带方式
                return next(context);
            });
        }
    }
}
