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
using Air.Cloud.Core.App;

using Microsoft.Extensions.Hosting;

namespace Air.Cloud.HostApp.Event;

/// <summary>
/// 监听泛型主机启动事件
/// </summary>
public class GenericHostLifetimeEventsHostedService : IHostedService
{
    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="host"></param>
    public GenericHostLifetimeEventsHostedService(IHost host)
    {
        // 存储根服务
        AppCore.RootServices = host.Services;
    }

    /// <summary>
    /// 监听主机启动
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public Task StartAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    /// <summary>
    /// 监听主机停止
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}