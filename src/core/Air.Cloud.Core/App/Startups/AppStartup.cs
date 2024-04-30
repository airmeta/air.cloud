// Copyright (c) 2020-2022 百小僧, Baiqian Co.,Ltd.
// Furion is licensed under Mulan PSL v2.
// You can use this software according to the terms and conditions of the Mulan PSL v2.
// You may obtain a copy of Mulan PSL v2 at:
//             https://gitee.com/dotnetchina/Furion/blob/master/LICENSE
// THIS SOFTWARE IS PROVIDED ON AN "AS IS" BASIS, WITHOUT WARRANTIES OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO NON-INFRINGEMENT, MERCHANTABILITY OR FIT FOR A PARTICULAR PURPOSE.
// See the Mulan PSL v2 for more details.
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