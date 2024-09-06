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
using Air.Cloud.Modules.Consul.Model;
using Air.Cloud.Modules.Consul.Util;

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using Ocelot.DependencyInjection;

namespace Air.Cloud.Modules.Ocelot.Extensions
{
    public static  class OcelotExtensions
    {
        /// <summary>
        /// <para>zh-cn:加载网关配置文件</para>
        /// <para>en-us:Load Ocelot configuration</para>
        /// </summary>
        /// <returns>
        /// <para>zh-cn:配置文件</para>
        /// <para>en-us:Configuration</para>
        /// </returns>
        public static ConfigurationManager AddGateWayConfiguration(this WebApplicationBuilder builder)
        {
            #region 加载配置信息
            var options = AppConfigurationLoader.InnerConfiguration.GetConfig<ConsulServiceOptions>();
            var Config = ConfigurationLoader.LoadRemoteConfiguration(options);
            ConfigurationManager configurationManager = new ConfigurationManager();
            configurationManager.AddConfiguration(Config.Item1);
            configurationManager.AddConfiguration(Config.Item2);
            #endregion
            //注入网关配置文件
            builder.Services.AddOcelot(configurationManager);
            return configurationManager;
        }
    }
}
