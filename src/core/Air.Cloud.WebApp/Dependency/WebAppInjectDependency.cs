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
using Air.Cloud.Core.App.Filters;
using Air.Cloud.Core.Extensions;
using Air.Cloud.Core.Plugins.Banner;
using Air.Cloud.Core.Standard.AppInject;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Primitives;

namespace Air.Cloud.WebApp.Dependency
{
    public  class WebAppInjectDependency : IAppInjectStandard
    {
        public T Inject<T>(T t, params object[] parameter) where T : class
        {
            var bannerPrint = AppRealization.AppPlugin.GetPlugin<IAppBannerPlugin>();
            bannerPrint.PrintOrganizationName();
            if (t is WebApplicationBuilder webApplicationBuilder)
            {
                ConfigureWebApplication(webApplicationBuilder.WebHost);
                return webApplicationBuilder as T ?? t;
            }
            throw new ArgumentException("执行WebApp的Inject方法时出现异常");
        }
        /// <summary>
        /// 加载配置文件
        /// </summary>
        /// <returns></returns>
        private  static IWebHostBuilder ConfigureWebAppConfiguration(IWebHostBuilder builder)
        {
            IConfiguration config = null;
            IChangeToken token = null;
            Action action = new Action(() =>
            {
                config = new ConfigurationBuilder().SetBasePath(AppConst.ApplicationPath)
                                                        .AddJsonFile(AppConst.DEFAULT_CONFIG_FILE, optional: true, reloadOnChange: true)
                                                        .Build();
                AppConfigurationLoader.InnerConfiguration = config;
                builder.ConfigureAppConfiguration(s =>
                {
                    s.AddConfiguration(config);
                });
            });
            action();
            token = config.GetReloadToken();
            token.RegisterChangeCallback(state =>
            {
                action();
            }, config);
            if (config == null) throw new Exception("加载内部配置文件" + AppConst.DEFAULT_CONFIG_FILE + "失败");
            return builder;
        }
        /// <summary>
        /// 配置WebApp
        /// </summary>
        /// <remarks>此次添加 <see cref="HostBuilder"/> 参数是为了兼容 .NET 5 直接升级到 .NET 6 问题</remarks>
        /// <param name="builder"></param>
        private static void ConfigureWebApplication(IWebHostBuilder builder)
        {
            // 自动装载配置
            ConfigureWebAppConfiguration(builder);
            // 监听全局异常
            AppDomain.CurrentDomain.UnhandledException += AppRealization.DomainExceptionHandler.OnException;

            // 应用初始化服务
            builder.ConfigureServices((hostContext, services) =>
            {
                // 存储配置对象
                AppConfigurationLoader.InnerConfiguration = hostContext.Configuration;

                // 存储服务提供器
                AppCore.InternalServices=services;
                // 注册 Startup 过滤器
                services.AddTransient<IStartupFilter, StartupFilter>();

                // 注册 HttpContextAccessor 服务
                services.AddHttpContextAccessor();
                builder.ConfigureLogging((log) =>
                {
                    log.AddCustomConsole();
                });
                // 初始化应用服务
                services.AddApplication();
            });

           
        }
    }
}
