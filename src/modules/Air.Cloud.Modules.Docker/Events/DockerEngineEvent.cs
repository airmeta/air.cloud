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
using Air.Cloud.Modules.Docker.Helper;
using Air.Cloud.Modules.Docker.Internal;
using Air.Cloud.Modules.Docker.Model;

using Docker.DotNet.Models;

namespace Air.Cloud.Modules.Docker.Events
{
    public static  class DockerEngineEvent
    {
        public static void DockerEngineEventSubscription()
        {
            Task.Run(async () =>
            {
                var Client = DockerEngineClientHelper.Client;
                CancellationTokenSource cancellation = new CancellationTokenSource();
                await Client.System.MonitorEventsAsync(new ContainerEventsParameters(), new Progress<Message>((m) =>
                {
                    AppRealization.Output.Print(new Core.Standard.Print.AppPrintInformation()
                    {
                        State = true,
                        Content = "Container status changed",
                        Level = Core.Standard.Print.AppPrintInformation.AppPrintLevel.Information,
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
