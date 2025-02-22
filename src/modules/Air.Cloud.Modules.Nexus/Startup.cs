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
using Air.Cloud.Modules.Nexus.Handlers;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace Air.Cloud.Modules.Nexus;

/// <summary>
/// 事件总线 行为
/// </summary>
public class Startup : AppStartup
{
    /// <summary>
    /// 配置后台服务
    /// </summary>
    /// <param name="services"></param>
    public override void ConfigureServices(IServiceCollection services)
    {
        // 通过工厂模式创建
        services.AddHostedService<EventBusHostedService>();
    }

    public override void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
    }
}