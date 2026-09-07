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
using Air.Cloud.EntityFrameWork.Core;
using Air.Cloud.EntityFrameWork.Core.Repositories;

using Microsoft.EntityFrameworkCore.ChangeTracking;

using System.Linq.Expressions;

namespace Air.Cloud.EntityFrameWork.Core.Internal;

/// <summary>
/// 实体执行组件
/// </summary>
public sealed partial class EntityExecutePart<TEntity>
    where TEntity : class, IPrivateEntity, new()
{
    /// <summary>
    /// 获取实体同类（族群）
    /// </summary>
    /// <returns>DbSet{TEntity}</returns>
    public DbSet<TEntity> Ethnics()
    {
        return GetRepository().Entities;
    }

    /// <summary>
    /// 新增一条记录
    /// </summary>
    /// <param name="ignoreNullValues"></param>
    /// <returns>代理的实体</returns>
    public EntityEntry<TEntity> Insert(bool? ignoreNullValues = null)
    {
        return GetRepository().Insert(Entity, ignoreNullValues);
    }

    /// <summary>
    /// 新增一条记录
    /// </summary>
    /// <param name="ignoreNullValues"></param>
    /// <param name="cancellationToken">异步取消令牌</param>
    /// <returns>代理的实体</returns>
    public Task<EntityEntry<TEntity>> InsertAsync(bool? ignoreNullValues = null, CancellationToken cancellationToken = default)
    {
        return GetRepository().InsertAsync(Entity, ignoreNullValues, cancellationToken);
    }

    /// <summary>
    /// 更新一条记录
    /// </summary>
    /// <param name="ignoreNullValues"></param>
    /// <returns>代理中的实体</returns>
    public EntityEntry<TEntity> Update(bool? ignoreNullValues = null)
    {
        return GetRepository().Update(Entity, ignoreNullValues);
    }

    /// <summary>
    /// 更新一条记录
    /// </summary>
    /// <param name="ignoreNullValues"></param>
    /// <returns>代理中的实体</returns>
    public Task<EntityEntry<TEntity>> UpdateAsync(bool? ignoreNullValues = null)
    {
        return GetRepository().UpdateAsync(Entity, ignoreNullValues);
    }

    /// <summary>
    /// 更新一条记录中特定属性
    /// </summary>
    /// <param name="propertyNames">属性名</param>
    /// <param name="ignoreNullValues"></param>
    /// <returns>代理中的实体</returns>
    public EntityEntry<TEntity> UpdateInclude(string[] propertyNames, bool? ignoreNullValues = null)
    {
        return GetRepository().UpdateInclude(Entity, propertyNames, ignoreNullValues);
    }

    /// <summary>
    /// 更新一条记录中特定属性
    /// </summary>
    /// <param name="propertyPredicates">属性表达式</param>
    /// <param name="ignoreNullValues"></param>
    /// <returns>代理中的实体</returns>
    public EntityEntry<TEntity> UpdateInclude(Expression<Func<TEntity, object>>[] propertyPredicates, bool? ignoreNullValues = null)
    {
        return GetRepository().UpdateInclude(Entity, propertyPredicates, ignoreNullValues);
    }

    /// <summary>
    /// 更新一条记录中特定属性
    /// </summary>
    /// <param name="propertyNames">属性名</param>
    /// <param name="ignoreNullValues"></param>
    /// <returns>代理中的实体</returns>
    public EntityEntry<TEntity> UpdateInclude(IEnumerable<string> propertyNames, bool? ignoreNullValues = null)
    {
        return GetRepository().UpdateInclude(Entity, propertyNames, ignoreNullValues);
    }

    /// <summary>
    /// 更新一条记录中特定属性
    /// </summary>
    /// <param name="propertyPredicates">属性表达式</param>
    /// <param name="ignoreNullValues"></param>
    /// <returns>代理中的实体</returns>
    public EntityEntry<TEntity> UpdateInclude(IEnumerable<Expression<Func<TEntity, object>>> propertyPredicates, bool? ignoreNullValues = null)
    {
        return GetRepository().UpdateInclude(Entity, propertyPredicates, ignoreNullValues);
    }

    /// <summary>
    /// 更新一条记录中的特定属性
    /// </summary>
    /// <param name="propertyNames">属性名</param>
    /// <param name="ignoreNullValues"></param>
    /// <returns>代理中的实体</returns>
    public Task<EntityEntry<TEntity>> UpdateIncludeAsync(string[] propertyNames, bool? ignoreNullValues = null)
    {
        return GetRepository().UpdateIncludeAsync(Entity, propertyNames, ignoreNullValues);
    }

    /// <summary>
    /// 更新一条记录中的特定属性
    /// </summary>
    /// <param name="propertyPredicates">属性表达式</param>
    /// <param name="ignoreNullValues"></param>
    /// <returns>代理中的实体</returns>
    public Task<EntityEntry<TEntity>> UpdateIncludeAsync(Expression<Func<TEntity, object>>[] propertyPredicates, bool? ignoreNullValues = null)
    {
        return GetRepository().UpdateIncludeAsync(Entity, propertyPredicates, ignoreNullValues);
    }

    /// <summary>
    /// 更新一条记录中的特定属性
    /// </summary>
    /// <param name="propertyNames">属性表达式</param>
    /// <param name="ignoreNullValues"></param>
    /// <returns>代理中的实体</returns>
    public Task<EntityEntry<TEntity>> UpdateIncludeAsync(IEnumerable<string> propertyNames, bool? ignoreNullValues = null)
    {
        return GetRepository().UpdateIncludeAsync(Entity, propertyNames, ignoreNullValues);
    }

    /// <summary>
    /// 更新一条记录中的特定属性
    /// </summary>
    /// <param name="propertyPredicates">属性表达式</param>
    /// <param name="ignoreNullValues"></param>
    /// <returns>代理中的实体</returns>
    public Task<EntityEntry<TEntity>> UpdateIncludeAsync(IEnumerable<Expression<Func<TEntity, object>>> propertyPredicates, bool? ignoreNullValues = null)
    {
        return GetRepository().UpdateIncludeAsync(Entity, propertyPredicates, ignoreNullValues);
    }

    /// <summary>
    /// 更新一条记录中特定属性
    /// </summary>
    /// <param name="propertyNames">属性名</param>
    /// <param name="ignoreNullValues"></param>
    /// <returns>代理中的实体</returns>
    public EntityEntry<TEntity> UpdateExclude(string[] propertyNames, bool? ignoreNullValues = null)
    {
        return GetRepository().UpdateExclude(Entity, propertyNames, ignoreNullValues);
    }

    /// <summary>
    /// 更新一条记录中特定属性
    /// </summary>
    /// <param name="propertyPredicates">属性表达式</param>
    /// <param name="ignoreNullValues"></param>
    /// <returns>代理中的实体</returns>
    public EntityEntry<TEntity> UpdateExclude(Expression<Func<TEntity, object>>[] propertyPredicates, bool? ignoreNullValues = null)
    {
        return GetRepository().UpdateExclude(Entity, propertyPredicates, ignoreNullValues);
    }

    /// <summary>
    /// 更新一条记录中特定属性
    /// </summary>
    /// <param name="propertyNames">属性名</param>
    /// <param name="ignoreNullValues"></param>
    /// <returns>代理中的实体</returns>
    public EntityEntry<TEntity> UpdateExclude(IEnumerable<string> propertyNames, bool? ignoreNullValues = null)
    {
        return GetRepository().UpdateExclude(Entity, propertyNames, ignoreNullValues);
    }

    /// <summary>
    /// 更新一条记录中特定属性
    /// </summary>
    /// <param name="propertyPredicates">属性表达式</param>
    /// <param name="ignoreNullValues"></param>
    /// <returns>代理中的实体</returns>
    public EntityEntry<TEntity> UpdateExclude(IEnumerable<Expression<Func<TEntity, object>>> propertyPredicates, bool? ignoreNullValues = null)
    {
        return GetRepository().UpdateExclude(Entity, propertyPredicates, ignoreNullValues);
    }

    /// <summary>
    /// 更新一条记录并排除属性
    /// </summary>
    /// <param name="propertyNames">属性名</param>
    /// <param name="ignoreNullValues"></param>
    /// <returns>代理中的实体</returns>
    public Task<EntityEntry<TEntity>> UpdateExcludeAsync(string[] propertyNames, bool? ignoreNullValues = null)
    {
        return GetRepository().UpdateExcludeAsync(Entity, propertyNames, ignoreNullValues);
    }

    /// <summary>
    /// 更新一条记录并排除属性
    /// </summary>
    /// <param name="propertyPredicates">属性表达式</param>
    /// <param name="ignoreNullValues"></param>
    /// <returns>代理中的实体</returns>
    public Task<EntityEntry<TEntity>> UpdateExcludeAsync(Expression<Func<TEntity, object>>[] propertyPredicates, bool? ignoreNullValues = null)
    {
        return GetRepository().UpdateExcludeAsync(Entity, propertyPredicates, ignoreNullValues);
    }

    /// <summary>
    /// 更新一条记录并排除属性
    /// </summary>
    /// <param name="propertyNames">属性表达式</param>
    /// <param name="ignoreNullValues"></param>
    /// <returns>代理中的实体</returns>
    public Task<EntityEntry<TEntity>> UpdateExcludeAsync(IEnumerable<string> propertyNames, bool? ignoreNullValues = null)
    {
        return GetRepository().UpdateExcludeAsync(Entity, propertyNames, ignoreNullValues);
    }

    /// <summary>
    /// 更新一条记录并排除属性
    /// </summary>
    /// <param name="propertyPredicates">属性表达式</param>
    /// <param name="ignoreNullValues"></param>
    /// <returns>代理中的实体</returns>
    public Task<EntityEntry<TEntity>> UpdateExcludeAsync(IEnumerable<Expression<Func<TEntity, object>>> propertyPredicates, bool? ignoreNullValues = null)
    {
        return GetRepository().UpdateExcludeAsync(Entity, propertyPredicates, ignoreNullValues);
    }

    /// <summary>
    /// 删除一条记录
    /// </summary>
    /// <returns>代理中的实体</returns>
    public EntityEntry<TEntity> Delete()
    {
        return GetRepository().Delete(Entity);
    }

    /// <summary>
    /// 删除一条记录
    /// </summary>
    /// <returns>代理中的实体</returns>
    public Task<EntityEntry<TEntity>> DeleteAsync()
    {
        return GetRepository().DeleteAsync(Entity);
    }

    /// <summary>
    /// 获取实体仓储
    /// </summary>
    /// <returns></returns>
    private IPrivateRepository<TEntity> GetRepository()
    {
        return Db.GetRepository<TEntity>(DbContextLocator, ContextScoped);
    }
}
