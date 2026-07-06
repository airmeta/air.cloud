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
using Air.Cloud.Core;
using Air.Cloud.Core.App;
using Air.Cloud.Core.App.Startups;
using Air.Cloud.EntityFrameWork.Core.BackgroundServices;
using Air.Cloud.EntityFrameWork.Core.Configure;
using Air.Cloud.EntityFrameWork.Core.Options;
using Air.Cloud.EntityFrameWork.Kingbase.Configure;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace Air.Cloud.EntityFrameWork.Kingbase
{
    /// <summary>
    /// <para>zh-cn:Kingbase Entity Framework 模块启动配置，负责注册 Kingbase 数据库配置和状态检查服务。</para>
    /// <para>en-us:Kingbase Entity Framework module startup configuration that registers Kingbase database configuration and status check services.</para>
    /// </summary>
    public class Startup : AppStartup
    {
        /// <summary>
        /// <para>zh-cn:配置 Kingbase 模块的应用管道。当前模块只注册数据库配置服务，不向请求管道添加中间件。</para>
        /// <para>en-us:Configures the application pipeline for the Kingbase module. The current module only registers database configuration services and does not add middleware to the request pipeline.</para>
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
        /// <para>zh-cn:注册 Kingbase 数据源选项、IDatabaseConfigure 实现以及可选的数据库状态检查后台服务。状态检查仅在 DataSourceSettings.ConnectionValidationSQL 非空时启用。</para>
        /// <para>en-us:Registers Kingbase data source options, the IDatabaseConfigure implementation, and the optional database status check background service. The status check is enabled only when DataSourceSettings.ConnectionValidationSQL is not empty.</para>
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
            services.AddSingleton<IDatabaseConfigure, KingbaseDatabaseConfigure>();
            var Options = AppCore.GetOptions<DataSourceOptions>();
            try
            {

                if (Options.EnableDatabaseStatusCheck())
                    services.AddHostedService<DatabaseStatusCheckBackgroundService>();
            }
            catch (Exception ex)
            {
                AppRealization.Output.Print("状态检查任务出现异常",ex.Message,AdditionalParams:new Dictionary<string, object>()
                {
                    { "StackTrace",ex.StackTrace },
                    { "Source",ex.Source }
                });
            }
        }
    }
}
