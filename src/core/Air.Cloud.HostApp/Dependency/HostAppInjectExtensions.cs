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
using Air.Cloud.Core.Enums;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

using System.Reflection;
using Air.Cloud.Core.Extensions;
namespace Air.Cloud.HostApp.Dependency
{
    public static class HostAppInjectExtensions
    {
        /// <summary>
        /// <para>zh-cn: Host控制台应用程序加载本地的配置文件</para>
        /// <para>en-us: Host console application loads local configuration files</para>
        /// </summary>
        /// <param name="builder">
        ///    <para>zh-cn:构建器</para>
        ///    <para>en-us:HostBuilder</para>
        /// </param>
        /// <returns></returns>
        public static IHostBuilder HostInjectInFile(this IHostBuilder builder,Assembly assembly=null)
        {
            AppConfigurationLoader.Configurations=AppConfiguration.AppDefaultInjectConfiguration<HostAppInjectDependency>(
                    AppStartupTypeEnum.HOST,
                    LoadConfigurationTypeEnum.File,
                    assembly);
            builder = builder.ConfigureAppConfiguration(a =>
            {
                a.AddConfiguration(AppConfigurationLoader.Configurations);
            });
            builder.ConfigureLogging((log) =>
            {
                log.AddCustomConsole();
            });
            builder = AppRealization.Injection.Inject(builder, true);
            return builder;
        }
    }
}
