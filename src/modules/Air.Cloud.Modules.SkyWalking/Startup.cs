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
using Air.Cloud.Core;
using Air.Cloud.Core.App;
using Air.Cloud.Core.App.Startups;
using Air.Cloud.Core.Attributes;
using Air.Cloud.Core.Standard.TraceLog;
using Air.Cloud.Modules.SkyWalking.Const;
using Air.Cloud.Modules.SkyWalking.Dependency;
using Air.Cloud.Modules.SkyWalking.Extensions;
using Air.Cloud.Modules.SkyWalking.Options;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;

using SkyApm.AspNetCore.Diagnostics;
using SkyApm.Utilities.DependencyInjection;

namespace Air.Cloud.Modules.SkyWalking
{
    [AppStartup(AppName="SkyWalking的TraceLog实现")]
    public  class Startup : AppStartup
    {
        public override void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            Environment.SetEnvironmentVariable(SkyApmConst.ASPNETCORE_HOSTINGSTARTUPASSEMBLIES, SkyApmConst.ASPNETCORE_HOSTINGSTARTUPASSEMBLIES_VALUE);
            Environment.SetEnvironmentVariable(SkyApmConst.SKYWALKING__SERVICENAME, AppConst.ApplicationInstanceName);

        }

        public override void ConfigureServices(IServiceCollection services)
        {
            services.AddSkyWalkingService() ;
            services.AddSkyAPM(ext => ext.AddAspNetCoreHosting());
            services.AddSkyApmExtensions();
            services.AddTransient<ITraceLogStandard, TraceLogDependency>();

        }
    }
}
