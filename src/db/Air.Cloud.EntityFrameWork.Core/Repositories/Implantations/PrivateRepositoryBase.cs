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
using Air.Cloud.Core.Standard.DynamicServer;
using Air.Cloud.EntityFrameWork.Core.ContextPools;
using Air.Cloud.EntityFrameWork.Core.Repositories;
using Air.Cloud.EntityFrameWork.Core.Repositories.Dependencies;

using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace Air.Cloud.EntityFrameWork.Core.Repositories.Implantations;

/// <summary>
/// <para>zh-cn:EF Core 仓储基础实现。</para>
/// <para>en-us:Base implementation for EF Core repositories.</para>
/// </summary>
public partial class PrivateRepositoryBase
{
    /// <summary>
    /// 服务提供器
    /// <para>zh-cn:该成员的数据库访问行为。</para>
    /// <para>en-us:Database access behavior provided by this member.</para>
    /// </summary>
    private readonly IServiceProvider _serviceProvider;

    /// <summary>
    /// <para>zh-cn:使用数据库上下文定位器和服务提供器初始化仓储基础实现。</para>
    /// <para>en-us:Initializes the repository base with a database-context locator and service provider.</para>
    /// </summary>
    /// <param name="dbContextLocator">
    /// <para>zh-cn:数据库上下文定位器类型。</para>
    /// <para>en-us:The database-context locator type.</para>
    /// </param>
    /// <param name="serviceProvider">
    /// <para>zh-cn:用于解析上下文和事务池的服务提供器。</para>
    /// <para>en-us:The service provider used to resolve the context and transaction pool.</para>
    /// </param>
    public PrivateRepositoryBase(Type dbContextLocator, IServiceProvider serviceProvider)
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
    /// <para>zh-cn:数据库上下文。</para>
    /// <para>en-us:The EF Core database context.</para>
    /// </summary>
    public virtual DbContext Context { get; }

    /// <summary>
    /// <para>zh-cn:动态数据库上下文。</para>
    /// <para>en-us:The dynamic database context.</para>
    /// </summary>
    public virtual dynamic DynamicContext { get; }

    /// <summary>
    /// <para>zh-cn:EF Core 数据库操作对象。</para>
    /// <para>en-us:The EF Core database facade.</para>
    /// </summary>
    public virtual DatabaseFacade Database { get; }

    /// <summary>
    /// 解析服务
    /// <para>zh-cn:该成员的数据库访问行为。</para>
    /// <para>en-us:Database access behavior provided by this member.</para>
    /// </summary>
    /// <typeparam name="TService"><para>zh-cn:泛型参数类型</para><para>en-us:The generic parameter type.</para></typeparam>
    /// <returns><para>zh-cn:操作结果。</para><para>en-us:The operation result.</para></returns>
    public virtual TService GetService<TService>()
        where TService : class
    {
        return _serviceProvider.GetRequiredService<TService>();
    }

    /// <summary>
    /// 解析服务
    /// <para>zh-cn:该成员的数据库访问行为。</para>
    /// <para>en-us:Database access behavior provided by this member.</para>
    /// </summary>
    /// <typeparam name="TService"><para>zh-cn:泛型参数类型</para><para>en-us:The generic parameter type.</para></typeparam>
    /// <returns><para>zh-cn:操作结果。</para><para>en-us:The operation result.</para></returns>
    public virtual TService GetRequiredService<TService>()
        where TService : class
    {
        return _serviceProvider.GetRequiredService<TService>();
    }

    /// <summary>
    /// <para>zh-cn:将仓储约束为特定的 EF Core 仓储接口。</para>
    /// <para>en-us:Constrains the repository to a specific EF Core repository interface.</para>
    /// </summary>
    /// <typeparam name="TRestrainRepository">
    /// <para>zh-cn:特定仓储接口。</para>
    /// <para>en-us:The target repository interface.</para>
    /// </typeparam>
    /// <returns>
    /// <para>zh-cn:当前仓储转换后的接口实例。</para>
    /// <para>en-us:The current repository cast to the target interface.</para>
    /// </returns>
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
    /// <para>zh-cn:确保工作单元（本地事务）可用。</para>
    /// <para>en-us:Ensures that the local unit-of-work transaction is available.</para>
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
