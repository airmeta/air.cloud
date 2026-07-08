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
using Air.Cloud.Core.Modules.AppAspect.Extensions;
using Air.Cloud.Core.Plugins;
using Air.Cloud.Core.Plugins.LogFiltering;
using Air.Cloud.Core.Plugins.Router;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Air.Cloud.Core
{
    /// <summary>
    /// <para>zh-cn:Air.Cloud.Core 核心启动模块，负责注册核心切面能力并挂载动态应用中间件。</para>
    /// <para>en-us:Air.Cloud.Core startup module, responsible for registering core aspect capabilities and mounting dynamic application middleware.</para>
    /// </summary>
    [AppStartup(AppName = "Air.Cloud.Core", Order = int.MinValue)]
    public  class Startup : AppStartup
    {
        /// <summary>
        /// <para>zh-cn:配置核心应用请求管道。</para>
        /// <para>en-us:Configures the core application request pipeline.</para>
        /// </summary>
        /// <param name="app">
        /// <para>zh-cn:应用构建器。</para>
        /// <para>en-us:The application builder.</para>
        /// </param>
        /// <param name="env">
        /// <para>zh-cn:Web 主机环境。</para>
        /// <para>en-us:The web host environment.</para>
        /// </param>
        public override void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.UseDynamicAppMiddleware();
            AppRealization.SetPlugin<IRouterMatcherPlugin>(new UniversalRouteMatcherCore());
        }

        /// <summary>
        /// <para>zh-cn:配置核心服务集合。</para>
        /// <para>en-us:Configures the core service collection.</para>
        /// </summary>
        /// <param name="services">
        /// <para>zh-cn:服务集合。</para>
        /// <para>en-us:The service collection.</para>
        /// </param>
        public override void ConfigureServices(IServiceCollection services)
        {
            services.AddAspect();
            services.AddOptions<AppLogFilterOptions>();
            services.TryAddSingleton<IAppLogFilterPlugin, DefaultAppLogFilterPlugin>();
        }
    }
}
