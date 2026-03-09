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

using Air.Cloud.Core.Standard.DataBase.Model;

using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace Air.Cloud.EntityFrameWork.Core.Repositories.Implantations;

/// <summary>
/// 可插入仓储分部类
/// </summary>
public partial class PrivateRepository<TEntity>
    where TEntity : class, IPrivateEntity, new()
{
    /// <summary>
    /// 新增一条记录
    /// </summary>
    /// <param name="entity">实体</param>
    /// <param name="ignoreNullValues"></param>
    /// <returns>代理的实体</returns>
    public virtual EntityEntry<TEntity> Insert(TEntity entity, bool? ignoreNullValues = null)
    {
        var entryEntity = Entities.Add(entity);

        // 忽略空值
        IgnoreNullValues(ref entity, ignoreNullValues);

        return entryEntity;
    }

    /// <summary>
    /// 新增多条记录
    /// </summary>
    /// <param name="entities">多个实体</param>
    public virtual void Insert(params TEntity[] entities)
    {
        Entities.AddRange(entities);
    }

    /// <summary>
    /// 新增多条记录
    /// </summary>
    /// <param name="entities">多个实体</param>
    public virtual void Insert(IEnumerable<TEntity> entities)
    {
        Entities.AddRange(entities);
    }

    /// <summary>
    /// 新增一条记录
    /// </summary>
    /// <param name="entity">实体</param>
    /// <param name="ignoreNullValues"></param>
    /// <param name="cancellationToken">取消异步令牌</param>
    /// <returns>代理的实体</returns>
    public virtual async Task<EntityEntry<TEntity>> InsertAsync(TEntity entity, bool? ignoreNullValues = null, CancellationToken cancellationToken = default)
    {
        var entityEntry = await Entities.AddAsync(entity, cancellationToken);

        // 忽略空值
        IgnoreNullValues(ref entity, ignoreNullValues);

        return entityEntry;
    }

    /// <summary>
    /// 新增多条记录
    /// </summary>
    /// <param name="entities">多个实体</param>
    /// <returns>Task</returns>
    public virtual Task InsertAsync(params TEntity[] entities)
    {
        return Entities.AddRangeAsync(entities);
    }

    /// <summary>
    /// 新增多条记录
    /// </summary>
    /// <param name="entities">多个实体</param>
    /// <param name="cancellationToken">取消异步令牌</param>
    /// <returns></returns>
    public virtual Task InsertAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default)
    {
        return Entities.AddRangeAsync(entities, cancellationToken);
    }

    /// <summary>
    /// 新增一条记录并立即提交
    /// </summary>
    /// <param name="entity">实体</param>
    /// <param name="ignoreNullValues"></param>
    /// <returns>数据库中返回的实体</returns>
    public virtual EntityEntry<TEntity> InsertNow(TEntity entity, bool? ignoreNullValues = null)
    {
        var entityEntry = Insert(entity, ignoreNullValues);
        SaveNow();
        return entityEntry;
    }

    /// <summary>
    /// 新增一条记录并立即提交
    /// </summary>
    /// <param name="entity">实体</param>
    /// <param name="acceptAllChangesOnSuccess">接受所有更改</param>
    /// <param name="ignoreNullValues"></param>
    /// <returns>数据库中返回的实体</returns>
    public virtual EntityEntry<TEntity> InsertNow(TEntity entity, bool acceptAllChangesOnSuccess, bool? ignoreNullValues = null)
    {
        var entityEntry = Insert(entity, ignoreNullValues);
        SaveNow(acceptAllChangesOnSuccess);
        return entityEntry;
    }

    /// <summary>
    /// 新增多条记录
    /// </summary>
    /// <param name="entities">多个实体</param>
    public virtual void InsertNow(params TEntity[] entities)
    {
        Insert(entities);
        SaveNow();
    }

    /// <summary>
    /// 新增多条记录
    /// </summary>
    /// <param name="entities">多个实体</param>
    /// <param name="acceptAllChangesOnSuccess">接受所有更改</param>
    public virtual void InsertNow(TEntity[] entities, bool acceptAllChangesOnSuccess)
    {
        Insert(entities);
        SaveNow(acceptAllChangesOnSuccess);
    }

    /// <summary>
    /// 新增多条记录
    /// </summary>
    /// <param name="entities">多个实体</param>
    public virtual void InsertNow(IEnumerable<TEntity> entities)
    {
        Insert(entities);
        SaveNow();
    }

    /// <summary>
    /// 新增多条记录
    /// </summary>
    /// <param name="entities">多个实体</param>
    /// <param name="acceptAllChangesOnSuccess">接受所有更改</param>
    public virtual void InsertNow(IEnumerable<TEntity> entities, bool acceptAllChangesOnSuccess)
    {
        Insert(entities);
        SaveNow(acceptAllChangesOnSuccess);
    }

    /// <summary>
    /// 新增一条记录并立即提交
    /// </summary>
    /// <param name="entity">实体</param>
    /// <param name="ignoreNullValues"></param>
    /// <param name="cancellationToken">取消异步令牌</param>
    /// <returns>数据库中返回的实体</returns>
    public virtual async Task<EntityEntry<TEntity>> InsertNowAsync(TEntity entity, bool? ignoreNullValues = null, CancellationToken cancellationToken = default)
    {
        var entityEntry = await InsertAsync(entity, ignoreNullValues, cancellationToken);
        await SaveNowAsync(cancellationToken);
        return entityEntry;
    }

    /// <summary>
    /// 新增一条记录并立即提交
    /// </summary>
    /// <param name="entity">实体</param>
    /// <param name="acceptAllChangesOnSuccess">接受所有更改</param>
    /// <param name="ignoreNullValues"></param>
    /// <param name="cancellationToken">取消异步令牌</param>
    /// <returns>数据库中返回的实体</returns>
    public virtual async Task<EntityEntry<TEntity>> InsertNowAsync(TEntity entity, bool acceptAllChangesOnSuccess, bool? ignoreNullValues = null, CancellationToken cancellationToken = default)
    {
        var entityEntry = await InsertAsync(entity, ignoreNullValues, cancellationToken);
        await SaveNowAsync(acceptAllChangesOnSuccess, cancellationToken);
        return entityEntry;
    }

    /// <summary>
    /// 新增多条记录并立即提交
    /// </summary>
    /// <param name="entities">多个实体</param>
    /// <returns>Task</returns>
    public virtual async Task InsertNowAsync(params TEntity[] entities)
    {
        await InsertAsync(entities);
        await SaveNowAsync();
    }

    /// <summary>
    /// 新增多条记录并立即提交
    /// </summary>
    /// <param name="entities">多个实体</param>
    /// <param name="cancellationToken">取消异步令牌</param>
    /// <returns>Task</returns>
    public virtual async Task InsertNowAsync(TEntity[] entities, CancellationToken cancellationToken = default)
    {
        await InsertAsync(entities);
        await SaveNowAsync(cancellationToken);
    }

    /// <summary>
    /// 新增多条记录并立即提交
    /// </summary>
    /// <param name="entities">多个实体</param>
    /// <param name="acceptAllChangesOnSuccess">接受所有更改</param>
    /// <param name="cancellationToken">取消异步令牌</param>
    /// <returns>Task</returns>
    public virtual async Task InsertNowAsync(TEntity[] entities, bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        await InsertAsync(entities);
        await SaveNowAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    /// <summary>
    /// 新增多条记录并立即提交
    /// </summary>
    /// <param name="entities">多个实体</param>
    /// <param name="cancellationToken">取消异步令牌</param>
    /// <returns>Task</returns>
    public virtual async Task InsertNowAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default)
    {
        await InsertAsync(entities, cancellationToken);
        await SaveNowAsync(cancellationToken);
    }

    /// <summary>
    /// 新增多条记录并立即提交
    /// </summary>
    /// <param name="entities">多个实体</param>
    /// <param name="acceptAllChangesOnSuccess">接受所有更改</param>
    /// <param name="cancellationToken">取消异步令牌</param>
    /// <returns>Task</returns>
    public virtual async Task InsertNowAsync(IEnumerable<TEntity> entities, bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        await InsertAsync(entities, cancellationToken);
        await SaveNowAsync(acceptAllChangesOnSuccess, cancellationToken);
    }
}