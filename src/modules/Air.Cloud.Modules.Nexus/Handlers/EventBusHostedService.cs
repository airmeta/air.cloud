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
using Air.Cloud.Core.Standard.Event;
using Microsoft.Extensions.Hosting;

namespace Air.Cloud.Modules.Nexus.Handlers;

/// <summary>
/// 事件总线后台服务
/// </summary>
public class EventBusHostedService : BackgroundService
{
    private IEventBusExecutor _eventBusExecutor;

    public EventBusHostedService()
    {

    }
    public EventBusHostedService(IEventBusExecutor eventBusExecutor) : this()
    {
        _eventBusExecutor = eventBusExecutor;
    }
    /// <summary>
    /// 执行事件总线后台任务
    /// </summary>
    /// <param name="stoppingToken">任务取消token</param>
    /// <returns></returns>
    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (_eventBusExecutor == null) return Task.CompletedTask;
        return _eventBusExecutor.ExecuteAsync(stoppingToken);
    }
}
