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
using Air.Cloud.Core.Extensions;
using Air.Cloud.Core.Plugins.Banner;
using Air.Cloud.Core.Standard.AppInject;
using Air.Cloud.HostApp.Event;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Primitives;

namespace Air.Cloud.HostApp.Dependency
{
    public class HostAppInjectDependency : IAppInjectStandard
    {
        public T Inject<T>(T hostBuilder, params object[] objects) where T : class
        {
            var bannerPrint = AppRealization.AppPlugin.GetPlugin<IAppBannerPlugin>();
            bannerPrint.PrintOrganizationName();
            bool? AuthRegisterBackgroundService = objects.First()?.ToString()?.ToLower() == "true";
            if (hostBuilder is IHostBuilder)
            {
                ConfigureHostApplication((IHostBuilder)hostBuilder, AuthRegisterBackgroundService ?? true);
            }
            else
            {
                throw new ArgumentException("执行HostApp的Inject方法时出现异常");
            }
            return hostBuilder;
        }

        /// <summary>
        /// 加载配置文件
        /// </summary>
        /// <returns></returns>
        private static IHostBuilder ConfigureHostAppConfiguration(IHostBuilder builder)
        {
            IConfiguration config = null;
            IChangeToken token = null;
            Action action = new Action(() =>
            {
                config = new ConfigurationBuilder().SetBasePath(AppConst.ApplicationPath)
                                                        .AddJsonFile(AppConst.DEFAULT_CONFIG_FILE, optional: true, reloadOnChange: true)
                                                        .Build();
                AppConfigurationLoader.InnerConfiguration = config;
                builder.ConfigureHostConfiguration((s) =>
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
        /// 配置Host主机应用程序
        /// </summary>
        /// <param name="builder">主机</param>
        /// <param name="autoRegisterBackgroundService">是否注册后台运行任务</param>
        private static void ConfigureHostApplication(IHostBuilder builder, bool autoRegisterBackgroundService = true)
        {
            // 自动装载配置
            ConfigureHostAppConfiguration(builder);
            // 监听全局异常
            AppDomain.CurrentDomain.UnhandledException += AppRealization.DomainExceptionHandler.OnException;

            // 自动注入 AddApplication() 服务
            builder.ConfigureServices((hostContext, services) =>
            {
                // 存储配置对象
                AppConfigurationLoader.InnerConfiguration = hostContext.Configuration;

                // 存储服务提供器
                AppCore.InternalServices = services;

                // 存储根服务
                services.AddHostedService<GenericHostLifetimeEventsHostedService>();
                builder.ConfigureLogging((log) =>
                {
                    log.AddCustomConsole();
                });
                // 初始化应用服务
                services.AddApplication();

                // 自动注册 BackgroundService
                if (autoRegisterBackgroundService) services.AddAppHostedService();
            });
        }
    }
}
