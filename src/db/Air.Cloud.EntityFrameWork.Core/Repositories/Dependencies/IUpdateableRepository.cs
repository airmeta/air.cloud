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

using System.Linq.Expressions;

namespace Air.Cloud.EntityFrameWork.Core.Repositories.Dependencies;

/// <summary>
/// <para>zh-cn:可更新仓储接口</para>
/// <para>en-us:Describes the behavior provided by this member.</para>
/// </summary>
/// <typeparam name="TEntity"><para>zh-cn:实体类型</para><para>en-us:The entity type.</para></typeparam>
public partial interface IUpdateableRepository<TEntity> : IUpdateableRepository<TEntity, MasterDbContextLocator>
    where TEntity : class, IPrivateEntity, new()
{
}

/// <summary>
/// <para>zh-cn:可更新仓储接口</para>
/// <para>en-us:Describes the behavior provided by this member.</para>
/// </summary>
/// <typeparam name="TEntity"><para>zh-cn:实体类型</para><para>en-us:The entity type.</para></typeparam>
/// <typeparam name="TDbContextLocator"><para>zh-cn:数据库上下文定位器</para><para>en-us:The database context locator type.</para></typeparam>
public partial interface IUpdateableRepository<TEntity, TDbContextLocator> : IPrivateUpdateableRepository<TEntity>
    where TEntity : class, IPrivateEntity, new()
    where TDbContextLocator : class, IDbContextLocator
{
}

/// <summary>
/// <para>zh-cn:可更新仓储接口</para>
/// <para>en-us:Describes the behavior provided by this member.</para>
/// </summary>
/// <typeparam name="TEntity"><para>zh-cn:实体类型</para><para>en-us:The entity type.</para></typeparam>
public partial interface IPrivateUpdateableRepository<TEntity> : IPrivateRootRepository
    where TEntity : class, IPrivateEntity, new()
{
    /// <summary>
    /// <para>zh-cn:更新一条记录</para>
    /// <para>en-us:Describes the behavior provided by this member.</para>
    /// </summary>
    /// <param name="entity"><para>zh-cn:实体</para><para>en-us:The entity.</para></param>
    /// <param name="ignoreNullValues"><para>zh-cn:是否忽略空值。</para><para>en-us:Whether to ignore null values.</para></param>
    /// <returns><para>zh-cn:代理中的实体</para><para>en-us:The operation result.</para></returns>
    EntityEntry<TEntity> Update(TEntity entity, bool? ignoreNullValues = null);

    /// <summary>
    /// <para>zh-cn:更新多条记录</para>
    /// <para>en-us:Describes the behavior provided by this member.</para>
    /// </summary>
    /// <param name="entities"><para>zh-cn:多个实体</para><para>en-us:The entities.</para></param>
    void Update(params TEntity[] entities);

    /// <summary>
    /// <para>zh-cn:更新多条记录</para>
    /// <para>en-us:Describes the behavior provided by this member.</para>
    /// </summary>
    /// <param name="entities"><para>zh-cn:多个实体</para><para>en-us:The entities.</para></param>
    void Update(IEnumerable<TEntity> entities);

    /// <summary>
    /// <para>zh-cn:更新一条记录</para>
    /// <para>en-us:Describes the behavior provided by this member.</para>
    /// </summary>
    /// <param name="entity"><para>zh-cn:实体</para><para>en-us:The entity.</para></param>
    /// <param name="ignoreNullValues"><para>zh-cn:是否忽略空值。</para><para>en-us:Whether to ignore null values.</para></param>
    /// <returns><para>zh-cn:代理中的实体</para><para>en-us:The operation result.</para></returns>
    Task<EntityEntry<TEntity>> UpdateAsync(TEntity entity, bool? ignoreNullValues = null);

    /// <summary>
    /// <para>zh-cn:更新多条记录</para>
    /// <para>en-us:Describes the behavior provided by this member.</para>
    /// </summary>
    /// <param name="entities"><para>zh-cn:多个实体</para><para>en-us:The entities.</para></param>
    /// <returns><para>zh-cn:Task</para><para>en-us:The operation result.</para></returns>
    Task UpdateAsync(params TEntity[] entities);

    /// <summary>
    /// <para>zh-cn:更新多条记录</para>
    /// <para>en-us:Describes the behavior provided by this member.</para>
    /// </summary>
    /// <param name="entities"><para>zh-cn:多个实体</para><para>en-us:The entities.</para></param>
    /// <returns><para>zh-cn:Task</para><para>en-us:The operation result.</para></returns>
    Task UpdateAsync(IEnumerable<TEntity> entities);

    /// <summary>
    /// <para>zh-cn:更新一条记录中特定属性</para>
    /// <para>en-us:Describes the behavior provided by this member.</para>
    /// </summary>
    /// <param name="entity"><para>zh-cn:实体</para><para>en-us:The entity.</para></param>
    /// <param name="propertyNames"><para>zh-cn:属性名</para><para>en-us:The property names.</para></param>
    /// <param name="ignoreNullValues"><para>zh-cn:是否忽略空值。</para><para>en-us:Whether to ignore null values.</para></param>
    /// <returns><para>zh-cn:代理中的实体</para><para>en-us:The operation result.</para></returns>
    EntityEntry<TEntity> UpdateInclude(TEntity entity, string[] propertyNames, bool? ignoreNullValues = null);

    /// <summary>
    /// <para>zh-cn:更新一条记录中特定属性</para>
    /// <para>en-us:Describes the behavior provided by this member.</para>
    /// </summary>
    /// <param name="entity"><para>zh-cn:实体</para><para>en-us:The entity.</para></param>
    /// <param name="propertyPredicates"><para>zh-cn:属性表达式</para><para>en-us:The property predicates.</para></param>
    /// <param name="ignoreNullValues"><para>zh-cn:是否忽略空值。</para><para>en-us:Whether to ignore null values.</para></param>
    /// <returns><para>zh-cn:代理中的实体</para><para>en-us:The operation result.</para></returns>
    EntityEntry<TEntity> UpdateInclude(TEntity entity, Expression<Func<TEntity, object>>[] propertyPredicates, bool? ignoreNullValues = null);

    /// <summary>
    /// <para>zh-cn:更新一条记录中特定属性</para>
    /// <para>en-us:Describes the behavior provided by this member.</para>
    /// </summary>
    /// <param name="entity"><para>zh-cn:实体</para><para>en-us:The entity.</para></param>
    /// <param name="propertyNames"><para>zh-cn:属性名</para><para>en-us:The property names.</para></param>
    /// <param name="ignoreNullValues"><para>zh-cn:是否忽略空值。</para><para>en-us:Whether to ignore null values.</para></param>
    /// <returns><para>zh-cn:代理中的实体</para><para>en-us:The operation result.</para></returns>
    EntityEntry<TEntity> UpdateInclude(TEntity entity, IEnumerable<string> propertyNames, bool? ignoreNullValues = null);

    /// <summary>
    /// <para>zh-cn:更新一条记录中特定属性</para>
    /// <para>en-us:Describes the behavior provided by this member.</para>
    /// </summary>
    /// <param name="entity"><para>zh-cn:实体</para><para>en-us:The entity.</para></param>
    /// <param name="propertyPredicates"><para>zh-cn:属性表达式</para><para>en-us:The property predicates.</para></param>
    /// <param name="ignoreNullValues"><para>zh-cn:是否忽略空值。</para><para>en-us:Whether to ignore null values.</para></param>
    /// <returns><para>zh-cn:代理中的实体</para><para>en-us:The operation result.</para></returns>
    EntityEntry<TEntity> UpdateInclude(TEntity entity, IEnumerable<Expression<Func<TEntity, object>>> propertyPredicates, bool? ignoreNullValues = null);

    /// <summary>
    /// <para>zh-cn:更新一条记录中的特定属性</para>
    /// <para>en-us:Describes the behavior provided by this member.</para>
    /// </summary>
    /// <param name="entity"><para>zh-cn:实体</para><para>en-us:The entity.</para></param>
    /// <param name="propertyNames"><para>zh-cn:属性名</para><para>en-us:The property names.</para></param>
    /// <param name="ignoreNullValues"><para>zh-cn:是否忽略空值。</para><para>en-us:Whether to ignore null values.</para></param>
    /// <returns><para>zh-cn:代理中的实体</para><para>en-us:The operation result.</para></returns>
    Task<EntityEntry<TEntity>> UpdateIncludeAsync(TEntity entity, string[] propertyNames, bool? ignoreNullValues = null);

    /// <summary>
    /// <para>zh-cn:更新一条记录中的特定属性</para>
    /// <para>en-us:Describes the behavior provided by this member.</para>
    /// </summary>
    /// <param name="entity"><para>zh-cn:实体</para><para>en-us:The entity.</para></param>
    /// <param name="propertyPredicates"><para>zh-cn:属性表达式</para><para>en-us:The property predicates.</para></param>
    /// <param name="ignoreNullValues"><para>zh-cn:是否忽略空值。</para><para>en-us:Whether to ignore null values.</para></param>
    /// <returns><para>zh-cn:代理中的实体</para><para>en-us:The operation result.</para></returns>
    Task<EntityEntry<TEntity>> UpdateIncludeAsync(TEntity entity, Expression<Func<TEntity, object>>[] propertyPredicates, bool? ignoreNullValues = null);

    /// <summary>
    /// <para>zh-cn:更新一条记录中的特定属性</para>
    /// <para>en-us:Describes the behavior provided by this member.</para>
    /// </summary>
    /// <param name="entity"><para>zh-cn:实体</para><para>en-us:The entity.</para></param>
    /// <param name="propertyNames"><para>zh-cn:属性表达式</para><para>en-us:The property names.</para></param>
    /// <param name="ignoreNullValues"><para>zh-cn:是否忽略空值。</para><para>en-us:Whether to ignore null values.</para></param>
    /// <returns><para>zh-cn:代理中的实体</para><para>en-us:The operation result.</para></returns>
    Task<EntityEntry<TEntity>> UpdateIncludeAsync(TEntity entity, IEnumerable<string> propertyNames, bool? ignoreNullValues = null);

    /// <summary>
    /// <para>zh-cn:更新一条记录中的特定属性</para>
    /// <para>en-us:Describes the behavior provided by this member.</para>
    /// </summary>
    /// <param name="entity"><para>zh-cn:实体</para><para>en-us:The entity.</para></param>
    /// <param name="propertyPredicates"><para>zh-cn:属性表达式</para><para>en-us:The property predicates.</para></param>
    /// <param name="ignoreNullValues"><para>zh-cn:是否忽略空值。</para><para>en-us:Whether to ignore null values.</para></param>
    /// <returns><para>zh-cn:代理中的实体</para><para>en-us:The operation result.</para></returns>
    Task<EntityEntry<TEntity>> UpdateIncludeAsync(TEntity entity, IEnumerable<Expression<Func<TEntity, object>>> propertyPredicates, bool? ignoreNullValues = null);

    /// <summary>
    /// <para>zh-cn:更新一条记录并排除属性</para>
    /// <para>en-us:Describes the behavior provided by this member.</para>
    /// </summary>
    /// <param name="entity"><para>zh-cn:实体</para><para>en-us:The entity.</para></param>
    /// <param name="propertyNames"><para>zh-cn:属性名</para><para>en-us:The property names.</para></param>
    /// <param name="ignoreNullValues"><para>zh-cn:是否忽略空值。</para><para>en-us:Whether to ignore null values.</para></param>
    /// <returns><para>zh-cn:代理中的实体</para><para>en-us:The operation result.</para></returns>
    EntityEntry<TEntity> UpdateExclude(TEntity entity, string[] propertyNames, bool? ignoreNullValues = null);

    /// <summary>
    /// <para>zh-cn:更新一条记录并排除属性</para>
    /// <para>en-us:Describes the behavior provided by this member.</para>
    /// </summary>
    /// <param name="entity"><para>zh-cn:实体</para><para>en-us:The entity.</para></param>
    /// <param name="propertyPredicates"><para>zh-cn:属性表达式</para><para>en-us:The property predicates.</para></param>
    /// <param name="ignoreNullValues"><para>zh-cn:是否忽略空值。</para><para>en-us:Whether to ignore null values.</para></param>
    /// <returns><para>zh-cn:代理中的实体</para><para>en-us:The operation result.</para></returns>
    EntityEntry<TEntity> UpdateExclude(TEntity entity, Expression<Func<TEntity, object>>[] propertyPredicates, bool? ignoreNullValues = null);

    /// <summary>
    /// <para>zh-cn:更新一条记录并排除属性</para>
    /// <para>en-us:Describes the behavior provided by this member.</para>
    /// </summary>
    /// <param name="entity"><para>zh-cn:实体</para><para>en-us:The entity.</para></param>
    /// <param name="propertyNames"><para>zh-cn:属性名</para><para>en-us:The property names.</para></param>
    /// <param name="ignoreNullValues"><para>zh-cn:是否忽略空值。</para><para>en-us:Whether to ignore null values.</para></param>
    /// <returns><para>zh-cn:代理中的实体</para><para>en-us:The operation result.</para></returns>
    EntityEntry<TEntity> UpdateExclude(TEntity entity, IEnumerable<string> propertyNames, bool? ignoreNullValues = null);

    /// <summary>
    /// <para>zh-cn:更新一条记录并排除属性</para>
    /// <para>en-us:Describes the behavior provided by this member.</para>
    /// </summary>
    /// <param name="entity"><para>zh-cn:实体</para><para>en-us:The entity.</para></param>
    /// <param name="propertyPredicates"><para>zh-cn:属性表达式</para><para>en-us:The property predicates.</para></param>
    /// <param name="ignoreNullValues"><para>zh-cn:是否忽略空值。</para><para>en-us:Whether to ignore null values.</para></param>
    /// <returns><para>zh-cn:代理中的实体</para><para>en-us:The operation result.</para></returns>
    EntityEntry<TEntity> UpdateExclude(TEntity entity, IEnumerable<Expression<Func<TEntity, object>>> propertyPredicates, bool? ignoreNullValues = null);

    /// <summary>
    /// <para>zh-cn:更新一条记录并排除属性</para>
    /// <para>en-us:Describes the behavior provided by this member.</para>
    /// </summary>
    /// <param name="entity"><para>zh-cn:实体</para><para>en-us:The entity.</para></param>
    /// <param name="propertyNames"><para>zh-cn:属性名</para><para>en-us:The property names.</para></param>
    /// <param name="ignoreNullValues"><para>zh-cn:是否忽略空值。</para><para>en-us:Whether to ignore null values.</para></param>
    /// <returns><para>zh-cn:代理中的实体</para><para>en-us:The operation result.</para></returns>
    Task<EntityEntry<TEntity>> UpdateExcludeAsync(TEntity entity, string[] propertyNames, bool? ignoreNullValues = null);

    /// <summary>
    /// <para>zh-cn:更新一条记录并排除属性</para>
    /// <para>en-us:Describes the behavior provided by this member.</para>
    /// </summary>
    /// <param name="entity"><para>zh-cn:实体</para><para>en-us:The entity.</para></param>
    /// <param name="propertyPredicates"><para>zh-cn:属性表达式</para><para>en-us:The property predicates.</para></param>
    /// <param name="ignoreNullValues"><para>zh-cn:是否忽略空值。</para><para>en-us:Whether to ignore null values.</para></param>
    /// <returns><para>zh-cn:代理中的实体</para><para>en-us:The operation result.</para></returns>
    Task<EntityEntry<TEntity>> UpdateExcludeAsync(TEntity entity, Expression<Func<TEntity, object>>[] propertyPredicates, bool? ignoreNullValues = null);

    /// <summary>
    /// <para>zh-cn:更新一条记录并排除属性</para>
    /// <para>en-us:Describes the behavior provided by this member.</para>
    /// </summary>
    /// <param name="entity"><para>zh-cn:实体</para><para>en-us:The entity.</para></param>
    /// <param name="propertyNames"><para>zh-cn:属性表达式</para><para>en-us:The property names.</para></param>
    /// <param name="ignoreNullValues"><para>zh-cn:是否忽略空值。</para><para>en-us:Whether to ignore null values.</para></param>
    /// <returns><para>zh-cn:代理中的实体</para><para>en-us:The operation result.</para></returns>
    Task<EntityEntry<TEntity>> UpdateExcludeAsync(TEntity entity, IEnumerable<string> propertyNames, bool? ignoreNullValues = null);

    /// <summary>
    /// <para>zh-cn:更新一条记录并排除属性</para>
    /// <para>en-us:Describes the behavior provided by this member.</para>
    /// </summary>
    /// <param name="entity"><para>zh-cn:实体</para><para>en-us:The entity.</para></param>
    /// <param name="propertyPredicates"><para>zh-cn:属性表达式</para><para>en-us:The property predicates.</para></param>
    /// <param name="ignoreNullValues"><para>zh-cn:是否忽略空值。</para><para>en-us:Whether to ignore null values.</para></param>
    /// <returns><para>zh-cn:代理中的实体</para><para>en-us:The operation result.</para></returns>
    Task<EntityEntry<TEntity>> UpdateExcludeAsync(TEntity entity, IEnumerable<Expression<Func<TEntity, object>>> propertyPredicates, bool? ignoreNullValues = null);
}
