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
namespace Air.Cloud.Core.Attributes
{
    /// <summary>
    /// 配置节名称
    /// </summary>
    public class ConfigurationInfoAttribute : Attribute
    {
        /// <summary>
        /// <para>zh-cn:配置节名称。</para>
        /// <para>en-us:Configuration section name.</para>
        /// </summary>
        public string ConfigurationName { get; set; }
        /// <summary>
        /// <para>zh-cn:使用指定配置节名称初始化配置标记。</para>
        /// <para>en-us:Initializes the configuration attribute with the specified section name.</para>
        /// </summary>
        /// <param name="configurationName">
        /// <para>zh-cn:配置节名称。</para>
        /// <para>en-us:Configuration section name.</para>
        /// </param>
        public ConfigurationInfoAttribute(string configurationName)
        {
            ConfigurationName = configurationName;
        }
        /// <summary>
        /// <para>zh-cn:初始化默认配置标记。</para>
        /// <para>en-us:Initializes a default configuration attribute.</para>
        /// </summary>
        public ConfigurationInfoAttribute() { }
    }
}
