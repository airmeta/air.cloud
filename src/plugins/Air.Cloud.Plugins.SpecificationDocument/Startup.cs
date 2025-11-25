
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
using Air.Cloud.Plugins.SpecificationDocument.Extensions;

using Microsoft.AspNetCore.Hosting;

namespace Air.Cloud.Plugins.SpecificationDocument
{
    [AppStartup(Order =9900)]
    public class Startup : AppStartup
    {
        public override void ConfigureServices(IServiceCollection services)
        {
            services.AddSpecificationDocuments();
            services.WebSpecificationDocumentInject();
        }

        public override void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        { // 判断是否启用规范化文档
            if (AppCore.Settings.InjectSpecificationDocument==true)
            {
                app.UseSwaggerDocumentPlugin();
            }
        }
    }
}