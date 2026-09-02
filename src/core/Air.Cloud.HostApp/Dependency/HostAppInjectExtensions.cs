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
            ConfigurationLoadSummary.Print("本地文件", AppConst.ApplicationPath,
                $"{Path.Combine(AppConst.ApplicationPath, AppConst.SystemEnvironmentConfigFileFullName)}; {Path.Combine(AppConst.ApplicationPath, AppConst.CommonEnvironmentConfigFileFullName)}",
                AppConfigurationLoader.Configurations);

            builder = builder.ConfigureAppConfiguration(a =>
            {
                // 不将 ConfigurationManager 作为单一 ChainedConfigurationSource 再次挂载。
                // 复制其底层配置源，避免聚合管理器互相监听造成变更令牌递归。
                // Copy the underlying sources instead of chaining the aggregate
                // ConfigurationManager itself, preventing recursive reload-token wiring.
                foreach (var source in AppConfigurationLoader.Configurations.Sources)
                {
                    a.Sources.Add(source);
                }
            });
            builder = AppRealization.Injection.Inject(builder, true);
            return builder;
        }
    }
}
