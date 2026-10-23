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
using Air.Cloud.Core.Standard.DataBase.Model;

using Microsoft.EntityFrameworkCore.Query;

using System.Linq.Expressions;

namespace Air.Cloud.EntityFrameWork.Core.Repositories.Dependencies;

/// <summary>
/// <para>zh-cn:可读仓储接口</para>
/// <para>en-us:Describes the behavior provided by this member.</para>
/// </summary>
/// <typeparam name="TEntity"><para>zh-cn:实体类型</para><para>en-us:The entity type.</para></typeparam>
public interface IReadableRepository<TEntity> : IReadableRepository<TEntity, MasterDbContextLocator>
    where TEntity : class, IPrivateEntity, new()
{
}

/// <summary>
/// <para>zh-cn:可读仓储接口</para>
/// <para>en-us:Describes the behavior provided by this member.</para>
/// </summary>
/// <typeparam name="TEntity"><para>zh-cn:实体类型</para><para>en-us:The entity type.</para></typeparam>
/// <typeparam name="TDbContextLocator"><para>zh-cn:数据库上下文定位器</para><para>en-us:The database context locator type.</para></typeparam>
public interface IReadableRepository<TEntity, TDbContextLocator> : IPrivateReadableRepository<TEntity>
    where TEntity : class, IPrivateEntity, new()
    where TDbContextLocator : class, IDbContextLocator
{
}

/// <summary>
/// <para>zh-cn:可读仓储接口</para>
/// <para>en-us:Describes the behavior provided by this member.</para>
/// </summary>
/// <typeparam name="TEntity"><para>zh-cn:实体类型</para><para>en-us:The entity type.</para></typeparam>
public interface IPrivateReadableRepository<TEntity> : IPrivateRootRepository
    where TEntity : class, IPrivateEntity, new()
{
    /// <summary>
    /// <para>zh-cn:根据键查询一条记录</para>
    /// <para>en-us:Describes the behavior provided by this member.</para>
    /// </summary>
    /// <param name="key"><para>zh-cn:键</para><para>en-us:The key value.</para></param>
    /// <returns><para>zh-cn:数据库中的实体</para><para>en-us:The operation result.</para></returns>
    TEntity Find(object key);

    /// <summary>
    /// <para>zh-cn:根据多个键查询一条记录</para>
    /// <para>en-us:Describes the behavior provided by this member.</para>
    /// </summary>
    /// <param name="keyValues"><para>zh-cn:多个键</para><para>en-us:The key values.</para></param>
    /// <returns><para>zh-cn:数据库中的实体</para><para>en-us:The operation result.</para></returns>
    TEntity Find(params object[] keyValues);

    /// <summary>
    /// <para>zh-cn:根据键查询一条记录</para>
    /// <para>en-us:Describes the behavior provided by this member.</para>
    /// </summary>
    /// <param name="key"><para>zh-cn:键</para><para>en-us:The key value.</para></param>
    /// <param name="cancellationToken"><para>zh-cn:取消异步令牌</para><para>en-us:The cancellation token.</para></param>
    /// <returns><para>zh-cn:数据库实体</para><para>en-us:The operation result.</para></returns>
    Task<TEntity> FindAsync(object key, CancellationToken cancellationToken = default);

    /// <summary>
    /// <para>zh-cn:根据多个键查询一条记录</para>
    /// <para>en-us:Describes the behavior provided by this member.</para>
    /// </summary>
    /// <param name="keyValues"><para>zh-cn:多个键</para><para>en-us:The key values.</para></param>
    /// <returns><para>zh-cn:数据库中的实体</para><para>en-us:The operation result.</para></returns>
    Task<TEntity> FindAsync(params object[] keyValues);

    /// <summary>
    /// <para>zh-cn:根据多个键查询一条记录</para>
    /// <para>en-us:Describes the behavior provided by this member.</para>
    /// </summary>
    /// <param name="keyValues"><para>zh-cn:多个键</para><para>en-us:The key values.</para></param>
    /// <param name="cancellationToken"><para>zh-cn:取消异步令牌</para><para>en-us:The cancellation token.</para></param>
    /// <returns><para>zh-cn:数据库中的实体</para><para>en-us:The operation result.</para></returns>
    Task<TEntity> FindAsync(object[] keyValues, CancellationToken cancellationToken = default);

    /// <summary>
    /// <para>zh-cn:根据键查询一条记录</para>
    /// <para>en-us:Describes the behavior provided by this member.</para>
    /// </summary>
    /// <param name="key"><para>zh-cn:键</para><para>en-us:The key value.</para></param>
    /// <returns><para>zh-cn:数据库中的实体</para><para>en-us:The operation result.</para></returns>
    TEntity? FindOrDefault(object key);

    /// <summary>
    /// <para>zh-cn:根据多个键查询一条记录</para>
    /// <para>en-us:Describes the behavior provided by this member.</para>
    /// </summary>
    /// <param name="keyValues"><para>zh-cn:多个键</para><para>en-us:The key values.</para></param>
    /// <returns><para>zh-cn:数据库中的实体</para><para>en-us:The operation result.</para></returns>
    TEntity? FindOrDefault(params object[] keyValues);

    /// <summary>
    /// <para>zh-cn:根据键查询一条记录</para>
    /// <para>en-us:Describes the behavior provided by this member.</para>
    /// </summary>
    /// <param name="key"><para>zh-cn:键</para><para>en-us:The key value.</para></param>
    /// <param name="cancellationToken"><para>zh-cn:取消异步令牌</para><para>en-us:The cancellation token.</para></param>
    /// <returns><para>zh-cn:数据库中的实体</para><para>en-us:The operation result.</para></returns>
    Task<TEntity?> FindOrDefaultAsync(object key, CancellationToken cancellationToken = default);

    /// <summary>
    /// <para>zh-cn:根据多个键查询一条记录</para>
    /// <para>en-us:Describes the behavior provided by this member.</para>
    /// </summary>
    /// <param name="keyValues"><para>zh-cn:多个键</para><para>en-us:The key values.</para></param>
    /// <returns><para>zh-cn:数据库中的实体</para><para>en-us:The operation result.</para></returns>
    Task<TEntity?> FindOrDefaultAsync(params object[] keyValues);

    /// <summary>
    /// <para>zh-cn:根据多个键查询一条记录</para>
    /// <para>en-us:Describes the behavior provided by this member.</para>
    /// </summary>
    /// <param name="keyValues"><para>zh-cn:多个键</para><para>en-us:The key values.</para></param>
    /// <param name="cancellationToken"><para>zh-cn:取消异步令牌</para><para>en-us:The cancellation token.</para></param>
    /// <returns><para>zh-cn:数据库中的实体</para><para>en-us:The operation result.</para></returns>
    Task<TEntity?> FindOrDefaultAsync(object[] keyValues, CancellationToken cancellationToken = default);

    /// <summary>
    /// <para>zh-cn:查询一条记录</para>
    /// <para>en-us:Describes the behavior provided by this member.</para>
    /// </summary>
    /// <param name="tracking"><para>zh-cn:是否跟踪实体</para><para>en-us:Whether to track entities.</para></param>
    /// <returns><para>zh-cn:数据库中的实体</para><para>en-us:The operation result.</para></returns>
    TEntity Single(bool? tracking = null);

    /// <summary>
    /// <para>zh-cn:根据表达式查询一条记录</para>
    /// <para>en-us:Describes the behavior provided by this member.</para>
    /// </summary>
    /// <param name="predicate"><para>zh-cn:表达式</para><para>en-us:The query predicate.</para></param>
    /// <param name="tracking"><para>zh-cn:是否跟踪实体</para><para>en-us:Whether to track entities.</para></param>
    /// <returns><para>zh-cn:数据库中的实体</para><para>en-us:The operation result.</para></returns>
    TEntity Single(Expression<Func<TEntity, bool>> predicate, bool? tracking = null);

    /// <summary>
    /// <para>zh-cn:查询一条记录</para>
    /// <para>en-us:Describes the behavior provided by this member.</para>
    /// </summary>
    /// <param name="tracking"><para>zh-cn:是否跟踪实体</para><para>en-us:Whether to track entities.</para></param>
    /// <returns><para>zh-cn:数据库中的实体</para><para>en-us:The operation result.</para></returns>
    TEntity? SingleOrDefault(bool? tracking = null);

    /// <summary>
    /// <para>zh-cn:根据表达式查询一条记录</para>
    /// <para>en-us:Describes the behavior provided by this member.</para>
    /// </summary>
    /// <param name="predicate"><para>zh-cn:表达式</para><para>en-us:The query predicate.</para></param>
    /// <param name="tracking"><para>zh-cn:是否跟踪实体</para><para>en-us:Whether to track entities.</para></param>
    /// <returns><para>zh-cn:数据库中的实体</para><para>en-us:The operation result.</para></returns>
    TEntity? SingleOrDefault(Expression<Func<TEntity, bool>> predicate, bool? tracking = null);

    /// <summary>
    /// <para>zh-cn:查询一条记录</para>
    /// <para>en-us:Describes the behavior provided by this member.</para>
    /// </summary>
    /// <param name="tracking"><para>zh-cn:是否跟踪实体</para><para>en-us:Whether to track entities.</para></param>
    /// <param name="cancellationToken"><para>zh-cn:取消异步令牌</para><para>en-us:The cancellation token.</para></param>
    /// <returns><para>zh-cn:数据库中的实体</para><para>en-us:The operation result.</para></returns>
    Task<TEntity> SingleAsync(bool? tracking = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// <para>zh-cn:根据表达式查询一条记录</para>
    /// <para>en-us:Describes the behavior provided by this member.</para>
    /// </summary>
    /// <param name="predicate"><para>zh-cn:表达式</para><para>en-us:The query predicate.</para></param>
    /// <param name="tracking"><para>zh-cn:是否跟踪实体</para><para>en-us:Whether to track entities.</para></param>
    /// <param name="cancellationToken"><para>zh-cn:取消异步令牌</para><para>en-us:The cancellation token.</para></param>
    /// <returns><para>zh-cn:数据库中的实体</para><para>en-us:The operation result.</para></returns>
    Task<TEntity> SingleAsync(Expression<Func<TEntity, bool>> predicate, bool? tracking = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// <para>zh-cn:查询一条记录</para>
    /// <para>en-us:Describes the behavior provided by this member.</para>
    /// </summary>
    /// <param name="tracking"><para>zh-cn:是否跟踪实体</para><para>en-us:Whether to track entities.</para></param>
    /// <param name="cancellationToken"><para>zh-cn:异步取消令牌</para><para>en-us:The cancellation token.</para></param>
    /// <returns><para>zh-cn:数据库中的实体</para><para>en-us:The operation result.</para></returns>
    Task<TEntity?> SingleOrDefaultAsync(bool? tracking = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// <para>zh-cn:根据表达式查询一条记录</para>
    /// <para>en-us:Describes the behavior provided by this member.</para>
    /// </summary>
    /// <param name="predicate"><para>zh-cn:表达式</para><para>en-us:The query predicate.</para></param>
    /// <param name="tracking"><para>zh-cn:是否跟踪实体</para><para>en-us:Whether to track entities.</para></param>
    /// <param name="cancellationToken"><para>zh-cn:异步取消令牌</para><para>en-us:The cancellation token.</para></param>
    /// <returns><para>zh-cn:数据库中的实体</para><para>en-us:The operation result.</para></returns>
    Task<TEntity?> SingleOrDefaultAsync(Expression<Func<TEntity, bool>> predicate, bool? tracking = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// <para>zh-cn:查询一条记录</para>
    /// <para>en-us:Describes the behavior provided by this member.</para>
    /// </summary>
    /// <param name="tracking"><para>zh-cn:是否跟踪实体</para><para>en-us:Whether to track entities.</para></param>
    /// <returns><para>zh-cn:数据库中的实体</para><para>en-us:The operation result.</para></returns>
    TEntity First(bool? tracking = null);

    /// <summary>
    /// <para>zh-cn:根据表达式查询一条记录</para>
    /// <para>en-us:Describes the behavior provided by this member.</para>
    /// </summary>
    /// <param name="predicate"><para>zh-cn:表达式</para><para>en-us:The query predicate.</para></param>
    /// <param name="tracking"><para>zh-cn:是否跟踪实体</para><para>en-us:Whether to track entities.</para></param>
    /// <returns><para>zh-cn:数据库中的实体</para><para>en-us:The operation result.</para></returns>
    TEntity First(Expression<Func<TEntity, bool>> predicate, bool? tracking = null);

    /// <summary>
    /// <para>zh-cn:查询一条记录</para>
    /// <para>en-us:Describes the behavior provided by this member.</para>
    /// </summary>
    /// <param name="tracking"><para>zh-cn:是否跟踪实体</para><para>en-us:Whether to track entities.</para></param>
    /// <returns><para>zh-cn:数据库中的实体</para><para>en-us:The operation result.</para></returns>
    TEntity? FirstOrDefault(bool? tracking = null);

    /// <summary>
    /// <para>zh-cn:根据表达式查询一条记录</para>
    /// <para>en-us:Describes the behavior provided by this member.</para>
    /// </summary>
    /// <param name="predicate"><para>zh-cn:表达式</para><para>en-us:The query predicate.</para></param>
    /// <param name="tracking"><para>zh-cn:是否跟踪实体</para><para>en-us:Whether to track entities.</para></param>
    /// <returns><para>zh-cn:数据库中的实体</para><para>en-us:The operation result.</para></returns>
    TEntity? FirstOrDefault(Expression<Func<TEntity, bool>> predicate, bool? tracking = null);

    /// <summary>
    /// <para>zh-cn:查询一条记录</para>
    /// <para>en-us:Describes the behavior provided by this member.</para>
    /// </summary>
    /// <param name="tracking"><para>zh-cn:是否跟踪实体</para><para>en-us:Whether to track entities.</para></param>
    /// <param name="cancellationToken"><para>zh-cn:取消异步令牌</para><para>en-us:The cancellation token.</para></param>
    /// <returns><para>zh-cn:数据库中的实体</para><para>en-us:The operation result.</para></returns>
    Task<TEntity> FirstAsync(bool? tracking = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// <para>zh-cn:根据表达式查询一条记录</para>
    /// <para>en-us:Describes the behavior provided by this member.</para>
    /// </summary>
    /// <param name="predicate"><para>zh-cn:表达式</para><para>en-us:The query predicate.</para></param>
    /// <param name="tracking"><para>zh-cn:是否跟踪实体</para><para>en-us:Whether to track entities.</para></param>
    /// <param name="cancellationToken"><para>zh-cn:取消异步令牌</para><para>en-us:The cancellation token.</para></param>
    /// <returns><para>zh-cn:数据库中的实体</para><para>en-us:The operation result.</para></returns>
    Task<TEntity> FirstAsync(Expression<Func<TEntity, bool>> predicate, bool? tracking = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// <para>zh-cn:查询一条记录</para>
    /// <para>en-us:Describes the behavior provided by this member.</para>
    /// </summary>
    /// <param name="tracking"><para>zh-cn:是否跟踪实体</para><para>en-us:Whether to track entities.</para></param>
    /// <param name="cancellationToken"><para>zh-cn:异步取消令牌</para><para>en-us:The cancellation token.</para></param>
    /// <returns><para>zh-cn:数据库中的实体</para><para>en-us:The operation result.</para></returns>
    Task<TEntity?> FirstOrDefaultAsync(bool? tracking = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// <para>zh-cn:根据表达式查询一条记录</para>
    /// <para>en-us:Describes the behavior provided by this member.</para>
    /// </summary>
    /// <param name="predicate"><para>zh-cn:表达式</para><para>en-us:The query predicate.</para></param>
    /// <param name="tracking"><para>zh-cn:是否跟踪实体</para><para>en-us:Whether to track entities.</para></param>
    /// <param name="cancellationToken"><para>zh-cn:异步取消令牌</para><para>en-us:The cancellation token.</para></param>
    /// <returns><para>zh-cn:数据库中的实体</para><para>en-us:The operation result.</para></returns>
    Task<TEntity?> FirstOrDefaultAsync(Expression<Func<TEntity, bool>> predicate, bool? tracking = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// <para>zh-cn:查询一条记录</para>
    /// <para>en-us:Describes the behavior provided by this member.</para>
    /// </summary>
    /// <param name="tracking"><para>zh-cn:是否跟踪实体</para><para>en-us:Whether to track entities.</para></param>
    /// <returns><para>zh-cn:数据库中的实体</para><para>en-us:The operation result.</para></returns>
    TEntity Last(bool? tracking = null);

    /// <summary>
    /// <para>zh-cn:根据表达式查询一条记录</para>
    /// <para>en-us:Describes the behavior provided by this member.</para>
    /// </summary>
    /// <param name="predicate"><para>zh-cn:表达式</para><para>en-us:The query predicate.</para></param>
    /// <param name="tracking"><para>zh-cn:是否跟踪实体</para><para>en-us:Whether to track entities.</para></param>
    /// <returns><para>zh-cn:数据库中的实体</para><para>en-us:The operation result.</para></returns>
    TEntity Last(Expression<Func<TEntity, bool>> predicate, bool? tracking = null);

    /// <summary>
    /// <para>zh-cn:查询一条记录</para>
    /// <para>en-us:Describes the behavior provided by this member.</para>
    /// </summary>
    /// <param name="tracking"><para>zh-cn:是否跟踪实体</para><para>en-us:Whether to track entities.</para></param>
    /// <returns><para>zh-cn:数据库中的实体</para><para>en-us:The operation result.</para></returns>
    TEntity? LastOrDefault(bool? tracking = null);

    /// <summary>
    /// <para>zh-cn:根据表达式查询一条记录</para>
    /// <para>en-us:Describes the behavior provided by this member.</para>
    /// </summary>
    /// <param name="predicate"><para>zh-cn:表达式</para><para>en-us:The query predicate.</para></param>
    /// <param name="tracking"><para>zh-cn:是否跟踪实体</para><para>en-us:Whether to track entities.</para></param>
    /// <returns><para>zh-cn:数据库中的实体</para><para>en-us:The operation result.</para></returns>
    TEntity? LastOrDefault(Expression<Func<TEntity, bool>> predicate, bool? tracking = null);

    /// <summary>
    /// <para>zh-cn:查询一条记录</para>
    /// <para>en-us:Describes the behavior provided by this member.</para>
    /// </summary>
    /// <param name="tracking"><para>zh-cn:是否跟踪实体</para><para>en-us:Whether to track entities.</para></param>
    /// <param name="cancellationToken"><para>zh-cn:取消异步令牌</para><para>en-us:The cancellation token.</para></param>
    /// <returns><para>zh-cn:数据库中的实体</para><para>en-us:The operation result.</para></returns>
    Task<TEntity> LastAsync(bool? tracking = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// <para>zh-cn:根据表达式查询一条记录</para>
    /// <para>en-us:Describes the behavior provided by this member.</para>
    /// </summary>
    /// <param name="predicate"><para>zh-cn:表达式</para><para>en-us:The query predicate.</para></param>
    /// <param name="tracking"><para>zh-cn:是否跟踪实体</para><para>en-us:Whether to track entities.</para></param>
    /// <param name="cancellationToken"><para>zh-cn:取消异步令牌</para><para>en-us:The cancellation token.</para></param>
    /// <returns><para>zh-cn:数据库中的实体</para><para>en-us:The operation result.</para></returns>
    Task<TEntity> LastAsync(Expression<Func<TEntity, bool>> predicate, bool? tracking = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// <para>zh-cn:查询一条记录</para>
    /// <para>en-us:Describes the behavior provided by this member.</para>
    /// </summary>
    /// <param name="tracking"><para>zh-cn:是否跟踪实体</para><para>en-us:Whether to track entities.</para></param>
    /// <param name="cancellationToken"><para>zh-cn:异步取消令牌</para><para>en-us:The cancellation token.</para></param>
    /// <returns><para>zh-cn:数据库中的实体</para><para>en-us:The operation result.</para></returns>
    Task<TEntity?> LastOrDefaultAsync(bool? tracking = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// <para>zh-cn:根据表达式查询一条记录</para>
    /// <para>en-us:Describes the behavior provided by this member.</para>
    /// </summary>
    /// <param name="predicate"><para>zh-cn:表达式</para><para>en-us:The query predicate.</para></param>
    /// <param name="tracking"><para>zh-cn:是否跟踪实体</para><para>en-us:Whether to track entities.</para></param>
    /// <param name="cancellationToken"><para>zh-cn:异步取消令牌</para><para>en-us:The cancellation token.</para></param>
    /// <returns><para>zh-cn:数据库中的实体</para><para>en-us:The operation result.</para></returns>
    Task<TEntity?> LastOrDefaultAsync(Expression<Func<TEntity, bool>> predicate, bool? tracking = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// <para>zh-cn:根据表达式查询多条记录</para>
    /// <para>en-us:Describes the behavior provided by this member.</para>
    /// </summary>
    /// <param name="predicate"><para>zh-cn:表达式</para><para>en-us:The query predicate.</para></param>
    /// <param name="tracking"><para>zh-cn:是否跟踪实体</para><para>en-us:Whether to track entities.</para></param>
    /// <param name="ignoreQueryFilters"><para>zh-cn:是否忽略查询过滤器</para><para>en-us:Whether to ignore query filters.</para></param>
    /// <returns><para>zh-cn:数据库中的多个实体</para><para>en-us:The operation result.</para></returns>
    IQueryable<TEntity> Where(Expression<Func<TEntity, bool>> predicate, bool? tracking = null, bool ignoreQueryFilters = false);

    /// <summary>
    /// <para>zh-cn:根据表达式查询多条记录</para>
    /// <para>en-us:Describes the behavior provided by this member.</para>
    /// </summary>
    /// <param name="predicate"><para>zh-cn:表达式</para><para>en-us:The query predicate.</para></param>
    /// <param name="tracking"><para>zh-cn:是否跟踪实体</para><para>en-us:Whether to track entities.</para></param>
    /// <param name="ignoreQueryFilters"><para>zh-cn:是否忽略查询过滤器</para><para>en-us:Whether to ignore query filters.</para></param>
    /// <returns><para>zh-cn:数据库中的多个实体</para><para>en-us:The operation result.</para></returns>
    IQueryable<TEntity> Where(Expression<Func<TEntity, int, bool>> predicate, bool? tracking = null, bool ignoreQueryFilters = false);

    /// <summary>
    /// <para>zh-cn:根据条件执行表达式查询多条记录</para>
    /// <para>en-us:Describes the behavior provided by this member.</para>
    /// </summary>
    /// <param name="condition"><para>zh-cn:条件</para><para>en-us:The condition value.</para></param>
    /// <param name="predicate"><para>zh-cn:表达式</para><para>en-us:The query predicate.</para></param>
    /// <param name="tracking"><para>zh-cn:是否跟踪实体</para><para>en-us:Whether to track entities.</para></param>
    /// <param name="ignoreQueryFilters"><para>zh-cn:是否忽略查询过滤器</para><para>en-us:Whether to ignore query filters.</para></param>
    /// <returns><para>zh-cn:数据库中的多个实体</para><para>en-us:The operation result.</para></returns>
    IQueryable<TEntity> Where(bool condition, Expression<Func<TEntity, bool>> predicate, bool? tracking = null, bool ignoreQueryFilters = false);

    /// <summary>
    /// <para>zh-cn:根据条件执行表达式查询多条记录</para>
    /// <para>en-us:Describes the behavior provided by this member.</para>
    /// </summary>
    /// <param name="condition"><para>zh-cn:条件</para><para>en-us:The condition value.</para></param>
    /// <param name="predicate"><para>zh-cn:表达式</para><para>en-us:The query predicate.</para></param>
    /// <param name="tracking"><para>zh-cn:是否跟踪实体</para><para>en-us:Whether to track entities.</para></param>
    /// <param name="ignoreQueryFilters"><para>zh-cn:是否忽略查询过滤器</para><para>en-us:Whether to ignore query filters.</para></param>
    /// <returns><para>zh-cn:数据库中的多个实体</para><para>en-us:The operation result.</para></returns>
    IQueryable<TEntity> Where(bool condition, Expression<Func<TEntity, int, bool>> predicate, bool? tracking = null, bool ignoreQueryFilters = false);

    /// <summary>
    /// <para>zh-cn:根据条件执行表达式查询多条记录</para>
    /// <para>en-us:Describes the behavior provided by this member.</para>
    /// </summary>
    /// <param name="predicates"><para>zh-cn:表达式集合</para><para>en-us:The query predicates.</para></param>
    /// <returns><para>zh-cn:数据库中的多个实体</para><para>en-us:The operation result.</para></returns>
    IQueryable<TEntity> Where(params Expression<Func<TEntity, bool>>[] predicates);

    /// <summary>
    /// <para>zh-cn:根据条件执行表达式查询多条记录</para>
    /// <para>en-us:Describes the behavior provided by this member.</para>
    /// </summary>
    /// <param name="predicates"><para>zh-cn:表达式集合</para><para>en-us:The query predicates.</para></param>
    /// <param name="tracking"><para>zh-cn:是否跟踪实体</para><para>en-us:Whether to track entities.</para></param>
    /// <param name="ignoreQueryFilters"><para>zh-cn:是否忽略查询过滤器</para><para>en-us:Whether to ignore query filters.</para></param>
    /// <returns><para>zh-cn:数据库中的多个实体</para><para>en-us:The operation result.</para></returns>
    IQueryable<TEntity> Where(Expression<Func<TEntity, bool>>[] predicates, bool? tracking = null, bool ignoreQueryFilters = false);

    /// <summary>
    /// <para>zh-cn:根据条件执行表达式查询多条记录</para>
    /// <para>en-us:Describes the behavior provided by this member.</para>
    /// </summary>
    /// <param name="predicates"><para>zh-cn:表达式集合</para><para>en-us:The query predicates.</para></param>
    /// <returns><para>zh-cn:数据库中的多个实体</para><para>en-us:The operation result.</para></returns>
    IQueryable<TEntity> Where(params Expression<Func<TEntity, int, bool>>[] predicates);

    /// <summary>
    /// <para>zh-cn:根据条件执行表达式查询多条记录</para>
    /// <para>en-us:Describes the behavior provided by this member.</para>
    /// </summary>
    /// <param name="predicates"><para>zh-cn:表达式集合</para><para>en-us:The query predicates.</para></param>
    /// <param name="tracking"><para>zh-cn:是否跟踪实体</para><para>en-us:Whether to track entities.</para></param>
    /// <param name="ignoreQueryFilters"><para>zh-cn:是否忽略查询过滤器</para><para>en-us:Whether to ignore query filters.</para></param>
    /// <returns><para>zh-cn:数据库中的多个实体</para><para>en-us:The operation result.</para></returns>
    IQueryable<TEntity> Where(Expression<Func<TEntity, int, bool>>[] predicates, bool? tracking = null, bool ignoreQueryFilters = false);

    /// <summary>
    /// <para>zh-cn:根据条件执行表达式查询多条记录</para>
    /// <para>en-us:Describes the behavior provided by this member.</para>
    /// </summary>
    /// <param name="conditionPredicates"><para>zh-cn:条件表达式集合</para><para>en-us:The conditionPredicates value.</para></param>
    /// <returns><para>zh-cn:数据库中的多个实体</para><para>en-us:The operation result.</para></returns>
    IQueryable<TEntity> Where(params (bool condition, Expression<Func<TEntity, bool>> expression)[] conditionPredicates);

    /// <summary>
    /// <para>zh-cn:根据条件执行表达式查询多条记录</para>
    /// <para>en-us:Describes the behavior provided by this member.</para>
    /// </summary>
    /// <param name="conditionPredicates"><para>zh-cn:条件表达式集合</para><para>en-us:The conditionPredicates value.</para></param>
    /// <param name="tracking"><para>zh-cn:是否跟踪实体</para><para>en-us:Whether to track entities.</para></param>
    /// <param name="ignoreQueryFilters"><para>zh-cn:是否忽略查询过滤器</para><para>en-us:Whether to ignore query filters.</para></param>
    /// <returns><para>zh-cn:数据库中的多个实体</para><para>en-us:The operation result.</para></returns>
    IQueryable<TEntity> Where((bool condition, Expression<Func<TEntity, bool>> expression)[] conditionPredicates, bool? tracking = null, bool ignoreQueryFilters = false);

    /// <summary>
    /// <para>zh-cn:根据条件执行表达式查询多条记录</para>
    /// <para>en-us:Describes the behavior provided by this member.</para>
    /// </summary>
    /// <param name="conditionPredicates"><para>zh-cn:条件表达式集合</para><para>en-us:The conditionPredicates value.</para></param>
    /// <returns><para>zh-cn:数据库中的多个实体</para><para>en-us:The operation result.</para></returns>
    IQueryable<TEntity> Where(params (bool condition, Expression<Func<TEntity, int, bool>> expression)[] conditionPredicates);

    /// <summary>
    /// <para>zh-cn:根据条件执行表达式查询多条记录</para>
    /// <para>en-us:Describes the behavior provided by this member.</para>
    /// </summary>
    /// <param name="conditionPredicates"><para>zh-cn:条件表达式集合</para><para>en-us:The conditionPredicates value.</para></param>
    /// <param name="tracking"><para>zh-cn:是否跟踪实体</para><para>en-us:Whether to track entities.</para></param>
    /// <param name="ignoreQueryFilters"><para>zh-cn:是否忽略查询过滤器</para><para>en-us:Whether to ignore query filters.</para></param>
    /// <returns><para>zh-cn:数据库中的多个实体</para><para>en-us:The operation result.</para></returns>
    IQueryable<TEntity> Where((bool condition, Expression<Func<TEntity, int, bool>> expression)[] conditionPredicates, bool? tracking = null, bool ignoreQueryFilters = false);

    /// <summary>
    /// <para>zh-cn:加载关联数据</para>
    /// <para>en-us:Describes the behavior provided by this member.</para>
    /// </summary>
    /// <param name="predicate"><para>zh-cn:表达式</para><para>en-us:The query predicate.</para></param>
    /// <param name="tracking"><para>zh-cn:是否跟踪实体</para><para>en-us:Whether to track entities.</para></param>
    /// <param name="ignoreQueryFilters"><para>zh-cn:是否忽略查询过滤器</para><para>en-us:Whether to ignore query filters.</para></param>
    /// <returns><para>zh-cn:数据库中的多个实体</para><para>en-us:The operation result.</para></returns>
    IIncludableQueryable<TEntity, TProperty> Include<TProperty>(Expression<Func<TEntity, TProperty>> predicate, bool? tracking = null, bool ignoreQueryFilters = false);
    /// <summary>
    /// <para>zh-cn:判断记录是否存在</para>
    /// <para>en-us:Describes the behavior provided by this member.</para>
    /// </summary>
    /// <param name="tracking"><para>zh-cn:是否跟踪实体</para><para>en-us:Whether to track entities.</para></param>
    /// <param name="ignoreQueryFilters"><para>zh-cn:是否忽略查询过滤器</para><para>en-us:Whether to ignore query filters.</para></param>
    /// <returns><para>zh-cn:bool</para><para>en-us:The operation result.</para></returns>
    bool Any(bool? tracking = null, bool ignoreQueryFilters = false);

    /// <summary>
    /// <para>zh-cn:根据表达式判断记录是否存在</para>
    /// <para>en-us:Describes the behavior provided by this member.</para>
    /// </summary>
    /// <param name="predicate"><para>zh-cn:表达式</para><para>en-us:The query predicate.</para></param>
    /// <param name="tracking"><para>zh-cn:是否跟踪实体</para><para>en-us:Whether to track entities.</para></param>
    /// <param name="ignoreQueryFilters"><para>zh-cn:是否忽略查询过滤器</para><para>en-us:Whether to ignore query filters.</para></param>
    /// <returns><para>zh-cn:bool</para><para>en-us:The operation result.</para></returns>
    bool Any(Expression<Func<TEntity, bool>> predicate, bool? tracking = null, bool ignoreQueryFilters = false);

    /// <summary>
    /// <para>zh-cn:判断记录是否存在</para>
    /// <para>en-us:Describes the behavior provided by this member.</para>
    /// </summary>
    /// <param name="tracking"><para>zh-cn:是否跟踪实体</para><para>en-us:Whether to track entities.</para></param>
    /// <param name="ignoreQueryFilters"><para>zh-cn:是否忽略查询过滤器</para><para>en-us:Whether to ignore query filters.</para></param>
    /// <param name="cancellationToken"><para>zh-cn:取消异步令牌</para><para>en-us:The cancellation token.</para></param>
    /// <returns><para>zh-cn:bool</para><para>en-us:The operation result.</para></returns>
    Task<bool> AnyAsync(bool? tracking = null, bool ignoreQueryFilters = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// <para>zh-cn:根据表达式判断记录是否存在</para>
    /// <para>en-us:Describes the behavior provided by this member.</para>
    /// </summary>
    /// <param name="predicate"><para>zh-cn:表达式</para><para>en-us:The query predicate.</para></param>
    /// <param name="tracking"><para>zh-cn:是否跟踪实体</para><para>en-us:Whether to track entities.</para></param>
    /// <param name="ignoreQueryFilters"><para>zh-cn:是否忽略查询过滤器</para><para>en-us:Whether to ignore query filters.</para></param>
    /// <param name="cancellationToken"><para>zh-cn:取消异步令牌</para><para>en-us:The cancellation token.</para></param>
    /// <returns><para>zh-cn:bool</para><para>en-us:The operation result.</para></returns>
    Task<bool> AnyAsync(Expression<Func<TEntity, bool>> predicate, bool? tracking = null, bool ignoreQueryFilters = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// <para>zh-cn:根据表达式判断记录是否全部满足条件</para>
    /// <para>en-us:Describes the behavior provided by this member.</para>
    /// </summary>
    /// <param name="predicate"><para>zh-cn:表达式</para><para>en-us:The query predicate.</para></param>
    /// <param name="tracking"><para>zh-cn:是否跟踪实体</para><para>en-us:Whether to track entities.</para></param>
    /// <param name="ignoreQueryFilters"><para>zh-cn:是否忽略查询过滤器</para><para>en-us:Whether to ignore query filters.</para></param>
    /// <returns><para>zh-cn:bool</para><para>en-us:The operation result.</para></returns>
    bool All(Expression<Func<TEntity, bool>> predicate, bool? tracking = null, bool ignoreQueryFilters = false);

    /// <summary>
    /// <para>zh-cn:根据表达式判断记录是否全部满足条件</para>
    /// <para>en-us:Describes the behavior provided by this member.</para>
    /// </summary>
    /// <param name="predicate"><para>zh-cn:表达式</para><para>en-us:The query predicate.</para></param>
    /// <param name="tracking"><para>zh-cn:是否跟踪实体</para><para>en-us:Whether to track entities.</para></param>
    /// <param name="ignoreQueryFilters"><para>zh-cn:是否忽略查询过滤器</para><para>en-us:Whether to ignore query filters.</para></param>
    /// <param name="cancellationToken"><para>zh-cn:取消异步令牌</para><para>en-us:The cancellation token.</para></param>
    /// <returns><para>zh-cn:bool</para><para>en-us:The operation result.</para></returns>
    Task<bool> AllAsync(Expression<Func<TEntity, bool>> predicate, bool? tracking = null, bool ignoreQueryFilters = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// <para>zh-cn:查看记录条数</para>
    /// <para>en-us:Describes the behavior provided by this member.</para>
    /// </summary>
    /// <param name="tracking"><para>zh-cn:是否跟踪实体</para><para>en-us:Whether to track entities.</para></param>
    /// <param name="ignoreQueryFilters"><para>zh-cn:是否忽略查询过滤器</para><para>en-us:Whether to ignore query filters.</para></param>
    /// <returns><para>zh-cn:int</para><para>en-us:The operation result.</para></returns>
    int Count(bool? tracking = null, bool ignoreQueryFilters = false);

    /// <summary>
    /// <para>zh-cn:根据表达式查询记录条数</para>
    /// <para>en-us:Describes the behavior provided by this member.</para>
    /// </summary>
    /// <param name="predicate"><para>zh-cn:表达式</para><para>en-us:The query predicate.</para></param>
    /// <param name="tracking"><para>zh-cn:是否跟踪实体</para><para>en-us:Whether to track entities.</para></param>
    /// <param name="ignoreQueryFilters"><para>zh-cn:是否忽略查询过滤器</para><para>en-us:Whether to ignore query filters.</para></param>
    /// <returns><para>zh-cn:int</para><para>en-us:The operation result.</para></returns>
    int Count(Expression<Func<TEntity, bool>> predicate, bool? tracking = null, bool ignoreQueryFilters = false);

    /// <summary>
    /// <para>zh-cn:查看记录条数</para>
    /// <para>en-us:Describes the behavior provided by this member.</para>
    /// </summary>
    /// <param name="tracking"><para>zh-cn:是否跟踪实体</para><para>en-us:Whether to track entities.</para></param>
    /// <param name="ignoreQueryFilters"><para>zh-cn:是否忽略查询过滤器</para><para>en-us:Whether to ignore query filters.</para></param>
    /// <param name="cancellationToken"><para>zh-cn:取消异步令牌</para><para>en-us:The cancellation token.</para></param>
    /// <returns><para>zh-cn:int</para><para>en-us:The operation result.</para></returns>
    Task<int> CountAsync(bool? tracking = null, bool ignoreQueryFilters = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// <para>zh-cn:根据表达式查询记录条数</para>
    /// <para>en-us:Describes the behavior provided by this member.</para>
    /// </summary>
    /// <param name="predicate"><para>zh-cn:表达式</para><para>en-us:The query predicate.</para></param>
    /// <param name="tracking"><para>zh-cn:是否跟踪实体</para><para>en-us:Whether to track entities.</para></param>
    /// <param name="ignoreQueryFilters"><para>zh-cn:是否忽略查询过滤器</para><para>en-us:Whether to ignore query filters.</para></param>
    /// <param name="cancellationToken"><para>zh-cn:取消异步令牌</para><para>en-us:The cancellation token.</para></param>
    /// <returns><para>zh-cn:int</para><para>en-us:The operation result.</para></returns>
    Task<int> CountAsync(Expression<Func<TEntity, bool>> predicate, bool? tracking = null, bool ignoreQueryFilters = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// <para>zh-cn:查看最小记录</para>
    /// <para>en-us:Describes the behavior provided by this member.</para>
    /// </summary>
    /// <param name="tracking"><para>zh-cn:是否跟踪实体</para><para>en-us:Whether to track entities.</para></param>
    /// <param name="ignoreQueryFilters"><para>zh-cn:是否忽略查询过滤器</para><para>en-us:Whether to ignore query filters.</para></param>
    /// <returns><para>zh-cn:实体</para><para>en-us:The operation result.</para></returns>
    TEntity? Min(bool? tracking = null, bool ignoreQueryFilters = false);

    /// <summary>
    /// <para>zh-cn:根据表达式查看最小值</para>
    /// <para>en-us:Describes the behavior provided by this member.</para>
    /// </summary>
    /// <typeparam name="TResult"><para>zh-cn:最小值类型</para><para>en-us:The generic parameter type.</para></typeparam>
    /// <param name="predicate"><para>zh-cn:表达式</para><para>en-us:The query predicate.</para></param>
    /// <param name="tracking"><para>zh-cn:是否跟踪实体</para><para>en-us:Whether to track entities.</para></param>
    /// <param name="ignoreQueryFilters"><para>zh-cn:是否忽略查询过滤器</para><para>en-us:Whether to ignore query filters.</para></param>
    /// <returns><para>zh-cn:TResult</para><para>en-us:The operation result.</para></returns>
    TResult? Min<TResult>(Expression<Func<TEntity, TResult>> predicate, bool? tracking = null, bool ignoreQueryFilters = false);

    /// <summary>
    /// <para>zh-cn:查看最小记录</para>
    /// <para>en-us:Describes the behavior provided by this member.</para>
    /// </summary>
    /// <param name="tracking"><para>zh-cn:是否跟踪实体</para><para>en-us:Whether to track entities.</para></param>
    /// <param name="ignoreQueryFilters"><para>zh-cn:是否忽略查询过滤器</para><para>en-us:Whether to ignore query filters.</para></param>
    /// <param name="cancellationToken"><para>zh-cn:取消异步令牌</para><para>en-us:The cancellation token.</para></param>
    /// <returns><para>zh-cn:实体</para><para>en-us:The operation result.</para></returns>
    Task<TEntity> MinAsync(bool? tracking = null, bool ignoreQueryFilters = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// <para>zh-cn:根据表达式查看最小值</para>
    /// <para>en-us:Describes the behavior provided by this member.</para>
    /// </summary>
    /// <typeparam name="TResult"><para>zh-cn:最小值类型</para><para>en-us:The generic parameter type.</para></typeparam>
    /// <param name="predicate"><para>zh-cn:表达式</para><para>en-us:The query predicate.</para></param>
    /// <param name="tracking"><para>zh-cn:是否跟踪实体</para><para>en-us:Whether to track entities.</para></param>
    /// <param name="ignoreQueryFilters"><para>zh-cn:是否忽略查询过滤器</para><para>en-us:Whether to ignore query filters.</para></param>
    /// <param name="cancellationToken"><para>zh-cn:取消异步令牌</para><para>en-us:The cancellation token.</para></param>
    /// <returns><para>zh-cn:TResult</para><para>en-us:The operation result.</para></returns>
    Task<TResult> MinAsync<TResult>(Expression<Func<TEntity, TResult>> predicate, bool? tracking = null, bool ignoreQueryFilters = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// <para>zh-cn:查看最大记录</para>
    /// <para>en-us:Describes the behavior provided by this member.</para>
    /// </summary>
    /// <param name="tracking"><para>zh-cn:是否跟踪实体</para><para>en-us:Whether to track entities.</para></param>
    /// <param name="ignoreQueryFilters"><para>zh-cn:是否忽略查询过滤器</para><para>en-us:Whether to ignore query filters.</para></param>
    /// <returns><para>zh-cn:实体</para><para>en-us:The operation result.</para></returns>
    TEntity? Max(bool? tracking = null, bool ignoreQueryFilters = false);

    /// <summary>
    /// <para>zh-cn:根据表达式查看最大值</para>
    /// <para>en-us:Describes the behavior provided by this member.</para>
    /// </summary>
    /// <typeparam name="TResult"><para>zh-cn:最大值类型</para><para>en-us:The generic parameter type.</para></typeparam>
    /// <param name="predicate"><para>zh-cn:表达式</para><para>en-us:The query predicate.</para></param>
    /// <param name="tracking"><para>zh-cn:是否跟踪实体</para><para>en-us:Whether to track entities.</para></param>
    /// <param name="ignoreQueryFilters"><para>zh-cn:是否忽略查询过滤器</para><para>en-us:Whether to ignore query filters.</para></param>
    /// <returns><para>zh-cn:TResult</para><para>en-us:The operation result.</para></returns>
    TResult? Max<TResult>(Expression<Func<TEntity, TResult>> predicate, bool? tracking = null, bool ignoreQueryFilters = false);

    /// <summary>
    /// <para>zh-cn:查看最大记录</para>
    /// <para>en-us:Describes the behavior provided by this member.</para>
    /// </summary>
    /// <param name="tracking"><para>zh-cn:是否跟踪实体</para><para>en-us:Whether to track entities.</para></param>
    /// <param name="ignoreQueryFilters"><para>zh-cn:是否忽略查询过滤器</para><para>en-us:Whether to ignore query filters.</para></param>
    /// <param name="cancellationToken"><para>zh-cn:取消异步令牌</para><para>en-us:The cancellation token.</para></param>
    /// <returns><para>zh-cn:实体</para><para>en-us:The operation result.</para></returns>
    Task<TEntity> MaxAsync(bool? tracking = null, bool ignoreQueryFilters = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// <para>zh-cn:根据表达式查看最大值</para>
    /// <para>en-us:Describes the behavior provided by this member.</para>
    /// </summary>
    /// <typeparam name="TResult"><para>zh-cn:最大值类型</para><para>en-us:The generic parameter type.</para></typeparam>
    /// <param name="predicate"><para>zh-cn:表达式</para><para>en-us:The query predicate.</para></param>
    /// <param name="tracking"><para>zh-cn:是否跟踪实体</para><para>en-us:Whether to track entities.</para></param>
    /// <param name="ignoreQueryFilters"><para>zh-cn:是否忽略查询过滤器</para><para>en-us:Whether to ignore query filters.</para></param>
    /// <param name="cancellationToken"><para>zh-cn:取消异步令牌</para><para>en-us:The cancellation token.</para></param>
    /// <returns><para>zh-cn:TResult</para><para>en-us:The operation result.</para></returns>
    Task<TResult> MaxAsync<TResult>(Expression<Func<TEntity, TResult>> predicate, bool? tracking = null, bool ignoreQueryFilters = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// <para>zh-cn:构建查询分析器</para>
    /// <para>en-us:Describes the behavior provided by this member.</para>
    /// </summary>
    /// <param name="tracking"><para>zh-cn:是否跟踪实体</para><para>en-us:Whether to track entities.</para></param>
    /// <returns><para>zh-cn:IQueryable{TEntity}</para><para>en-us:The operation result.</para></returns>
    IQueryable<TEntity> AsQueryable(bool? tracking = null);

    /// <summary>
    /// <para>zh-cn:构建查询分析器</para>
    /// <para>en-us:Describes the behavior provided by this member.</para>
    /// </summary>
    /// <param name="predicate"><para>zh-cn:表达式</para><para>en-us:The query predicate.</para></param>
    /// <param name="tracking"><para>zh-cn:是否跟踪实体</para><para>en-us:Whether to track entities.</para></param>
    /// <param name="ignoreQueryFilters"><para>zh-cn:是否忽略查询过滤器</para><para>en-us:Whether to ignore query filters.</para></param>
    /// <returns><para>zh-cn:IQueryable{TEntity}</para><para>en-us:The operation result.</para></returns>
    IQueryable<TEntity> AsQueryable(Expression<Func<TEntity, bool>> predicate, bool? tracking = null, bool ignoreQueryFilters = false);

    /// <summary>
    /// <para>zh-cn:构建查询分析器</para>
    /// <para>en-us:Describes the behavior provided by this member.</para>
    /// </summary>
    /// <param name="predicate"><para>zh-cn:表达式</para><para>en-us:The query predicate.</para></param>
    /// <param name="tracking"><para>zh-cn:是否跟踪实体</para><para>en-us:Whether to track entities.</para></param>
    /// <param name="ignoreQueryFilters"><para>zh-cn:是否忽略查询过滤器</para><para>en-us:Whether to ignore query filters.</para></param>
    /// <returns><para>zh-cn:IQueryable{TEntity}</para><para>en-us:The operation result.</para></returns>
    IQueryable<TEntity> AsQueryable(Expression<Func<TEntity, int, bool>> predicate, bool? tracking = null, bool ignoreQueryFilters = false);

    /// <summary>
    /// <para>zh-cn:直接返回数据库结果</para>
    /// <para>en-us:Describes the behavior provided by this member.</para>
    /// </summary>
    /// <param name="tracking"><para>zh-cn:是否跟踪实体</para><para>en-us:Whether to track entities.</para></param>
    /// <returns><para>zh-cn:IEnumerable{TEntity}</para><para>en-us:The operation result.</para></returns>
    IEnumerable<TEntity> AsEnumerable(bool? tracking = null);

    /// <summary>
    /// <para>zh-cn:直接返回数据库结果</para>
    /// <para>en-us:Describes the behavior provided by this member.</para>
    /// </summary>
    /// <param name="predicate"><para>zh-cn:表达式</para><para>en-us:The query predicate.</para></param>
    /// <param name="tracking"><para>zh-cn:是否跟踪实体</para><para>en-us:Whether to track entities.</para></param>
    /// <param name="ignoreQueryFilters"><para>zh-cn:是否忽略查询过滤器</para><para>en-us:Whether to ignore query filters.</para></param>
    /// <returns><para>zh-cn:List{TEntity}</para><para>en-us:The operation result.</para></returns>
    IEnumerable<TEntity> AsEnumerable(Expression<Func<TEntity, bool>> predicate, bool? tracking = null, bool ignoreQueryFilters = false);

    /// <summary>
    /// <para>zh-cn:直接返回数据库结果</para>
    /// <para>en-us:Describes the behavior provided by this member.</para>
    /// </summary>
    /// <param name="predicate"><para>zh-cn:表达式</para><para>en-us:The query predicate.</para></param>
    /// <param name="tracking"><para>zh-cn:是否跟踪实体</para><para>en-us:Whether to track entities.</para></param>
    /// <param name="ignoreQueryFilters"><para>zh-cn:是否忽略查询过滤器</para><para>en-us:Whether to ignore query filters.</para></param>
    /// <returns><para>zh-cn:IEnumerable{TEntity}</para><para>en-us:The operation result.</para></returns>
    IEnumerable<TEntity> AsEnumerable(Expression<Func<TEntity, int, bool>> predicate, bool? tracking = null, bool ignoreQueryFilters = false);

    /// <summary>
    /// <para>zh-cn:直接返回数据库结果</para>
    /// <para>en-us:Describes the behavior provided by this member.</para>
    /// </summary>
    /// <param name="tracking"><para>zh-cn:是否跟踪实体</para><para>en-us:Whether to track entities.</para></param>
    /// <returns><para>zh-cn:List{TEntity}</para><para>en-us:The operation result.</para></returns>
    IAsyncEnumerable<TEntity> AsAsyncEnumerable(bool? tracking = null);

    /// <summary>
    /// <para>zh-cn:直接返回数据库结果</para>
    /// <para>en-us:Describes the behavior provided by this member.</para>
    /// </summary>
    /// <param name="predicate"><para>zh-cn:表达式</para><para>en-us:The query predicate.</para></param>
    /// <param name="tracking"><para>zh-cn:是否跟踪实体</para><para>en-us:Whether to track entities.</para></param>
    /// <param name="ignoreQueryFilters"><para>zh-cn:是否忽略查询过滤器</para><para>en-us:Whether to ignore query filters.</para></param>
    /// <returns><para>zh-cn:IAsyncEnumerable{TEntity}</para><para>en-us:The operation result.</para></returns>
    IAsyncEnumerable<TEntity> AsAsyncEnumerable(Expression<Func<TEntity, bool>> predicate, bool? tracking = null, bool ignoreQueryFilters = false);

    /// <summary>
    /// <para>zh-cn:直接返回数据库结果</para>
    /// <para>en-us:Describes the behavior provided by this member.</para>
    /// </summary>
    /// <param name="predicate"><para>zh-cn:表达式</para><para>en-us:The query predicate.</para></param>
    /// <param name="tracking"><para>zh-cn:是否跟踪实体</para><para>en-us:Whether to track entities.</para></param>
    /// <param name="ignoreQueryFilters"><para>zh-cn:是否忽略查询过滤器</para><para>en-us:Whether to ignore query filters.</para></param>
    /// <returns><para>zh-cn:IAsyncEnumerable{TEntity}</para><para>en-us:The operation result.</para></returns>
    IAsyncEnumerable<TEntity> AsAsyncEnumerable(Expression<Func<TEntity, int, bool>> predicate, bool? tracking = null, bool ignoreQueryFilters = false);

    /// <summary>
    /// <para>zh-cn:执行 Sql 返回 IQueryable</para>
    /// <para>en-us:Describes the behavior provided by this member.</para>
    /// </summary>
    /// <param name="sql"><para>zh-cn:sql 语句</para><para>en-us:The sql value.</para></param>
    /// <param name="parameters"><para>zh-cn:命令参数</para><para>en-us:The parameters value.</para></param>
    /// <returns><para>zh-cn:IQueryable{TEntity}</para><para>en-us:The operation result.</para></returns>
    IQueryable<TEntity> FromSqlRaw(string sql, params object[] parameters);

    /// <summary>
    /// <para>zh-cn:执行 Sql 返回 IQueryable</para>
    /// <para>en-us:Describes the behavior provided by this member.</para>
    /// </summary>
    /// <remarks>
    /// 支持字符串内插语法
    /// </remarks>
    /// <param name="sql"><para>zh-cn:sql 语句</para><para>en-us:The sql value.</para></param>
    /// <returns><para>zh-cn:IQueryable{TEntity}</para><para>en-us:The operation result.</para></returns>
    IQueryable<TEntity> FromSqlInterpolated(FormattableString sql);
}
