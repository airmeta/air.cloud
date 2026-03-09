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

using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace Air.Cloud.EntityFrameWork.Core.Repositories.Dependencies;

/// <summary>
/// 可插入仓储接口
/// </summary>
/// <typeparam name="TEntity">实体类型</typeparam>
public partial interface IInsertableRepository<TEntity> : IInsertableRepository<TEntity, MasterDbContextLocator>
    where TEntity : class, IPrivateEntity, new()
{
}

/// <summary>
/// 可插入仓储接口
/// </summary>
/// <typeparam name="TEntity">实体类型</typeparam>
/// <typeparam name="TDbContextLocator">数据库上下文定位器</typeparam>
public partial interface IInsertableRepository<TEntity, TDbContextLocator> : IPrivateInsertableRepository<TEntity>
    where TEntity : class, IPrivateEntity, new()
    where TDbContextLocator : class, IDbContextLocator
{
}

/// <summary>
/// 可插入仓储接口
/// </summary>
/// <typeparam name="TEntity">实体类型</typeparam>
public interface IPrivateInsertableRepository<TEntity> : IPrivateRootRepository
    where TEntity : class, IPrivateEntity, new()
{
    /// <summary>
    /// 新增一条记录
    /// </summary>
    /// <param name="entity">实体</param>
    /// <param name="ignoreNullValues"></param>
    /// <returns>代理的实体</returns>
    EntityEntry<TEntity> Insert(TEntity entity, bool? ignoreNullValues = null);

    /// <summary>
    /// 新增多条记录
    /// </summary>
    /// <param name="entities">多个实体</param>
    void Insert(params TEntity[] entities);

    /// <summary>
    /// 新增多条记录
    /// </summary>
    /// <param name="entities">多个实体</param>
    void Insert(IEnumerable<TEntity> entities);

    /// <summary>
    /// 新增一条记录
    /// </summary>
    /// <param name="entity">实体</param>
    /// <param name="ignoreNullValues"></param>
    /// <param name="cancellationToken">取消异步令牌</param>
    /// <returns>代理的实体</returns>
    Task<EntityEntry<TEntity>> InsertAsync(TEntity entity, bool? ignoreNullValues = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// 新增多条记录
    /// </summary>
    /// <param name="entities">多个实体</param>
    /// <returns>Task</returns>
    Task InsertAsync(params TEntity[] entities);

    /// <summary>
    /// 新增多条记录
    /// </summary>
    /// <param name="entities">多个实体</param>
    /// <param name="cancellationToken">取消异步令牌</param>
    /// <returns></returns>
    Task InsertAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default);

    /// <summary>
    /// 新增一条记录并立即提交
    /// </summary>
    /// <param name="entity">实体</param>
    /// <param name="ignoreNullValues"></param>
    /// <returns>数据库中返回的实体</returns>
    EntityEntry<TEntity> InsertNow(TEntity entity, bool? ignoreNullValues = null);

    /// <summary>
    /// 新增一条记录并立即提交
    /// </summary>
    /// <param name="entity">实体</param>
    /// <param name="acceptAllChangesOnSuccess">接受所有更改</param>
    /// <param name="ignoreNullValues"></param>
    /// <returns>数据库中返回的实体</returns>
    EntityEntry<TEntity> InsertNow(TEntity entity, bool acceptAllChangesOnSuccess, bool? ignoreNullValues = null);

    /// <summary>
    /// 新增多条记录
    /// </summary>
    /// <param name="entities">多个实体</param>
    void InsertNow(params TEntity[] entities);

    /// <summary>
    /// 新增多条记录
    /// </summary>
    /// <param name="entities">多个实体</param>
    /// <param name="acceptAllChangesOnSuccess">接受所有更改</param>
    void InsertNow(TEntity[] entities, bool acceptAllChangesOnSuccess);

    /// <summary>
    /// 新增多条记录
    /// </summary>
    /// <param name="entities">多个实体</param>
    void InsertNow(IEnumerable<TEntity> entities);

    /// <summary>
    /// 新增多条记录
    /// </summary>
    /// <param name="entities">多个实体</param>
    /// <param name="acceptAllChangesOnSuccess">接受所有更改</param>
    void InsertNow(IEnumerable<TEntity> entities, bool acceptAllChangesOnSuccess);

    /// <summary>
    /// 新增一条记录并立即提交
    /// </summary>
    /// <param name="entity">实体</param>
    /// <param name="cancellationToken">取消异步令牌</param>
    /// <param name="ignoreNullValues"></param>
    /// <returns>数据库中返回的实体</returns>
    Task<EntityEntry<TEntity>> InsertNowAsync(TEntity entity, bool? ignoreNullValues = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// 新增一条记录并立即提交
    /// </summary>
    /// <param name="entity">实体</param>
    /// <param name="acceptAllChangesOnSuccess">接受所有更改</param>
    /// <param name="ignoreNullValues"></param>
    /// <param name="cancellationToken">取消异步令牌</param>
    /// <returns>数据库中返回的实体</returns>
    Task<EntityEntry<TEntity>> InsertNowAsync(TEntity entity, bool acceptAllChangesOnSuccess, bool? ignoreNullValues = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// 新增多条记录并立即提交
    /// </summary>
    /// <param name="entities">多个实体</param>
    /// <returns>Task</returns>
    Task InsertNowAsync(params TEntity[] entities);

    /// <summary>
    /// 新增多条记录并立即提交
    /// </summary>
    /// <param name="entities">多个实体</param>
    /// <param name="cancellationToken">取消异步令牌</param>
    /// <returns>Task</returns>
    Task InsertNowAsync(TEntity[] entities, CancellationToken cancellationToken = default);

    /// <summary>
    /// 新增多条记录并立即提交
    /// </summary>
    /// <param name="entities">多个实体</param>
    /// <param name="acceptAllChangesOnSuccess">接受所有更改</param>
    /// <param name="cancellationToken">取消异步令牌</param>
    /// <returns>Task</returns>
    Task InsertNowAsync(TEntity[] entities, bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default);

    /// <summary>
    /// 新增多条记录并立即提交
    /// </summary>
    /// <param name="entities">多个实体</param>
    /// <param name="cancellationToken">取消异步令牌</param>
    /// <returns>Task</returns>
    Task InsertNowAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default);

    /// <summary>
    /// 新增多条记录并立即提交
    /// </summary>
    /// <param name="entities">多个实体</param>
    /// <param name="acceptAllChangesOnSuccess">接受所有更改</param>
    /// <param name="cancellationToken">取消异步令牌</param>
    /// <returns>Task</returns>
    Task InsertNowAsync(IEnumerable<TEntity> entities, bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default);
}