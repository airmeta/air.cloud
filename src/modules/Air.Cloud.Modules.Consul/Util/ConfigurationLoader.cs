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
using Air.Cloud.Core.App;
using Air.Cloud.Modules.Consul.Model;

using Microsoft.Extensions.Configuration;

using Winton.Extensions.Configuration.Consul;

namespace Air.Cloud.Modules.Consul.Util
{
    /// <summary>
    ///配置文件加载器
    /// </summary>
    public static class ConfigurationLoader 
    {
        /// <summary>
        /// <para>zh-cn:加载配置文件</para>
        /// <para>en-us:load remote configuration</para>
        /// </summary>
        /// <param name="ServiceOptions">
        /// <para>zh-cn:Consul 服务配置；当 `EnableConfigurationCenter` 为 false 时不会访问 Consul，只返回空配置。</para>
        /// <para>en-us:The Consul service options; when `EnableConfigurationCenter` is false, Consul is not accessed and an empty configuration is returned.</para>
        /// </param>
        /// <returns>
        /// <para>zh-cn:item1 为当前服务配置，item2 为公共配置；未启用公共配置或配置中心时 item2 为 null。</para>
        /// <para>en-us:item1 is the current service configuration, and item2 is the common configuration; item2 is null when common configuration or the configuration center is disabled.</para>
        /// </returns>
        public static (IConfiguration, IConfiguration) LoadRemoteConfiguration(ConsulServiceOptions ServiceOptions)
        {
            if (!ServiceOptions.EnableConfigurationCenter)
            {
                return (new ConfigurationBuilder().Build(), null);
            }

            //加载配置文件 
            var config = new ConfigurationBuilder().AddConsul($"{ServiceOptions.ConfigurationServiceName.Replace(".", "/")}/{AppConst.SystemEnvironmentConfigFileFullName}",
                        options =>
                        {
                            options.Optional = true;
                            options.ReloadOnChange = true;
                            options.ConsulConfigurationOptions = cco => { cco.Address = new Uri(ServiceOptions.ConsulAddress); };
                        }).Build();
            //不加载公共配置文件 直接返回
            if (!ServiceOptions.EnableCommonConfig) return (config, null);

            //加载公共配置文件
            string CommonConfigurationInConsulKVPath = $"{ServiceOptions.CommonConfigFileRoute}/{AppConst.CommonEnvironmentConfigFileFullName}";
            var CommonConfig = new ConfigurationBuilder().AddConsul($"{CommonConfigurationInConsulKVPath}",
                                       options =>
                                       {
                                           options.Optional = true;
                                           options.ReloadOnChange = true;
                                           options.ConsulConfigurationOptions = cco => { cco.Address = new Uri(ServiceOptions.ConsulAddress); };
                                       }).Build();
            return (config, CommonConfig);
        }
    }
}
