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
using Air.Cloud.Modules.SkyWalking.Const;
using Air.Cloud.Modules.SkyWalking.Options;

using Microsoft.Extensions.DependencyInjection;

namespace Air.Cloud.Modules.SkyWalking.Extensions
{
    /// <summary>
    /// <para>zh-cn:提供 SkyWalking 模块服务注册与配置文件生成相关的扩展方法。</para>
    /// <para>en-us:Provides extension methods for registering SkyWalking module services and generating configuration files.</para>
    /// </summary>
    public static  class SkyWalkingModuleExtensions
    {
        /// <summary>
        /// <para>zh-cn:添加SkyWalking 中间件支持</para>
        /// <para>en-us:Adds SkyWalking middleware support.</para>
        /// </summary>
        /// <param name="services">
        /// <para>zh-cn:应用服务集合。</para>
        /// <para>en-us:The application service collection.</para>
        /// </param>
        /// <returns>
        /// <para>zh-cn:完成 SkyWalking 配置处理后的服务集合。</para>
        /// <para>en-us:The service collection after SkyWalking configuration processing.</para>
        /// </returns>
        public static IServiceCollection AddSkyWalkingService(this IServiceCollection services)
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
