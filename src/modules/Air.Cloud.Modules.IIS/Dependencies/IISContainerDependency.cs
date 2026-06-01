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
using Air.Cloud.Core.Standard.Container;
using Air.Cloud.Core.Standard.Container.Model;
using Air.Cloud.Modules.IIS.Helper;
using Air.Cloud.Modules.IIS.Model;

using Mapster;

using Microsoft.Web.Administration;

using System.Collections.Concurrent;

namespace Air.Cloud.Modules.IIS.Dependencies
{
    /// <summary>
    /// <para>zh-cn: 基于IIS的容器标准实现</para>
    /// <para>en-us: Windows IIS container standard dependency</para>
    /// </summary>
    public class IISContainerDependency : IContainerStandard
    {
        /// <summary>
        /// <para>zh-cn:查询当前 IIS 中的站点实例，并转换为指定的容器实例类型。</para>
        /// <para>en-us:Queries current IIS site instances and converts them to the specified container instance type.</para>
        /// </summary>
        /// <typeparam name="TIISContainerInstance">
        /// <para>zh-cn:要返回的 IIS 容器实例类型。</para>
        /// <para>en-us:The IIS container instance type to return.</para>
        /// </typeparam>
        /// <returns>
        /// <para>zh-cn:包含 IIS 容器实例的并发集合。</para>
        /// <para>en-us:A concurrent collection that contains IIS container instances.</para>
        /// </returns>
        public Task<ConcurrentBag<TIISContainerInstance>> QueryAsync<TIISContainerInstance>() where TIISContainerInstance : IContainerInstance, new()
        {
            ConcurrentBag<TIISContainerInstance> dockerContainerInstances = new ConcurrentBag<TIISContainerInstance>();

            foreach (var item in IISContainerHelper.ReadIISInstance())
            {
                dockerContainerInstances.Add(item.Adapt<TIISContainerInstance>());
            }
            return Task.FromResult(dockerContainerInstances);
        }

        /// <summary>
        /// <para>zh-cn:启动指定 IIS 容器实例对应的站点和应用程序池。</para>
        /// <para>en-us:Starts the site and application pool that correspond to the specified IIS container instance.</para>
        /// </summary>
        /// <typeparam name="TIISContainerInstance">
        /// <para>zh-cn:IIS 容器实例类型。</para>
        /// <para>en-us:The IIS container instance type.</para>
        /// </typeparam>
        /// <param name="Information">
        /// <para>zh-cn:需要启动的 IIS 容器实例信息。</para>
        /// <para>en-us:The IIS container instance information to start.</para>
        /// </param>
        /// <returns>
        /// <para>zh-cn:启动后的容器实例信息。</para>
        /// <para>en-us:The container instance information after the start operation.</para>
        /// </returns>
        public Task<TIISContainerInstance> StartAsync<TIISContainerInstance>(TIISContainerInstance Information) where TIISContainerInstance : IContainerInstance, new()
        {
            if (!(Information is IISContainerInstance)) AppRealization.Output.Error(new Exception("该实例非IIS服务器实例,无法启动"));
            IISContainerInstance containerInstance = Information as IISContainerInstance;
            using (var serverManager = new ServerManager())
            {
                var site = serverManager.Sites[containerInstance.BriefSite.Name];
                if (site != null)
                {
                    var poolObj = serverManager.ApplicationPools[containerInstance.BriefSite.Name];
                    if (poolObj != null)
                    {
                        poolObj.Start();
                        site.Start();
                        serverManager.CommitChanges();
                        return Task.FromResult(Information);
                    }
                }
                return Task.FromResult(Information);
            }
        }

        /// <summary>
        /// <para>zh-cn:停止指定 IIS 容器实例对应的站点和应用程序池。</para>
        /// <para>en-us:Stops the site and application pool that correspond to the specified IIS container instance.</para>
        /// </summary>
        /// <typeparam name="TIISContainerInstance">
        /// <para>zh-cn:IIS 容器实例类型。</para>
        /// <para>en-us:The IIS container instance type.</para>
        /// </typeparam>
        /// <param name="Information">
        /// <para>zh-cn:需要停止的 IIS 容器实例信息。</para>
        /// <para>en-us:The IIS container instance information to stop.</para>
        /// </param>
        /// <returns>
        /// <para>zh-cn:停止后的容器实例信息。</para>
        /// <para>en-us:The container instance information after the stop operation.</para>
        /// </returns>
        public Task<TIISContainerInstance> StopAsync<TIISContainerInstance>(TIISContainerInstance Information) where TIISContainerInstance : IContainerInstance, new()
        {
            if (!(Information is IISContainerInstance)) AppRealization.Output.Error(new Exception("该实例非IIS服务器实例,无法启动"));
            IISContainerInstance containerInstance = Information as IISContainerInstance;
            using (var serverManager = new ServerManager())
            {
                var site = serverManager.Sites[containerInstance.BriefSite.Name];
                if (site != null)
                {
                    site.Stop();
                    var poolObj = serverManager.ApplicationPools[containerInstance.BriefSite.Name];
                    if (poolObj != null)
                    {
                        poolObj.Stop();
                    }
                    serverManager.CommitChanges();
                    return Task.FromResult(Information);
                }
                return Task.FromResult(Information);
            }
           
        }
    }
}
