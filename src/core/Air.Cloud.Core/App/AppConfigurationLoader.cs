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
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Primitives;

namespace Air.Cloud.Core.App
{
    /// <summary>
    /// 系统配置加载器
    /// </summary>
    public static partial class AppConfigurationLoader
    {
        /// <summary>
        /// 内部配置文件
        /// </summary>
        public static IConfiguration InnerConfiguration;
        /// <summary>
        /// 外部加载的配置文件
        /// </summary>
        public static ConfigurationManager Configurations;
        /// <summary>
        /// 修改令牌 远程加载的时候为null
        /// </summary>
        public static IChangeToken ExternalConfigChangeToken;
        /// <summary>
        /// 修改令牌 远程加载的时候为null
        /// </summary>
        public static IChangeToken CommonConfigChangeToken;
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
        /// 设置系统配置文件信息
        /// </summary>
        /// <param name="configuration">配置文件</param>
        /// <param name="token">修改令牌</param>
        public static IChangeToken SetExternalConfiguration(IConfiguration configuration, bool LoadToken = false)
        {
            Configurations.AddConfiguration(configuration);
            ExternalConfigChangeToken = LoadToken ? configuration.GetReloadToken() : null;
            return ExternalConfigChangeToken;
        }
        /// <summary>
        /// 设置系统公共配置文件信息
        /// </summary>
        /// <param name="configuration">配置文件</param>
        /// <param name="LoadToken">修改令牌</param>
        /// <returns>修改令牌</returns>
        public static IChangeToken SetCommonConfiguration(IConfiguration configuration, bool LoadToken = false)
        {
            Configurations.AddConfiguration(configuration);
            CommonConfigChangeToken = LoadToken ? configuration.GetReloadToken() : null;
            return CommonConfigChangeToken;
        }
       
    }
}
