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
    /// <summary>
    /// <para>zh-cn:基于 Docker Engine 的容器标准实现，负责查询、启动和停止 Docker 容器实例。</para>
    /// <para>en-us:Container standard implementation based on Docker Engine that queries, starts, and stops Docker container instances.</para>
    /// </summary>
    public class DockerContainerDependency : IContainerStandard
    {
        /// <summary>
        /// <para>zh-cn:查询当前 Docker Engine 中的容器实例，并转换为指定的容器实例类型。</para>
        /// <para>en-us:Queries container instances from the current Docker Engine and converts them to the specified container instance type.</para>
        /// </summary>
        /// <typeparam name="TDockerContainerInstance">
        /// <para>zh-cn:要返回的 Docker 容器实例类型。</para>
        /// <para>en-us:The Docker container instance type to return.</para>
        /// </typeparam>
        /// <returns>
        /// <para>zh-cn:包含 Docker 容器实例的并发集合。</para>
        /// <para>en-us:A concurrent collection that contains Docker container instances.</para>
        /// </returns>
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
                AppRealization.Output.Print(new AppPrintInformation()
                {
                    Level = AppPrintLevel.Error,
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
