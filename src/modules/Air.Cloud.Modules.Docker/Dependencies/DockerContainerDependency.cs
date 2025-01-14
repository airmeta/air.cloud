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
using Air.Cloud.Modules.Docker.Helper;
using Air.Cloud.Modules.Docker.Internal;
using Air.Cloud.Modules.Docker.Model;

using Docker.DotNet.Models;

using Mapster;

using System.Collections.Concurrent;

namespace Air.Cloud.Modules.Docker.Dependencies
{
    public class DockerContainerDependency : IContainerStandard
    {
        public async Task<ConcurrentBag<TDockerContainerInstance>> QueryAsync<TDockerContainerInstance>() where TDockerContainerInstance : IContainerInstance, new()
        {
            try
            {
                var Stored = DockerEngineInternalStore<TDockerContainerInstance>.DockerContainerInstances;
                if (Stored == null||Stored.IsEmpty||Stored.Count==0)
                {
                    ConcurrentBag<TDockerContainerInstance> dockerContainerInstances = new ConcurrentBag<TDockerContainerInstance>();
                    //获取容器信息
                    IList<ContainerListResponse> containers = await DockerEngineClientHelper.Client.Containers.ListContainersAsync(
                        new ContainersListParameters()
                        {
                            Limit = int.MaxValue,
                        });
                    foreach (var item in containers)
                    {
                        var dockerContainerInstance = new DockerContainerInstance()
                        {
                            IPAddress = item.NetworkSettings.Networks.Values.FirstOrDefault()?.IPAddress,
                            Port = item.Ports.FirstOrDefault()?.PublicPort,
                            Container = item
                        };
                        dockerContainerInstances.Add(dockerContainerInstance.Adapt<TDockerContainerInstance>());
                    }
                    DockerEngineInternalStore<TDockerContainerInstance>.DockerContainerInstances = dockerContainerInstances;
                    return dockerContainerInstances;

                }
                else
                {
                    return DockerEngineInternalStore<TDockerContainerInstance>.DockerContainerInstances;
                }
            }
            catch (Exception ex)
            {
                //记录日志
                AppRealization.Output.Print(new Air.Cloud.Core.Standard.Print.AppPrintInformation()
                {
                    Level = Air.Cloud.Core.Standard.Print.AppPrintInformation.AppPrintLevel.Error,
                    Title = "容器信息读取异常",
                    Content = ex.Message
                });
                return new ConcurrentBag<TDockerContainerInstance>();
            }
        }

        async Task<TContainerInformation> IContainerStandard.StartAsync<TContainerInformation>(TContainerInformation Information)
        {
            if (!(Information is DockerContainerInstance)) AppRealization.Output.Error(new Exception("该实例非Docker容器实例,无法启动"));
            DockerContainerInstance containerInstance = Information as DockerContainerInstance;
            await DockerEngineClientHelper.Client.Containers.StartContainerAsync(containerInstance.Container.ID,new ContainerStartParameters());
            return Information;
        }

        async Task<TContainerInformation> IContainerStandard.StopAsync<TContainerInformation>(TContainerInformation Information)
        {
            if (!(Information is DockerContainerInstance)) AppRealization.Output.Error(new Exception("该实例非Docker容器实例,无法启动"));
            DockerContainerInstance containerInstance = Information as DockerContainerInstance;
            await DockerEngineClientHelper.Client.Containers.StopContainerAsync(containerInstance.Container.ID, new ContainerStopParameters());
            return Information;
        }
    }
}
