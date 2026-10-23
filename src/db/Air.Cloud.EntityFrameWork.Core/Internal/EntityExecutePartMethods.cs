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
/// <para>zh-cn:实体执行组件。</para>
/// <para>en-us:Provides entity insert, update, and delete operations.</para>
/// </summary>
public sealed partial class EntityExecutePart<TEntity>
    where TEntity : class, IPrivateEntity, new()
{
    /// <summary>
    /// <para>zh-cn:获取实体同类（族群）</para>
    /// <para>en-us:Gets the entity set for this entity type.</para>
    /// </summary>
    /// <returns><para>zh-cn:DbSet{TEntity}</para><para>en-us:The tracked entity entry.</para></returns>
    public DbSet<TEntity> Ethnics()
    {
        return GetRepository().Entities;
    }

    /// <summary>
    /// <para>zh-cn:新增一条记录</para>
    /// <para>en-us:Inserts a record.</para>
    /// </summary>
    /// <param name="ignoreNullValues"><para>zh-cn:ignoreNullValues 参数。</para><para>en-us:The ignoreNullValues value.</para></param>
    /// <returns><para>zh-cn:代理的实体</para><para>en-us:The tracked entity entry.</para></returns>
    public EntityEntry<TEntity> Insert(bool? ignoreNullValues = null)
    {
        return GetRepository().Insert(Entity, ignoreNullValues);
    }

    /// <summary>
    /// <para>zh-cn:新增一条记录</para>
    /// <para>en-us:Inserts a record.</para>
    /// </summary>
    /// <param name="ignoreNullValues"><para>zh-cn:ignoreNullValues 参数。</para><para>en-us:The ignoreNullValues value.</para></param>
    /// <param name="cancellationToken"><para>zh-cn:异步取消令牌</para><para>en-us:The cancellation token.</para></param>
    /// <returns><para>zh-cn:代理的实体</para><para>en-us:The tracked entity entry.</para></returns>
    public Task<EntityEntry<TEntity>> InsertAsync(bool? ignoreNullValues = null, CancellationToken cancellationToken = default)
    {
        return GetRepository().InsertAsync(Entity, ignoreNullValues, cancellationToken);
    }

    /// <summary>
    /// <para>zh-cn:更新一条记录</para>
    /// <para>en-us:Updates a record.</para>
    /// </summary>
    /// <param name="ignoreNullValues"><para>zh-cn:ignoreNullValues 参数。</para><para>en-us:The ignoreNullValues value.</para></param>
    /// <returns><para>zh-cn:代理中的实体</para><para>en-us:The tracked entity entry.</para></returns>
    public EntityEntry<TEntity> Update(bool? ignoreNullValues = null)
    {
        return GetRepository().Update(Entity, ignoreNullValues);
    }

    /// <summary>
    /// <para>zh-cn:更新一条记录</para>
    /// <para>en-us:Updates a record.</para>
    /// </summary>
    /// <param name="ignoreNullValues"><para>zh-cn:ignoreNullValues 参数。</para><para>en-us:The ignoreNullValues value.</para></param>
    /// <returns><para>zh-cn:代理中的实体</para><para>en-us:The tracked entity entry.</para></returns>
    public Task<EntityEntry<TEntity>> UpdateAsync(bool? ignoreNullValues = null)
    {
        return GetRepository().UpdateAsync(Entity, ignoreNullValues);
    }

    /// <summary>
    /// <para>zh-cn:更新一条记录中特定属性</para>
    /// <para>en-us:Updates selected properties of a record.</para>
    /// </summary>
    /// <param name="propertyNames"><para>zh-cn:属性名</para><para>en-us:The property names.</para></param>
    /// <param name="ignoreNullValues"><para>zh-cn:ignoreNullValues 参数。</para><para>en-us:The ignoreNullValues value.</para></param>
    /// <returns><para>zh-cn:代理中的实体</para><para>en-us:The tracked entity entry.</para></returns>
    public EntityEntry<TEntity> UpdateInclude(string[] propertyNames, bool? ignoreNullValues = null)
    {
        return GetRepository().UpdateInclude(Entity, propertyNames, ignoreNullValues);
    }

    /// <summary>
    /// <para>zh-cn:更新一条记录中特定属性</para>
    /// <para>en-us:Updates selected properties of a record.</para>
    /// </summary>
    /// <param name="propertyPredicates"><para>zh-cn:属性表达式</para><para>en-us:The property expressions.</para></param>
    /// <param name="ignoreNullValues"><para>zh-cn:ignoreNullValues 参数。</para><para>en-us:The ignoreNullValues value.</para></param>
    /// <returns><para>zh-cn:代理中的实体</para><para>en-us:The tracked entity entry.</para></returns>
    public EntityEntry<TEntity> UpdateInclude(Expression<Func<TEntity, object>>[] propertyPredicates, bool? ignoreNullValues = null)
    {
        return GetRepository().UpdateInclude(Entity, propertyPredicates, ignoreNullValues);
    }

    /// <summary>
    /// <para>zh-cn:更新一条记录中特定属性</para>
    /// <para>en-us:Updates selected properties of a record.</para>
    /// </summary>
    /// <param name="propertyNames"><para>zh-cn:属性名</para><para>en-us:The property names.</para></param>
    /// <param name="ignoreNullValues"><para>zh-cn:ignoreNullValues 参数。</para><para>en-us:The ignoreNullValues value.</para></param>
    /// <returns><para>zh-cn:代理中的实体</para><para>en-us:The tracked entity entry.</para></returns>
    public EntityEntry<TEntity> UpdateInclude(IEnumerable<string> propertyNames, bool? ignoreNullValues = null)
    {
        return GetRepository().UpdateInclude(Entity, propertyNames, ignoreNullValues);
    }

    /// <summary>
    /// <para>zh-cn:更新一条记录中特定属性</para>
    /// <para>en-us:Updates selected properties of a record.</para>
    /// </summary>
    /// <param name="propertyPredicates"><para>zh-cn:属性表达式</para><para>en-us:The property expressions.</para></param>
    /// <param name="ignoreNullValues"><para>zh-cn:ignoreNullValues 参数。</para><para>en-us:The ignoreNullValues value.</para></param>
    /// <returns><para>zh-cn:代理中的实体</para><para>en-us:The tracked entity entry.</para></returns>
    public EntityEntry<TEntity> UpdateInclude(IEnumerable<Expression<Func<TEntity, object>>> propertyPredicates, bool? ignoreNullValues = null)
    {
        return GetRepository().UpdateInclude(Entity, propertyPredicates, ignoreNullValues);
    }

    /// <summary>
    /// <para>zh-cn:更新一条记录中的特定属性</para>
    /// <para>en-us:Updates selected properties of a record.</para>
    /// </summary>
    /// <param name="propertyNames"><para>zh-cn:属性名</para><para>en-us:The property names.</para></param>
    /// <param name="ignoreNullValues"><para>zh-cn:ignoreNullValues 参数。</para><para>en-us:The ignoreNullValues value.</para></param>
    /// <returns><para>zh-cn:代理中的实体</para><para>en-us:The tracked entity entry.</para></returns>
    public Task<EntityEntry<TEntity>> UpdateIncludeAsync(string[] propertyNames, bool? ignoreNullValues = null)
    {
        return GetRepository().UpdateIncludeAsync(Entity, propertyNames, ignoreNullValues);
    }

    /// <summary>
    /// <para>zh-cn:更新一条记录中的特定属性</para>
    /// <para>en-us:Updates selected properties of a record.</para>
    /// </summary>
    /// <param name="propertyPredicates"><para>zh-cn:属性表达式</para><para>en-us:The property expressions.</para></param>
    /// <param name="ignoreNullValues"><para>zh-cn:ignoreNullValues 参数。</para><para>en-us:The ignoreNullValues value.</para></param>
    /// <returns><para>zh-cn:代理中的实体</para><para>en-us:The tracked entity entry.</para></returns>
    public Task<EntityEntry<TEntity>> UpdateIncludeAsync(Expression<Func<TEntity, object>>[] propertyPredicates, bool? ignoreNullValues = null)
    {
        return GetRepository().UpdateIncludeAsync(Entity, propertyPredicates, ignoreNullValues);
    }

    /// <summary>
    /// <para>zh-cn:更新一条记录中的特定属性</para>
    /// <para>en-us:Updates selected properties of a record.</para>
    /// </summary>
    /// <param name="propertyNames"><para>zh-cn:属性表达式</para><para>en-us:The property expressions.</para></param>
    /// <param name="ignoreNullValues"><para>zh-cn:ignoreNullValues 参数。</para><para>en-us:The ignoreNullValues value.</para></param>
    /// <returns><para>zh-cn:代理中的实体</para><para>en-us:The tracked entity entry.</para></returns>
    public Task<EntityEntry<TEntity>> UpdateIncludeAsync(IEnumerable<string> propertyNames, bool? ignoreNullValues = null)
    {
        return GetRepository().UpdateIncludeAsync(Entity, propertyNames, ignoreNullValues);
    }

    /// <summary>
    /// <para>zh-cn:更新一条记录中的特定属性</para>
    /// <para>en-us:Updates selected properties of a record.</para>
    /// </summary>
    /// <param name="propertyPredicates"><para>zh-cn:属性表达式</para><para>en-us:The property expressions.</para></param>
    /// <param name="ignoreNullValues"><para>zh-cn:ignoreNullValues 参数。</para><para>en-us:The ignoreNullValues value.</para></param>
    /// <returns><para>zh-cn:代理中的实体</para><para>en-us:The tracked entity entry.</para></returns>
    public Task<EntityEntry<TEntity>> UpdateIncludeAsync(IEnumerable<Expression<Func<TEntity, object>>> propertyPredicates, bool? ignoreNullValues = null)
    {
        return GetRepository().UpdateIncludeAsync(Entity, propertyPredicates, ignoreNullValues);
    }

    /// <summary>
    /// <para>zh-cn:更新一条记录中特定属性</para>
    /// <para>en-us:Updates selected properties of a record.</para>
    /// </summary>
    /// <param name="propertyNames"><para>zh-cn:属性名</para><para>en-us:The property names.</para></param>
    /// <param name="ignoreNullValues"><para>zh-cn:ignoreNullValues 参数。</para><para>en-us:The ignoreNullValues value.</para></param>
    /// <returns><para>zh-cn:代理中的实体</para><para>en-us:The tracked entity entry.</para></returns>
    public EntityEntry<TEntity> UpdateExclude(string[] propertyNames, bool? ignoreNullValues = null)
    {
        return GetRepository().UpdateExclude(Entity, propertyNames, ignoreNullValues);
    }

    /// <summary>
    /// <para>zh-cn:更新一条记录中特定属性</para>
    /// <para>en-us:Updates selected properties of a record.</para>
    /// </summary>
    /// <param name="propertyPredicates"><para>zh-cn:属性表达式</para><para>en-us:The property expressions.</para></param>
    /// <param name="ignoreNullValues"><para>zh-cn:ignoreNullValues 参数。</para><para>en-us:The ignoreNullValues value.</para></param>
    /// <returns><para>zh-cn:代理中的实体</para><para>en-us:The tracked entity entry.</para></returns>
    public EntityEntry<TEntity> UpdateExclude(Expression<Func<TEntity, object>>[] propertyPredicates, bool? ignoreNullValues = null)
    {
        return GetRepository().UpdateExclude(Entity, propertyPredicates, ignoreNullValues);
    }

    /// <summary>
    /// <para>zh-cn:更新一条记录中特定属性</para>
    /// <para>en-us:Updates selected properties of a record.</para>
    /// </summary>
    /// <param name="propertyNames"><para>zh-cn:属性名</para><para>en-us:The property names.</para></param>
    /// <param name="ignoreNullValues"><para>zh-cn:ignoreNullValues 参数。</para><para>en-us:The ignoreNullValues value.</para></param>
    /// <returns><para>zh-cn:代理中的实体</para><para>en-us:The tracked entity entry.</para></returns>
    public EntityEntry<TEntity> UpdateExclude(IEnumerable<string> propertyNames, bool? ignoreNullValues = null)
    {
        return GetRepository().UpdateExclude(Entity, propertyNames, ignoreNullValues);
    }

    /// <summary>
    /// <para>zh-cn:更新一条记录中特定属性</para>
    /// <para>en-us:Updates selected properties of a record.</para>
    /// </summary>
    /// <param name="propertyPredicates"><para>zh-cn:属性表达式</para><para>en-us:The property expressions.</para></param>
    /// <param name="ignoreNullValues"><para>zh-cn:ignoreNullValues 参数。</para><para>en-us:The ignoreNullValues value.</para></param>
    /// <returns><para>zh-cn:代理中的实体</para><para>en-us:The tracked entity entry.</para></returns>
    public EntityEntry<TEntity> UpdateExclude(IEnumerable<Expression<Func<TEntity, object>>> propertyPredicates, bool? ignoreNullValues = null)
    {
        return GetRepository().UpdateExclude(Entity, propertyPredicates, ignoreNullValues);
    }

    /// <summary>
    /// <para>zh-cn:更新一条记录并排除属性</para>
    /// <para>en-us:Updates a record while excluding selected properties.</para>
    /// </summary>
    /// <param name="propertyNames"><para>zh-cn:属性名</para><para>en-us:The property names.</para></param>
    /// <param name="ignoreNullValues"><para>zh-cn:ignoreNullValues 参数。</para><para>en-us:The ignoreNullValues value.</para></param>
    /// <returns><para>zh-cn:代理中的实体</para><para>en-us:The tracked entity entry.</para></returns>
    public Task<EntityEntry<TEntity>> UpdateExcludeAsync(string[] propertyNames, bool? ignoreNullValues = null)
    {
        return GetRepository().UpdateExcludeAsync(Entity, propertyNames, ignoreNullValues);
    }

    /// <summary>
    /// <para>zh-cn:更新一条记录并排除属性</para>
    /// <para>en-us:Updates a record while excluding selected properties.</para>
    /// </summary>
    /// <param name="propertyPredicates"><para>zh-cn:属性表达式</para><para>en-us:The property expressions.</para></param>
    /// <param name="ignoreNullValues"><para>zh-cn:ignoreNullValues 参数。</para><para>en-us:The ignoreNullValues value.</para></param>
    /// <returns><para>zh-cn:代理中的实体</para><para>en-us:The tracked entity entry.</para></returns>
    public Task<EntityEntry<TEntity>> UpdateExcludeAsync(Expression<Func<TEntity, object>>[] propertyPredicates, bool? ignoreNullValues = null)
    {
        return GetRepository().UpdateExcludeAsync(Entity, propertyPredicates, ignoreNullValues);
    }

    /// <summary>
    /// <para>zh-cn:更新一条记录并排除属性</para>
    /// <para>en-us:Updates a record while excluding selected properties.</para>
    /// </summary>
    /// <param name="propertyNames"><para>zh-cn:属性表达式</para><para>en-us:The property expressions.</para></param>
    /// <param name="ignoreNullValues"><para>zh-cn:ignoreNullValues 参数。</para><para>en-us:The ignoreNullValues value.</para></param>
    /// <returns><para>zh-cn:代理中的实体</para><para>en-us:The tracked entity entry.</para></returns>
    public Task<EntityEntry<TEntity>> UpdateExcludeAsync(IEnumerable<string> propertyNames, bool? ignoreNullValues = null)
    {
        return GetRepository().UpdateExcludeAsync(Entity, propertyNames, ignoreNullValues);
    }

    /// <summary>
    /// <para>zh-cn:更新一条记录并排除属性</para>
    /// <para>en-us:Updates a record while excluding selected properties.</para>
    /// </summary>
    /// <param name="propertyPredicates"><para>zh-cn:属性表达式</para><para>en-us:The property expressions.</para></param>
    /// <param name="ignoreNullValues"><para>zh-cn:ignoreNullValues 参数。</para><para>en-us:The ignoreNullValues value.</para></param>
    /// <returns><para>zh-cn:代理中的实体</para><para>en-us:The tracked entity entry.</para></returns>
    public Task<EntityEntry<TEntity>> UpdateExcludeAsync(IEnumerable<Expression<Func<TEntity, object>>> propertyPredicates, bool? ignoreNullValues = null)
    {
        return GetRepository().UpdateExcludeAsync(Entity, propertyPredicates, ignoreNullValues);
    }

    /// <summary>
    /// <para>zh-cn:删除一条记录</para>
    /// <para>en-us:Deletes a record.</para>
    /// </summary>
    /// <returns><para>zh-cn:代理中的实体</para><para>en-us:The tracked entity entry.</para></returns>
    public EntityEntry<TEntity> Delete()
    {
        return GetRepository().Delete(Entity);
    }

    /// <summary>
    /// <para>zh-cn:删除一条记录</para>
    /// <para>en-us:Deletes a record.</para>
    /// </summary>
    /// <returns><para>zh-cn:代理中的实体</para><para>en-us:The tracked entity entry.</para></returns>
    public Task<EntityEntry<TEntity>> DeleteAsync()
    {
        return GetRepository().DeleteAsync(Entity);
    }

    /// <summary>
    /// <para>zh-cn:获取实体仓储</para>
    /// <para>en-us:Performs the operation described by this member.</para>
    /// </summary>
    /// <returns><para>zh-cn:操作结果。</para><para>en-us:The operation result.</para></returns>
    private IPrivateRepository<TEntity> GetRepository()
    {
        return Db.GetRepository<TEntity>(DbContextLocator, ContextScoped);
    }
}
