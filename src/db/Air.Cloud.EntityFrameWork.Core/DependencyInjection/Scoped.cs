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

using Microsoft.Extensions.DependencyInjection;

using Air.Cloud.EntityFrameWork.Core.ContextPools;

namespace Air.Cloud.DataBase.Oracle.DependencyInjection;

/// <summary>
/// 创建作用域静态类
/// </summary>
public static partial class Scoped
{
    /// <summary>
    /// 创建一个工作单元作用域
    /// </summary>
    /// <param name="handler"></param>
    /// <param name="scopeFactory"></param>
    public static void CreateUow(Action<IServiceScopeFactory, IServiceScope> handler, IServiceScopeFactory scopeFactory = default)
    {
        CreateUow(async (fac, scope) =>
        {
            handler(fac, scope);
            await Task.CompletedTask;
        }, scopeFactory).GetAwaiter().GetResult();
    }

    /// <summary>
    /// 创建一个工作单元作用域
    /// </summary>
    /// <param name="handler"></param>
    /// <param name="scopeFactory"></param>
    public static async Task CreateUow(Func<IServiceScopeFactory, IServiceScope, Task> handler, IServiceScopeFactory scopeFactory = default)
    {
        // 禁止空调用
        if (handler == null) throw new ArgumentNullException(nameof(handler));

        // 创建作用域
        var (scoped, serviceProvider) = CreateScope(ref scopeFactory);

        try
        {
            // 创建一个数据库上下文池
            var dbContextPool = scoped.ServiceProvider.GetService<IDbContextPool>();

            // 执行方法
            await handler(scopeFactory, scoped);

            // 提交工作单元
            dbContextPool.SavePoolNow();
        }
        finally
        {
            // 释放
            scoped.Dispose();
            if (serviceProvider != null) await serviceProvider.DisposeAsync();
        }
    }
    /// <summary>
    /// 创建一个作用域
    /// </summary>
    /// <param name="scopeFactory"></param>
    /// <returns></returns>
    private static (IServiceScope Scoped, ServiceProvider ServiceProvider) CreateScope(ref IServiceScopeFactory scopeFactory)
    {
        ServiceProvider undisposeServiceProvider = default;
        if (scopeFactory == null)
        {
            // 默认返回根服务
            if (AppCore.RootServices != null) scopeFactory = AppCore.RootServices.GetService<IServiceScopeFactory>();
            else
            {
                // 这里创建了一个待释放服务提供器（这里会有性能小问题，如果走到这一步）
                undisposeServiceProvider = AppCore.InternalServices.BuildServiceProvider();
                scopeFactory = undisposeServiceProvider.GetService<IServiceScopeFactory>();
            }
        }
        // 解析服务作用域工厂
        var scoped = scopeFactory.CreateScope();
        return (scoped, undisposeServiceProvider);
    }
}