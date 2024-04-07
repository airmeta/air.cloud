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
using Air.Cloud.Core.Plugins;

using System.Diagnostics;

namespace Air.Cloud.Core.Plugins.OpenShell
{
    /// <summary>
    /// 打开Shell插件
    /// </summary>
    public class OpenShellPlugin : IPlugin
    {
        /// <summary>
        /// 进程对象
        /// </summary>
        private Process pro;

        /// <summary>
        /// 文件路径URL
        /// </summary>
        private string pathURL;

        /// <summary>
        /// 启动应用程序
        /// </summary>
        /// <param name="FilePathAndName">文件路径和名称</param>
        /// <param name="ShellExecuteFlag">是否使用Shell执行标志</param>
        public void Start(string FilePathAndName, bool ShellExecuteFlag)
        {
            pathURL = FilePathAndName;
            pro = new Process
            {
                StartInfo =
                    {
                        WindowStyle = ProcessWindowStyle.Normal,
                        FileName = pathURL,
                        UseShellExecute = ShellExecuteFlag
                    }
            };
            pro.Start();
        }

        /// <summary>
        /// 销毁应用程序
        /// </summary>
        public void OnDestroy()
        {
            pro?.Kill();
            pro?.Dispose();
            pro?.Close();
        }
    }
}
