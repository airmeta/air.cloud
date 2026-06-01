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
using Air.Cloud.EntityFrameWork.Oracle.Configure;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace Air.Cloud.EntityFrameWork.Oracle
{
    /// <summary>
    /// <para>zh-cn:Oracle Entity Framework 模块启动配置，负责注册 Oracle 数据库配置和状态检查服务。</para>
    /// <para>en-us:Oracle Entity Framework module startup configuration that registers Oracle database configuration and status check services.</para>
    /// </summary>
    public class Startup : AppStartup
    {
        /// <summary>
        /// <para>zh-cn:配置 Oracle 模块的应用管道。</para>
        /// <para>en-us:Configures the application pipeline for the Oracle module.</para>
        /// </summary>
        /// <param name="app">
        /// <para>zh-cn:应用程序构建器。</para>
        /// <para>en-us:The application builder.</para>
        /// </param>
        /// <param name="env">
        /// <para>zh-cn:Web 主机环境。</para>
        /// <para>en-us:The web host environment.</para>
        /// </param>
        public override void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {


        }

        /// <summary>
        /// <para>zh-cn:注册 Oracle 数据源选项、数据库配置实现以及可选的数据库状态检查后台服务。</para>
        /// <para>en-us:Registers Oracle data source options, database configuration implementation, and optional database status check background service.</para>
        /// </summary>
        /// <param name="services">
        /// <para>zh-cn:服务集合。</para>
        /// <para>en-us:The service collection.</para>
        /// </param>
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
        }
    }
}
