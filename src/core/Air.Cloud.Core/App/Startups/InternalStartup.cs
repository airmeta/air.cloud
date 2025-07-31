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
using Air.Cloud.Core.Modules.AppAspect.Extensions;
using Air.Cloud.Core.Plugins;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace Air.Cloud.Core.App.Startups
{
    [AppStartup(AppName = "Air.Cloud.Core", Order = int.MinValue)]
    public  class InternalStartup : AppStartup
    {
        public override void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            
        }

        public override void ConfigureServices(IServiceCollection services)
        {
            services.AddAspect();
        }
    }
}
