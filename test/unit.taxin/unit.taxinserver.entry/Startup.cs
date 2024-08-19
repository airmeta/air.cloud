
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
using Air.Cloud.DataBase.Filters;
using Air.Cloud.Modules.Taxin.Client;
using Air.Cloud.Modules.Taxin.Extensions;
using Air.Cloud.Modules.Taxin.Server;
using Air.Cloud.WebApp.Extensions;

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

using System.Text;
using Air.Cloud.Modules.Clairvoyance.Extensions;
using unit.webapp.common.Filters;
using unit.taxinclient.entry;
using Air.Cloud.Modules.Clairvoyance.Dependencies;
using System.Net.Sockets;
namespace unit.taxinserver.entry
{
    [AppStartup(500)]
    public class Startup : AppStartup
    {
        public override void ConfigureServices(IServiceCollection services)
        {
            //客户端
            services.WebInjectClairvoyanceServer(s =>
            {
                
            });
            //services.AddHostedService<TestService>();
           
            services.AddTaxinServer<TaxinServerDependency, TaxinClientDependency>();
            //注入
            services.AddControllers(a =>
            {
                a.Filters.Add<ActionLogFilter>();
                a.Filters.Add<AutoSaveChangesFilter>();
            }).AddInjectWithUnifyResult().AddNewtonsoftJson(s =>
            {
                //全局设置json 序列化enum int 转string
                s.SerializerSettings.Converters.Add(new IsoDateTimeConverter()
                { DateTimeFormat = "yyyy-MM-dd HH:mm:ss" });
                s.SerializerSettings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
            });
        }
        public override void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.UseTaxinServer<TaxinServerDependency>();
        }
    }
}
