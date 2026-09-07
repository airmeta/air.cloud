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
/// 可删除仓储分部类
/// </summary>
public partial class PrivateRepository<TEntity>
    where TEntity : class, IPrivateEntity, new()
{
    /// <summary>
    /// 删除一条记录
    /// </summary>
    /// <param name="entity">实体</param>
    /// <returns>代理中的实体</returns>
    public virtual EntityEntry<TEntity> Delete(TEntity entity)
    {
        return Entities.Remove(entity);
    }

    /// <summary>
    /// 删除多条记录
    /// </summary>
    /// <param name="entities">多个实体</param>
    public virtual void Delete(params TEntity[] entities)
    {
        Entities.RemoveRange(entities);
    }

    /// <summary>
    /// 删除多条记录
    /// </summary>
    /// <param name="entities">多个实体</param>
    public virtual void Delete(IEnumerable<TEntity> entities)
    {
        Entities.RemoveRange(entities);
    }

    /// <summary>
    /// 删除一条记录
    /// </summary>
    /// <param name="entity">实体</param>
    /// <returns>代理中的实体</returns>
    public virtual Task<EntityEntry<TEntity>> DeleteAsync(TEntity entity)
    {
        return Task.FromResult(Delete(entity));
    }

    /// <summary>
    /// 删除多条记录
    /// </summary>
    /// <param name="entities">多个实体</param>
    /// <returns>Task</returns>
    public virtual Task DeleteAsync(params TEntity[] entities)
    {
        Delete(entities);
        return Task.CompletedTask;
    }

    /// <summary>
    /// 删除多条记录
    /// </summary>
    /// <param name="entities">多个实体</param>
    /// <returns>Task</returns>
    public virtual Task DeleteAsync(IEnumerable<TEntity> entities)
    {
        Delete(entities);
        return Task.CompletedTask;
    }

    /// <summary>
    /// 根据主键删除一条记录
    /// </summary>
    /// <param name="key">主键</param>
    public virtual void Delete(object key)
    {
        var deletedEntity = BuildDeletedEntity(key);
        if (deletedEntity != null) return;

        // 如果主键不存在，则采用 Find 查询
        var entity = FindOrDefault(key);
        if (entity != null) Delete(entity);
    }

    /// <summary>
    /// 根据主键删除一条记录
    /// </summary>
    /// <param name="key">主键</param>
    /// <param name="cancellationToken">异步取消令牌</param>
    /// <returns>Task</returns>
    public virtual async Task DeleteAsync(object key, CancellationToken cancellationToken = default)
    {
        var deletedEntity = BuildDeletedEntity(key);
        if (deletedEntity != null) return;

        // 如果主键不存在，则采用 FindAsync 查询
        var entity = await FindOrDefaultAsync(key, cancellationToken);
        if (entity != null) await DeleteAsync(entity);
    }

    /// <summary>
    /// 构建被删除的实体
    /// </summary>
    /// <param name="key">主键</param>
    /// <param name="isRealDelete">是否真删除</param>
    /// <returns></returns>
    private TEntity BuildDeletedEntity(object key, bool isRealDelete = true)
    {
        // 读取主键
        var keyProperty = EntityType.FindPrimaryKey()?.Properties.AsEnumerable().FirstOrDefault()?.PropertyInfo
            ?? throw new InvalidOperationException($"实体 {typeof(TEntity).FullName} 未配置主键，无法构建删除实体。");

        // 判断当前主键是否被跟踪了
        var tracking = CheckTrackState(key, out var entityEntry, keyProperty.Name);
        if (tracking)
        {
            // 设置实体状态为已删除
            if (isRealDelete) ChangeEntityState(entityEntry, EntityState.Deleted);

            return entityEntry.Entity as TEntity
                ?? throw new InvalidOperationException($"被跟踪实体无法转换为 {typeof(TEntity).FullName}。");
        }

        // 如果没有被跟踪，创建实体对象并设置主键值
        var entity = Activator.CreateInstance<TEntity>();
        keyProperty.SetValue(entity, key);

        // 设置实体状态为已删除
        if (isRealDelete) ChangeEntityState(entity, EntityState.Deleted);

        return entity;
    }
}
