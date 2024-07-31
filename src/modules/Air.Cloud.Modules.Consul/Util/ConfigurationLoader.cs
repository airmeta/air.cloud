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
using Air.Cloud.Modules.Consul.Resolver;

using Microsoft.Extensions.Configuration;

using System.Reflection;

using Winton.Extensions.Configuration.Consul;

namespace Air.Cloud.Modules.Consul.Util
{
    /// <summary>
    ///配置文件加载器
    /// </summary>
    public class ConfigurationLoader : IConfigurationLoader
    {
        public Assembly CallAssembly = null;
        /// <summary>
        /// 配置文件加载器
        /// </summary>
        /// <param name="assembly">启动项目程序集</param>
        public ConfigurationLoader(Assembly assembly = null)
        {
            CallAssembly = assembly;
        }
        public (IConfiguration, IConfiguration) LoadRemoteConfiguration(string RemoteUrl = null, string key = null, string FileName = null)
        {
            if (CallAssembly == null) CallAssembly = Assembly.GetCallingAssembly();
            //是否启用公共配置文件 默认启用
            bool EnableCommonConfig = true;
            //公共配置文件路由地址
            string CommonConfigFileRoute = null;
            if (string.IsNullOrEmpty(RemoteUrl))
            {
                var ServiceOptions = AppConfigurationLoader.InnerConfiguration.GetConfig<ConsulServiceOptions>();
                if (ServiceOptions == null) throw new FileNotFoundException("未找到配置文件appsettings.json或配置文件中不包含服务注册必须项");
                RemoteUrl = ServiceOptions.ConsulAddress;
                EnableCommonConfig = ServiceOptions.EnableCommonConfig;
                CommonConfigFileRoute = ServiceOptions.CommonConfigFileRoute;
            }
            //配置中心文件夹路径
            string ConfigurationInConsulKVPath = string.IsNullOrEmpty(key) ? ProjectDataResolver.GetCurrentProjectConsulServiceNameInReslover(key, CallAssembly).Replace(".Test", "").Replace(".", "/") : key.Replace(".", "/");
            //配置中心文件名称
            string SystemApplicationEnvoriment = string.IsNullOrEmpty(FileName) ? AppConst.SystemEnvironmentConfigFileFullName : FileName;
            //加载配置文件 
            var config = new ConfigurationBuilder().AddConsul($"{ConfigurationInConsulKVPath}/{SystemApplicationEnvoriment}",
                        options =>
                        {
                            options.Optional = true;
                            options.ReloadOnChange = true;
                            options.ConsulConfigurationOptions = cco => { cco.Address = new Uri(RemoteUrl); };
                        }).Build();
            //不加载公共配置文件 直接返回
            if (!EnableCommonConfig) return (config, null);

            //加载公共配置文件
            string CommonConfigurationInConsulKVPath = $"{CommonConfigFileRoute}/{AppConst.CommonEnvironmentConfigFileFullName}";
            var CommonConfig = new ConfigurationBuilder().AddConsul($"{CommonConfigurationInConsulKVPath}",
                                       options =>
                                       {
                                           options.Optional = true;
                                           options.ReloadOnChange = true;
                                           options.ConsulConfigurationOptions = cco => { cco.Address = new Uri(RemoteUrl); };
                                       }).Build();
            return (config, CommonConfig);
        }
    }
}
