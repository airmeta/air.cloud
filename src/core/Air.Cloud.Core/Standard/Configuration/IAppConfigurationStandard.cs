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

namespace Air.Cloud.Core.Standard.Configuration
{
    /// <summary>
    /// 配置文件标准
    /// </summary>
    public interface IAppConfigurationStandard : IStandard
    {
        /// <summary>
        /// <para>zh-cn:加载指定名称的配置文件。</para>
        /// <para>en-us:Loads the configuration file with the specified name.</para>
        /// </summary>
        /// <param name="ConfigurationName">
        /// <para>zh-cn:配置文件名称或配置标识。</para>
        /// <para>en-us:Configuration file name or configuration identifier.</para>
        /// </param>
        /// <param name="IsCommonConfiguration">
        /// <para>zh-cn:是否按公共配置加载。</para>
        /// <para>en-us:Indicates whether the configuration is loaded as a common configuration.</para>
        /// </param>
        /// <returns>
        /// <para>zh-cn:加载后的配置对象。</para>
        /// <para>en-us:Loaded configuration object.</para>
        /// </returns>
        public IConfiguration LoadConfiguration(string ConfigurationName, bool IsCommonConfiguration);

    }
}
