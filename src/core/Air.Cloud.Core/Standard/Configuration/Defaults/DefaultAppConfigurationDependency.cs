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
using Air.Cloud.Core.Aspects;
using Air.Cloud.Core.Modules.AppAspect.Attributes;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Primitives;

namespace Air.Cloud.Core.Standard.Configuration.Defaults
{
    /// <summary>
    /// <para>zh-cn:提供默认应用配置加载实现，支持加载公共配置或外部配置并注册配置文件变更回调。</para>
    /// <para>en-us:Provides the default application configuration loading implementation, supporting public or external configuration loading and configuration-file change callbacks.</para>
    /// </summary>
    [IgnoreScanning]
    public class DefaultAppConfigurationDependency : IAppConfigurationStandard
    {
        /// <summary>
        /// <para>zh-cn:从应用程序目录加载指定 JSON 配置文件。</para>
        /// <para>en-us:Loads the specified JSON configuration file from the application directory.</para>
        /// </summary>
        /// <param name="ConfigurationName"><para>zh-cn:配置文件名称。</para><para>en-us:The configuration file name.</para></param>
        /// <param name="IsCommonConfiguration"><para>zh-cn:是否作为公共配置加载；否则作为外部配置加载。</para><para>en-us:Whether to load as public configuration; otherwise it is loaded as external configuration.</para></param>
        /// <returns><para>zh-cn:加载后的配置对象。</para><para>en-us:The loaded configuration object.</para></returns>
        [Aspect(typeof(IfHttpRequestExceptionHandler))]
        public IConfiguration LoadConfiguration(string ConfigurationName, bool IsCommonConfiguration)
        {
            IConfiguration config = null;
            IChangeToken token = null;
            Action action = new Action(() =>
            {
                config = new ConfigurationBuilder().SetBasePath(AppConst.ApplicationPath)
                                                          .AddJsonFile(ConfigurationName, optional: true, reloadOnChange: true)
                                                          .Build();
                if (IsCommonConfiguration) token = AppConfigurationLoader.SetPublicConfiguration(config);
                else token = AppConfigurationLoader.SetExternalConfiguration(config);
            });
            action();
            if (config == null) throw new Exception("加载外部配置文件" + ConfigurationName + "失败");
            if (token != null)
            {
                //监视文件变更
                token.RegisterChangeCallback((state) =>
                {
                    action();
                }, config);
            }
            return config;
        }
    }
}
