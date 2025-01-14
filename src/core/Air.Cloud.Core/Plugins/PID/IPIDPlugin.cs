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
using Air.Cloud.Core.Dependencies;

namespace Air.Cloud.Core.Plugins.PID
{
    /// <summary>
    /// PID 插件
    /// </summary>
    public interface IPIDPlugin:IPlugin,ISingleton
    {

        public static string PID_FILE_PATH = "start.pid";
        public static string StartPath = AppDomain.CurrentDomain.SetupInformation.ApplicationBase + "/" + PID_FILE_PATH;

        /// <summary>
        /// 获取PID
        /// </summary>
        /// <returns></returns>
        public string Get();

        /// <summary>
        /// 设置PID
        /// </summary>
        /// <param name="PID">PID内容 允许你手动指定</param>
        /// <returns></returns>
        public string Set(string PID = null);
    }
}
