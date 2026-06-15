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
using Air.Cloud.Core.Standard.KVCenter;
using Air.Cloud.Core.Standard.ServerCenter;
using Air.Cloud.Modules.Nacos.Extensions;
using Air.Cloud.Modules.Nacos.Service;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace Air.Cloud.Modules.Nacos
{
    /// <summary>
    /// <para>zh-cn:Nacos 模块启动配置，负责注册服务中心、键值中心以及 Nacos SDK 客户端。</para>
    /// <para>en-us>Nacos module startup configuration that registers server-center, key-value center, and Nacos SDK clients.</para>
    /// </summary>
    [AppStartupAttribute(AppName = "Air.Cloud.Core", Order = int.MinValue)]
    public class Startup : AppStartup
    {
        /// <summary>
        /// <para>zh-cn:配置 Nacos 模块的应用管道；当前模块无额外中间件顺序要求。</para>
        /// <para>en-us>Configures the application pipeline for the Nacos module; no extra middleware ordering is required.</para>
        /// </summary>
        /// <param name="app">
        /// <para>zh-cn:应用程序构建器。</para>
        /// <para>en-us>The application builder.</para>
        /// </param>
        /// <param name="env">
        /// <para>zh-cn:Web 主机环境。</para>
        /// <para>en-us>The web host environment.</para>
        /// </param>
        public override void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
        }

        /// <summary>
        /// <para>zh-cn:注册 Nacos 服务中心、键值中心和官方 SDK 客户端；当缺少 Nacos 地址时仅保留标准实现类型。</para>
        /// <para>en-us>Registers Nacos server-center, key-value center, and official SDK clients; when server addresses are missing, only standard implementation types are kept.</para>
        /// </summary>
        /// <param name="services">
        /// <para>zh-cn:服务集合。</para>
        /// <para>en-us>The service collection.</para>
        /// </param>
        public override void ConfigureServices(IServiceCollection services)
        {
            try
            {
                services.AddNacosModule(AppConfigurationLoader.InnerConfiguration);
            }
            catch (ArgumentException)
            {
                services.AddTransient<IServerCenterStandard, NacosServerCenterDependency>();
                services.AddTransient<IKVCenterStandard, NacosKVCenterDependency>();
            }
        }
    }
}
