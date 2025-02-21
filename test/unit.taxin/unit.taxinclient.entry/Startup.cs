
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
using Air.Cloud.Core.App.Startups;
using Air.Cloud.Core.Attributes;
using Air.Cloud.Core.Standard.JSON.Extensions;
using Air.Cloud.Modules.Taxin.Client;
using Air.Cloud.Modules.Taxin.Extensions;

using System.Text.Encodings.Web;
using System.Text.Unicode;

using unit.webapp.common.Filters;
namespace unit.webapp.entry
{
    [AppStartup(Order = 12000)]
    public class Startup :AppStartup
    {
        public override void ConfigureServices(IServiceCollection services)
        {
            services.AddTaxinClient<TaxinClientDependency>();
            //注入
            services.AddControllers(a =>
            {
                a.Filters.Add<ActionLogFilter>();
            });
        }
        public override void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
          
        }
    }
}
