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

namespace Air.Cloud.Core.Plugins.Security.Util
{
    public class ConfigReader
    {
        /// <summary>
        /// 远程配置文件  动态内容
        /// </summary>
        public static IConfiguration Configuration;
        static ConfigReader()
        {
            Action action = new Action(() =>
            {
                string path = AppDomain.CurrentDomain.SetupInformation.ApplicationBase;
                Configuration = new ConfigurationBuilder().SetBasePath(path)
                                                          .AddJsonFile("system.security.json", optional: true, reloadOnChange: true)
                                                          .Build();
            });
            action();
            //监视文件变更
            ChangeToken.OnChange(() => Configuration.GetReloadToken(), action);
        }
    }
}
