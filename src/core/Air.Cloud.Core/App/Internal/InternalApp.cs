// Copyright (c) 2020-2022 百小僧, Baiqian Co.,Ltd.
// Furion is licensed under Mulan PSL v2.
// You can use this software according to the terms and conditions of the Mulan PSL v2.
// You may obtain a copy of Mulan PSL v2 at:
//             https://gitee.com/dotnetchina/Furion/blob/master/LICENSE
// THIS SOFTWARE IS PROVIDED ON AN "AS IS" BASIS, WITHOUT WARRANTIES OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO NON-INFRINGEMENT, MERCHANTABILITY OR FIT FOR A PARTICULAR PURPOSE.
// See the Mulan PSL v2 for more details.

using Air.Cloud.Core.App.Filters;
using Air.Cloud.Core.App.Startups;
using Air.Cloud.Core.Standard;

using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Air.Cloud.Core.App.Internal;

/// <summary>
/// 内部 App 副本
/// </summary>
internal static partial class InternalApp
{
    /// <summary>
    /// 内置应用服务
    /// </summary>
    internal static IServiceCollection InternalServices;

    /// <summary>
    /// 根服务
    /// </summary>
    internal static IServiceProvider RootServices;
    /// <summary>
    /// 配置对象
    /// </summary>
    internal static IConfiguration Configuration => AppConfigurationLoader.Configurations;
    /// <summary>
    /// 配置 Furion 框架（Web）
    /// </summary>
    /// <remarks>此次添加 <see cref="HostBuilder"/> 参数是为了兼容 .NET 5 直接升级到 .NET 6 问题</remarks>
    /// <param name="builder"></param>
    /// <param name="hostBuilder"></param>
    internal static void ConfigureWebApplication(IWebHostBuilder builder, IHostBuilder hostBuilder = default)
    {
        // 自动装载配置
        if (hostBuilder == default) ConfigureHostAppConfiguration(hostBuilder);
        else ConfigureWebAppConfiguration(builder);
        // 应用初始化服务
        builder.ConfigureServices((hostContext, services) =>
        {
            // 存储配置对象
            AppConfigurationLoader.InnerConfiguration = hostContext.Configuration;

            // 存储服务提供器
            InternalServices = services;
            // 注册 Startup 过滤器
            services.AddTransient<IStartupFilter, StartupFilter>();
           
            // 注册 HttpContextAccessor 服务
            services.AddHttpContextAccessor();

            // 初始化应用服务
            services.AddApplication();
        });
    }

    /// <summary>
    /// 配置Host主机应用程序
    /// </summary>
    /// <param name="builder">主机</param>
    /// <param name="autoRegisterBackgroundService">是否注册后台运行任务</param>
    internal static void ConfigureHostApplication(IHostBuilder builder, bool autoRegisterBackgroundService = true)
    {
        // 自动装载配置
        ConfigureHostAppConfiguration(builder);

        // 自动注入 AddApplication() 服务
        builder.ConfigureServices((hostContext, services) =>
        {
            // 存储配置对象
            AppConfigurationLoader.InnerConfiguration = hostContext.Configuration;

            // 存储服务提供器
            InternalServices = services;

            // 存储根服务
            services.AddHostedService<GenericHostLifetimeEventsHostedService>();

            // 初始化应用服务
            services.AddApplication();

            // 自动注册 BackgroundService
            if (autoRegisterBackgroundService) services.AddAppHostedService();
        });
    }

    /// <summary>
    /// 装载Host主机配置
    /// </summary>
    /// <param name="builder"></param>
    public static void ConfigureHostAppConfiguration(IHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((hostContext, configurationBuilder) =>
        {
        });
    }
    /// <summary>
    /// 装载Web主机配置
    /// </summary>
    /// <param name="builder"></param>
    public static void ConfigureWebAppConfiguration(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((hostContext, configurationBuilder) =>
        {

        });
    }
}