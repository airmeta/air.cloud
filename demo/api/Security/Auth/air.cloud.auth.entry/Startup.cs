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
using air.cloud.security.common.Auths;

using Air.Cloud.Core.App.Startups;
using Air.Cloud.Modules.Taxin.Client;
using Air.Cloud.Modules.Taxin.Extensions;
using Air.Cloud.Modules.Taxin.Store;
using Air.Cloud.WebApp.Extensions;

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace air.security.auth.entry
{
    public class Startup : AppStartup
    { 
        public override void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {

        }

        public override void ConfigureServices(IServiceCollection services)
        {
            services.AddScoped<IUserAccountStore, UserAccountStore>();
            services.AddSkyMirrorShieldClient();
            services.AddTaxinClient<TaxinClientDependency, TaxinStoreDependency>();
            services.AddControllers(a =>
            {
            }).AddWebAppUnifyResult().AddNewtonsoftJson(o =>
            {
                //全局设置json 序列化enum int 转string
                o.SerializerSettings.Converters.Add(new IsoDateTimeConverter()
                { DateTimeFormat = "yyyy-MM-dd HH:mm:ss" });
                //序列化属性名大写
                //o.SerializerSettings.ContractResolver = new DefaultContractResolver();
                //忽略循环引用
                o.SerializerSettings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
            });

        }
    }
}
