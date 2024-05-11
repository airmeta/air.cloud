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
using Microsoft.AspNetCore.Builder;

using System.Reflection;

namespace Air.Cloud.Modules.Consul.Resolver
{
    /// <summary>
    /// 程序信息解析器
    /// </summary>
    public static class ProjectDataResolver
    {
        /// <summary>
        /// 程序启动地址
        /// </summary>
        public static string ApplicationUrl = AppDomain.CurrentDomain.SetupInformation.ApplicationBase;
        #region 获取应用程序在Consul中注册的相关信息
        /// <summary>
        /// 获取当前网站在Consul中的服务名 忽略Entry
        /// </summary>
        /// <remarks>
        /// 启动类库名称
        /// 例如: xxx.xxx.Entry 这里获取的就是 xxx.xxx
        /// </remarks>
        /// <param name="DefaultServiceName">默认服务名称(配置文件中的配置项)</param>
        /// <returns>服务名称</returns>
        public static string GetCurrentProjectConsulServiceName(this IApplicationBuilder app, string DefaultServiceName = null, Assembly assembly = null)
        {
            return GetCurrentProjectConsulServiceNameInReslover(DefaultServiceName, assembly);
        }

        /// <summary>
        /// 获取当前程序在Consul中的服务名 忽略Entry
        /// </summary>
        /// <param name="DefaultServiceName">默认服务名称(配置文件中的配置项)</param>
        /// <returns>服务名称</returns>
        public static string GetCurrentProjectConsulServiceNameInReslover(string DefaultServiceName = null, Assembly assembly = null)
        {
            if (!string.IsNullOrEmpty(DefaultServiceName)) return DefaultServiceName;
            if (assembly == null) assembly = Assembly.GetCallingAssembly();
            string ProjectName = assembly?.FullName?.Split(",")?.First()?.Trim();
            //如果启动路径中包含test的名称 则表示当前环境为测试环境
            if (ApplicationUrl.ToLower().Contains("test"))
            {
                ProjectName = ProjectName + ".Test";
            }
            return ProjectName.Replace(".Entry", "");
        }
        #endregion
    }
}
