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
using Air.Cloud.EntityFrameWork.Core.BackgroundServices;
using Air.Cloud.EntityFrameWork.Core.Configure;
using Air.Cloud.EntityFrameWork.Core.Options;
using Air.Cloud.EntityFrameWork.Oracle.Bulk;
using Air.Cloud.EntityFrameWork.Oracle.Configure;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace Air.Cloud.EntityFrameWork.Oracle
{
    public class Startup : AppStartup
    {
        public override void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {


        }

        public override void ConfigureServices(IServiceCollection services)
        {
            services.AddOptions<DataSourceOptions>()
               .BindConfiguration("DataSourceSettings")
               .ValidateDataAnnotations()
               .PostConfigure(options =>
               { });
            services.AddSingleton<IDatabaseConfigure, OracleDatabaseConfigure>();
            var Options = AppCore.GetOptions<DataSourceOptions>();
            if (Options.EnableDatabaseStatusCheck())
                services.AddHostedService<DatabaseStatusCheckBackgroundService>();
            services.AddBatchEF_Oracle();
        }
    }
}
