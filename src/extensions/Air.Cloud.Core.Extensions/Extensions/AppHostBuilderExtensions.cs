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
using Air.Cloud.Core.App.Loader;
using Air.Cloud.Core.Attributes;
using Air.Cloud.Core.Plugins.Reflection;

using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;

namespace Air.Cloud.Core.Extensions
{

    /// <summary>
    /// 主机构建器拓展类
    /// </summary>
    [IgnoreScanning]
    public static class AppHostBuilderExtensions
    {
        /// <summary>
        /// Web 主机注入
        /// </summary>
        /// <param name="hostBuilder">Web主机构建器</param>
        /// <param name="assemblyName">外部程序集名称</param>
        /// <returns>IWebHostBuilder</returns>
        public static IWebHostBuilder Inject(this IWebHostBuilder hostBuilder, string assemblyName = default)
        {
            // 获取默认程序集名称
            var defaultAssemblyName = assemblyName ?? Reflect.GetAssemblyName(typeof(AppHostBuilderExtensions));

            //  获取环境变量 ASPNETCORE_HOSTINGSTARTUPASSEMBLIES 配置
            var environmentVariables = Environment.GetEnvironmentVariable("ASPNETCORE_HOSTINGSTARTUPASSEMBLIES");
            var combineAssembliesName = $"{defaultAssemblyName};{environmentVariables}".ClearStringAffixes(1, ";");

            hostBuilder.UseSetting(WebHostDefaults.HostingStartupAssembliesKey, combineAssembliesName);

            // 实现假的 Starup，解决泛型主机启动问题
            hostBuilder.UseStartup<InternalStartup>();
            return hostBuilder;
        }

        /// <summary>
        /// 泛型主机注入
        /// </summary>
        /// <param name="hostBuilder">泛型主机注入构建器</param>
        /// <param name="autoRegisterBackgroundService">是否自动注册 BackgroundService</param>
        /// <returns>IWebHostBuilder</returns>
        public static IHostBuilder Inject(this IHostBuilder hostBuilder, bool autoRegisterBackgroundService = true)
        {
             //AppCore.ConfigureApplication(hostBuilder, autoRegisterBackgroundService);
            return hostBuilder;
        }
    }
}
