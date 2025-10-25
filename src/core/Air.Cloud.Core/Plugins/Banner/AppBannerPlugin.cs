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

using System;
using System.IO;
using System.Linq;
using System.Runtime;

namespace Air.Cloud.Core.Plugins.Banner
{
    /// <summary>
    /// <para>zh-cn: 应用横幅插件</para>
    /// <para> en-us: Application banner plugin</para>
    /// </summary>
    public class AppBannerPlugin:IAppBannerPlugin
    {
        /// <inheritdoc/>
        public void PrintOrganizationName()
        {
            string[] lines = {
                        "    ::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::",
                        @"    :::::  █████╗ ██╗██████╗     ██████╗██╗      ██████╗ ██╗   ██╗██████╗  :::::",
                        @"    ::::: ██╔══██╗██║██╔══██╗   ██╔════╝██║     ██╔═══██╗██║   ██║██╔══██╗ :::::",
                        @"    ::::: ███████║██║██████╔╝   ██║     ██║     ██║   ██║██║   ██║██║  ██║ :::::",
                        @"    ::::: ██╔══██║██║██╔══██╗   ██║     ██║     ██║   ██║██║   ██║██║  ██║ :::::",
                        @"    ::::: ██║  ██║██║██║  ██║   ╚██████╗███████╗╚██████╔╝╚██████╔╝██████╔╝ :::::",
                        "    ::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::"
            };
            Console.WriteLine("  ");
            for (int i = 0; i < lines.Length; i++)
            {
                Console.WriteLine(lines[i]);
            }
            Console.WriteLine("  ");
        }
        /// <inheritdoc/>
        public void PrintSystemModuleInformation(IList<string> Paths)
        {
            Dictionary<string, object> dic = new Dictionary<string, object>();
            foreach (var s in Paths)
            {
                var dirInfo = new DirectoryInfo(s);
                dic.Add($"模块名: {dirInfo.Name}", $"模块安装地址: {s}");
            }
            AppRealization.Output.Print("系统初始化", $"系统完成模块扫描 共加载了 {Paths.Count} 个模块", AppPrintLevel.Information,
                AdditionalParams: dic);
        }
        /// <inheritdoc/>
        public void PrintSystemPluginInformation(IList<string> Paths)
        {

            Dictionary<string, object> dic = new Dictionary<string, object>();
            foreach (var s in Paths)
            {
                var dirInfo = new DirectoryInfo(s);
                dic.Add($"插件名: {dirInfo.Name}", $"插件安装地址: {s}");
            }
            AppRealization.Output.Print("系统初始化", $"系统完成插件扫描 共加载了 {Paths.Count} 个模块", AppPrintLevel.Information,
                AdditionalParams: dic);
        }
    }
}
