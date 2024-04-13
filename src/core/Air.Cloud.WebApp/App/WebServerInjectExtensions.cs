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
using Air.Cloud.Core.App;
using Air.Cloud.Core.Enums;
using Air.Cloud.Core.Standard;
using Air.Cloud.WebApp.Extensions;

using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using System.Reflection;
using System.Runtime.Loader;

namespace Air.Cloud.WebApp.App
{
    public static class WebServerInjectExtensions
    {
        public static MethodInfo? AddMVCFilter = null;
        /// <summary>
        /// Web应用程序加载本地的配置文件
        /// </summary>
        /// <param name="builder"></param>
        /// <returns></returns>
        public static WebApplication WebInjectInFile(this WebApplicationBuilder builder)
        {
            var Configuration = AppStandardRealization.Inject.LoadConfiguration(AppConst.SystemEnvironmentConfigFileFullName, false);
            var CommonConfiguration = AppStandardRealization.Inject.LoadConfiguration(AppConst.CommonEnvironmentConfigFileFullName, true);
            AppConst.LoadConfigurationTypeEnum = LoadConfigurationTypeEnum.File;
            AppConst.ApplicationName = Assembly.GetCallingAssembly().GetName().Name;
            builder.Configuration.AddConfiguration(Configuration).AddConfiguration(CommonConfiguration);
            builder.Inject();
            var app = builder.Build();
            return app;
        }
    }
}
