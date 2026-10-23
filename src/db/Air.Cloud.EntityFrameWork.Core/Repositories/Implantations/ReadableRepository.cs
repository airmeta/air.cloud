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
using Air.Cloud.Core.Extensions.IEnumerables;
using Air.Cloud.Core.Standard.DataBase.Model;
using Air.Cloud.EntityFrameWork.Core.Helpers;

using Microsoft.EntityFrameworkCore.Query;

using System.Linq.Expressions;

namespace Air.Cloud.EntityFrameWork.Core.Repositories.Implantations;

/// <summary>
/// <para>zh-cn:可读仓储实现。</para>
/// <para>en-us:Provides the corresponding database repository operation.</para>
/// </summary>
public partial class PrivateRepository<TEntity>
    where TEntity : class, IPrivateEntity, new()
{
    /// <summary>
    /// <para>zh-cn:根据键查询一条记录</para>
    /// <para>en-us:Provides the corresponding database repository operation.</para>
    /// </summary>
    /// <param name="key"><para>zh-cn:键</para><para>en-us:The key value.</para></param>
    /// <returns><para>zh-cn:数据库中的实体</para><para>en-us:The operation result.</para></returns>
    public virtual TEntity Find(object key)
    {
        var entity = FindOrDefault(key) ?? throw RepositoryExceptionHelpers.DataNotFoundException();
        return entity;
    }

    /// <summary>
    /// <para>zh-cn:根据多个键查询一条记录</para>
    /// <para>en-us:Provides the corresponding database repository operation.</para>
    /// </summary>
    /// <param name="keyValues"><para>zh-cn:多个键</para><para>en-us:The key values.</para></param>
    /// <returns><para>zh-cn:数据库中的实体</para><para>en-us:The operation result.</para></returns>
    public virtual TEntity Find(params object[] keyValues)
    {
        var entity = FindOrDefault(keyValues) ?? throw RepositoryExceptionHelpers.DataNotFoundException();
        return entity;
    }

    /// <summary>
    /// <para>zh-cn:根据键查询一条记录</para>
    /// <para>en-us:Provides the corresponding database repository operation.</para>
    /// </summary>
    /// <param name="key"><para>zh-cn:键</para><para>en-us:The key value.</para></param>
    /// <param name="cancellationToken"><para>zh-cn:取消异步令牌</para><para>en-us:The cancellation token.</para></param>
    /// <returns><para>zh-cn:数据库实体</para><para>en-us:The operation result.</para></returns>
    public virtual async Task<TEntity> FindAsync(object key, CancellationToken cancellationToken = default)
    {
        var entity = await FindOrDefaultAsync(key, cancellationToken);
        return entity ?? throw RepositoryExceptionHelpers.DataNotFoundException();
    }

    /// <summary>
    /// <para>zh-cn:根据多个键查询一条记录</para>
    /// <para>en-us:Provides the corresponding database repository operation.</para>
    /// </summary>
    /// <param name="keyValues"><para>zh-cn:多个键</para><para>en-us:The key values.</para></param>
    /// <returns><para>zh-cn:数据库中的实体</para><para>en-us:The operation result.</para></returns>
    public virtual async Task<TEntity> FindAsync(params object[] keyValues)
    {
        var entity = await FindOrDefaultAsync(keyValues);
        return entity ?? throw RepositoryExceptionHelpers.DataNotFoundException();
    }

    /// <summary>
    /// <para>zh-cn:根据多个键查询一条记录</para>
    /// <para>en-us:Provides the corresponding database repository operation.</para>
    /// </summary>
    /// <param name="keyValues"><para>zh-cn:多个键</para><para>en-us:The key values.</para></param>
    /// <param name="cancellationToken"><para>zh-cn:取消异步令牌</para><para>en-us:The cancellation token.</para></param>
    /// <returns><para>zh-cn:数据库中的实体</para><para>en-us:The operation result.</para></returns>
    public virtual async Task<TEntity> FindAsync(object[] keyValues, CancellationToken cancellationToken = default)
    {
        var entity = await FindOrDefaultAsync(keyValues, cancellationToken);
        return entity ?? throw RepositoryExceptionHelpers.DataNotFoundException();
    }

    /// <summary>
    /// <para>zh-cn:根据键查询一条记录</para>
    /// <para>en-us:Provides the corresponding database repository operation.</para>
    /// </summary>
    /// <param name="key"><para>zh-cn:键</para><para>en-us:The key value.</para></param>
    /// <returns><para>zh-cn:数据库中的实体</para><para>en-us:The operation result.</para></returns>
    public virtual TEntity? FindOrDefault(object key)
    {
        return Entities.Find(key);
    }

    /// <summary>
    /// <para>zh-cn:根据多个键查询一条记录</para>
    /// <para>en-us:Provides the corresponding database repository operation.</para>
    /// </summary>
    /// <param name="keyValues"><para>zh-cn:多个键</para><para>en-us:The key values.</para></param>
    /// <returns><para>zh-cn:数据库中的实体</para><para>en-us:The operation result.</para></returns>
    public virtual TEntity? FindOrDefault(params object[] keyValues)
    {
        return Entities.Find(keyValues);
    }

    /// <summary>
    /// <para>zh-cn:根据键查询一条记录</para>
    /// <para>en-us:Provides the corresponding database repository operation.</para>
    /// </summary>
    /// <param name="key"><para>zh-cn:键</para><para>en-us:The key value.</para></param>
    /// <param name="cancellationToken"><para>zh-cn:取消异步令牌</para><para>en-us:The cancellation token.</para></param>
    /// <returns><para>zh-cn:数据库中的实体</para><para>en-us:The operation result.</para></returns>
    public virtual async Task<TEntity?> FindOrDefaultAsync(object key, CancellationToken cancellationToken = default)
    {
        var entity = await Entities.FindAsync(new object[] { key }, cancellationToken);
        return entity;
    }

    /// <summary>
    /// <para>zh-cn:根据多个键查询一条记录</para>
    /// <para>en-us:Provides the corresponding database repository operation.</para>
    /// </summary>
    /// <param name="keyValues"><para>zh-cn:多个键</para><para>en-us:The key values.</para></param>
    /// <returns><para>zh-cn:数据库中的实体</para><para>en-us:The operation result.</para></returns>
    public virtual async Task<TEntity?> FindOrDefaultAsync(params object[] keyValues)
    {
        var entity = await Entities.FindAsync(keyValues);
        return entity;
    }

    /// <summary>
    /// <para>zh-cn:根据多个键查询一条记录</para>
    /// <para>en-us:Provides the corresponding database repository operation.</para>
    /// </summary>
    /// <param name="keyValues"><para>zh-cn:多个键</para><para>en-us:The key values.</para></param>
    /// <param name="cancellationToken"><para>zh-cn:取消异步令牌</para><para>en-us:The cancellation token.</para></param>
    /// <returns><para>zh-cn:数据库中的实体</para><para>en-us:The operation result.</para></returns>
    public virtual async Task<TEntity?> FindOrDefaultAsync(object[] keyValues, CancellationToken cancellationToken = default)
    {
        var entity = await Entities.FindAsync(keyValues, cancellationToken);
        return entity;
    }

    /// <summary>
    /// <para>zh-cn:查询一条记录</para>
    /// <para>en-us:Provides the corresponding database repository operation.</para>
    /// </summary>
    /// <param name="tracking"><para>zh-cn:是否跟踪实体</para><para>en-us:Whether to track entities.</para></param>
    /// <returns><para>zh-cn:数据库中的实体</para><para>en-us:The operation result.</para></returns>
    public virtual TEntity Single(bool? tracking = null)
    {
        return AsQueryable(tracking).Single();
    }

    /// <summary>
    /// <para>zh-cn:根据表达式查询一条记录</para>
    /// <para>en-us:Provides the corresponding database repository operation.</para>
    /// </summary>
    /// <param name="predicate"><para>zh-cn:表达式</para><para>en-us:The query predicate.</para></param>
    /// <param name="tracking"><para>zh-cn:是否跟踪实体</para><para>en-us:Whether to track entities.</para></param>
    /// <returns><para>zh-cn:数据库中的实体</para><para>en-us:The operation result.</para></returns>
    public virtual TEntity Single(Expression<Func<TEntity, bool>> predicate, bool? tracking = null)
    {
        return AsQueryable(tracking).Single(predicate);
    }

    /// <summary>
    /// <para>zh-cn:查询一条记录</para>
    /// <para>en-us:Provides the corresponding database repository operation.</para>
    /// </summary>
    /// <param name="tracking"><para>zh-cn:是否跟踪实体</para><para>en-us:Whether to track entities.</para></param>
    /// <returns><para>zh-cn:数据库中的实体</para><para>en-us:The operation result.</para></returns>
    public virtual TEntity? SingleOrDefault(bool? tracking = null)
    {
        return AsQueryable(tracking).SingleOrDefault();
    }

    /// <summary>
    /// <para>zh-cn:根据表达式查询一条记录</para>
    /// <para>en-us:Provides the corresponding database repository operation.</para>
    /// </summary>
    /// <param name="predicate"><para>zh-cn:表达式</para><para>en-us:The query predicate.</para></param>
    /// <param name="tracking"><para>zh-cn:是否跟踪实体</para><para>en-us:Whether to track entities.</para></param>
    /// <returns><para>zh-cn:数据库中的实体</para><para>en-us:The operation result.</para></returns>
    public virtual TEntity? SingleOrDefault(Expression<Func<TEntity, bool>> predicate, bool? tracking = null)
    {
        return AsQueryable(tracking).SingleOrDefault(predicate);
    }

    /// <summary>
    /// <para>zh-cn:查询一条记录</para>
    /// <para>en-us:Provides the corresponding database repository operation.</para>
    /// </summary>
    /// <param name="tracking"><para>zh-cn:是否跟踪实体</para><para>en-us:Whether to track entities.</para></param>
    /// <param name="cancellationToken"><para>zh-cn:取消异步令牌</para><para>en-us:The cancellation token.</para></param>
    /// <returns><para>zh-cn:数据库中的实体</para><para>en-us:The operation result.</para></returns>
    public virtual Task<TEntity> SingleAsync(bool? tracking = null, CancellationToken cancellationToken = default)
    {
        return AsQueryable(tracking).SingleAsync(cancellationToken);
    }

    /// <summary>
    /// <para>zh-cn:根据表达式查询一条记录</para>
    /// <para>en-us:Provides the corresponding database repository operation.</para>
    /// </summary>
    /// <param name="predicate"><para>zh-cn:表达式</para><para>en-us:The query predicate.</para></param>
    /// <param name="tracking"><para>zh-cn:是否跟踪实体</para><para>en-us:Whether to track entities.</para></param>
    /// <param name="cancellationToken"><para>zh-cn:取消异步令牌</para><para>en-us:The cancellation token.</para></param>
    /// <returns><para>zh-cn:数据库中的实体</para><para>en-us:The operation result.</para></returns>
    public virtual Task<TEntity> SingleAsync(Expression<Func<TEntity, bool>> predicate, bool? tracking = null, CancellationToken cancellationToken = default)
    {
        return AsQueryable(tracking).SingleAsync(predicate, cancellationToken);
    }

    /// <summary>
    /// <para>zh-cn:查询一条记录</para>
    /// <para>en-us:Provides the corresponding database repository operation.</para>
    /// </summary>
    /// <param name="tracking"><para>zh-cn:是否跟踪实体</para><para>en-us:Whether to track entities.</para></param>
    /// <returns><para>zh-cn:数据库中的实体</para><para>en-us:The operation result.</para></returns>
    /// <param name="cancellationToken"><para>zh-cn:异步取消令牌</para><para>en-us:The cancellation token.</para></param>
    public virtual Task<TEntity?> SingleOrDefaultAsync(bool? tracking = null, CancellationToken cancellationToken = default)
    {
        return AsQueryable(tracking).SingleOrDefaultAsync(cancellationToken);
    }

    /// <summary>
    /// <para>zh-cn:根据表达式查询一条记录</para>
    /// <para>en-us:Provides the corresponding database repository operation.</para>
    /// </summary>
    /// <param name="predicate"><para>zh-cn:表达式</para><para>en-us:The query predicate.</para></param>
    /// <param name="tracking"><para>zh-cn:是否跟踪实体</para><para>en-us:Whether to track entities.</para></param>
    /// <param name="cancellationToken"><para>zh-cn:异步取消令牌</para><para>en-us:The cancellation token.</para></param>
    /// <returns><para>zh-cn:数据库中的实体</para><para>en-us:The operation result.</para></returns>
    public virtual Task<TEntity?> SingleOrDefaultAsync(Expression<Func<TEntity, bool>> predicate, bool? tracking = null, CancellationToken cancellationToken = default)
    {
        return AsQueryable(tracking).SingleOrDefaultAsync(predicate, cancellationToken);
    }

    /// <summary>
    /// <para>zh-cn:查询一条记录</para>
    /// <para>en-us:Provides the corresponding database repository operation.</para>
    /// </summary>
    /// <param name="tracking"><para>zh-cn:是否跟踪实体</para><para>en-us:Whether to track entities.</para></param>
    /// <returns><para>zh-cn:数据库中的实体</para><para>en-us:The operation result.</para></returns>
    public virtual TEntity First(bool? tracking = null)
    {
        return AsQueryable(tracking).First();
    }

    /// <summary>
    /// <para>zh-cn:根据表达式查询一条记录</para>
    /// <para>en-us:Provides the corresponding database repository operation.</para>
    /// </summary>
    /// <param name="predicate"><para>zh-cn:表达式</para><para>en-us:The query predicate.</para></param>
    /// <param name="tracking"><para>zh-cn:是否跟踪实体</para><para>en-us:Whether to track entities.</para></param>
    /// <returns><para>zh-cn:数据库中的实体</para><para>en-us:The operation result.</para></returns>
    public virtual TEntity First(Expression<Func<TEntity, bool>> predicate, bool? tracking = null)
    {
        return AsQueryable(tracking).First(predicate);
    }

    /// <summary>
    /// <para>zh-cn:查询一条记录</para>
    /// <para>en-us:Provides the corresponding database repository operation.</para>
    /// </summary>
    /// <param name="tracking"><para>zh-cn:是否跟踪实体</para><para>en-us:Whether to track entities.</para></param>
    /// <returns><para>zh-cn:数据库中的实体</para><para>en-us:The operation result.</para></returns>
    public virtual TEntity? FirstOrDefault(bool? tracking = null)
    {
        return AsQueryable(tracking).FirstOrDefault();
    }

    /// <summary>
    /// <para>zh-cn:根据表达式查询一条记录</para>
    /// <para>en-us:Provides the corresponding database repository operation.</para>
    /// </summary>
    /// <param name="predicate"><para>zh-cn:表达式</para><para>en-us:The query predicate.</para></param>
    /// <param name="tracking"><para>zh-cn:是否跟踪实体</para><para>en-us:Whether to track entities.</para></param>
    /// <returns><para>zh-cn:数据库中的实体</para><para>en-us:The operation result.</para></returns>
    public virtual TEntity? FirstOrDefault(Expression<Func<TEntity, bool>> predicate, bool? tracking = null)
    {
        return AsQueryable(tracking).FirstOrDefault(predicate);
    }

    /// <summary>
    /// <para>zh-cn:查询一条记录</para>
    /// <para>en-us:Provides the corresponding database repository operation.</para>
    /// </summary>
    /// <param name="tracking"><para>zh-cn:是否跟踪实体</para><para>en-us:Whether to track entities.</para></param>
    /// <param name="cancellationToken"><para>zh-cn:取消异步令牌</para><para>en-us:The cancellation token.</para></param>
    /// <returns><para>zh-cn:数据库中的实体</para><para>en-us:The operation result.</para></returns>
    public virtual Task<TEntity> FirstAsync(bool? tracking = null, CancellationToken cancellationToken = default)
    {
        return AsQueryable(tracking).FirstAsync(cancellationToken);
    }

    /// <summary>
    /// <para>zh-cn:根据表达式查询一条记录</para>
    /// <para>en-us:Provides the corresponding database repository operation.</para>
    /// </summary>
    /// <param name="predicate"><para>zh-cn:表达式</para><para>en-us:The query predicate.</para></param>
    /// <param name="tracking"><para>zh-cn:是否跟踪实体</para><para>en-us:Whether to track entities.</para></param>
    /// <param name="cancellationToken"><para>zh-cn:取消异步令牌</para><para>en-us:The cancellation token.</para></param>
    /// <returns><para>zh-cn:数据库中的实体</para><para>en-us:The operation result.</para></returns>
    public virtual Task<TEntity> FirstAsync(Expression<Func<TEntity, bool>> predicate, bool? tracking = null, CancellationToken cancellationToken = default)
    {
        return AsQueryable(tracking).FirstAsync(predicate, cancellationToken);
    }

    /// <summary>
    /// <para>zh-cn:查询一条记录</para>
    /// <para>en-us:Provides the corresponding database repository operation.</para>
    /// </summary>
    /// <param name="tracking"><para>zh-cn:是否跟踪实体</para><para>en-us:Whether to track entities.</para></param>
    /// <param name="cancellationToken"><para>zh-cn:异步取消令牌</para><para>en-us:The cancellation token.</para></param>
    /// <returns><para>zh-cn:数据库中的实体</para><para>en-us:The operation result.</para></returns>
    public virtual Task<TEntity?> FirstOrDefaultAsync(bool? tracking = null, CancellationToken cancellationToken = default)
    {
        return AsQueryable(tracking).FirstOrDefaultAsync(cancellationToken);
    }

    /// <summary>
    /// <para>zh-cn:根据表达式查询一条记录</para>
    /// <para>en-us:Provides the corresponding database repository operation.</para>
    /// </summary>
    /// <param name="predicate"><para>zh-cn:表达式</para><para>en-us:The query predicate.</para></param>
    /// <param name="tracking"><para>zh-cn:是否跟踪实体</para><para>en-us:Whether to track entities.</para></param>
    /// <param name="cancellationToken"><para>zh-cn:异步取消令牌</para><para>en-us:The cancellation token.</para></param>
    /// <returns><para>zh-cn:数据库中的实体</para><para>en-us:The operation result.</para></returns>
    public virtual Task<TEntity?> FirstOrDefaultAsync(Expression<Func<TEntity, bool>> predicate, bool? tracking = null, CancellationToken cancellationToken = default)
    {
        return AsQueryable(tracking).FirstOrDefaultAsync(predicate, cancellationToken);
    }

    /// <summary>
    /// <para>zh-cn:查询一条记录</para>
    /// <para>en-us:Provides the corresponding database repository operation.</para>
    /// </summary>
    /// <param name="tracking"><para>zh-cn:是否跟踪实体</para><para>en-us:Whether to track entities.</para></param>
    /// <returns><para>zh-cn:数据库中的实体</para><para>en-us:The operation result.</para></returns>
    public virtual TEntity Last(bool? tracking = null)
    {
        return AsQueryable(tracking).Last();
    }

    /// <summary>
    /// <para>zh-cn:根据表达式查询一条记录</para>
    /// <para>en-us:Provides the corresponding database repository operation.</para>
    /// </summary>
    /// <param name="predicate"><para>zh-cn:表达式</para><para>en-us:The query predicate.</para></param>
    /// <param name="tracking"><para>zh-cn:是否跟踪实体</para><para>en-us:Whether to track entities.</para></param>
    /// <returns><para>zh-cn:数据库中的实体</para><para>en-us:The operation result.</para></returns>
    public virtual TEntity Last(Expression<Func<TEntity, bool>> predicate, bool? tracking = null)
    {
        return AsQueryable(tracking).Last(predicate);
    }

    /// <summary>
    /// <para>zh-cn:查询一条记录</para>
    /// <para>en-us:Provides the corresponding database repository operation.</para>
    /// </summary>
    /// <param name="tracking"><para>zh-cn:是否跟踪实体</para><para>en-us:Whether to track entities.</para></param>
    /// <returns><para>zh-cn:数据库中的实体</para><para>en-us:The operation result.</para></returns>
    public virtual TEntity? LastOrDefault(bool? tracking = null)
    {
        return AsQueryable(tracking).LastOrDefault();
    }

    /// <summary>
    /// <para>zh-cn:根据表达式查询一条记录</para>
    /// <para>en-us:Provides the corresponding database repository operation.</para>
    /// </summary>
    /// <param name="predicate"><para>zh-cn:表达式</para><para>en-us:The query predicate.</para></param>
    /// <param name="tracking"><para>zh-cn:是否跟踪实体</para><para>en-us:Whether to track entities.</para></param>
    /// <returns><para>zh-cn:数据库中的实体</para><para>en-us:The operation result.</para></returns>
    public virtual TEntity? LastOrDefault(Expression<Func<TEntity, bool>> predicate, bool? tracking = null)
    {
        return AsQueryable(tracking).LastOrDefault(predicate);
    }

    /// <summary>
    /// <para>zh-cn:查询一条记录</para>
    /// <para>en-us:Provides the corresponding database repository operation.</para>
    /// </summary>
    /// <param name="tracking"><para>zh-cn:是否跟踪实体</para><para>en-us:Whether to track entities.</para></param>
    /// <param name="cancellationToken"><para>zh-cn:取消异步令牌</para><para>en-us:The cancellation token.</para></param>
    /// <returns><para>zh-cn:数据库中的实体</para><para>en-us:The operation result.</para></returns>
    public virtual Task<TEntity> LastAsync(bool? tracking = null, CancellationToken cancellationToken = default)
    {
        return AsQueryable(tracking).LastAsync(cancellationToken);
    }

    /// <summary>
    /// <para>zh-cn:根据表达式查询一条记录</para>
    /// <para>en-us:Provides the corresponding database repository operation.</para>
    /// </summary>
    /// <param name="predicate"><para>zh-cn:表达式</para><para>en-us:The query predicate.</para></param>
    /// <param name="tracking"><para>zh-cn:是否跟踪实体</para><para>en-us:Whether to track entities.</para></param>
    /// <param name="cancellationToken"><para>zh-cn:取消异步令牌</para><para>en-us:The cancellation token.</para></param>
    /// <returns><para>zh-cn:数据库中的实体</para><para>en-us:The operation result.</para></returns>
    public virtual Task<TEntity> LastAsync(Expression<Func<TEntity, bool>> predicate, bool? tracking = null, CancellationToken cancellationToken = default)
    {
        return AsQueryable(tracking).LastAsync(predicate, cancellationToken);
    }

    /// <summary>
    /// <para>zh-cn:查询一条记录</para>
    /// <para>en-us:Provides the corresponding database repository operation.</para>
    /// </summary>
    /// <param name="tracking"><para>zh-cn:是否跟踪实体</para><para>en-us:Whether to track entities.</para></param>
    /// <param name="cancellationToken"><para>zh-cn:异步取消令牌</para><para>en-us:The cancellation token.</para></param>
    /// <returns><para>zh-cn:数据库中的实体</para><para>en-us:The operation result.</para></returns>
    public virtual Task<TEntity?> LastOrDefaultAsync(bool? tracking = null, CancellationToken cancellationToken = default)
    {
        return AsQueryable(tracking).LastOrDefaultAsync(cancellationToken);
    }

    /// <summary>
    /// <para>zh-cn:根据表达式查询一条记录</para>
    /// <para>en-us:Provides the corresponding database repository operation.</para>
    /// </summary>
    /// <param name="predicate"><para>zh-cn:表达式</para><para>en-us:The query predicate.</para></param>
    /// <param name="tracking"><para>zh-cn:是否跟踪实体</para><para>en-us:Whether to track entities.</para></param>
    /// <param name="cancellationToken"><para>zh-cn:异步取消令牌</para><para>en-us:The cancellation token.</para></param>
    /// <returns><para>zh-cn:数据库中的实体</para><para>en-us:The operation result.</para></returns>
    public virtual Task<TEntity?> LastOrDefaultAsync(Expression<Func<TEntity, bool>> predicate, bool? tracking = null, CancellationToken cancellationToken = default)
    {
        return AsQueryable(tracking).LastOrDefaultAsync(predicate, cancellationToken);
    }

    /// <summary>
    /// <para>zh-cn:根据表达式查询多条记录</para>
    /// <para>en-us:Provides the corresponding database repository operation.</para>
    /// </summary>
    /// <param name="predicate"><para>zh-cn:表达式</para><para>en-us:The query predicate.</para></param>
    /// <param name="tracking"><para>zh-cn:是否跟踪实体</para><para>en-us:Whether to track entities.</para></param>
    /// <param name="ignoreQueryFilters"><para>zh-cn:是否忽略查询过滤器</para><para>en-us:Whether to ignore query filters.</para></param>
    /// <returns><para>zh-cn:数据库中的多个实体</para><para>en-us:The operation result.</para></returns>
    public virtual IQueryable<TEntity> Where(Expression<Func<TEntity, bool>> predicate, bool? tracking = null, bool ignoreQueryFilters = false)
    {
        return AsQueryable(predicate, tracking, ignoreQueryFilters);
    }

    /// <summary>
    /// <para>zh-cn:根据表达式查询多条记录</para>
    /// <para>en-us:Provides the corresponding database repository operation.</para>
    /// </summary>
    /// <param name="predicate"><para>zh-cn:表达式</para><para>en-us:The query predicate.</para></param>
    /// <param name="tracking"><para>zh-cn:是否跟踪实体</para><para>en-us:Whether to track entities.</para></param>
    /// <param name="ignoreQueryFilters"><para>zh-cn:是否忽略查询过滤器</para><para>en-us:Whether to ignore query filters.</para></param>
    /// <returns><para>zh-cn:数据库中的多个实体</para><para>en-us:The operation result.</para></returns>
    public virtual IQueryable<TEntity> Where(Expression<Func<TEntity, int, bool>> predicate, bool? tracking = null, bool ignoreQueryFilters = false)
    {
        return AsQueryable(predicate, tracking, ignoreQueryFilters);
    }

    /// <summary>
    /// <para>zh-cn:根据条件执行表达式查询多条记录</para>
    /// <para>en-us:Provides the corresponding database repository operation.</para>
    /// </summary>
    /// <param name="condition"><para>zh-cn:条件</para><para>en-us:The condition value.</para></param>
    /// <param name="predicate"><para>zh-cn:表达式</para><para>en-us:The query predicate.</para></param>
    /// <param name="tracking"><para>zh-cn:是否跟踪实体</para><para>en-us:Whether to track entities.</para></param>
    /// <param name="ignoreQueryFilters"><para>zh-cn:是否忽略查询过滤器</para><para>en-us:Whether to ignore query filters.</para></param>
    /// <returns><para>zh-cn:数据库中的多个实体</para><para>en-us:The operation result.</para></returns>
    public virtual IQueryable<TEntity> Where(bool condition, Expression<Func<TEntity, bool>> predicate, bool? tracking = null, bool ignoreQueryFilters = false)
    {
        return AsQueryable(default(Expression<Func<TEntity, bool>>), tracking, ignoreQueryFilters).Where(condition, predicate);
    }

    /// <summary>
    /// <para>zh-cn:根据条件执行表达式查询多条记录</para>
    /// <para>en-us:Provides the corresponding database repository operation.</para>
    /// </summary>
    /// <param name="condition"><para>zh-cn:条件</para><para>en-us:The condition value.</para></param>
    /// <param name="predicate"><para>zh-cn:表达式</para><para>en-us:The query predicate.</para></param>
    /// <param name="tracking"><para>zh-cn:是否跟踪实体</para><para>en-us:Whether to track entities.</para></param>
    /// <param name="ignoreQueryFilters"><para>zh-cn:是否忽略查询过滤器</para><para>en-us:Whether to ignore query filters.</para></param>
    /// <returns><para>zh-cn:数据库中的多个实体</para><para>en-us:The operation result.</para></returns>
    public virtual IQueryable<TEntity> Where(bool condition, Expression<Func<TEntity, int, bool>> predicate, bool? tracking = null, bool ignoreQueryFilters = false)
    {
        return AsQueryable(default(Expression<Func<TEntity, int, bool>>), tracking, ignoreQueryFilters).Where(condition, predicate);
    }

    /// <summary>
    /// <para>zh-cn:根据条件执行表达式查询多条记录</para>
    /// <para>en-us:Provides the corresponding database repository operation.</para>
    /// </summary>
    /// <param name="predicates"><para>zh-cn:表达式集合</para><para>en-us:The query predicates.</para></param>
    /// <returns><para>zh-cn:数据库中的多个实体</para><para>en-us:The operation result.</para></returns>
    public virtual IQueryable<TEntity> Where(params Expression<Func<TEntity, bool>>[] predicates)
    {
        return AsQueryable(default(Expression<Func<TEntity, bool>>)).Where(predicates);
    }

    /// <summary>
    /// <para>zh-cn:根据条件执行表达式查询多条记录</para>
    /// <para>en-us:Provides the corresponding database repository operation.</para>
    /// </summary>
    /// <param name="predicates"><para>zh-cn:表达式集合</para><para>en-us:The query predicates.</para></param>
    /// <param name="tracking"><para>zh-cn:是否跟踪实体</para><para>en-us:Whether to track entities.</para></param>
    /// <param name="ignoreQueryFilters"><para>zh-cn:是否忽略查询过滤器</para><para>en-us:Whether to ignore query filters.</para></param>
    /// <returns><para>zh-cn:数据库中的多个实体</para><para>en-us:The operation result.</para></returns>
    public virtual IQueryable<TEntity> Where(Expression<Func<TEntity, bool>>[] predicates, bool? tracking = null, bool ignoreQueryFilters = false)
    {
        return AsQueryable(default(Expression<Func<TEntity, bool>>), tracking, ignoreQueryFilters).Where(predicates);
    }

    /// <summary>
    /// <para>zh-cn:根据条件执行表达式查询多条记录</para>
    /// <para>en-us:Provides the corresponding database repository operation.</para>
    /// </summary>
    /// <param name="predicates"><para>zh-cn:表达式集合</para><para>en-us:The query predicates.</para></param>
    /// <returns><para>zh-cn:数据库中的多个实体</para><para>en-us:The operation result.</para></returns>
    public virtual IQueryable<TEntity> Where(params Expression<Func<TEntity, int, bool>>[] predicates)
    {
        return AsQueryable(default(Expression<Func<TEntity, int, bool>>)).Where(predicates);
    }

    /// <summary>
    /// <para>zh-cn:根据条件执行表达式查询多条记录</para>
    /// <para>en-us:Provides the corresponding database repository operation.</para>
    /// </summary>
    /// <param name="predicates"><para>zh-cn:表达式集合</para><para>en-us:The query predicates.</para></param>
    /// <param name="tracking"><para>zh-cn:是否跟踪实体</para><para>en-us:Whether to track entities.</para></param>
    /// <param name="ignoreQueryFilters"><para>zh-cn:是否忽略查询过滤器</para><para>en-us:Whether to ignore query filters.</para></param>
    /// <returns><para>zh-cn:数据库中的多个实体</para><para>en-us:The operation result.</para></returns>
    public virtual IQueryable<TEntity> Where(Expression<Func<TEntity, int, bool>>[] predicates, bool? tracking = null, bool ignoreQueryFilters = false)
    {
        return AsQueryable(default(Expression<Func<TEntity, int, bool>>), tracking, ignoreQueryFilters).Where(predicates);
    }

    /// <summary>
    /// <para>zh-cn:根据条件执行表达式查询多条记录</para>
    /// <para>en-us:Provides the corresponding database repository operation.</para>
    /// </summary>
    /// <param name="conditionPredicates"><para>zh-cn:条件表达式集合</para><para>en-us:The conditionPredicates value.</para></param>
    /// <returns><para>zh-cn:数据库中的多个实体</para><para>en-us:The operation result.</para></returns>
    public virtual IQueryable<TEntity> Where(params (bool condition, Expression<Func<TEntity, bool>> expression)[] conditionPredicates)
    {
        return AsQueryable(default(Expression<Func<TEntity, bool>>)).Where(conditionPredicates);
    }

    /// <summary>
    /// <para>zh-cn:根据条件执行表达式查询多条记录</para>
    /// <para>en-us:Provides the corresponding database repository operation.</para>
    /// </summary>
    /// <param name="conditionPredicates"><para>zh-cn:条件表达式集合</para><para>en-us:The conditionPredicates value.</para></param>
    /// <param name="tracking"><para>zh-cn:是否跟踪实体</para><para>en-us:Whether to track entities.</para></param>
    /// <param name="ignoreQueryFilters"><para>zh-cn:是否忽略查询过滤器</para><para>en-us:Whether to ignore query filters.</para></param>
    /// <returns><para>zh-cn:数据库中的多个实体</para><para>en-us:The operation result.</para></returns>
    public virtual IQueryable<TEntity> Where((bool condition, Expression<Func<TEntity, bool>> expression)[] conditionPredicates, bool? tracking = null, bool ignoreQueryFilters = false)
    {
        return AsQueryable(default(Expression<Func<TEntity, bool>>), tracking, ignoreQueryFilters).Where(conditionPredicates);
    }

    /// <summary>
    /// <para>zh-cn:根据条件执行表达式查询多条记录</para>
    /// <para>en-us:Provides the corresponding database repository operation.</para>
    /// </summary>
    /// <param name="conditionPredicates"><para>zh-cn:条件表达式集合</para><para>en-us:The conditionPredicates value.</para></param>
    /// <returns><para>zh-cn:数据库中的多个实体</para><para>en-us:The operation result.</para></returns>
    public virtual IQueryable<TEntity> Where(params (bool condition, Expression<Func<TEntity, int, bool>> expression)[] conditionPredicates)
    {
        return AsQueryable(default(Expression<Func<TEntity, int, bool>>)).Where(conditionPredicates);
    }

    /// <summary>
    /// <para>zh-cn:根据条件执行表达式查询多条记录</para>
    /// <para>en-us:Provides the corresponding database repository operation.</para>
    /// </summary>
    /// <param name="conditionPredicates"><para>zh-cn:条件表达式集合</para><para>en-us:The conditionPredicates value.</para></param>
    /// <param name="tracking"><para>zh-cn:是否跟踪实体</para><para>en-us:Whether to track entities.</para></param>
    /// <param name="ignoreQueryFilters"><para>zh-cn:是否忽略查询过滤器</para><para>en-us:Whether to ignore query filters.</para></param>
    /// <returns><para>zh-cn:数据库中的多个实体</para><para>en-us:The operation result.</para></returns>
    public virtual IQueryable<TEntity> Where((bool condition, Expression<Func<TEntity, int, bool>> expression)[] conditionPredicates, bool? tracking = null, bool ignoreQueryFilters = false)
    {
        return AsQueryable(default(Expression<Func<TEntity, int, bool>>), tracking, ignoreQueryFilters).Where(conditionPredicates);
    }

    /// <summary>
    /// <para>zh-cn:加载关联数据</para>
    /// <para>en-us:Provides the corresponding database repository operation.</para>
    /// </summary>
    /// <param name="predicate"><para>zh-cn:表达式</para><para>en-us:The query predicate.</para></param>
    /// <param name="tracking"><para>zh-cn:是否跟踪实体</para><para>en-us:Whether to track entities.</para></param>
    /// <param name="ignoreQueryFilters"><para>zh-cn:是否忽略查询过滤器</para><para>en-us:Whether to ignore query filters.</para></param>
    /// <returns><para>zh-cn:数据库中的多个实体</para><para>en-us:The operation result.</para></returns>
    public virtual IIncludableQueryable<TEntity, TProperty> Include<TProperty>(Expression<Func<TEntity, TProperty>> predicate, bool? tracking = null, bool ignoreQueryFilters = false)
    {
        return AsQueryable(default(Expression<Func<TEntity, bool>>), tracking, ignoreQueryFilters).Include(predicate);
    }

    /// <summary>
    /// <para>zh-cn:判断记录是否存在</para>
    /// <para>en-us:Provides the corresponding database repository operation.</para>
    /// </summary>
    /// <param name="tracking"><para>zh-cn:是否跟踪实体</para><para>en-us:Whether to track entities.</para></param>
    /// <param name="ignoreQueryFilters"><para>zh-cn:是否忽略查询过滤器</para><para>en-us:Whether to ignore query filters.</para></param>
    /// <returns><para>zh-cn:bool</para><para>en-us:The operation result.</para></returns>
    public virtual bool Any(bool? tracking = null, bool ignoreQueryFilters = false)
    {
        return AsQueryable(default(Expression<Func<TEntity, bool>>), tracking, ignoreQueryFilters).Any();
    }

    /// <summary>
    /// <para>zh-cn:根据表达式判断记录是否存在</para>
    /// <para>en-us:Provides the corresponding database repository operation.</para>
    /// </summary>
    /// <param name="predicate"><para>zh-cn:表达式</para><para>en-us:The query predicate.</para></param>
    /// <param name="tracking"><para>zh-cn:是否跟踪实体</para><para>en-us:Whether to track entities.</para></param>
    /// <param name="ignoreQueryFilters"><para>zh-cn:是否忽略查询过滤器</para><para>en-us:Whether to ignore query filters.</para></param>
    /// <returns><para>zh-cn:bool</para><para>en-us:The operation result.</para></returns>
    public virtual bool Any(Expression<Func<TEntity, bool>> predicate, bool? tracking = null, bool ignoreQueryFilters = false)
    {
        return AsQueryable(default(Expression<Func<TEntity, bool>>), tracking, ignoreQueryFilters).Any(predicate);
    }

    /// <summary>
    /// <para>zh-cn:判断记录是否存在</para>
    /// <para>en-us:Provides the corresponding database repository operation.</para>
    /// </summary>
    /// <param name="tracking"><para>zh-cn:是否跟踪实体</para><para>en-us:Whether to track entities.</para></param>
    /// <param name="ignoreQueryFilters"><para>zh-cn:是否忽略查询过滤器</para><para>en-us:Whether to ignore query filters.</para></param>
    /// <param name="cancellationToken"><para>zh-cn:取消异步令牌</para><para>en-us:The cancellation token.</para></param>
    /// <returns><para>zh-cn:bool</para><para>en-us:The operation result.</para></returns>
    public virtual Task<bool> AnyAsync(bool? tracking = null, bool ignoreQueryFilters = false, CancellationToken cancellationToken = default)
    {
        return AsQueryable(default(Expression<Func<TEntity, bool>>), tracking, ignoreQueryFilters).AnyAsync(cancellationToken);
    }

    /// <summary>
    /// <para>zh-cn:根据表达式判断记录是否存在</para>
    /// <para>en-us:Provides the corresponding database repository operation.</para>
    /// </summary>
    /// <param name="predicate"><para>zh-cn:表达式</para><para>en-us:The query predicate.</para></param>
    /// <param name="tracking"><para>zh-cn:是否跟踪实体</para><para>en-us:Whether to track entities.</para></param>
    /// <param name="ignoreQueryFilters"><para>zh-cn:是否忽略查询过滤器</para><para>en-us:Whether to ignore query filters.</para></param>
    /// <param name="cancellationToken"><para>zh-cn:取消异步令牌</para><para>en-us:The cancellation token.</para></param>
    /// <returns><para>zh-cn:bool</para><para>en-us:The operation result.</para></returns>
    public virtual Task<bool> AnyAsync(Expression<Func<TEntity, bool>> predicate, bool? tracking = null, bool ignoreQueryFilters = false, CancellationToken cancellationToken = default)
    {
        return AsQueryable(default(Expression<Func<TEntity, bool>>), tracking, ignoreQueryFilters).AnyAsync(predicate, cancellationToken);
    }

    /// <summary>
    /// <para>zh-cn:根据表达式判断记录是否全部满足条件</para>
    /// <para>en-us:Provides the corresponding database repository operation.</para>
    /// </summary>
    /// <param name="predicate"><para>zh-cn:表达式</para><para>en-us:The query predicate.</para></param>
    /// <param name="tracking"><para>zh-cn:是否跟踪实体</para><para>en-us:Whether to track entities.</para></param>
    /// <param name="ignoreQueryFilters"><para>zh-cn:是否忽略查询过滤器</para><para>en-us:Whether to ignore query filters.</para></param>
    /// <returns><para>zh-cn:bool</para><para>en-us:The operation result.</para></returns>
    public virtual bool All(Expression<Func<TEntity, bool>> predicate, bool? tracking = null, bool ignoreQueryFilters = false)
    {
        return AsQueryable(default(Expression<Func<TEntity, bool>>), tracking, ignoreQueryFilters).All(predicate);
    }

    /// <summary>
    /// <para>zh-cn:根据表达式判断记录是否全部满足条件</para>
    /// <para>en-us:Provides the corresponding database repository operation.</para>
    /// </summary>
    /// <param name="predicate"><para>zh-cn:表达式</para><para>en-us:The query predicate.</para></param>
    /// <param name="tracking"><para>zh-cn:是否跟踪实体</para><para>en-us:Whether to track entities.</para></param>
    /// <param name="ignoreQueryFilters"><para>zh-cn:是否忽略查询过滤器</para><para>en-us:Whether to ignore query filters.</para></param>
    /// <param name="cancellationToken"><para>zh-cn:取消异步令牌</para><para>en-us:The cancellation token.</para></param>
    /// <returns><para>zh-cn:bool</para><para>en-us:The operation result.</para></returns>
    public virtual Task<bool> AllAsync(Expression<Func<TEntity, bool>> predicate, bool? tracking = null, bool ignoreQueryFilters = false, CancellationToken cancellationToken = default)
    {
        return AsQueryable(default(Expression<Func<TEntity, bool>>), tracking, ignoreQueryFilters).AllAsync(predicate, cancellationToken);
    }

    /// <summary>
    /// <para>zh-cn:查看记录条数</para>
    /// <para>en-us:Provides the corresponding database repository operation.</para>
    /// </summary>
    /// <param name="tracking"><para>zh-cn:是否跟踪实体</para><para>en-us:Whether to track entities.</para></param>
    /// <param name="ignoreQueryFilters"><para>zh-cn:是否忽略查询过滤器</para><para>en-us:Whether to ignore query filters.</para></param>
    /// <returns><para>zh-cn:int</para><para>en-us:The operation result.</para></returns>
    public virtual int Count(bool? tracking = null, bool ignoreQueryFilters = false)
    {
        return AsQueryable(default(Expression<Func<TEntity, bool>>), tracking, ignoreQueryFilters).Count();
    }

    /// <summary>
    /// <para>zh-cn:根据表达式查询记录条数</para>
    /// <para>en-us:Provides the corresponding database repository operation.</para>
    /// </summary>
    /// <param name="predicate"><para>zh-cn:表达式</para><para>en-us:The query predicate.</para></param>
    /// <param name="tracking"><para>zh-cn:是否跟踪实体</para><para>en-us:Whether to track entities.</para></param>
    /// <param name="ignoreQueryFilters"><para>zh-cn:是否忽略查询过滤器</para><para>en-us:Whether to ignore query filters.</para></param>
    /// <returns><para>zh-cn:int</para><para>en-us:The operation result.</para></returns>
    public virtual int Count(Expression<Func<TEntity, bool>> predicate, bool? tracking = null, bool ignoreQueryFilters = false)
    {
        return AsQueryable(default(Expression<Func<TEntity, bool>>), tracking, ignoreQueryFilters).Count(predicate);
    }

    /// <summary>
    /// <para>zh-cn:查看记录条数</para>
    /// <para>en-us:Provides the corresponding database repository operation.</para>
    /// </summary>
    /// <param name="tracking"><para>zh-cn:是否跟踪实体</para><para>en-us:Whether to track entities.</para></param>
    /// <param name="ignoreQueryFilters"><para>zh-cn:是否忽略查询过滤器</para><para>en-us:Whether to ignore query filters.</para></param>
    /// <param name="cancellationToken"><para>zh-cn:取消异步令牌</para><para>en-us:The cancellation token.</para></param>
    /// <returns><para>zh-cn:int</para><para>en-us:The operation result.</para></returns>
    public virtual Task<int> CountAsync(bool? tracking = null, bool ignoreQueryFilters = false, CancellationToken cancellationToken = default)
    {
        return AsQueryable(default(Expression<Func<TEntity, bool>>), tracking, ignoreQueryFilters).CountAsync(cancellationToken);
    }

    /// <summary>
    /// <para>zh-cn:根据表达式查询记录条数</para>
    /// <para>en-us:Provides the corresponding database repository operation.</para>
    /// </summary>
    /// <param name="predicate"><para>zh-cn:表达式</para><para>en-us:The query predicate.</para></param>
    /// <param name="tracking"><para>zh-cn:是否跟踪实体</para><para>en-us:Whether to track entities.</para></param>
    /// <param name="ignoreQueryFilters"><para>zh-cn:是否忽略查询过滤器</para><para>en-us:Whether to ignore query filters.</para></param>
    /// <param name="cancellationToken"><para>zh-cn:取消异步令牌</para><para>en-us:The cancellation token.</para></param>
    /// <returns><para>zh-cn:int</para><para>en-us:The operation result.</para></returns>
    public virtual Task<int> CountAsync(Expression<Func<TEntity, bool>> predicate, bool? tracking = null, bool ignoreQueryFilters = false, CancellationToken cancellationToken = default)
    {
        return AsQueryable(default(Expression<Func<TEntity, bool>>), tracking, ignoreQueryFilters).CountAsync(predicate, cancellationToken);
    }

    /// <summary>
    /// <para>zh-cn:查看最小记录</para>
    /// <para>en-us:Provides the corresponding database repository operation.</para>
    /// </summary>
    /// <param name="tracking"><para>zh-cn:是否跟踪实体</para><para>en-us:Whether to track entities.</para></param>
    /// <param name="ignoreQueryFilters"><para>zh-cn:是否忽略查询过滤器</para><para>en-us:Whether to ignore query filters.</para></param>
    /// <returns><para>zh-cn:实体</para><para>en-us:The operation result.</para></returns>
    public virtual TEntity? Min(bool? tracking = null, bool ignoreQueryFilters = false)
    {
        return AsQueryable(default(Expression<Func<TEntity, bool>>), tracking, ignoreQueryFilters).Min();
    }

    /// <summary>
    /// <para>zh-cn:根据表达式查看最小值</para>
    /// <para>en-us:Provides the corresponding database repository operation.</para>
    /// </summary>
    /// <typeparam name="TResult"><para>zh-cn:最小值类型</para><para>en-us:The generic parameter type.</para></typeparam>
    /// <param name="predicate"><para>zh-cn:表达式</para><para>en-us:The query predicate.</para></param>
    /// <param name="tracking"><para>zh-cn:是否跟踪实体</para><para>en-us:Whether to track entities.</para></param>
    /// <param name="ignoreQueryFilters"><para>zh-cn:是否忽略查询过滤器</para><para>en-us:Whether to ignore query filters.</para></param>
    /// <returns><para>zh-cn:TResult</para><para>en-us:The operation result.</para></returns>
    public virtual TResult? Min<TResult>(Expression<Func<TEntity, TResult>> predicate, bool? tracking = null, bool ignoreQueryFilters = false)
    {
        return AsQueryable(default(Expression<Func<TEntity, bool>>), tracking, ignoreQueryFilters).Min(predicate);
    }

    /// <summary>
    /// <para>zh-cn:查看最小记录</para>
    /// <para>en-us:Provides the corresponding database repository operation.</para>
    /// </summary>
    /// <param name="tracking"><para>zh-cn:是否跟踪实体</para><para>en-us:Whether to track entities.</para></param>
    /// <param name="ignoreQueryFilters"><para>zh-cn:是否忽略查询过滤器</para><para>en-us:Whether to ignore query filters.</para></param>
    /// <param name="cancellationToken"><para>zh-cn:取消异步令牌</para><para>en-us:The cancellation token.</para></param>
    /// <returns><para>zh-cn:实体</para><para>en-us:The operation result.</para></returns>
    public virtual Task<TEntity> MinAsync(bool? tracking = null, bool ignoreQueryFilters = false, CancellationToken cancellationToken = default)
    {
        return AsQueryable(default(Expression<Func<TEntity, bool>>), tracking, ignoreQueryFilters).MinAsync(cancellationToken);
    }

    /// <summary>
    /// <para>zh-cn:根据表达式查看最小值</para>
    /// <para>en-us:Provides the corresponding database repository operation.</para>
    /// </summary>
    /// <typeparam name="TResult"><para>zh-cn:最小值类型</para><para>en-us:The generic parameter type.</para></typeparam>
    /// <param name="predicate"><para>zh-cn:表达式</para><para>en-us:The query predicate.</para></param>
    /// <param name="tracking"><para>zh-cn:是否跟踪实体</para><para>en-us:Whether to track entities.</para></param>
    /// <param name="ignoreQueryFilters"><para>zh-cn:是否忽略查询过滤器</para><para>en-us:Whether to ignore query filters.</para></param>
    /// <param name="cancellationToken"><para>zh-cn:取消异步令牌</para><para>en-us:The cancellation token.</para></param>
    /// <returns><para>zh-cn:TResult</para><para>en-us:The operation result.</para></returns>
    public virtual Task<TResult> MinAsync<TResult>(Expression<Func<TEntity, TResult>> predicate, bool? tracking = null, bool ignoreQueryFilters = false, CancellationToken cancellationToken = default)
    {
        return AsQueryable(default(Expression<Func<TEntity, bool>>), tracking, ignoreQueryFilters).MinAsync(predicate, cancellationToken);
    }

    /// <summary>
    /// <para>zh-cn:查看最大记录</para>
    /// <para>en-us:Provides the corresponding database repository operation.</para>
    /// </summary>
    /// <param name="tracking"><para>zh-cn:是否跟踪实体</para><para>en-us:Whether to track entities.</para></param>
    /// <param name="ignoreQueryFilters"><para>zh-cn:是否忽略查询过滤器</para><para>en-us:Whether to ignore query filters.</para></param>
    /// <returns><para>zh-cn:实体</para><para>en-us:The operation result.</para></returns>
    public virtual TEntity? Max(bool? tracking = null, bool ignoreQueryFilters = false)
    {
        return AsQueryable(default(Expression<Func<TEntity, bool>>), tracking, ignoreQueryFilters).Max();
    }

    /// <summary>
    /// <para>zh-cn:根据表达式查看最大值</para>
    /// <para>en-us:Provides the corresponding database repository operation.</para>
    /// </summary>
    /// <typeparam name="TResult"><para>zh-cn:最大值类型</para><para>en-us:The generic parameter type.</para></typeparam>
    /// <param name="predicate"><para>zh-cn:表达式</para><para>en-us:The query predicate.</para></param>
    /// <param name="tracking"><para>zh-cn:是否跟踪实体</para><para>en-us:Whether to track entities.</para></param>
    /// <param name="ignoreQueryFilters"><para>zh-cn:是否忽略查询过滤器</para><para>en-us:Whether to ignore query filters.</para></param>
    /// <returns><para>zh-cn:TResult</para><para>en-us:The operation result.</para></returns>
    public virtual TResult? Max<TResult>(Expression<Func<TEntity, TResult>> predicate, bool? tracking = null, bool ignoreQueryFilters = false)
    {
        return AsQueryable(default(Expression<Func<TEntity, bool>>), tracking, ignoreQueryFilters).Max(predicate);
    }

    /// <summary>
    /// <para>zh-cn:查看最大记录</para>
    /// <para>en-us:Provides the corresponding database repository operation.</para>
    /// </summary>
    /// <param name="tracking"><para>zh-cn:是否跟踪实体</para><para>en-us:Whether to track entities.</para></param>
    /// <param name="ignoreQueryFilters"><para>zh-cn:是否忽略查询过滤器</para><para>en-us:Whether to ignore query filters.</para></param>
    /// <param name="cancellationToken"><para>zh-cn:取消异步令牌</para><para>en-us:The cancellation token.</para></param>
    /// <returns><para>zh-cn:实体</para><para>en-us:The operation result.</para></returns>
    public virtual Task<TEntity> MaxAsync(bool? tracking = null, bool ignoreQueryFilters = false, CancellationToken cancellationToken = default)
    {
        return AsQueryable(default(Expression<Func<TEntity, bool>>), tracking, ignoreQueryFilters).MaxAsync(cancellationToken);
    }

    /// <summary>
    /// <para>zh-cn:根据表达式查看最大值</para>
    /// <para>en-us:Provides the corresponding database repository operation.</para>
    /// </summary>
    /// <typeparam name="TResult"><para>zh-cn:最大值类型</para><para>en-us:The generic parameter type.</para></typeparam>
    /// <param name="predicate"><para>zh-cn:表达式</para><para>en-us:The query predicate.</para></param>
    /// <param name="tracking"><para>zh-cn:是否跟踪实体</para><para>en-us:Whether to track entities.</para></param>
    /// <param name="ignoreQueryFilters"><para>zh-cn:是否忽略查询过滤器</para><para>en-us:Whether to ignore query filters.</para></param>
    /// <param name="cancellationToken"><para>zh-cn:取消异步令牌</para><para>en-us:The cancellation token.</para></param>
    /// <returns><para>zh-cn:TResult</para><para>en-us:The operation result.</para></returns>
    public virtual Task<TResult> MaxAsync<TResult>(Expression<Func<TEntity, TResult>> predicate, bool? tracking = null, bool ignoreQueryFilters = false, CancellationToken cancellationToken = default)
    {
        return AsQueryable(default(Expression<Func<TEntity, bool>>), tracking, ignoreQueryFilters).MaxAsync(predicate, cancellationToken);
    }

    /// <summary>
    /// <para>zh-cn:构建查询分析器</para>
    /// <para>en-us:Provides the corresponding database repository operation.</para>
    /// </summary>
    /// <param name="tracking"><para>zh-cn:是否跟踪实体</para><para>en-us:Whether to track entities.</para></param>
    /// <returns><para>zh-cn:IQueryable{TEntity}</para><para>en-us:The operation result.</para></returns>
    public virtual IQueryable<TEntity> AsQueryable(bool? tracking = null)
    {
        // 启用实体跟踪
        var isTracking = tracking ?? DynamicContext.EnabledEntityStateTracked;

        return isTracking ? Entities : DetachedEntities;
    }

    /// <summary>
    /// <para>zh-cn:构建查询分析器</para>
    /// <para>en-us:Provides the corresponding database repository operation.</para>
    /// </summary>
    /// <param name="predicate"><para>zh-cn:表达式</para><para>en-us:The query predicate.</para></param>
    /// <param name="tracking"><para>zh-cn:是否跟踪实体</para><para>en-us:Whether to track entities.</para></param>
    /// <param name="ignoreQueryFilters"><para>zh-cn:是否忽略查询过滤器</para><para>en-us:Whether to ignore query filters.</para></param>
    /// <returns><para>zh-cn:IQueryable{TEntity}</para><para>en-us:The operation result.</para></returns>
    public virtual IQueryable<TEntity> AsQueryable(Expression<Func<TEntity, bool>> predicate, bool? tracking = null, bool ignoreQueryFilters = false)
    {
        var entities = AsQueryable(tracking);
        if (ignoreQueryFilters) entities = entities.IgnoreQueryFilters();
        if (predicate != null) entities = entities.Where(predicate);

        return entities;
    }

    /// <summary>
    /// <para>zh-cn:构建查询分析器</para>
    /// <para>en-us:Provides the corresponding database repository operation.</para>
    /// </summary>
    /// <param name="predicate"><para>zh-cn:表达式</para><para>en-us:The query predicate.</para></param>
    /// <param name="tracking"><para>zh-cn:是否跟踪实体</para><para>en-us:Whether to track entities.</para></param>
    /// <param name="ignoreQueryFilters"><para>zh-cn:是否忽略查询过滤器</para><para>en-us:Whether to ignore query filters.</para></param>
    /// <returns><para>zh-cn:IQueryable{TEntity}</para><para>en-us:The operation result.</para></returns>
    public virtual IQueryable<TEntity> AsQueryable(Expression<Func<TEntity, int, bool>> predicate, bool? tracking = null, bool ignoreQueryFilters = false)
    {
        var entities = AsQueryable(tracking);
        if (ignoreQueryFilters) entities = entities.IgnoreQueryFilters();
        if (predicate != null) entities = entities.Where(predicate);

        return entities;
    }

    /// <summary>
    /// <para>zh-cn:直接返回数据库结果</para>
    /// <para>en-us:Provides the corresponding database repository operation.</para>
    /// </summary>
    /// <param name="tracking"><para>zh-cn:是否跟踪实体</para><para>en-us:Whether to track entities.</para></param>
    /// <returns><para>zh-cn:IEnumerable{TEntity}</para><para>en-us:The operation result.</para></returns>
    public virtual IEnumerable<TEntity> AsEnumerable(bool? tracking = null)
    {
        return AsQueryable(tracking).AsEnumerable();
    }

    /// <summary>
    /// <para>zh-cn:直接返回数据库结果</para>
    /// <para>en-us:Provides the corresponding database repository operation.</para>
    /// </summary>
    /// <param name="predicate"><para>zh-cn:表达式</para><para>en-us:The query predicate.</para></param>
    /// <param name="tracking"><para>zh-cn:是否跟踪实体</para><para>en-us:Whether to track entities.</para></param>
    /// <param name="ignoreQueryFilters"><para>zh-cn:是否忽略查询过滤器</para><para>en-us:Whether to ignore query filters.</para></param>
    /// <returns><para>zh-cn:List{TEntity}</para><para>en-us:The operation result.</para></returns>
    public virtual IEnumerable<TEntity> AsEnumerable(Expression<Func<TEntity, bool>> predicate, bool? tracking = null, bool ignoreQueryFilters = false)
    {
        return AsQueryable(predicate, tracking, ignoreQueryFilters).AsEnumerable();
    }

    /// <summary>
    /// <para>zh-cn:直接返回数据库结果</para>
    /// <para>en-us:Provides the corresponding database repository operation.</para>
    /// </summary>
    /// <param name="predicate"><para>zh-cn:表达式</para><para>en-us:The query predicate.</para></param>
    /// <param name="tracking"><para>zh-cn:是否跟踪实体</para><para>en-us:Whether to track entities.</para></param>
    /// <param name="ignoreQueryFilters"><para>zh-cn:是否忽略查询过滤器</para><para>en-us:Whether to ignore query filters.</para></param>
    /// <returns><para>zh-cn:IEnumerable{TEntity}</para><para>en-us:The operation result.</para></returns>
    public virtual IEnumerable<TEntity> AsEnumerable(Expression<Func<TEntity, int, bool>> predicate, bool? tracking = null, bool ignoreQueryFilters = false)
    {
        return AsQueryable(predicate, tracking, ignoreQueryFilters).AsEnumerable();
    }

    /// <summary>
    /// <para>zh-cn:直接返回数据库结果</para>
    /// <para>en-us:Provides the corresponding database repository operation.</para>
    /// </summary>
    /// <param name="tracking"><para>zh-cn:是否跟踪实体</para><para>en-us:Whether to track entities.</para></param>
    /// <returns><para>zh-cn:List{TEntity}</para><para>en-us:The operation result.</para></returns>
    public virtual IAsyncEnumerable<TEntity> AsAsyncEnumerable(bool? tracking = null)
    {
        return AsQueryable(tracking).AsAsyncEnumerable();
    }

    /// <summary>
    /// <para>zh-cn:直接返回数据库结果</para>
    /// <para>en-us:Provides the corresponding database repository operation.</para>
    /// </summary>
    /// <param name="predicate"><para>zh-cn:表达式</para><para>en-us:The query predicate.</para></param>
    /// <param name="tracking"><para>zh-cn:是否跟踪实体</para><para>en-us:Whether to track entities.</para></param>
    /// <param name="ignoreQueryFilters"><para>zh-cn:是否忽略查询过滤器</para><para>en-us:Whether to ignore query filters.</para></param>
    /// <returns><para>zh-cn:IAsyncEnumerable{TEntity}</para><para>en-us:The operation result.</para></returns>
    public virtual IAsyncEnumerable<TEntity> AsAsyncEnumerable(Expression<Func<TEntity, bool>> predicate, bool? tracking = null, bool ignoreQueryFilters = false)
    {
        return AsQueryable(predicate, tracking, ignoreQueryFilters).AsAsyncEnumerable();
    }

    /// <summary>
    /// <para>zh-cn:直接返回数据库结果</para>
    /// <para>en-us:Provides the corresponding database repository operation.</para>
    /// </summary>
    /// <param name="predicate"><para>zh-cn:表达式</para><para>en-us:The query predicate.</para></param>
    /// <param name="tracking"><para>zh-cn:是否跟踪实体</para><para>en-us:Whether to track entities.</para></param>
    /// <param name="ignoreQueryFilters"><para>zh-cn:是否忽略查询过滤器</para><para>en-us:Whether to ignore query filters.</para></param>
    /// <returns><para>zh-cn:IAsyncEnumerable{TEntity}</para><para>en-us:The operation result.</para></returns>
    public virtual IAsyncEnumerable<TEntity> AsAsyncEnumerable(Expression<Func<TEntity, int, bool>> predicate, bool? tracking = null, bool ignoreQueryFilters = false)
    {
        return AsQueryable(predicate, tracking, ignoreQueryFilters).AsAsyncEnumerable();
    }

    /// <summary>
    /// <para>zh-cn:执行 Sql 返回 IQueryable</para>
    /// <para>en-us:Provides the corresponding database repository operation.</para>
    /// </summary>
    /// <param name="sql"><para>zh-cn:sql 语句</para><para>en-us:The sql value.</para></param>
    /// <param name="parameters"><para>zh-cn:命令参数</para><para>en-us:The parameters value.</para></param>
    /// <returns><para>zh-cn:IQueryable</para><para>en-us:The operation result.</para></returns>
    public virtual IQueryable<TEntity> FromSqlRaw(string sql, params object[] parameters)
    {
        // 支持读取配置渲染
        var realSql = sql.Render();

        return Entities.FromSqlRaw(realSql, parameters);
    }

    /// <summary>
    /// <para>zh-cn:执行 Sql 返回 IQueryable</para>
    /// <para>en-us:Provides the corresponding database repository operation.</para>
    /// </summary>
    /// <remarks>
    /// 支持字符串内插语法
    /// </remarks>
    /// <param name="sql"><para>zh-cn:sql 语句</para><para>en-us:The sql value.</para></param>
    /// <returns><para>zh-cn:IQueryable</para><para>en-us:The operation result.</para></returns>
    public virtual IQueryable<TEntity> FromSqlInterpolated(FormattableString sql)
    {
        return Entities.FromSqlInterpolated(sql);
    }
}
