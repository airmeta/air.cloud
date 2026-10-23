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
/// <para>zh-cn:可删除仓储实现。</para>
/// <para>en-us:Repository implementation for delete operations.</para>
/// </summary>
public partial class PrivateRepository<TEntity>
    where TEntity : class, IPrivateEntity, new()
{
    /// <summary>
    /// <para>zh-cn:删除一条记录</para>
    /// <para>en-us:Deletes a record.</para>
    /// </summary>
    /// <param name="entity"><para>zh-cn:实体</para><para>en-us:The entity.</para></param>
    /// <returns><para>zh-cn:代理中的实体</para><para>en-us:The tracked entity entry.</para></returns>
    public virtual EntityEntry<TEntity> Delete(TEntity entity)
    {
        return Entities.Remove(entity);
    }

    /// <summary>
    /// <para>zh-cn:删除多条记录</para>
    /// <para>en-us:Deletes a record.</para>
    /// </summary>
    /// <param name="entities"><para>zh-cn:多个实体</para><para>en-us:The entities to process.</para></param>
    public virtual void Delete(params TEntity[] entities)
    {
        Entities.RemoveRange(entities);
    }

    /// <summary>
    /// <para>zh-cn:删除多条记录</para>
    /// <para>en-us:Deletes a record.</para>
    /// </summary>
    /// <param name="entities"><para>zh-cn:多个实体</para><para>en-us:The entities to process.</para></param>
    public virtual void Delete(IEnumerable<TEntity> entities)
    {
        Entities.RemoveRange(entities);
    }

    /// <summary>
    /// <para>zh-cn:删除一条记录</para>
    /// <para>en-us:Deletes a record.</para>
    /// </summary>
    /// <param name="entity"><para>zh-cn:实体</para><para>en-us:The entity.</para></param>
    /// <returns><para>zh-cn:代理中的实体</para><para>en-us:The tracked entity entry.</para></returns>
    public virtual Task<EntityEntry<TEntity>> DeleteAsync(TEntity entity)
    {
        return Task.FromResult(Delete(entity));
    }

    /// <summary>
    /// <para>zh-cn:删除多条记录</para>
    /// <para>en-us:Deletes a record.</para>
    /// </summary>
    /// <param name="entities"><para>zh-cn:多个实体</para><para>en-us:The entities to process.</para></param>
    /// <returns><para>zh-cn:Task</para><para>en-us:A task representing completion.</para></returns>
    public virtual Task DeleteAsync(params TEntity[] entities)
    {
        Delete(entities);
        return Task.CompletedTask;
    }

    /// <summary>
    /// <para>zh-cn:删除多条记录</para>
    /// <para>en-us:Deletes a record.</para>
    /// </summary>
    /// <param name="entities"><para>zh-cn:多个实体</para><para>en-us:The entities to process.</para></param>
    /// <returns><para>zh-cn:Task</para><para>en-us:A task representing completion.</para></returns>
    public virtual Task DeleteAsync(IEnumerable<TEntity> entities)
    {
        Delete(entities);
        return Task.CompletedTask;
    }

    /// <summary>
    /// <para>zh-cn:根据主键删除一条记录</para>
    /// <para>en-us:Deletes a record.</para>
    /// </summary>
    /// <param name="key"><para>zh-cn:主键</para><para>en-us:The key value.</para></param>
    public virtual void Delete(object key)
    {
        var deletedEntity = BuildDeletedEntity(key);
        if (deletedEntity != null) return;

        // 如果主键不存在，则采用 Find 查询
        var entity = FindOrDefault(key);
        if (entity != null) Delete(entity);
    }

    /// <summary>
    /// <para>zh-cn:根据主键删除一条记录</para>
    /// <para>en-us:Deletes a record.</para>
    /// </summary>
    /// <param name="key"><para>zh-cn:主键</para><para>en-us:The key value.</para></param>
    /// <param name="cancellationToken"><para>zh-cn:异步取消令牌</para><para>en-us:The cancellation token.</para></param>
    /// <returns><para>zh-cn:Task</para><para>en-us:A task representing completion.</para></returns>
    public virtual async Task DeleteAsync(object key, CancellationToken cancellationToken = default)
    {
        var deletedEntity = BuildDeletedEntity(key);
        if (deletedEntity != null) return;

        // 如果主键不存在，则采用 FindAsync 查询
        var entity = await FindOrDefaultAsync(key, cancellationToken);
        if (entity != null) await DeleteAsync(entity);
    }

    /// <summary>
    /// <para>zh-cn:构建被删除的实体</para>
    /// <para>en-us:Deletes a record.</para>
    /// </summary>
    /// <param name="key"><para>zh-cn:主键</para><para>en-us:The key value.</para></param>
    /// <param name="isRealDelete"><para>zh-cn:是否真删除</para><para>en-us:The isRealDelete value.</para></param>
    /// <returns><para>zh-cn:操作结果。</para><para>en-us:The operation result.</para></returns>
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
