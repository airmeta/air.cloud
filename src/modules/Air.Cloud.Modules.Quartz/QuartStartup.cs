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
using Air.Cloud.Core.App;
using Air.Cloud.Core.App.Startups;
using Air.Cloud.Core.Attributes;
using Air.Cloud.Modules.Quartz.Extensions;
using Air.Cloud.Modules.Quartz.Options;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace Air.Cloud.Modules.Quartz
{
    /// <summary>
    /// <para>zh-cn:启动项</para>
    /// <para>en-us:Startup</para>
    /// </summary>
    [AppStartup(Order =1100)]
    public class QuartStartup : AppStartup
    {
        /// <inheritdoc/>
        public override void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (EnableQuartzAutoInject()) {
                app.UseQuartzServices<QuartzSchedulerStandardOptions>();
            }
        }
        /// <inheritdoc/>
        public override void ConfigureServices(IServiceCollection services)
        {
            if (EnableQuartzAutoInject())
            {
                services.AddQuartzService<QuartzSchedulerStandardOptions>();
            }
        }
        /// <summary>
        ///  <para>zh-cn:是否启用Quartz自动注入</para>
        ///  <para>en-us:Whether to enable Quartz automatic injection</para>
        /// </summary>
        /// <returns></returns>
        public bool EnableQuartzAutoInject()
        {
            string EnableQuartzAutoInject = AppConfigurationLoader.InnerConfiguration["AppSettings:EnableQuartzAutoInject"]?.ToString();
            if (EnableQuartzAutoInject?.ToLower()=="false")
            {
                return false;
            }
            EnableQuartzAutoInject = AppCore.Configuration["AppSettings:EnableQuartzAutoInject"]?.ToString();
            if (EnableQuartzAutoInject?.ToLower() == "false")
            {
                return false;
            }
            return true;
        }

    }
}
