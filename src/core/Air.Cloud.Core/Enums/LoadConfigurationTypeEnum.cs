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
namespace Air.Cloud.Core.Enums
{
    /// <summary>
    /// <para>zh-cn:加载配置文件的方式</para>
    /// <para>en-us:How to load the configuration file</para>
    /// </summary>
    public enum LoadConfigurationTypeEnum
    {
        /// <summary>
        /// <para>zh-cn:加载本地配置文件</para>
        /// <para>en-us:Load configuration file</para>
        /// </summary>
        File,
        /// <summary>
        ///  <para>zh-cn:从远程加载配置文件</para>
        ///  <para>en-us:Load configuration file from remote</para>
        /// </summary>
        /// <remarks>
        /// <para>zh-cn:核心库中未实现 请使用相关的配置中心模组</para>
        /// <para>en-us:does not implement it in the core library. Please use the relevant configuration center module</para>
        /// </remarks>
        Remote
    }
}
