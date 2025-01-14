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
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Primitives;

namespace Air.Cloud.Core.App
{
    /// <summary>
    /// <para>zh-cn:应用程序配置加载器</para>
    /// <para>en-us:Application configuration loader</para>
    /// </summary>
    public static partial class AppConfigurationLoader
    {
        /// <summary>
        /// <para>zh-cn:内部配置文件</para>
        /// <para>en-us:Internal configuration file</para>
        /// </summary>
        /// <remarks>
        /// <para>zh-cn:泛指appsettings.json文件</para>
        /// <para>en-us:Refers to the appsettings.json file</para>
        /// </remarks>
        public static IConfiguration InnerConfiguration;
        /// <summary>
        /// <para>zh-cn:外部加载的配置文件</para>
        /// <para>en-us:Externally loaded configuration file</para>
        /// </summary>
        /// <remarks>
        /// <para>zh-cn:泛指从外部加载过来的配置文件,例如:Consul等</para>
        /// <para>en-us:Refers to the configuration file loaded from the outside, such as: Consul, etc.</para>
        /// </remarks>
        public static ConfigurationManager Configurations;
        /// <summary>
        /// <para>zh-cn:应用程序配置修改令牌</para>
        /// <para>en-us:Application configuration modification token</para>
        /// </summary>
        public static IChangeToken ExternalConfigChangeToken;
        /// <summary>
        /// <para>zh-cn:公共配置修改令牌</para>
        /// <para>en-us:Public configuration modification token</para>
        /// </summary>
        public static IChangeToken CommonConfigChangeToken;
        /// <summary>
        /// <para>zh-cn:静态构造函数,加载默认配置文件appsettings.json</para>
        /// <para>en-us:Static constructor, load the default configuration file appsettings.json</para>
        /// </summary>
        static AppConfigurationLoader()
        {
            Action action = new Action(() =>
            {
                string path = AppDomain.CurrentDomain.SetupInformation.ApplicationBase;
                InnerConfiguration = new ConfigurationBuilder().SetBasePath(path)
                                                          .AddJsonFile(AppConst.DEFAULT_CONFIG_FILE, optional: true, reloadOnChange: true)
                                                          .Build();
            });
            action();
            //监视文件变更
            ChangeToken.OnChange(() => InnerConfiguration.GetReloadToken(), action);
            Configurations = new ConfigurationManager();
        }
        /// <summary>
        /// <para>zh-cn:设置系统配置文件信息</para>
        /// <para>en-us:Set system configuration file information</para>
        /// </summary>
        /// <param name="configuration">
        /// <para>zh-cn:配置</para>
        /// <para>en-us:Configuration</para>
        /// </param>
        public static IChangeToken SetExternalConfiguration(IConfiguration configuration)
        {
            Configurations.AddConfiguration(configuration);
            ExternalConfigChangeToken = configuration.GetReloadToken();
            return ExternalConfigChangeToken;
        }
        /// <summary>
        /// <para>zh-cn:设置公共配置文件信息</para>
        /// <para>en-us:Set public configuration file information</para>
        /// </summary>
        /// <param name="configuration">
        /// <para>zh-cn:配置</para>
        /// <para>en-us:Configuration</para>
        /// </param>
        /// <returns>
        /// <para>zh-cn:返回配置文件修改令牌</para>
        /// <para>en-us:Returns the configuration change token</para>
        /// </returns>
        public static IChangeToken SetPublicConfiguration(IConfiguration configuration)
        {
            Configurations.AddConfiguration(configuration);
            CommonConfigChangeToken = configuration.GetReloadToken();
            return CommonConfigChangeToken;
        }
       
    }
}
