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
using Air.Cloud.EntityFrameWork.Core.Internal;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;


using System.Linq.Expressions;

namespace Air.Cloud.EntityFrameWork.Core.Extensions.Repositories;

/// <summary>
/// <para>zh-cn:实体扩展方法。</para>
/// <para>en-us:Entity extension methods for repository operations.</para>
/// </summary>
[IgnoreScanning]
public static class IEntityExtensions
{
    /// <summary>
    /// <para>zh-cn:设置实体</para>
    /// <para>en-us:Sets the entity used by this operation.</para>
    /// </summary>
    /// <typeparam name="TEntity"><para>zh-cn:实体类型。</para><para>en-us:The entity type.</para></typeparam>
    /// <param name="entity"><para>zh-cn:entity 参数。</para><para>en-us:The entity value.</para></param>
    /// <returns><para>zh-cn:操作结果。</para><para>en-us:The operation result.</para></returns>
    public static EntityExecutePart<TEntity> SetEntity<TEntity>(this TEntity entity)
        where TEntity : class, IPrivateEntity, new()
    {
        return new EntityExecutePart<TEntity>().SetEntity(entity);
    }

    /// <summary>
    /// <para>zh-cn:设置数据库执行作用域</para>
    /// <para>en-us:Performs the operation described by this member.</para>
    /// </summary>
    /// <typeparam name="TEntity"><para>zh-cn:实体类型。</para><para>en-us:The entity type.</para></typeparam>
    /// <param name="entity"><para>zh-cn:entity 参数。</para><para>en-us:The entity value.</para></param>
    /// <param name="serviceProvider"><para>zh-cn:serviceProvider 参数。</para><para>en-us:The serviceProvider value.</para></param>
    /// <returns><para>zh-cn:操作结果。</para><para>en-us:The operation result.</para></returns>
    public static EntityExecutePart<TEntity> SetContextScoped<TEntity>(this TEntity entity, IServiceProvider serviceProvider)
        where TEntity : class, IPrivateEntity, new()
    {
        return new EntityExecutePart<TEntity>().SetEntity(entity).SetContextScoped(serviceProvider);
    }

    /// <summary>
    /// <para>zh-cn:设置数据库上下文定位器</para>
    /// <para>en-us:Sets or selects the database context for this operation.</para>
    /// </summary>
    /// <typeparam name="TEntity"><para>zh-cn:实体类型。</para><para>en-us:The entity type.</para></typeparam>
    /// <typeparam name="TDbContextLocator"><para>zh-cn:数据库上下文定位器类型。</para><para>en-us:The database context locator type.</para></typeparam>
    /// <param name="entity"><para>zh-cn:entity 参数。</para><para>en-us:The entity value.</para></param>
    /// <returns><para>zh-cn:操作结果。</para><para>en-us:The operation result.</para></returns>
    public static EntityExecutePart<TEntity> Change<TEntity, TDbContextLocator>(this TEntity entity)
        where TEntity : class, IPrivateEntity, new()
        where TDbContextLocator : class, IDbContextLocator
    {
        return new EntityExecutePart<TEntity>().SetEntity(entity).Change<TDbContextLocator>();
    }

    /// <summary>
    /// <para>zh-cn:设置数据库上下文定位器</para>
    /// <para>en-us:Sets or selects the database context for this operation.</para>
    /// </summary>
    /// <typeparam name="TEntity"><para>zh-cn:实体类型。</para><para>en-us:The entity type.</para></typeparam>
    /// <param name="entity"><para>zh-cn:entity 参数。</para><para>en-us:The entity value.</para></param>
    /// <param name="dbContextLocator"><para>zh-cn:dbContextLocator 参数。</para><para>en-us:The dbContextLocator value.</para></param>
    /// <returns><para>zh-cn:操作结果。</para><para>en-us:The operation result.</para></returns>
    public static EntityExecutePart<TEntity> Change<TEntity>(this TEntity entity, Type dbContextLocator)
        where TEntity : class, IPrivateEntity, new()
    {
        return new EntityExecutePart<TEntity>().SetEntity(entity).Change(dbContextLocator);
    }

    /// <summary>
    /// <para>zh-cn:获取实体同类（族群）</para>
    /// <para>en-us:Gets the entity set for this entity type.</para>
    /// </summary>
    /// <typeparam name="TEntity"><para>zh-cn:实体</para><para>en-us:The generic parameter type.</para></typeparam>
    /// <returns><para>zh-cn:DbSet{TEntity}</para><para>en-us:The tracked entity entry.</para></returns>
    public static DbSet<TEntity> Ethnics<TEntity>(this TEntity entity)
        where TEntity : class, IPrivateEntity, new()
    {
        return new EntityExecutePart<TEntity>().SetEntity(entity).Ethnics();
    }

    /// <summary>
    /// <para>zh-cn:新增一条记录</para>
    /// <para>en-us:Inserts a record.</para>
    /// </summary>
    /// <typeparam name="TEntity"><para>zh-cn:实体</para><para>en-us:The generic parameter type.</para></typeparam>
    /// <param name="entity"><para>zh-cn:实体</para><para>en-us:The entity.</para></param>
    /// <param name="ignoreNullValues"><para>zh-cn:ignoreNullValues 参数。</para><para>en-us:The ignoreNullValues value.</para></param>
    /// <returns><para>zh-cn:代理的实体</para><para>en-us:The tracked entity entry.</para></returns>
    public static EntityEntry<TEntity> Insert<TEntity>(this TEntity entity, bool? ignoreNullValues = null)
        where TEntity : class, IPrivateEntity, new()
    {
        return new EntityExecutePart<TEntity>().SetEntity(entity).Insert(ignoreNullValues);
    }

    /// <summary>
    /// <para>zh-cn:新增一条记录</para>
    /// <para>en-us:Inserts a record.</para>
    /// </summary>
    /// <typeparam name="TEntity"><para>zh-cn:实体</para><para>en-us:The generic parameter type.</para></typeparam>
    /// <param name="entity"><para>zh-cn:实体</para><para>en-us:The entity.</para></param>
    /// <param name="ignoreNullValues"><para>zh-cn:ignoreNullValues 参数。</para><para>en-us:The ignoreNullValues value.</para></param>
    /// <param name="cancellationToken"><para>zh-cn:异步取消令牌</para><para>en-us:The cancellation token.</para></param>
    /// <returns><para>zh-cn:代理的实体</para><para>en-us:The tracked entity entry.</para></returns>
    public static Task<EntityEntry<TEntity>> InsertAsync<TEntity>(this TEntity entity, bool? ignoreNullValues = null, CancellationToken cancellationToken = default)
        where TEntity : class, IPrivateEntity, new()
    {
        return new EntityExecutePart<TEntity>().SetEntity(entity).InsertAsync(ignoreNullValues, cancellationToken);
    }

    /// <summary>
    /// <para>zh-cn:更新一条记录</para>
    /// <para>en-us:Updates a record.</para>
    /// </summary>
    /// <typeparam name="TEntity"><para>zh-cn:实体</para><para>en-us:The generic parameter type.</para></typeparam>
    /// <param name="entity"><para>zh-cn:实体</para><para>en-us:The entity.</para></param>
    /// <param name="ignoreNullValues"><para>zh-cn:ignoreNullValues 参数。</para><para>en-us:The ignoreNullValues value.</para></param>
    /// <returns><para>zh-cn:代理中的实体</para><para>en-us:The tracked entity entry.</para></returns>
    public static EntityEntry<TEntity> Update<TEntity>(this TEntity entity, bool? ignoreNullValues = null)
        where TEntity : class, IPrivateEntity, new()
    {
        return new EntityExecutePart<TEntity>().SetEntity(entity).Update(ignoreNullValues);
    }

    /// <summary>
    /// <para>zh-cn:更新一条记录</para>
    /// <para>en-us:Updates a record.</para>
    /// </summary>
    /// <typeparam name="TEntity"><para>zh-cn:实体</para><para>en-us:The generic parameter type.</para></typeparam>
    /// <param name="entity"><para>zh-cn:实体</para><para>en-us:The entity.</para></param>
    /// <param name="ignoreNullValues"><para>zh-cn:ignoreNullValues 参数。</para><para>en-us:The ignoreNullValues value.</para></param>
    /// <returns><para>zh-cn:代理中的实体</para><para>en-us:The tracked entity entry.</para></returns>
    public static Task<EntityEntry<TEntity>> UpdateAsync<TEntity>(this TEntity entity, bool? ignoreNullValues = null)
         where TEntity : class, IPrivateEntity, new()
    {
        return new EntityExecutePart<TEntity>().SetEntity(entity).UpdateAsync(ignoreNullValues);
    }

    /// <summary>
    /// <para>zh-cn:更新一条记录中特定属性</para>
    /// <para>en-us:Updates selected properties of a record.</para>
    /// </summary>
    /// <typeparam name="TEntity"><para>zh-cn:实体</para><para>en-us:The generic parameter type.</para></typeparam>
    /// <param name="entity"><para>zh-cn:实体</para><para>en-us:The entity.</para></param>
    /// <param name="propertyNames"><para>zh-cn:属性名</para><para>en-us:The property names.</para></param>
    /// <param name="ignoreNullValues"><para>zh-cn:ignoreNullValues 参数。</para><para>en-us:The ignoreNullValues value.</para></param>
    /// <returns><para>zh-cn:代理中的实体</para><para>en-us:The tracked entity entry.</para></returns>
    public static EntityEntry<TEntity> UpdateInclude<TEntity>(this TEntity entity, string[] propertyNames, bool? ignoreNullValues = null)
        where TEntity : class, IPrivateEntity, new()
    {
        return new EntityExecutePart<TEntity>().SetEntity(entity).UpdateInclude(propertyNames, ignoreNullValues);
    }

    /// <summary>
    /// <para>zh-cn:更新一条记录中特定属性</para>
    /// <para>en-us:Updates selected properties of a record.</para>
    /// </summary>
    /// <typeparam name="TEntity"><para>zh-cn:实体</para><para>en-us:The generic parameter type.</para></typeparam>
    /// <param name="entity"><para>zh-cn:实体</para><para>en-us:The entity.</para></param>
    /// <param name="propertyPredicates"><para>zh-cn:属性表达式</para><para>en-us:The property expressions.</para></param>
    /// <param name="ignoreNullValues"><para>zh-cn:ignoreNullValues 参数。</para><para>en-us:The ignoreNullValues value.</para></param>
    /// <returns><para>zh-cn:代理中的实体</para><para>en-us:The tracked entity entry.</para></returns>
    public static EntityEntry<TEntity> UpdateInclude<TEntity>(this TEntity entity, Expression<Func<TEntity, object>>[] propertyPredicates, bool? ignoreNullValues = null)
        where TEntity : class, IPrivateEntity, new()
    {
        return new EntityExecutePart<TEntity>().SetEntity(entity).UpdateInclude(propertyPredicates, ignoreNullValues);
    }

    /// <summary>
    /// <para>zh-cn:更新一条记录中特定属性</para>
    /// <para>en-us:Updates selected properties of a record.</para>
    /// </summary>
    /// <typeparam name="TEntity"><para>zh-cn:实体</para><para>en-us:The generic parameter type.</para></typeparam>
    /// <param name="entity"><para>zh-cn:实体</para><para>en-us:The entity.</para></param>
    /// <param name="propertyNames"><para>zh-cn:属性名</para><para>en-us:The property names.</para></param>
    /// <param name="ignoreNullValues"><para>zh-cn:ignoreNullValues 参数。</para><para>en-us:The ignoreNullValues value.</para></param>
    /// <returns><para>zh-cn:代理中的实体</para><para>en-us:The tracked entity entry.</para></returns>
    public static EntityEntry<TEntity> UpdateInclude<TEntity>(this TEntity entity, IEnumerable<string> propertyNames, bool? ignoreNullValues = null)
        where TEntity : class, IPrivateEntity, new()
    {
        return new EntityExecutePart<TEntity>().SetEntity(entity).UpdateInclude(propertyNames, ignoreNullValues);
    }

    /// <summary>
    /// <para>zh-cn:更新一条记录中特定属性</para>
    /// <para>en-us:Updates selected properties of a record.</para>
    /// </summary>
    /// <typeparam name="TEntity"><para>zh-cn:实体</para><para>en-us:The generic parameter type.</para></typeparam>
    /// <param name="entity"><para>zh-cn:实体</para><para>en-us:The entity.</para></param>
    /// <param name="propertyPredicates"><para>zh-cn:属性表达式</para><para>en-us:The property expressions.</para></param>
    /// <param name="ignoreNullValues"><para>zh-cn:ignoreNullValues 参数。</para><para>en-us:The ignoreNullValues value.</para></param>
    /// <returns><para>zh-cn:代理中的实体</para><para>en-us:The tracked entity entry.</para></returns>
    public static EntityEntry<TEntity> UpdateInclude<TEntity>(this TEntity entity, IEnumerable<Expression<Func<TEntity, object>>> propertyPredicates, bool? ignoreNullValues = null)
        where TEntity : class, IPrivateEntity, new()
    {
        return new EntityExecutePart<TEntity>().SetEntity(entity).UpdateInclude(propertyPredicates, ignoreNullValues);
    }

    /// <summary>
    /// <para>zh-cn:更新一条记录中的特定属性</para>
    /// <para>en-us:Updates selected properties of a record.</para>
    /// </summary>
    /// <typeparam name="TEntity"><para>zh-cn:实体</para><para>en-us:The generic parameter type.</para></typeparam>
    /// <param name="entity"><para>zh-cn:实体</para><para>en-us:The entity.</para></param>
    /// <param name="propertyNames"><para>zh-cn:属性名</para><para>en-us:The property names.</para></param>
    /// <param name="ignoreNullValues"><para>zh-cn:ignoreNullValues 参数。</para><para>en-us:The ignoreNullValues value.</para></param>
    /// <returns><para>zh-cn:代理中的实体</para><para>en-us:The tracked entity entry.</para></returns>
    public static Task<EntityEntry<TEntity>> UpdateIncludeAsync<TEntity>(this TEntity entity, string[] propertyNames, bool? ignoreNullValues = null)
        where TEntity : class, IPrivateEntity, new()
    {
        return new EntityExecutePart<TEntity>().SetEntity(entity).UpdateIncludeAsync(propertyNames, ignoreNullValues);
    }

    /// <summary>
    /// <para>zh-cn:更新一条记录中的特定属性</para>
    /// <para>en-us:Updates selected properties of a record.</para>
    /// </summary>
    /// <typeparam name="TEntity"><para>zh-cn:实体</para><para>en-us:The generic parameter type.</para></typeparam>
    /// <param name="entity"><para>zh-cn:实体</para><para>en-us:The entity.</para></param>
    /// <param name="propertyPredicates"><para>zh-cn:属性表达式</para><para>en-us:The property expressions.</para></param>
    /// <param name="ignoreNullValues"><para>zh-cn:ignoreNullValues 参数。</para><para>en-us:The ignoreNullValues value.</para></param>
    /// <returns><para>zh-cn:代理中的实体</para><para>en-us:The tracked entity entry.</para></returns>
    public static Task<EntityEntry<TEntity>> UpdateIncludeAsync<TEntity>(this TEntity entity, Expression<Func<TEntity, object>>[] propertyPredicates, bool? ignoreNullValues = null)
        where TEntity : class, IPrivateEntity, new()
    {
        return new EntityExecutePart<TEntity>().SetEntity(entity).UpdateIncludeAsync(propertyPredicates, ignoreNullValues);
    }

    /// <summary>
    /// <para>zh-cn:更新一条记录中的特定属性</para>
    /// <para>en-us:Updates selected properties of a record.</para>
    /// </summary>
    /// <typeparam name="TEntity"><para>zh-cn:实体</para><para>en-us:The generic parameter type.</para></typeparam>
    /// <param name="entity"><para>zh-cn:实体</para><para>en-us:The entity.</para></param>
    /// <param name="propertyNames"><para>zh-cn:属性表达式</para><para>en-us:The property expressions.</para></param>
    /// <param name="ignoreNullValues"><para>zh-cn:ignoreNullValues 参数。</para><para>en-us:The ignoreNullValues value.</para></param>
    /// <returns><para>zh-cn:代理中的实体</para><para>en-us:The tracked entity entry.</para></returns>
    public static Task<EntityEntry<TEntity>> UpdateIncludeAsync<TEntity>(this TEntity entity, IEnumerable<string> propertyNames, bool? ignoreNullValues = null)
        where TEntity : class, IPrivateEntity, new()
    {
        return new EntityExecutePart<TEntity>().SetEntity(entity).UpdateIncludeAsync(propertyNames, ignoreNullValues);
    }

    /// <summary>
    /// <para>zh-cn:更新一条记录中的特定属性</para>
    /// <para>en-us:Updates selected properties of a record.</para>
    /// </summary>
    /// <typeparam name="TEntity"><para>zh-cn:实体</para><para>en-us:The generic parameter type.</para></typeparam>
    /// <param name="entity"><para>zh-cn:实体</para><para>en-us:The entity.</para></param>
    /// <param name="propertyPredicates"><para>zh-cn:属性表达式</para><para>en-us:The property expressions.</para></param>
    /// <param name="ignoreNullValues"><para>zh-cn:ignoreNullValues 参数。</para><para>en-us:The ignoreNullValues value.</para></param>
    /// <returns><para>zh-cn:代理中的实体</para><para>en-us:The tracked entity entry.</para></returns>
    public static Task<EntityEntry<TEntity>> UpdateIncludeAsync<TEntity>(this TEntity entity, IEnumerable<Expression<Func<TEntity, object>>> propertyPredicates, bool? ignoreNullValues = null)
        where TEntity : class, IPrivateEntity, new()
    {
        return new EntityExecutePart<TEntity>().SetEntity(entity).UpdateIncludeAsync(propertyPredicates, ignoreNullValues);
    }

    /// <summary>
    /// <para>zh-cn:更新一条记录中特定属性</para>
    /// <para>en-us:Updates selected properties of a record.</para>
    /// </summary>
    /// <typeparam name="TEntity"><para>zh-cn:实体</para><para>en-us:The generic parameter type.</para></typeparam>
    /// <param name="entity"><para>zh-cn:实体</para><para>en-us:The entity.</para></param>
    /// <param name="propertyNames"><para>zh-cn:属性名</para><para>en-us:The property names.</para></param>
    /// <param name="ignoreNullValues"><para>zh-cn:ignoreNullValues 参数。</para><para>en-us:The ignoreNullValues value.</para></param>
    /// <returns><para>zh-cn:代理中的实体</para><para>en-us:The tracked entity entry.</para></returns>
    public static EntityEntry<TEntity> UpdateExclude<TEntity>(this TEntity entity, string[] propertyNames, bool? ignoreNullValues = null)
        where TEntity : class, IPrivateEntity, new()
    {
        return new EntityExecutePart<TEntity>().SetEntity(entity).UpdateExclude(propertyNames, ignoreNullValues);
    }

    /// <summary>
    /// <para>zh-cn:更新一条记录中特定属性</para>
    /// <para>en-us:Updates selected properties of a record.</para>
    /// </summary>
    /// <typeparam name="TEntity"><para>zh-cn:实体</para><para>en-us:The generic parameter type.</para></typeparam>
    /// <param name="entity"><para>zh-cn:实体</para><para>en-us:The entity.</para></param>
    /// <param name="propertyPredicates"><para>zh-cn:属性表达式</para><para>en-us:The property expressions.</para></param>
    /// <param name="ignoreNullValues"><para>zh-cn:ignoreNullValues 参数。</para><para>en-us:The ignoreNullValues value.</para></param>
    /// <returns><para>zh-cn:代理中的实体</para><para>en-us:The tracked entity entry.</para></returns>
    public static EntityEntry<TEntity> UpdateExclude<TEntity>(this TEntity entity, Expression<Func<TEntity, object>>[] propertyPredicates, bool? ignoreNullValues = null)
        where TEntity : class, IPrivateEntity, new()
    {
        return new EntityExecutePart<TEntity>().SetEntity(entity).UpdateExclude(propertyPredicates, ignoreNullValues);
    }

    /// <summary>
    /// <para>zh-cn:更新一条记录中特定属性</para>
    /// <para>en-us:Updates selected properties of a record.</para>
    /// </summary>
    /// <typeparam name="TEntity"><para>zh-cn:实体</para><para>en-us:The generic parameter type.</para></typeparam>
    /// <param name="entity"><para>zh-cn:实体</para><para>en-us:The entity.</para></param>
    /// <param name="propertyNames"><para>zh-cn:属性名</para><para>en-us:The property names.</para></param>
    /// <param name="ignoreNullValues"><para>zh-cn:ignoreNullValues 参数。</para><para>en-us:The ignoreNullValues value.</para></param>
    /// <returns><para>zh-cn:代理中的实体</para><para>en-us:The tracked entity entry.</para></returns>
    public static EntityEntry<TEntity> UpdateExclude<TEntity>(this TEntity entity, IEnumerable<string> propertyNames, bool? ignoreNullValues = null)
        where TEntity : class, IPrivateEntity, new()
    {
        return new EntityExecutePart<TEntity>().SetEntity(entity).UpdateExclude(propertyNames, ignoreNullValues);
    }

    /// <summary>
    /// <para>zh-cn:更新一条记录中特定属性</para>
    /// <para>en-us:Updates selected properties of a record.</para>
    /// </summary>
    /// <typeparam name="TEntity"><para>zh-cn:实体</para><para>en-us:The generic parameter type.</para></typeparam>
    /// <param name="entity"><para>zh-cn:实体</para><para>en-us:The entity.</para></param>
    /// <param name="propertyPredicates"><para>zh-cn:属性表达式</para><para>en-us:The property expressions.</para></param>
    /// <param name="ignoreNullValues"><para>zh-cn:ignoreNullValues 参数。</para><para>en-us:The ignoreNullValues value.</para></param>
    /// <returns><para>zh-cn:代理中的实体</para><para>en-us:The tracked entity entry.</para></returns>
    public static EntityEntry<TEntity> UpdateExclude<TEntity>(this TEntity entity, IEnumerable<Expression<Func<TEntity, object>>> propertyPredicates, bool? ignoreNullValues = null)
        where TEntity : class, IPrivateEntity, new()
    {
        return new EntityExecutePart<TEntity>().SetEntity(entity).UpdateExclude(propertyPredicates, ignoreNullValues);
    }

    /// <summary>
    /// <para>zh-cn:更新一条记录并排除属性</para>
    /// <para>en-us:Updates a record while excluding selected properties.</para>
    /// </summary>
    /// <typeparam name="TEntity"><para>zh-cn:实体</para><para>en-us:The generic parameter type.</para></typeparam>
    /// <param name="entity"><para>zh-cn:实体</para><para>en-us:The entity.</para></param>
    /// <param name="propertyNames"><para>zh-cn:属性名</para><para>en-us:The property names.</para></param>
    /// <param name="ignoreNullValues"><para>zh-cn:ignoreNullValues 参数。</para><para>en-us:The ignoreNullValues value.</para></param>
    /// <returns><para>zh-cn:代理中的实体</para><para>en-us:The tracked entity entry.</para></returns>
    public static Task<EntityEntry<TEntity>> UpdateExcludeAsync<TEntity>(this TEntity entity, string[] propertyNames, bool? ignoreNullValues = null)
        where TEntity : class, IPrivateEntity, new()
    {
        return new EntityExecutePart<TEntity>().SetEntity(entity).UpdateExcludeAsync(propertyNames, ignoreNullValues);
    }

    /// <summary>
    /// <para>zh-cn:更新一条记录并排除属性</para>
    /// <para>en-us:Updates a record while excluding selected properties.</para>
    /// </summary>
    /// <typeparam name="TEntity"><para>zh-cn:实体</para><para>en-us:The generic parameter type.</para></typeparam>
    /// <param name="entity"><para>zh-cn:实体</para><para>en-us:The entity.</para></param>
    /// <param name="propertyPredicates"><para>zh-cn:属性表达式</para><para>en-us:The property expressions.</para></param>
    /// <param name="ignoreNullValues"><para>zh-cn:ignoreNullValues 参数。</para><para>en-us:The ignoreNullValues value.</para></param>
    /// <returns><para>zh-cn:代理中的实体</para><para>en-us:The tracked entity entry.</para></returns>
    public static Task<EntityEntry<TEntity>> UpdateExcludeAsync<TEntity>(this TEntity entity, Expression<Func<TEntity, object>>[] propertyPredicates, bool? ignoreNullValues = null)
        where TEntity : class, IPrivateEntity, new()
    {
        return new EntityExecutePart<TEntity>().SetEntity(entity).UpdateExcludeAsync(propertyPredicates, ignoreNullValues);
    }

    /// <summary>
    /// <para>zh-cn:更新一条记录并排除属性</para>
    /// <para>en-us:Updates a record while excluding selected properties.</para>
    /// </summary>
    /// <typeparam name="TEntity"><para>zh-cn:实体</para><para>en-us:The generic parameter type.</para></typeparam>
    /// <param name="entity"><para>zh-cn:实体</para><para>en-us:The entity.</para></param>
    /// <param name="propertyNames"><para>zh-cn:属性表达式</para><para>en-us:The property expressions.</para></param>
    /// <param name="ignoreNullValues"><para>zh-cn:ignoreNullValues 参数。</para><para>en-us:The ignoreNullValues value.</para></param>
    /// <returns><para>zh-cn:代理中的实体</para><para>en-us:The tracked entity entry.</para></returns>
    public static Task<EntityEntry<TEntity>> UpdateExcludeAsync<TEntity>(this TEntity entity, IEnumerable<string> propertyNames, bool? ignoreNullValues = null)
        where TEntity : class, IPrivateEntity, new()
    {
        return new EntityExecutePart<TEntity>().SetEntity(entity).UpdateExcludeAsync(propertyNames, ignoreNullValues);
    }

    /// <summary>
    /// <para>zh-cn:更新一条记录并排除属性</para>
    /// <para>en-us:Updates a record while excluding selected properties.</para>
    /// </summary>
    /// <typeparam name="TEntity"><para>zh-cn:实体</para><para>en-us:The generic parameter type.</para></typeparam>
    /// <param name="entity"><para>zh-cn:实体</para><para>en-us:The entity.</para></param>
    /// <param name="propertyPredicates"><para>zh-cn:属性表达式</para><para>en-us:The property expressions.</para></param>
    /// <param name="ignoreNullValues"><para>zh-cn:ignoreNullValues 参数。</para><para>en-us:The ignoreNullValues value.</para></param>
    /// <returns><para>zh-cn:代理中的实体</para><para>en-us:The tracked entity entry.</para></returns>
    public static Task<EntityEntry<TEntity>> UpdateExcludeAsync<TEntity>(this TEntity entity, IEnumerable<Expression<Func<TEntity, object>>> propertyPredicates, bool? ignoreNullValues = null)
        where TEntity : class, IPrivateEntity, new()
    {
        return new EntityExecutePart<TEntity>().SetEntity(entity).UpdateExcludeAsync(propertyPredicates, ignoreNullValues);
    }

    /// <summary>
    /// <para>zh-cn:删除一条记录</para>
    /// <para>en-us:Deletes a record.</para>
    /// </summary>
    /// <typeparam name="TEntity"><para>zh-cn:实体</para><para>en-us:The generic parameter type.</para></typeparam>
    /// <param name="entity"><para>zh-cn:实体</para><para>en-us:The entity.</para></param>
    /// <returns><para>zh-cn:代理中的实体</para><para>en-us:The tracked entity entry.</para></returns>
    public static EntityEntry<TEntity> Delete<TEntity>(this TEntity entity)
        where TEntity : class, IPrivateEntity, new()
    {
        return new EntityExecutePart<TEntity>().SetEntity(entity).Delete();
    }

    /// <summary>
    /// <para>zh-cn:删除一条记录</para>
    /// <para>en-us:Deletes a record.</para>
    /// </summary>
    /// <typeparam name="TEntity"><para>zh-cn:实体</para><para>en-us:The generic parameter type.</para></typeparam>
    /// <param name="entity"><para>zh-cn:实体</para><para>en-us:The entity.</para></param>
    /// <returns><para>zh-cn:代理中的实体</para><para>en-us:The tracked entity entry.</para></returns>
    public static Task<EntityEntry<TEntity>> DeleteAsync<TEntity>(this TEntity entity)
        where TEntity : class, IPrivateEntity, new()
    {
        return new EntityExecutePart<TEntity>().SetEntity(entity).DeleteAsync();
    }
}
