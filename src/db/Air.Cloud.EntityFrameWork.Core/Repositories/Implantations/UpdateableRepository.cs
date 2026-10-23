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

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

using System.Linq.Expressions;

namespace Air.Cloud.EntityFrameWork.Core.Repositories.Implantations;

/// <summary>
/// <para>zh-cn:可更新仓储实现。</para>
/// <para>en-us:Repository implementation for update operations.</para>
/// </summary>
public partial class PrivateRepository<TEntity>
    where TEntity : class, IPrivateEntity, new()
{
    /// <summary>
    /// <para>zh-cn:更新一条记录</para>
    /// <para>en-us:Updates a record.</para>
    /// </summary>
    /// <param name="entity"><para>zh-cn:实体</para><para>en-us:The entity.</para></param>
    /// <param name="ignoreNullValues"><para>zh-cn:ignoreNullValues 参数。</para><para>en-us:The ignoreNullValues value.</para></param>
    /// <returns><para>zh-cn:代理中的实体</para><para>en-us:The tracked entity entry.</para></returns>
    public virtual EntityEntry<TEntity> Update(TEntity entity, bool? ignoreNullValues = null)
    {
        var entityEntry = Entities.Update(entity);

        // 忽略空值
        IgnoreNullValues(ref entity, ignoreNullValues);

        return entityEntry;
    }

    /// <summary>
    /// <para>zh-cn:更新多条记录</para>
    /// <para>en-us:Updates a record.</para>
    /// </summary>
    /// <param name="entities"><para>zh-cn:多个实体</para><para>en-us:The entities to process.</para></param>
    public virtual void Update(params TEntity[] entities)
    {
        Entities.UpdateRange(entities);
    }

    /// <summary>
    /// <para>zh-cn:更新多条记录</para>
    /// <para>en-us:Updates a record.</para>
    /// </summary>
    /// <param name="entities"><para>zh-cn:多个实体</para><para>en-us:The entities to process.</para></param>
    public virtual void Update(IEnumerable<TEntity> entities)
    {
        Entities.UpdateRange(entities);
    }

    /// <summary>
    /// <para>zh-cn:更新一条记录</para>
    /// <para>en-us:Updates a record.</para>
    /// </summary>
    /// <param name="entity"><para>zh-cn:实体</para><para>en-us:The entity.</para></param>
    /// <param name="ignoreNullValues"><para>zh-cn:ignoreNullValues 参数。</para><para>en-us:The ignoreNullValues value.</para></param>
    /// <returns><para>zh-cn:代理中的实体</para><para>en-us:The tracked entity entry.</para></returns>
    public virtual Task<EntityEntry<TEntity>> UpdateAsync(TEntity entity, bool? ignoreNullValues = null)
    {
        return Task.FromResult(Update(entity, ignoreNullValues));
    }

    /// <summary>
    /// <para>zh-cn:更新多条记录</para>
    /// <para>en-us:Updates a record.</para>
    /// </summary>
    /// <param name="entities"><para>zh-cn:多个实体</para><para>en-us:The entities to process.</para></param>
    /// <returns><para>zh-cn:Task</para><para>en-us:A task representing completion.</para></returns>
    public virtual Task UpdateAsync(params TEntity[] entities)
    {
        Update(entities);
        return Task.CompletedTask;
    }

    /// <summary>
    /// <para>zh-cn:更新多条记录</para>
    /// <para>en-us:Updates a record.</para>
    /// </summary>
    /// <param name="entities"><para>zh-cn:多个实体</para><para>en-us:The entities to process.</para></param>
    /// <returns><para>zh-cn:Task</para><para>en-us:A task representing completion.</para></returns>
    public virtual Task UpdateAsync(IEnumerable<TEntity> entities)
    {
        Update(entities);
        return Task.CompletedTask;
    }

    /// <summary>
    /// <para>zh-cn:更新一条记录中特定属性</para>
    /// <para>en-us:Updates selected properties of a record.</para>
    /// </summary>
    /// <param name="entity"><para>zh-cn:实体</para><para>en-us:The entity.</para></param>
    /// <param name="propertyNames"><para>zh-cn:属性名</para><para>en-us:The property names.</para></param>
    /// <param name="ignoreNullValues"><para>zh-cn:ignoreNullValues 参数。</para><para>en-us:The ignoreNullValues value.</para></param>
    /// <returns><para>zh-cn:代理中的实体</para><para>en-us:The tracked entity entry.</para></returns>
    public virtual EntityEntry<TEntity> UpdateInclude(TEntity entity, string[] propertyNames, bool? ignoreNullValues = null)
    {
        var entityEntry = ChangeEntityState(entity, EntityState.Detached);
        foreach (var propertyName in propertyNames)
        {
            EntityPropertyEntry(entity, propertyName).IsModified = true;
        }

        // 忽略空值
        IgnoreNullValues(ref entity, ignoreNullValues);

        return entityEntry;
    }

    /// <summary>
    /// <para>zh-cn:更新一条记录中特定属性</para>
    /// <para>en-us:Updates selected properties of a record.</para>
    /// </summary>
    /// <param name="entity"><para>zh-cn:实体</para><para>en-us:The entity.</para></param>
    /// <param name="propertyPredicates"><para>zh-cn:属性表达式</para><para>en-us:The property expressions.</para></param>
    /// <param name="ignoreNullValues"><para>zh-cn:ignoreNullValues 参数。</para><para>en-us:The ignoreNullValues value.</para></param>
    /// <returns><para>zh-cn:代理中的实体</para><para>en-us:The tracked entity entry.</para></returns>
    public virtual EntityEntry<TEntity> UpdateInclude(TEntity entity, Expression<Func<TEntity, object>>[] propertyPredicates, bool? ignoreNullValues = null)
    {
        // 判断是非参数只有一个，且是一个匿名类型
        if (propertyPredicates?.Length == 1 && propertyPredicates[0].Body is NewExpression newExpression)
        {
            var propertyNames = newExpression.Members?.Select(u => u.Name) ?? Enumerable.Empty<string>();
            return UpdateInclude(entity, propertyNames, ignoreNullValues);
        }
        else
        {
            var entityEntry = ChangeEntityState(entity, EntityState.Detached);
            foreach (var propertyPredicate in propertyPredicates ?? throw new ArgumentNullException(nameof(propertyPredicates)))
            {
                EntityPropertyEntry(entity, propertyPredicate).IsModified = true;
            }

            // 忽略空值
            IgnoreNullValues(ref entity, ignoreNullValues);

            return entityEntry;
        }
    }

    /// <summary>
    /// <para>zh-cn:更新一条记录中特定属性</para>
    /// <para>en-us:Updates selected properties of a record.</para>
    /// </summary>
    /// <param name="entity"><para>zh-cn:实体</para><para>en-us:The entity.</para></param>
    /// <param name="propertyNames"><para>zh-cn:属性名</para><para>en-us:The property names.</para></param>
    /// <param name="ignoreNullValues"><para>zh-cn:ignoreNullValues 参数。</para><para>en-us:The ignoreNullValues value.</para></param>
    /// <returns><para>zh-cn:代理中的实体</para><para>en-us:The tracked entity entry.</para></returns>
    public virtual EntityEntry<TEntity> UpdateInclude(TEntity entity, IEnumerable<string> propertyNames, bool? ignoreNullValues = null)
    {
        return UpdateInclude(entity, propertyNames.ToArray(), ignoreNullValues);
    }

    /// <summary>
    /// <para>zh-cn:更新一条记录中特定属性</para>
    /// <para>en-us:Updates selected properties of a record.</para>
    /// </summary>
    /// <param name="entity"><para>zh-cn:实体</para><para>en-us:The entity.</para></param>
    /// <param name="propertyPredicates"><para>zh-cn:属性表达式</para><para>en-us:The property expressions.</para></param>
    /// <param name="ignoreNullValues"><para>zh-cn:ignoreNullValues 参数。</para><para>en-us:The ignoreNullValues value.</para></param>
    /// <returns><para>zh-cn:代理中的实体</para><para>en-us:The tracked entity entry.</para></returns>
    public virtual EntityEntry<TEntity> UpdateInclude(TEntity entity, IEnumerable<Expression<Func<TEntity, object>>> propertyPredicates, bool? ignoreNullValues = null)
    {
        return UpdateInclude(entity, propertyPredicates.ToArray(), ignoreNullValues);
    }

    /// <summary>
    /// <para>zh-cn:更新一条记录中的特定属性</para>
    /// <para>en-us:Updates selected properties of a record.</para>
    /// </summary>
    /// <param name="entity"><para>zh-cn:实体</para><para>en-us:The entity.</para></param>
    /// <param name="propertyNames"><para>zh-cn:属性名</para><para>en-us:The property names.</para></param>
    /// <param name="ignoreNullValues"><para>zh-cn:ignoreNullValues 参数。</para><para>en-us:The ignoreNullValues value.</para></param>
    /// <returns><para>zh-cn:代理中的实体</para><para>en-us:The tracked entity entry.</para></returns>
    public virtual Task<EntityEntry<TEntity>> UpdateIncludeAsync(TEntity entity, string[] propertyNames, bool? ignoreNullValues = null)
    {
        return Task.FromResult(UpdateInclude(entity, propertyNames, ignoreNullValues));
    }

    /// <summary>
    /// <para>zh-cn:更新一条记录中的特定属性</para>
    /// <para>en-us:Updates selected properties of a record.</para>
    /// </summary>
    /// <param name="entity"><para>zh-cn:实体</para><para>en-us:The entity.</para></param>
    /// <param name="propertyPredicates"><para>zh-cn:属性表达式</para><para>en-us:The property expressions.</para></param>
    /// <param name="ignoreNullValues"><para>zh-cn:ignoreNullValues 参数。</para><para>en-us:The ignoreNullValues value.</para></param>
    /// <returns><para>zh-cn:代理中的实体</para><para>en-us:The tracked entity entry.</para></returns>
    public virtual Task<EntityEntry<TEntity>> UpdateIncludeAsync(TEntity entity, Expression<Func<TEntity, object>>[] propertyPredicates, bool? ignoreNullValues = null)
    {
        return Task.FromResult(UpdateInclude(entity, propertyPredicates, ignoreNullValues));
    }

    /// <summary>
    /// <para>zh-cn:更新一条记录中的特定属性</para>
    /// <para>en-us:Updates selected properties of a record.</para>
    /// </summary>
    /// <param name="entity"><para>zh-cn:实体</para><para>en-us:The entity.</para></param>
    /// <param name="propertyNames"><para>zh-cn:属性表达式</para><para>en-us:The property expressions.</para></param>
    /// <param name="ignoreNullValues"><para>zh-cn:ignoreNullValues 参数。</para><para>en-us:The ignoreNullValues value.</para></param>
    /// <returns><para>zh-cn:代理中的实体</para><para>en-us:The tracked entity entry.</para></returns>
    public virtual Task<EntityEntry<TEntity>> UpdateIncludeAsync(TEntity entity, IEnumerable<string> propertyNames, bool? ignoreNullValues = null)
    {
        return Task.FromResult(UpdateInclude(entity, propertyNames, ignoreNullValues));
    }

    /// <summary>
    /// <para>zh-cn:更新一条记录中的特定属性</para>
    /// <para>en-us:Updates selected properties of a record.</para>
    /// </summary>
    /// <param name="entity"><para>zh-cn:实体</para><para>en-us:The entity.</para></param>
    /// <param name="propertyPredicates"><para>zh-cn:属性表达式</para><para>en-us:The property expressions.</para></param>
    /// <param name="ignoreNullValues"><para>zh-cn:ignoreNullValues 参数。</para><para>en-us:The ignoreNullValues value.</para></param>
    /// <returns><para>zh-cn:代理中的实体</para><para>en-us:The tracked entity entry.</para></returns>
    public virtual Task<EntityEntry<TEntity>> UpdateIncludeAsync(TEntity entity, IEnumerable<Expression<Func<TEntity, object>>> propertyPredicates, bool? ignoreNullValues = null)
    {
        return Task.FromResult(UpdateInclude(entity, propertyPredicates, ignoreNullValues));
    }

    /// <summary>
    /// <para>zh-cn:更新一条记录并排除属性</para>
    /// <para>en-us:Updates a record while excluding selected properties.</para>
    /// </summary>
    /// <param name="entity"><para>zh-cn:实体</para><para>en-us:The entity.</para></param>
    /// <param name="propertyNames"><para>zh-cn:属性名</para><para>en-us:The property names.</para></param>
    /// <param name="ignoreNullValues"><para>zh-cn:ignoreNullValues 参数。</para><para>en-us:The ignoreNullValues value.</para></param>
    /// <returns><para>zh-cn:代理中的实体</para><para>en-us:The tracked entity entry.</para></returns>
    public virtual EntityEntry<TEntity> UpdateExclude(TEntity entity, string[] propertyNames, bool? ignoreNullValues = null)
    {
        var entityEntry = ChangeEntityState(entity, EntityState.Modified);
        foreach (var propertyName in propertyNames)
        {
            EntityPropertyEntry(entity, propertyName).IsModified = false;
        }

        // 忽略空值
        IgnoreNullValues(ref entity, ignoreNullValues);

        return entityEntry;
    }

    /// <summary>
    /// <para>zh-cn:更新一条记录并排除属性</para>
    /// <para>en-us:Updates a record while excluding selected properties.</para>
    /// </summary>
    /// <param name="entity"><para>zh-cn:实体</para><para>en-us:The entity.</para></param>
    /// <param name="propertyPredicates"><para>zh-cn:属性表达式</para><para>en-us:The property expressions.</para></param>
    /// <param name="ignoreNullValues"><para>zh-cn:ignoreNullValues 参数。</para><para>en-us:The ignoreNullValues value.</para></param>
    /// <returns><para>zh-cn:代理中的实体</para><para>en-us:The tracked entity entry.</para></returns>
    public virtual EntityEntry<TEntity> UpdateExclude(TEntity entity, Expression<Func<TEntity, object>>[] propertyPredicates, bool? ignoreNullValues = null)
    {
        // 判断是非参数只有一个，且是一个匿名类型
        if (propertyPredicates?.Length == 1 && propertyPredicates[0].Body is NewExpression newExpression)
        {
            var propertyNames = newExpression.Members?.Select(u => u.Name) ?? Enumerable.Empty<string>();
            return UpdateExclude(entity, propertyNames, ignoreNullValues);
        }
        else
        {
            var entityEntry = ChangeEntityState(entity, EntityState.Modified);
            foreach (var propertyPredicate in propertyPredicates ?? throw new ArgumentNullException(nameof(propertyPredicates)))
            {
                EntityPropertyEntry(entity, propertyPredicate).IsModified = false;
            }

            // 忽略空值
            IgnoreNullValues(ref entity, ignoreNullValues);

            return entityEntry;
        }
    }

    /// <summary>
    /// <para>zh-cn:更新一条记录并排除属性</para>
    /// <para>en-us:Updates a record while excluding selected properties.</para>
    /// </summary>
    /// <param name="entity"><para>zh-cn:实体</para><para>en-us:The entity.</para></param>
    /// <param name="propertyNames"><para>zh-cn:属性名</para><para>en-us:The property names.</para></param>
    /// <param name="ignoreNullValues"><para>zh-cn:ignoreNullValues 参数。</para><para>en-us:The ignoreNullValues value.</para></param>
    /// <returns><para>zh-cn:代理中的实体</para><para>en-us:The tracked entity entry.</para></returns>
    public virtual EntityEntry<TEntity> UpdateExclude(TEntity entity, IEnumerable<string> propertyNames, bool? ignoreNullValues = null)
    {
        return UpdateExclude(entity, propertyNames.ToArray(), ignoreNullValues);
    }

    /// <summary>
    /// <para>zh-cn:更新一条记录并排除属性</para>
    /// <para>en-us:Updates a record while excluding selected properties.</para>
    /// </summary>
    /// <param name="entity"><para>zh-cn:实体</para><para>en-us:The entity.</para></param>
    /// <param name="propertyPredicates"><para>zh-cn:属性表达式</para><para>en-us:The property expressions.</para></param>
    /// <param name="ignoreNullValues"><para>zh-cn:ignoreNullValues 参数。</para><para>en-us:The ignoreNullValues value.</para></param>
    /// <returns><para>zh-cn:代理中的实体</para><para>en-us:The tracked entity entry.</para></returns>
    public virtual EntityEntry<TEntity> UpdateExclude(TEntity entity, IEnumerable<Expression<Func<TEntity, object>>> propertyPredicates, bool? ignoreNullValues = null)
    {
        return UpdateExclude(entity, propertyPredicates.ToArray(), ignoreNullValues);
    }

    /// <summary>
    /// <para>zh-cn:更新一条记录并排除属性</para>
    /// <para>en-us:Updates a record while excluding selected properties.</para>
    /// </summary>
    /// <param name="entity"><para>zh-cn:实体</para><para>en-us:The entity.</para></param>
    /// <param name="propertyNames"><para>zh-cn:属性名</para><para>en-us:The property names.</para></param>
    /// <param name="ignoreNullValues"><para>zh-cn:ignoreNullValues 参数。</para><para>en-us:The ignoreNullValues value.</para></param>
    /// <returns><para>zh-cn:代理中的实体</para><para>en-us:The tracked entity entry.</para></returns>
    public virtual Task<EntityEntry<TEntity>> UpdateExcludeAsync(TEntity entity, string[] propertyNames, bool? ignoreNullValues = null)
    {
        return Task.FromResult(UpdateExclude(entity, propertyNames, ignoreNullValues));
    }

    /// <summary>
    /// <para>zh-cn:更新一条记录并排除属性</para>
    /// <para>en-us:Updates a record while excluding selected properties.</para>
    /// </summary>
    /// <param name="entity"><para>zh-cn:实体</para><para>en-us:The entity.</para></param>
    /// <param name="propertyPredicates"><para>zh-cn:属性表达式</para><para>en-us:The property expressions.</para></param>
    /// <param name="ignoreNullValues"><para>zh-cn:ignoreNullValues 参数。</para><para>en-us:The ignoreNullValues value.</para></param>
    /// <returns><para>zh-cn:代理中的实体</para><para>en-us:The tracked entity entry.</para></returns>
    public virtual Task<EntityEntry<TEntity>> UpdateExcludeAsync(TEntity entity, Expression<Func<TEntity, object>>[] propertyPredicates, bool? ignoreNullValues = null)
    {
        return Task.FromResult(UpdateExclude(entity, propertyPredicates, ignoreNullValues));
    }

    /// <summary>
    /// <para>zh-cn:更新一条记录并排除属性</para>
    /// <para>en-us:Updates a record while excluding selected properties.</para>
    /// </summary>
    /// <param name="entity"><para>zh-cn:实体</para><para>en-us:The entity.</para></param>
    /// <param name="propertyNames"><para>zh-cn:属性表达式</para><para>en-us:The property expressions.</para></param>
    /// <param name="ignoreNullValues"><para>zh-cn:ignoreNullValues 参数。</para><para>en-us:The ignoreNullValues value.</para></param>
    /// <returns><para>zh-cn:代理中的实体</para><para>en-us:The tracked entity entry.</para></returns>
    public virtual Task<EntityEntry<TEntity>> UpdateExcludeAsync(TEntity entity, IEnumerable<string> propertyNames, bool? ignoreNullValues = null)
    {
        return Task.FromResult(UpdateExclude(entity, propertyNames, ignoreNullValues));
    }

    /// <summary>
    /// <para>zh-cn:更新一条记录并排除属性</para>
    /// <para>en-us:Updates a record while excluding selected properties.</para>
    /// </summary>
    /// <param name="entity"><para>zh-cn:实体</para><para>en-us:The entity.</para></param>
    /// <param name="propertyPredicates"><para>zh-cn:属性表达式</para><para>en-us:The property expressions.</para></param>
    /// <param name="ignoreNullValues"><para>zh-cn:ignoreNullValues 参数。</para><para>en-us:The ignoreNullValues value.</para></param>
    /// <returns><para>zh-cn:代理中的实体</para><para>en-us:The tracked entity entry.</para></returns>
    public virtual Task<EntityEntry<TEntity>> UpdateExcludeAsync(TEntity entity, IEnumerable<Expression<Func<TEntity, object>>> propertyPredicates, bool? ignoreNullValues = null)
    {
        return Task.FromResult(UpdateExclude(entity, propertyPredicates, ignoreNullValues));
    }

    /// <summary>
    /// <para>zh-cn:忽略空值属性</para>
    /// <para>en-us:Performs the operation described by this member.</para>
    /// </summary>
    /// <param name="entity"><para>zh-cn:entity 参数。</para><para>en-us:The entity value.</para></param>
    /// <param name="ignoreNullValues"><para>zh-cn:ignoreNullValues 参数。</para><para>en-us:The ignoreNullValues value.</para></param>
    private void IgnoreNullValues(ref TEntity entity, bool? ignoreNullValues = null)
    {
        var isIgnore = ignoreNullValues ?? DynamicContext.InsertOrUpdateIgnoreNullValues;
        if (isIgnore == false) return;

        // 获取所有的属性
        var properties = EntityType?.GetProperties();
        if (properties == null) return;

        foreach (var propety in properties)
        {
            var entityProperty = EntityPropertyEntry(entity, propety.Name);
            var propertyValue = entityProperty?.CurrentValue;
            var propertyType = entityProperty?.Metadata?.PropertyInfo?.PropertyType;

            // 判断是否是无效的值，比如为 null，默认时间，以及空 Guid 值
            var isInvalid = propertyValue == null
                            || (propertyType == typeof(DateTime) && propertyValue?.ToString() == new DateTime().ToString())
                            || (propertyType == typeof(DateTimeOffset) && propertyValue?.ToString() == new DateTimeOffset().ToString())
                            || (propertyType == typeof(Guid) && propertyValue?.ToString() == Guid.Empty.ToString());

            if (isInvalid && entityProperty != null)
            {
                entityProperty.IsModified = false;
            }
        }
    }
}
