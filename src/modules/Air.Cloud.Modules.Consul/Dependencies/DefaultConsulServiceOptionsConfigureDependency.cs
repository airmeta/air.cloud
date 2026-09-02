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
using Air.Cloud.Core.Enums;
using Air.Cloud.Core.Extensions;
using Air.Cloud.Modules.Consul.Model;
using Air.Cloud.Modules.Consul.Standard;

using Microsoft.VisualBasic;

namespace Air.Cloud.Modules.Consul.Dependencies
{
    /// <summary>
        /// <para>zh-cn:Consul服务选项配置实现</para>
        /// <para>en-us:Consul service options configure dependency</para>
        /// </summary>
    internal class DefaultConsulServiceOptionsConfigureDependency : IConsulServiceOptionsConfigureStandard
    {
        /// <summary>
        /// <para>zh-cn:规整 Consul 服务配置，补齐配置中心名称和注册中心名称，并按功能开关校验 Consul 地址与服务地址。</para>
        /// <para>en-us:Normalizes Consul service options by filling configuration-center and registry service names, then validates Consul and service addresses according to feature switches.</para>
        /// </summary>
        /// <param name="options">
        /// <para>zh-cn:从本地配置读取到的 Consul 服务配置。</para>
        /// <para>en-us:The Consul service options read from local configuration.</para>
        /// </param>
        /// <param name="action">
        /// <para>zh-cn:调用方提供的补充配置委托，会在默认校验前执行。</para>
        /// <para>en-us:Caller-provided supplemental configuration delegate executed before default validation.</para>
        /// </param>
        /// <returns>
        /// <para>zh-cn:规整后的 Consul 服务配置。</para>
        /// <para>en-us:The normalized Consul service options.</para>
        /// </returns>
        public ConsulServiceOptions Configure(ConsulServiceOptions options,Action<ConsulServiceOptions> action = null)
        {
            action?.Invoke(options);
            if ((options.EnableConfigurationCenter || options.EnableServiceRegistration || options.EnableHealthCheck)
                && options.ConsulAddress.IsNullOrEmpty())
            {
                Console.WriteLine("你需要配置Consul服务运行IP地址");
                throw new Exception("你需要配置Consul服务运行IP地址");
            }
            if (options.ConfigurationServiceName.IsNullOrEmpty()) options.ConfigurationServiceName = GetDefaultServiceName(options.IsIgnoreServiceNameKey, options.IgnoreKey);
            if (options.RegistryServiceName.IsNullOrEmpty()) options.RegistryServiceName = GetDefaultServiceName(options.IsIgnoreServiceNameKey, options.IgnoreKey);
            if (options.EnableServiceRegistration && !options.DynamicAppManaged && options.ServiceAddress.IsNullOrEmpty())
            {
                Console.WriteLine("你需要配置当前服务运行IP地址");
                throw new Exception("你需要配置当前服务运行IP地址");
            }
            return options;
        }

        /// <summary>
        /// <para>zh-cn:获取默认服务名称</para>
        /// <para>en-us:Get default service name</para>
        /// </summary>
        /// <param name="IsIgnoreServiceNameKey">
        /// <para>zh-cn:是否忽略启动项关键字</para>
        /// <para>en-us:is Ignore entry key</para>
        /// </param>
        /// <param name="IgnoreKey">
        /// <para>zh-cn:忽略启动项关键字</para>
        /// <para>en-us:Ignore entry key</para>
        /// </param>
        /// <returns></returns>
        public static string GetDefaultServiceName(bool IsIgnoreServiceNameKey = true,string IgnoreKey=null)
        {
            string ProjectName = AppConst.ApplicationName;
            if (AppEnvironment.IsTest) ProjectName = $"{ProjectName}.{AppConst.ENVIRONMENT_TEST_KEY}";
            return (IsIgnoreServiceNameKey && (!IgnoreKey.IsNullOrEmpty()))? Strings.Replace(ProjectName, IgnoreKey, string.Empty, 1, -1, CompareMethod.Text) : ProjectName;
        }
    }
}
