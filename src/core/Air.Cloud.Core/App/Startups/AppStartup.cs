/*
 * Copyright (c) 2024 星曳数据
 *
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at https://mozilla.org/MPL/2.0/.
 *
 * This file is provided under the Mozilla Public License Version 2.0,
 * and the "NO WARRANTY" clause of the MPL is hereby expressly
 * acknowledged.
 */
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace Air.Cloud.Core.App.Startups;

/// <summary>
/// <para>zh-cn:应用启动配置模块</para>
/// <para>en-us:Application startup configuration module</para>
/// </summary>
[IgnoreScanning]
public abstract class AppStartup
{
    /// <summary>
    /// <para>zh-cn:配置服务集合</para>
    /// <para>en-us:Configure service collection</para>
    /// </summary>
    /// <param name="services">
    /// <para>zh-cn:服务集合</para>
    /// <para>en-us:Service collection</para>
    /// </param>
    public abstract void ConfigureServices(IServiceCollection services);
    /// <summary>
    /// <para>zh-cn:配置应用程序构建器</para>
    /// <para>en-us:Configure the application builder</para>
    /// </summary>
    /// <param name="app">
    /// <para>zh-cn:应用程序构建器</para>
    /// <para>en-us:Application builder</para>
    /// </param>
    /// <param name="env">
    /// <para>zh-cn:宿主环境,仅作为判断宿主机环境使用,实际环境获取请使用:AppEnvironment类</para>
    /// <para>en-us:Host environment, only used as a judgment host machine environment, actual environment acquisition please use: AppEnvironment class</para>
    /// </param>
    public abstract void Configure(IApplicationBuilder app, IWebHostEnvironment env);
}