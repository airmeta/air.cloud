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
using Air.Cloud.Core;
using Air.Cloud.Core.App;
using Air.Cloud.Core.Enums;
using Air.Cloud.Core.Standard.AppInject;
using Air.Cloud.HostApp.Dependency;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

using System.Reflection;

namespace Air.Cloud.HostApp.Extensions
{
    public static class HostAppInjectExtensions
    {
        /// <summary>
        /// Host控制台应用程序加载本地的配置文件
        /// </summary>
        /// <returns></returns>
        public static IHostBuilder HostInjectInFile(this IHostBuilder builder)
        {
            //加载远程配置文件
            AppRealization.Configuration.LoadConfiguration(AppConst.SystemEnvironmentConfigFileFullName, false);
            AppRealization.Configuration.LoadConfiguration(AppConst.CommonEnvironmentConfigFileFullName, true);
            AppConst.LoadConfigurationTypeEnum = LoadConfigurationTypeEnum.File;
            AppConst.ApplicationName = Assembly.GetCallingAssembly().GetName().Name;
            AppConst.ApplicationInstanceName = $"{AppConst.ApplicationName}_{AppRealization.PID.Get()}";
            AppCore.AppStartType = AppStartupTypeEnum.HOST;
            builder = builder.ConfigureAppConfiguration(a =>
            {
                a.AddConfiguration(AppConfigurationLoader.Configurations);
            });
            AppRealization.SetDependency<IAppInjectStandard>(new HostAppInjectDependency());
            builder = AppRealization.Injection.Inject(builder,true);
            return builder;
        }

    }
}
