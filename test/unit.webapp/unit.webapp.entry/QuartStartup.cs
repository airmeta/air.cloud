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
using Air.Cloud.Core.Extensions.Aspect;
using Air.Cloud.Core.Modules.AppAspect.Attributes;
using Air.Cloud.Modules.Quartz.Extensions;
using Air.Cloud.Modules.Quartz.Options;

namespace unit.webapp.entry
{
    /// <summary>
    /// <para>zh-cn:启动项</para>
    /// <para>en-us:Startup</para>
    /// </summary>
    [AppStartup(Order = 3000)]
    public class QuartStartup : AppStartup
    {
            public static CancellationTokenSource cts = new CancellationTokenSource();
            public override void Configure(IApplicationBuilder app, IWebHostEnvironment env)
            {
                app.UseQuartzServices<QuartzSchedulerStandardOptions>();
            }
            public override void ConfigureServices(IServiceCollection services)
            {
                services.AddQuartzService<QuartzSchedulerStandardOptions>();
            }
        
    }
}
