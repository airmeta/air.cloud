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
        public Task<ConcurrentBag<TIISContainerInstance>> QueryAsync<TIISContainerInstance>() where TIISContainerInstance : IContainerInstance, new()
        {
            ConcurrentBag<TIISContainerInstance> dockerContainerInstances = new ConcurrentBag<TIISContainerInstance>();

            foreach (var item in IISContainerHelper.ReadIISInstance())
            {
                dockerContainerInstances.Add(item.Adapt<TIISContainerInstance>());
            }
            return Task.FromResult(dockerContainerInstances);
        }

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
