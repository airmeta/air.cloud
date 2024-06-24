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
using Air.Cloud.Modules.SkyWalking.Const;
using Air.Cloud.Modules.SkyWalking.Options;

using Microsoft.Extensions.DependencyInjection;

namespace Air.Cloud.Modules.SkyWalking.Extensions
{
    public static  class SkyWalkingModuleExtensions
    {
        public static IServiceCollection AddSkyWalkingOptions(this IServiceCollection services)
        {
            string ConfigFilePath = $"{AppConst.ApplicationPath}{SkyApmConst.SKYWALKING_CONFIG_NAME}";
            var Options = AppConfiguration.GetConfig<SkyApmOptions>();
            Options.ServiceName = AppConst.ApplicationInstanceName;
            if (File.Exists(ConfigFilePath)) File.Delete(ConfigFilePath);
            File.WriteAllText(ConfigFilePath, AppRealization.JSON.Serialize(new
            {
                SkyWalking=Options
            }));
            return services;
        }
    }
}
