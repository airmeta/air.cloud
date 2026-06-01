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
using Air.Cloud.Core.Standard.DataBase.Locators;
using Air.Cloud.Core.Standard.DynamicServer;
using Air.Cloud.EntityFrameWork.Core.ContextPools;
using Air.Cloud.EntityFrameWork.Core.Repositories;
using Air.Cloud.EntityFrameWork.Core.Repositories.Dependencies;

using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace Air.Cloud.EntityFrameWork.Core.Repositories.Implantations;

/// <summary>
/// 私有 Sql 仓储
/// </summary>
public partial class PrivateSqlRepository : IPrivateSqlRepository
{
    /// <summary>
    /// 服务提供器
    /// </summary>
    private readonly IServiceProvider _serviceProvider;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="dbContextLocator"></param>
    /// <param name="serviceProvider">服务提供器</param>
    public PrivateSqlRepository(Type dbContextLocator, IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;

        // 解析数据库上下文
        var dbContextResolve = serviceProvider.GetService<Func<Type, IScoped, DbContext>>()
            ?? throw new InvalidOperationException("未注册数据库上下文解析委托。");
        var dbContext = dbContextResolve(dbContextLocator, default);
        DynamicContext = Context = dbContext;

        // 初始化数据库相关数据
        Database = Context.Database;
    }

    /// <summary>
    /// 数据库上下文
    /// </summary>
    public virtual DbContext Context { get; }

    /// <summary>
    /// 动态数据库上下文
    /// </summary>
    public virtual dynamic DynamicContext { get; }

    /// <summary>
    /// 数据库操作对象
    /// </summary>
    public virtual DatabaseFacade Database { get; }

    /// <summary>
    /// 切换仓储
    /// </summary>
    /// <typeparam name="TChangeDbContextLocator">数据库上下文定位器</typeparam>
    /// <returns>仓储</returns>
    public virtual ISqlRepository<TChangeDbContextLocator> Change<TChangeDbContextLocator>()
         where TChangeDbContextLocator : class, IDbContextLocator
    {
        return _serviceProvider.GetRequiredService<ISqlRepository<TChangeDbContextLocator>>();
    }

    /// <summary>
    /// 解析服务
    /// </summary>
    /// <typeparam name="TService"></typeparam>
    /// <returns></returns>
    public virtual TService GetService<TService>()
        where TService : class
    {
        return _serviceProvider.GetRequiredService<TService>();
    }

    /// <summary>
    /// 解析服务
    /// </summary>
    /// <typeparam name="TService"></typeparam>
    /// <returns></returns>
    public virtual TService GetRequiredService<TService>()
        where TService : class
    {
        return _serviceProvider.GetRequiredService<TService>();
    }

    /// <summary>
    /// 将仓储约束为特定仓储
    /// </summary>
    /// <typeparam name="TRestrainRepository">特定仓储</typeparam>
    /// <returns>TRestrainRepository</returns>
    public virtual TRestrainRepository Constraint<TRestrainRepository>()
        where TRestrainRepository : class, IPrivateRootRepository
    {
        var type = typeof(TRestrainRepository);
        if (!type.IsInterface || typeof(IPrivateRootRepository) == type || type.Name.Equals(nameof(IRepository)) || (type.IsGenericType && type.GetGenericTypeDefinition().Name.Equals(nameof(IRepository))))
        {
            throw new InvalidCastException("Invalid type conversion.");
        }

        return this as TRestrainRepository
            ?? throw new InvalidCastException($"当前仓储无法转换为 {typeof(TRestrainRepository).FullName}。");
    }

    /// <summary>
    /// 确保工作单元（事务）可用
    /// </summary>
    public virtual void EnsureTransaction()
    {
        var httpContext = AppCore.HttpContext;

        // 如果请求上下文为空，则跳过
        if (httpContext == null) return;

        // 获取数据库上下文
        var dbContextPool = httpContext.RequestServices.GetService<IDbContextPool>();
        if (dbContextPool == null) return;

        // 追加上下文
        dbContextPool.AddToPool(Context);
        // 开启事务
        dbContextPool.BeginTransaction();
    }
}
