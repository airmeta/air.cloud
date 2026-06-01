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
using Air.Cloud.Modules.Docker.Helper;
using Air.Cloud.Modules.Docker.Internal;
using Air.Cloud.Modules.Docker.Model;

using Docker.DotNet.Models;

namespace Air.Cloud.Modules.Docker.Events
{
    /// <summary>
    /// <para>zh-cn:Docker Engine 事件订阅入口，用于监听容器状态变化并刷新容器缓存。</para>
    /// <para>en-us:Docker Engine event subscription entry that listens for container state changes and refreshes container cache.</para>
    /// </summary>
    public static  class DockerEngineEvent
    {
        /// <summary>
        /// <para>zh-cn:订阅 Docker Engine 容器事件，收到状态变化后清理缓存并重新查询容器信息。</para>
        /// <para>en-us:Subscribes to Docker Engine container events, clears cache on state changes, and reloads container information.</para>
        /// </summary>
        public static void DockerEngineEventSubscription()
        {
            Task.Run(async () =>
            {
                var Client = DockerEngineClientHelper.Client;
                CancellationTokenSource cancellation = new CancellationTokenSource();
                await Client.System.MonitorEventsAsync(new ContainerEventsParameters(), new Progress<Message>((m) =>
                {
                    AppRealization.Output.Print(new AppPrintInformation()
                    {
                        State = true,
                        Content = "Container status changed",
                        Level = AppPrintLevel.Information,
                        Title = "Air.Cloud.Modules.Docker"
                    });
                    //清理掉现在的状态
                    DockerEngineInternalStore<DockerContainerInstance>.DockerContainerInstances?.Clear();
                    //重新进行查询新的结果
                    AppRealization.Container.QueryAsync<DockerContainerInstance>();
                }), CancellationToken.None);
            });
        }
    }
}
