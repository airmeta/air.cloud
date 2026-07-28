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
using air.cloud.security.common.Auths;

using Air.Cloud.Core.App.Startups;
using Air.Cloud.EntityFrameWork.Core.Filters;
using Air.Cloud.Modules.Taxin.Client;
using Air.Cloud.Modules.Taxin.Extensions;
using Air.Cloud.Modules.Taxin.Store;
using Air.Cloud.WebApp.Extensions;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace air.cloud.account.entry
{
    /// <summary>
    /// <para>zh-cn:账户服务启动配置。账户服务拥有业务数据库，因此在本服务的 MVC 管道中注册自动保存筛选器。</para>
    /// <para>en-us:Account service startup configuration. The account service owns business database contexts, so it registers the automatic-save filter in this service's MVC pipeline.</para>
    /// </summary>
    public class Startup : AppStartup
    {
        /// <summary>
        /// <para>zh-cn:配置账户服务的应用管道。账户服务的公共中间件由宿主启动流程注册，因此此服务启动类无需追加中间件。</para>
        /// <para>en-us:Configures the account service application pipeline. The host startup flow registers shared middleware, so this service startup class adds none.</para>
        /// </summary>
        /// <param name="app">
        /// <para>zh-cn:应用程序管道构建器。</para>
        /// <para>en-us:The application pipeline builder.</para>
        /// </param>
        /// <param name="env">
        /// <para>zh-cn:当前 Web 宿主环境。</para>
        /// <para>en-us:The current web host environment.</para>
        /// </param>
        public override void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
        }

        /// <summary>
        /// <para>zh-cn:注册账户服务专用的 MVC 筛选器。数据库访问器由账户依赖的仓储启动模块注册；筛选器不注册到公共安全模块，避免无数据库的认证服务解析数据库池。</para>
        /// <para>en-us:Registers MVC filters specific to the account service. The database accessor is registered by the repository startup module used by Account; the filter is not registered in the shared security module so database-free auth services do not resolve a database pool.</para>
        /// </summary>
        /// <param name="services">
        /// <para>zh-cn:当前应用的服务集合。</para>
        /// <para>en-us:The service collection for the current application.</para>
        /// </param>
        public override void ConfigureServices(IServiceCollection services)
        {
            services.AddScoped<IUserAccountStore, UserAccountStore>();
            services.AddSkyMirrorShieldClient();
            services.AddTaxinClient<TaxinClientDependency, TaxinStoreDependency>();
            services.AddControllers(options => options.Filters.Add<AutoSaveChangesFilter>())
                .AddWebAppUnifyResult()
                .AddNewtonsoftJson(options =>
                {
                    options.SerializerSettings.Converters.Add(new IsoDateTimeConverter
                    {
                        DateTimeFormat = "yyyy-MM-dd HH:mm:ss"
                    });
                    options.SerializerSettings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
                });
        }
    }
}
