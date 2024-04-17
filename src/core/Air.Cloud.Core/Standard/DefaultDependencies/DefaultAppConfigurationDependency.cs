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
using Air.Cloud.Core.Standard.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Primitives;

namespace Air.Cloud.Core.Standard.DefaultDependencies
{
    [IgnoreScanning]
    public class DefaultAppConfigurationDependency : IAppConfigurationStandard
    {
        public IConfiguration LoadConfiguration(string ConfigurationName, bool IsCommonConfiguration)
        {
            IConfiguration config = null;
            IChangeToken token = null;
            Action action = new Action(() =>
            {
                config = new ConfigurationBuilder().SetBasePath(AppConst.ApplicationPath)
                                                          .AddJsonFile(ConfigurationName, optional: true, reloadOnChange: true)
                                                          .Build();
                if (IsCommonConfiguration) token = AppConfigurationLoader.SetCommonConfiguration(config, true);
                else token = AppConfigurationLoader.SetExternalConfiguration(config, true);
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
