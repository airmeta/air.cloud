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
using Air.Cloud.Core;
using Air.Cloud.Core.Modules.AppPrint;
using Air.Cloud.Core.Standard.Container.Model;

using Microsoft.Web.Administration;

using System.Collections.Concurrent;
using System.ServiceProcess;

namespace Air.Cloud.Modules.IIS.Helper
{
    /// <summary>
    /// <para>zh-cn: IIS 容器帮助类</para>
    /// <para>en-us: IIS container helper</para>
    /// </summary>
    public static class IISContainerHelper
    {
        /// <summary>
        /// <para>zh-cn: 读取当前服务器的所有站点信息</para>
        /// <para>en-us: Read all sites information of current server</para>    
        /// </summary>
        /// <remarks>
        /// 该方法需要在Windows环境下运行 并且Windows 服务器需要安装IIS 
        ///      并且IIS需要调整config文件允许应用程序进行访问 否则会出现读取不到的情况
        /// 注意: 本机开发环境的IIS Express不支持该方法 需要进入 Windows Server 2012 R2 或者 Windows Server 2016 等服务器环境 方可进行读取
        /// </remarks>
        public static ConcurrentBag<ContainerEngineInstance> ReadIISInstance()
        {
            try
            {
                var SiteManager = new ServerManager();
                ConcurrentBag<ContainerEngineInstance> iISContainerInstances = new ConcurrentBag<ContainerEngineInstance>();
                foreach (var s in SiteManager.Sites)
                {
                    iISContainerInstances.Add(ToContainerEngineInstance(s));
                }
                return iISContainerInstances;
            }
            catch (Exception ex)
            {
                AppRealization.Output.Print(new AppPrintInformation
                {
                    State = true,
                    AdditionalParams = new Dictionary<string, object>
                    {
                        { "Title","读取站点信息失败"},
                        { "Content",ex.Message}
                    },
                    Level = AppPrintLevel.Error,
                    Title = "读取站点信息失败"
                });
                return new ConcurrentBag<ContainerEngineInstance>();
            }
        }

        /// <summary>
        /// <para>zh-cn:将 IIS 站点转换为 Core 容器引擎实例视图。</para>
        /// <para>en-us:Converts an IIS site into the Core container engine instance view.</para>
        /// </summary>
        /// <param name="site">
        /// <para>zh-cn:IIS 站点。</para>
        /// <para>en-us:The IIS site.</para>
        /// </param>
        /// <returns>
        /// <para>zh-cn:容器引擎实例视图。</para>
        /// <para>en-us:The container engine instance view.</para>
        /// </returns>
        public static ContainerEngineInstance ToContainerEngineInstance(Site site)
        {
            ArgumentNullException.ThrowIfNull(site);
            var state = site.State.ToString();
            var isRunning = site.State == ObjectState.Started || site.State == ObjectState.Starting;
            return new ContainerEngineInstance
            {
                Id = site.Id.ToString(),
                Name = site.Name,
                Image = site.Applications.FirstOrDefault()?.VirtualDirectories.FirstOrDefault()?.PhysicalPath ?? string.Empty,
                Runtime = "IIS",
                State = state,
                Status = state,
                HealthStatus = string.Empty,
                IsRunning = isRunning,
                IsAvailable = site.State == ObjectState.Started
            };
        }


        /// <summary>
        /// 判断IIS服务器是否存在
        /// </summary>
        /// <returns></returns>
        public static bool IsIISExist()
        {
            return ExistService("W3SVC");
        }

        /// <summary>
        /// 判断IIS服务器是否存在
        /// </summary>
        /// <returns></returns>
        public static bool ExistService(string serviceName)
        {
            var services = ServiceController.GetServices();
            return services.Count(it => it.ServiceName.Equals(serviceName, StringComparison.Ordinal)) > 0;
        }

        /// <summary>
        /// 判断IIS服务器是否在运行
        /// </summary>
        /// <returns></returns>
        public static bool IsIISRunning()
        {
            var services = ServiceController.GetServices();
            return services.Count(it => it.ServiceName == "W3SVC" && it.Status == ServiceControllerStatus.Running) > 0;
        }
    }
}
