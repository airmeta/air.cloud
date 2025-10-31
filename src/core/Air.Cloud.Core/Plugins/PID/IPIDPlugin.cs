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
using Air.Cloud.Core.Plugins.DefaultDependencies;
using Air.Cloud.Core.Standard.DynamicServer;

namespace Air.Cloud.Core.Plugins.PID
{
    /// <summary>
    /// <para>zh-cn:PID 插件</para>
    /// <para>en-us:PID Plugin</para>
    /// </summary>
    /// <remarks>
    /// <para>zh-cn:此插件为特殊插件类型, 不能依赖于ServiceProcider 因为它在ServiceProvider之前就开始使用</para>
    /// <para>en-us: This plugin is a special type of plugin, it cannot depend on ServiceProvider because it is used before ServiceProvider starts.</para>
    /// </remarks>
    public interface IPIDPlugin:IPlugin,ISingleton
    {
        /// <summary>
        /// <para>zh-cn:PID文件路径</para>
        /// <para>en-us:PID file path</para>
        /// </summary>
        public static string PID_FILE_PATH = "start.pid";
        /// <summary>
        /// <para>zh-cn:PID文件完整路径</para>
        /// <para>en-us:Full path of PID file</para>
        /// </summary>
        public static string StartPath = AppDomain.CurrentDomain.SetupInformation.ApplicationBase + "/" + PID_FILE_PATH;

        /// <summary>
        /// <para>zh-cn:获取PID</para>
        /// <para>en-us:Get PID</para>
        /// </summary>
        /// <returns>
        ///  <para>zh-cn:PID</para>
        ///  <para>en-us:PID</para>
        /// </returns>
        public string Get();

        /// <summary>
        /// <para>zh-cn:设置PID</para>
        /// <para>en-us:Set PID</para>
        /// </summary>
        /// <param name="PID">
        ///  <para>zh-cn:要设置的PID, 如果为null则由系统生成</para>
        /// </param>
        /// <returns></returns>
        public string Set(string PID = null);
    }
}
