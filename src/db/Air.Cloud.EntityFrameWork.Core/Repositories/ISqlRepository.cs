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
using Air.Cloud.EntityFrameWork.Core.Repositories.Dependencies;

using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Air.Cloud.EntityFrameWork.Core.Repositories;

/// <summary>
/// Sql 操作仓储接口
/// </summary>
public interface ISqlRepository : ISqlRepository<MasterDbContextLocator>
    , ISqlExecutableRepository
    , ISqlReaderRepository
{
}

/// <summary>
/// Sql 操作仓储接口
/// </summary>
/// <typeparam name="TDbContextLocator">数据库上下文定位器</typeparam>
public interface ISqlRepository<TDbContextLocator> : IPrivateSqlRepository
    , ISqlExecutableRepository<TDbContextLocator>
    , ISqlReaderRepository<TDbContextLocator>
   where TDbContextLocator : class, IDbContextLocator
{
}

/// <summary>
/// 私有 Sql 仓储
/// </summary>
public interface IPrivateSqlRepository : IPrivateSqlExecutableRepository
    , IPrivateSqlReaderRepository
    , IPrivateRootRepository
{
    /// <summary>
    /// 数据库操作对象
    /// </summary>
    DatabaseFacade Database { get; }

    /// <summary>
    /// 数据库上下文
    /// </summary>
    DbContext Context { get; }

    /// <summary>
    /// 动态数据库上下文
    /// </summary>
    dynamic DynamicContext { get; }

    /// <summary>
    /// 切换仓储
    /// </summary>
    /// <typeparam name="TChangeDbContextLocator">数据库上下文定位器</typeparam>
    /// <returns>仓储</returns>
    ISqlRepository<TChangeDbContextLocator> Change<TChangeDbContextLocator>()
        where TChangeDbContextLocator : class, IDbContextLocator;

    /// <summary>
    /// 解析服务
    /// </summary>
    /// <typeparam name="TService"></typeparam>
    /// <returns></returns>
    TService GetService<TService>()
        where TService : class;

    /// <summary>
    /// 解析服务
    /// </summary>
    /// <typeparam name="TService"></typeparam>
    /// <returns></returns>
    TService GetRequiredService<TService>()
        where TService : class;

    /// <summary>
    /// 将仓储约束为特定仓储
    /// </summary>
    /// <typeparam name="TRestrainRepository">特定仓储</typeparam>
    /// <returns>TRestrainRepository</returns>
    TRestrainRepository Constraint<TRestrainRepository>()
        where TRestrainRepository : class, IPrivateRootRepository;

    /// <summary>
    /// 确保工作单元（事务）可用
    /// </summary>
    void EnsureTransaction();
}