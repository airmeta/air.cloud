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

using System.Collections.Generic;

using Air.Cloud.EntityFrameWork.Core.ContextPools;

namespace Air.Cloud.DataBase.Oracle.DependencyInjection;

/// <summary>
/// <para>zh-cn:提供创建工作单元服务作用域的静态入口。</para>
/// <para>en-us:Provides static entry points for creating service scopes used by work units.</para>
/// </summary>
public static partial class Scoped
{
    /// <summary>
    /// <para>zh-cn:创建并同步执行一个工作单元服务作用域。</para>
    /// <para>en-us:Creates and executes a work-unit service scope synchronously.</para>
    /// </summary>
    /// <param name="handler"><para>zh-cn:在新作用域中执行的回调。</para><para>en-us:The callback executed in the new scope.</para></param>
    /// <param name="scopeFactory"><para>zh-cn:可选的作用域工厂；为空时从应用服务解析。</para><para>en-us:Optional scope factory; resolved from application services when null.</para></param>
    public static void CreateUow(Action<IServiceScopeFactory, IServiceScope> handler, IServiceScopeFactory? scopeFactory = default)
    {
        CreateUow(async (fac, scope) =>
        {
            handler(fac, scope);
            await Task.CompletedTask;
        }, scopeFactory).GetAwaiter().GetResult();
    }

    /// <summary>
    /// <para>zh-cn:创建并异步执行一个工作单元服务作用域。</para>
    /// <para>en-us:Creates and executes a work-unit service scope asynchronously.</para>
    /// </summary>
    /// <param name="handler"><para>zh-cn:在新作用域中执行的异步回调。</para><para>en-us:The asynchronous callback executed in the new scope.</para></param>
    /// <param name="scopeFactory"><para>zh-cn:可选的作用域工厂；为空时从应用服务解析。</para><para>en-us:Optional scope factory; resolved from application services when null.</para></param>
    public static async Task CreateUow(Func<IServiceScopeFactory, IServiceScope, Task> handler, IServiceScopeFactory? scopeFactory = default)
    {
        // 禁止空调用
        if (handler == null) throw new ArgumentNullException(nameof(handler));

        // 创建作用域
        var (scoped, serviceProvider, resolvedScopeFactory) = CreateScope(scopeFactory);

        try
        {
            // 创建一个数据库上下文池
            var dbContextPool = scoped.ServiceProvider.GetRequiredService<IDbContextPool>();

            // 执行方法
            await handler(resolvedScopeFactory, scoped);

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
    /// <para>zh-cn:该成员的数据库访问行为。</para>
    /// <para>en-us:Database access behavior provided by this member.</para>
    /// </summary>
    /// <param name="scopeFactory"><para>zh-cn:参数值。</para><para>en-us:The scopeFactory value.</para></param>
    /// <returns><para>zh-cn:操作结果。</para><para>en-us:The operation result.</para></returns>
    private static (IServiceScope Scoped, ServiceProvider? ServiceProvider, IServiceScopeFactory ScopeFactory) CreateScope(IServiceScopeFactory? scopeFactory)
    {
        ServiceProvider? undisposeServiceProvider = default;
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
        if (scopeFactory == null)
        {
            throw new InvalidOperationException($"{nameof(IServiceScopeFactory)} was not registered.");
        }
        // 解析服务作用域工厂
        var scoped = scopeFactory.CreateScope();
        return (scoped, undisposeServiceProvider, scopeFactory);
    }
}
