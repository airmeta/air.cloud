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
using Air.Cloud.Modules.Akka.Extensions;
using Air.Cloud.Modules.Akka.Options;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace Air.Cloud.Modules.Akka;

/// <summary>
/// <para>zh-cn:Akka.Cluster 模块启动入口，负责把配置绑定和运行时服务注册接入 Air.Cloud 应用启动流程。</para>
/// <para>en-us:Akka.Cluster module startup entry that connects configuration binding and runtime service registration to the Air.Cloud application startup flow.</para>
/// </summary>
public class Startup : AppStartup
{
    /// <summary>
    /// <para>zh-cn:注册 Akka.Cluster 运行时服务，并从 `AkkaSettings` 配置节绑定模块选项；ActorSystem 的启动和停止由托管服务接管。</para>
    /// <para>en-us:Registers the Akka.Cluster module runtime services, binds the `AkkaSettings` configuration section, and lets the hosted service manage the ActorSystem lifecycle.</para>
    /// </summary>
    /// <param name="services">
    /// <para>zh-cn:当前应用的服务集合。</para>
    /// <para>en-us:The current application service collection.</para>
    /// </param>
    public override void ConfigureServices(IServiceCollection services)
    {
        services.AddOptions<AkkaSettingsOptions>()
            .BindConfiguration("AkkaSettings");

        services.AddAkkaCluster();
    }

    /// <summary>
    /// <para>zh-cn:配置 Akka 模块的 Web 管道；当前集群运行时不需要中间件，因此该方法保持为空。</para>
    /// <para>en-us:Configures the Akka module web pipeline; the current Cluster runtime does not require middleware, so this method intentionally stays empty.</para>
    /// </summary>
    /// <param name="app">
    /// <para>zh-cn:应用程序管道构建器。</para>
    /// <para>en-us:The application builder.</para>
    /// </param>
    /// <param name="env">
    /// <para>zh-cn:当前 Web 宿主环境。</para>
    /// <para>en-us:The web hosting environment.</para>
    /// </param>
    public override void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
    }
}
