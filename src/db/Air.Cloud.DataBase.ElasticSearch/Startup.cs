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
using Air.Cloud.Core.Standard.DataBase.Options;
using Air.Cloud.DataBase.ElasticSearch.Extensions;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace Air.Cloud.DataBase.ElasticSearch
{
    /// <summary>
    /// <para>zh-cn:ElasticSearch 数据库模块启动配置，负责注册 ElasticSearch 选项和仓储服务。</para>
    /// <para>en-us:ElasticSearch database module startup configuration that registers ElasticSearch options and repository services.</para>
    /// </summary>
    public class Startup : AppStartup
    {
        /// <summary>
        /// <para>zh-cn:配置 ElasticSearch 模块的应用管道。</para>
        /// <para>en-us:Configures the application pipeline for the ElasticSearch module.</para>
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
        /// <para>zh-cn:注册 ElasticSearch 数据库配置项和仓储服务。</para>
        /// <para>en-us:Registers ElasticSearch database options and repository services.</para>
        /// </summary>
        /// <param name="services">
        /// <para>zh-cn:服务集合。</para>
        /// <para>en-us:The service collection.</para>
        /// </param>
        public override void ConfigureServices(IServiceCollection services)
        {
            services.AddOptions<DataBaseOptions>()
               .BindConfiguration("DataBaseSettings")
               .ValidateDataAnnotations()
               .PostConfigure(options =>
               { });
            services.AddElasticSearch();
        }
    }
}
