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

namespace Air.Cloud.Modules.Consul.Util
{
    public interface IConfigurationLoader
    {
        /// <summary>
        /// 加载远程配置文件
        /// </summary>
        /// <param name="key"></param>
        /// <param name="FileName"></param>
        /// <returns></returns>
        (IConfiguration, IConfiguration) LoadRemoteConfiguration(string RemoteUrl = null, string key = null, string FileName = null);
    }
}
