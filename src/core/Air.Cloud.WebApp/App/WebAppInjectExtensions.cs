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

using Microsoft.Extensions.Configuration;

using System.Reflection;

namespace Air.Cloud.WebApp.App
{
    public static class WebAppInjectExtensions
    {
        /// <summary>
        /// Web应用程序加载本地的配置文件
        /// </summary>
        /// <param name="builder"></param>
        /// <returns></returns>
        public static WebApplication WebInjectInFile(this WebApplicationBuilder builder)
        {
            var Configuration = AppRealization.Configuration.LoadConfiguration(AppConst.SystemEnvironmentConfigFileFullName, false);
            var CommonConfiguration = AppRealization.Configuration.LoadConfiguration(AppConst.CommonEnvironmentConfigFileFullName, true);
            AppConst.LoadConfigurationTypeEnum = LoadConfigurationTypeEnum.File;
            AppConst.ApplicationName = Assembly.GetCallingAssembly().GetName().Name;
            AppCore.AppStartType = AppStartupTypeEnum.HOST;
            builder.Configuration.AddConfiguration(Configuration).AddConfiguration(CommonConfiguration);
            builder = AppRealization.Injection.Inject(builder);
            var app = builder.Build();
            return app;
        }


    }
}
