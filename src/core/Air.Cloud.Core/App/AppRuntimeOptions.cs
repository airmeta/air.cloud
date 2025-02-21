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

namespace Air.Cloud.Core.App
{
    /// <summary>
    /// 应用程序运行参数
    /// </summary>
    [ConfigurationInfo("AppRuntimeOptions")]
    public  class AppRuntimeOptions
    {
        /// <summary>
        /// 端口号
        /// </summary>
        public int Port { get; set; }

        /// <summary>
        /// IP地址
        /// </summary>
        public string IPAddress { get; set; }

        /// <summary>
        /// 当前程序运行路径
        /// </summary>
        public string RuningDirectory => AppDomain.CurrentDomain?.SetupInformation?.ApplicationBase;

        /// <summary>
        /// 当前程序运行PID信息
        /// </summary>
        public string PID => AppRealization.PID.Get();

    }
}
