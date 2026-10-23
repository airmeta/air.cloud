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
using Air.Cloud.EntityFrameWork.Core.BackgroundServices;
using Air.Cloud.EntityFrameWork.Core.Configure;
using Air.Cloud.EntityFrameWork.Core.Extensions.DatabaseProvider;
using Air.Cloud.EntityFrameWork.Core.Options;
using Air.Cloud.EntityFrameWork.PostgreSQL.Configure;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace Air.Cloud.EntityFrameWork.PostgreSQL
{
    /// <summary>
    /// <para>zh-cn:PostgreSQL 模块启动类，负责配置 PostgreSQL 数据库相关服务和中间件。</para>
    /// <para>en-us:PostgreSQL module startup class responsible for configuring PostgreSQL database-related services and middleware.</para>
    /// </summary>
    public class Startup : AppStartup
    {
        
        /// <summary><para>zh-cn:配置 PostgreSQL 模块的应用中间件。</para><para>en-us:Configures application middleware for the PostgreSQL module.</para></summary>
        /// <param name="app"><para>zh-cn:应用构建器。</para><para>en-us:The application builder.</para></param>
        /// <param name="env"><para>zh-cn:当前主机环境。</para><para>en-us:The current hosting environment.</para></param>
        public override void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {


        }

        /// <summary><para>zh-cn:注册 PostgreSQL 数据库配置器和状态检查服务。</para><para>en-us:Registers the PostgreSQL database configurator and status-check service.</para></summary>
        /// <param name="services"><para>zh-cn:应用服务集合。</para><para>en-us:The application service collection.</para></param>
        public override void ConfigureServices(IServiceCollection services)
        {
            services.AddOptions<DataSourceOptions>()
               .BindConfiguration("DataSourceSettings")
               .ValidateDataAnnotations()
               .PostConfigure(options =>
               { });
            services.AddDatabaseConfigure<PostgreSQLDatabaseConfigure, PostgreSQLDataBaseProvider>();
            var Options = AppCore.GetOptions<DataSourceOptions>();
            if (Options.EnableDatabaseStatusCheck())
                services.AddHostedService<DatabaseStatusCheckBackgroundService>();
        }
    }
}
