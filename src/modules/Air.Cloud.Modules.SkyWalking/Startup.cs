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
using Air.Cloud.Core.Attributes;
using Air.Cloud.Core.Standard.TraceLog;
using Air.Cloud.Modules.SkyWalking.Const;
using Air.Cloud.Modules.SkyWalking.Dependency;
using Air.Cloud.Modules.SkyWalking.Extensions;
using Air.Cloud.Modules.SkyWalking.Options;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;

using SkyApm.AspNetCore.Diagnostics;
using SkyApm.Utilities.DependencyInjection;

namespace Air.Cloud.Modules.SkyWalking
{
    /// <summary>
    /// <para>zh-cn:SkyWalking 模块启动配置，负责注册 SkyWalking APM、扩展服务和链路日志实现。</para>
    /// <para>en-us:SkyWalking module startup configuration that registers SkyWalking APM, extension services, and trace log implementation.</para>
    /// </summary>
    [AppStartup(AppName="SkyWalking的TraceLog实现")]
    public  class Startup : AppStartup
    {
        /// <summary>
        /// <para>zh-cn:配置 SkyWalking 运行所需的环境变量。</para>
        /// <para>en-us:Configures environment variables required by SkyWalking runtime.</para>
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
            Environment.SetEnvironmentVariable(SkyApmConst.ASPNETCORE_HOSTINGSTARTUPASSEMBLIES, SkyApmConst.ASPNETCORE_HOSTINGSTARTUPASSEMBLIES_VALUE);
            Environment.SetEnvironmentVariable(SkyApmConst.SKYWALKING__SERVICENAME, AppConst.ApplicationInstanceName);

        }

        /// <summary>
        /// <para>zh-cn:注册 SkyWalking 采集、ASP.NET Core 诊断和链路日志服务。</para>
        /// <para>en-us:Registers SkyWalking tracing, ASP.NET Core diagnostics, and trace log services.</para>
        /// </summary>
        /// <param name="services">
        /// <para>zh-cn:服务集合。</para>
        /// <para>en-us:The service collection.</para>
        /// </param>
        public override void ConfigureServices(IServiceCollection services)
        {
            services.AddSkyWalkingService() ;
            services.AddSkyAPM(ext => ext.AddAspNetCoreHosting());
            services.AddSkyApmExtensions();
            services.AddTransient<ITraceLogStandard, TraceLogDependency>();

        }
    }
}
