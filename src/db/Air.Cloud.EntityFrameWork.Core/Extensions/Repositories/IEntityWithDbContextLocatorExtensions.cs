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

using Microsoft.EntityFrameworkCore.ChangeTracking;


using System.Linq.Expressions;

namespace Air.Cloud.EntityFrameWork.Core.Extensions.Repositories;

/// <summary>
/// 实体多数据库上下文拓展类
/// </summary>
[IgnoreScanning]
public static class IEntityWithDbContextLocatorExtensions
{
    /// <summary>
    /// 获取实体同类（族群）
    /// </summary>
    /// <typeparam name="TEntity">实体</typeparam>
    /// <typeparam name="TDbContextLocator">数据库上下文定位器</typeparam>
    /// <returns>DbSet{TEntity}</returns>
    public static DbSet<TEntity> Ethnics<TEntity, TDbContextLocator>(this TEntity entity)
        where TEntity : class, IPrivateEntity, new()
        where TDbContextLocator : class, IDbContextLocator
    {
        return new EntityExecutePart<TEntity>().SetEntity(entity).Change<TDbContextLocator>().Ethnics();
    }

    /// <summary>
    /// 新增一条记录
    /// </summary>
    /// <typeparam name="TEntity">实体</typeparam>
    /// <typeparam name="TDbContextLocator">数据库上下文定位器</typeparam>
    /// <param name="entity">实体</param>
    /// <param name="ignoreNullValues"></param>
    /// <returns>代理的实体</returns>
    public static EntityEntry<TEntity> Insert<TEntity, TDbContextLocator>(this TEntity entity, bool? ignoreNullValues = null)
        where TEntity : class, IPrivateEntity, new()
        where TDbContextLocator : class, IDbContextLocator
    {
        return new EntityExecutePart<TEntity>().SetEntity(entity).Change<TDbContextLocator>().Insert(ignoreNullValues);
    }

    /// <summary>
    /// 新增一条记录
    /// </summary>
    /// <typeparam name="TEntity">实体</typeparam>
    /// <typeparam name="TDbContextLocator">数据库上下文定位器</typeparam>
    /// <param name="entity">实体</param>
    /// <param name="ignoreNullValues"></param>
    /// <param name="cancellationToken">异步取消令牌</param>
    /// <returns>代理的实体</returns>
    public static Task<EntityEntry<TEntity>> InsertAsync<TEntity, TDbContextLocator>(this TEntity entity, bool? ignoreNullValues = null, CancellationToken cancellationToken = default)
        where TEntity : class, IPrivateEntity, new()
        where TDbContextLocator : class, IDbContextLocator
    {
        return new EntityExecutePart<TEntity>().SetEntity(entity).Change<TDbContextLocator>().InsertAsync(ignoreNullValues, cancellationToken);
    }

    /// <summary>
    /// 更新一条记录
    /// </summary>
    /// <typeparam name="TEntity">实体</typeparam>
    /// <typeparam name="TDbContextLocator">数据库上下文定位器</typeparam>
    /// <param name="entity">实体</param>
    /// <param name="ignoreNullValues"></param>
    /// <returns>代理中的实体</returns>
    public static EntityEntry<TEntity> Update<TEntity, TDbContextLocator>(this TEntity entity, bool? ignoreNullValues = null)
        where TEntity : class, IPrivateEntity, new()
        where TDbContextLocator : class, IDbContextLocator
    {
        return new EntityExecutePart<TEntity>().SetEntity(entity).Change<TDbContextLocator>().Update(ignoreNullValues);
    }

    /// <summary>
    /// 更新一条记录
    /// </summary>
    /// <typeparam name="TEntity">实体</typeparam>
    /// <typeparam name="TDbContextLocator">数据库上下文定位器</typeparam>
    /// <param name="entity">实体</param>
    /// <param name="ignoreNullValues"></param>
    /// <returns>代理中的实体</returns>
    public static Task<EntityEntry<TEntity>> UpdateAsync<TEntity, TDbContextLocator>(this TEntity entity, bool? ignoreNullValues = null)
         where TEntity : class, IPrivateEntity, new()
         where TDbContextLocator : class, IDbContextLocator
    {
        return new EntityExecutePart<TEntity>().SetEntity(entity).Change<TDbContextLocator>().UpdateAsync(ignoreNullValues);
    }

    /// <summary>
    /// 更新一条记录中特定属性
    /// </summary>
    /// <typeparam name="TEntity">实体</typeparam>
    /// <typeparam name="TDbContextLocator">数据库上下文定位器</typeparam>
    /// <param name="entity">实体</param>
    /// <param name="propertyNames">属性名</param>
    /// <param name="ignoreNullValues"></param>
    /// <returns>代理中的实体</returns>
    public static EntityEntry<TEntity> UpdateInclude<TEntity, TDbContextLocator>(this TEntity entity, string[] propertyNames, bool? ignoreNullValues = null)
        where TEntity : class, IPrivateEntity, new()
        where TDbContextLocator : class, IDbContextLocator
    {
        return new EntityExecutePart<TEntity>().SetEntity(entity).Change<TDbContextLocator>().UpdateInclude(propertyNames, ignoreNullValues);
    }

    /// <summary>
    /// 更新一条记录中特定属性
    /// </summary>
    /// <typeparam name="TEntity">实体</typeparam>
    /// <typeparam name="TDbContextLocator">数据库上下文定位器</typeparam>
    /// <param name="entity">实体</param>
    /// <param name="propertyPredicates">属性表达式</param>
    /// <param name="ignoreNullValues"></param>
    /// <returns>代理中的实体</returns>
    public static EntityEntry<TEntity> UpdateInclude<TEntity, TDbContextLocator>(this TEntity entity, Expression<Func<TEntity, object>>[] propertyPredicates, bool? ignoreNullValues = null)
        where TEntity : class, IPrivateEntity, new()
        where TDbContextLocator : class, IDbContextLocator
    {
        return new EntityExecutePart<TEntity>().SetEntity(entity).Change<TDbContextLocator>().UpdateInclude(propertyPredicates, ignoreNullValues);
    }

    /// <summary>
    /// 更新一条记录中特定属性
    /// </summary>
    /// <typeparam name="TEntity">实体</typeparam>
    /// <typeparam name="TDbContextLocator">数据库上下文定位器</typeparam>
    /// <param name="entity">实体</param>
    /// <param name="propertyNames">属性名</param>
    /// <param name="ignoreNullValues"></param>
    /// <returns>代理中的实体</returns>
    public static EntityEntry<TEntity> UpdateInclude<TEntity, TDbContextLocator>(this TEntity entity, IEnumerable<string> propertyNames, bool? ignoreNullValues = null)
        where TEntity : class, IPrivateEntity, new()
        where TDbContextLocator : class, IDbContextLocator
    {
        return new EntityExecutePart<TEntity>().SetEntity(entity).Change<TDbContextLocator>().UpdateInclude(propertyNames, ignoreNullValues);
    }

    /// <summary>
    /// 更新一条记录中特定属性
    /// </summary>
    /// <typeparam name="TEntity">实体</typeparam>
    /// <typeparam name="TDbContextLocator">数据库上下文定位器</typeparam>
    /// <param name="entity">实体</param>
    /// <param name="propertyPredicates">属性表达式</param>
    /// <param name="ignoreNullValues"></param>
    /// <returns>代理中的实体</returns>
    public static EntityEntry<TEntity> UpdateInclude<TEntity, TDbContextLocator>(this TEntity entity, IEnumerable<Expression<Func<TEntity, object>>> propertyPredicates, bool? ignoreNullValues = null)
        where TEntity : class, IPrivateEntity, new()
        where TDbContextLocator : class, IDbContextLocator
    {
        return new EntityExecutePart<TEntity>().SetEntity(entity).Change<TDbContextLocator>().UpdateInclude(propertyPredicates, ignoreNullValues);
    }

    /// <summary>
    /// 更新一条记录中的特定属性
    /// </summary>
    /// <typeparam name="TEntity">实体</typeparam>
    /// <typeparam name="TDbContextLocator">数据库上下文定位器</typeparam>
    /// <param name="entity">实体</param>
    /// <param name="propertyNames">属性名</param>
    /// <param name="ignoreNullValues"></param>
    /// <returns>代理中的实体</returns>
    public static Task<EntityEntry<TEntity>> UpdateIncludeAsync<TEntity, TDbContextLocator>(this TEntity entity, string[] propertyNames, bool? ignoreNullValues = null)
        where TEntity : class, IPrivateEntity, new()
        where TDbContextLocator : class, IDbContextLocator
    {
        return new EntityExecutePart<TEntity>().SetEntity(entity).Change<TDbContextLocator>().UpdateIncludeAsync(propertyNames, ignoreNullValues);
    }

    /// <summary>
    /// 更新一条记录中的特定属性
    /// </summary>
    /// <typeparam name="TEntity">实体</typeparam>
    /// <typeparam name="TDbContextLocator">数据库上下文定位器</typeparam>
    /// <param name="entity">实体</param>
    /// <param name="propertyPredicates">属性表达式</param>
    /// <param name="ignoreNullValues"></param>
    /// <returns>代理中的实体</returns>
    public static Task<EntityEntry<TEntity>> UpdateIncludeAsync<TEntity, TDbContextLocator>(this TEntity entity, Expression<Func<TEntity, object>>[] propertyPredicates, bool? ignoreNullValues = null)
        where TEntity : class, IPrivateEntity, new()
        where TDbContextLocator : class, IDbContextLocator
    {
        return new EntityExecutePart<TEntity>().SetEntity(entity).Change<TDbContextLocator>().UpdateIncludeAsync(propertyPredicates, ignoreNullValues);
    }

    /// <summary>
    /// 更新一条记录中的特定属性
    /// </summary>
    /// <typeparam name="TEntity">实体</typeparam>
    /// <typeparam name="TDbContextLocator">数据库上下文定位器</typeparam>
    /// <param name="entity">实体</param>
    /// <param name="propertyNames">属性表达式</param>
    /// <param name="ignoreNullValues"></param>
    /// <returns>代理中的实体</returns>
    public static Task<EntityEntry<TEntity>> UpdateIncludeAsync<TEntity, TDbContextLocator>(this TEntity entity, IEnumerable<string> propertyNames, bool? ignoreNullValues = null)
        where TEntity : class, IPrivateEntity, new()
        where TDbContextLocator : class, IDbContextLocator
    {
        return new EntityExecutePart<TEntity>().SetEntity(entity).Change<TDbContextLocator>().UpdateIncludeAsync(propertyNames, ignoreNullValues);
    }

    /// <summary>
    /// 更新一条记录中的特定属性
    /// </summary>
    /// <typeparam name="TEntity">实体</typeparam>
    /// <typeparam name="TDbContextLocator">数据库上下文定位器</typeparam>
    /// <param name="entity">实体</param>
    /// <param name="propertyPredicates">属性表达式</param>
    /// <param name="ignoreNullValues"></param>
    /// <returns>代理中的实体</returns>
    public static Task<EntityEntry<TEntity>> UpdateIncludeAsync<TEntity, TDbContextLocator>(this TEntity entity, IEnumerable<Expression<Func<TEntity, object>>> propertyPredicates, bool? ignoreNullValues = null)
        where TEntity : class, IPrivateEntity, new()
        where TDbContextLocator : class, IDbContextLocator
    {
        return new EntityExecutePart<TEntity>().SetEntity(entity).Change<TDbContextLocator>().UpdateIncludeAsync(propertyPredicates, ignoreNullValues);
    }

    /// <summary>
    /// 更新一条记录中特定属性
    /// </summary>
    /// <typeparam name="TEntity">实体</typeparam>
    /// <typeparam name="TDbContextLocator">数据库上下文定位器</typeparam>
    /// <param name="entity">实体</param>
    /// <param name="propertyNames">属性名</param>
    /// <param name="ignoreNullValues"></param>
    /// <returns>代理中的实体</returns>
    public static EntityEntry<TEntity> UpdateExclude<TEntity, TDbContextLocator>(this TEntity entity, string[] propertyNames, bool? ignoreNullValues = null)
        where TEntity : class, IPrivateEntity, new()
        where TDbContextLocator : class, IDbContextLocator
    {
        return new EntityExecutePart<TEntity>().SetEntity(entity).Change<TDbContextLocator>().UpdateExclude(propertyNames, ignoreNullValues);
    }

    /// <summary>
    /// 更新一条记录中特定属性
    /// </summary>
    /// <typeparam name="TEntity">实体</typeparam>
    /// <typeparam name="TDbContextLocator">数据库上下文定位器</typeparam>
    /// <param name="entity">实体</param>
    /// <param name="propertyPredicates">属性表达式</param>
    /// <param name="ignoreNullValues"></param>
    /// <returns>代理中的实体</returns>
    public static EntityEntry<TEntity> UpdateExclude<TEntity, TDbContextLocator>(this TEntity entity, Expression<Func<TEntity, object>>[] propertyPredicates, bool? ignoreNullValues = null)
        where TEntity : class, IPrivateEntity, new()
        where TDbContextLocator : class, IDbContextLocator
    {
        return new EntityExecutePart<TEntity>().SetEntity(entity).Change<TDbContextLocator>().UpdateExclude(propertyPredicates, ignoreNullValues);
    }

    /// <summary>
    /// 更新一条记录中特定属性
    /// </summary>
    /// <typeparam name="TEntity">实体</typeparam>
    /// <typeparam name="TDbContextLocator">数据库上下文定位器</typeparam>
    /// <param name="entity">实体</param>
    /// <param name="propertyNames">属性名</param>
    /// <param name="ignoreNullValues"></param>
    /// <returns>代理中的实体</returns>
    public static EntityEntry<TEntity> UpdateExclude<TEntity, TDbContextLocator>(this TEntity entity, IEnumerable<string> propertyNames, bool? ignoreNullValues = null)
        where TEntity : class, IPrivateEntity, new()
        where TDbContextLocator : class, IDbContextLocator
    {
        return new EntityExecutePart<TEntity>().SetEntity(entity).Change<TDbContextLocator>().UpdateExclude(propertyNames, ignoreNullValues);
    }

    /// <summary>
    /// 更新一条记录中特定属性
    /// </summary>
    /// <typeparam name="TEntity">实体</typeparam>
    /// <typeparam name="TDbContextLocator">数据库上下文定位器</typeparam>
    /// <param name="entity">实体</param>
    /// <param name="propertyPredicates">属性表达式</param>
    /// <param name="ignoreNullValues"></param>
    /// <returns>代理中的实体</returns>
    public static EntityEntry<TEntity> UpdateExclude<TEntity, TDbContextLocator>(this TEntity entity, IEnumerable<Expression<Func<TEntity, object>>> propertyPredicates, bool? ignoreNullValues = null)
        where TEntity : class, IPrivateEntity, new()
        where TDbContextLocator : class, IDbContextLocator
    {
        return new EntityExecutePart<TEntity>().SetEntity(entity).Change<TDbContextLocator>().UpdateExclude(propertyPredicates, ignoreNullValues);
    }

    /// <summary>
    /// 更新一条记录并排除属性
    /// </summary>
    /// <typeparam name="TEntity">实体</typeparam>
    /// <typeparam name="TDbContextLocator">数据库上下文定位器</typeparam>
    /// <param name="entity">实体</param>
    /// <param name="propertyNames">属性名</param>
    /// <param name="ignoreNullValues"></param>
    /// <returns>代理中的实体</returns>
    public static Task<EntityEntry<TEntity>> UpdateExcludeAsync<TEntity, TDbContextLocator>(this TEntity entity, string[] propertyNames, bool? ignoreNullValues = null)
        where TEntity : class, IPrivateEntity, new()
        where TDbContextLocator : class, IDbContextLocator
    {
        return new EntityExecutePart<TEntity>().SetEntity(entity).Change<TDbContextLocator>().UpdateExcludeAsync(propertyNames, ignoreNullValues);
    }

    /// <summary>
    /// 更新一条记录并排除属性
    /// </summary>
    /// <typeparam name="TEntity">实体</typeparam>
    /// <typeparam name="TDbContextLocator">数据库上下文定位器</typeparam>
    /// <param name="entity">实体</param>
    /// <param name="propertyPredicates">属性表达式</param>
    /// <param name="ignoreNullValues"></param>
    /// <returns>代理中的实体</returns>
    public static Task<EntityEntry<TEntity>> UpdateExcludeAsync<TEntity, TDbContextLocator>(this TEntity entity, Expression<Func<TEntity, object>>[] propertyPredicates, bool? ignoreNullValues = null)
        where TEntity : class, IPrivateEntity, new()
        where TDbContextLocator : class, IDbContextLocator
    {
        return new EntityExecutePart<TEntity>().SetEntity(entity).Change<TDbContextLocator>().UpdateExcludeAsync(propertyPredicates, ignoreNullValues);
    }

    /// <summary>
    /// 更新一条记录并排除属性
    /// </summary>
    /// <typeparam name="TEntity">实体</typeparam>
    /// <typeparam name="TDbContextLocator">数据库上下文定位器</typeparam>
    /// <param name="entity">实体</param>
    /// <param name="propertyNames">属性表达式</param>
    /// <param name="ignoreNullValues"></param>
    /// <returns>代理中的实体</returns>
    public static Task<EntityEntry<TEntity>> UpdateExcludeAsync<TEntity, TDbContextLocator>(this TEntity entity, IEnumerable<string> propertyNames, bool? ignoreNullValues = null)
        where TEntity : class, IPrivateEntity, new()
        where TDbContextLocator : class, IDbContextLocator
    {
        return new EntityExecutePart<TEntity>().SetEntity(entity).Change<TDbContextLocator>().UpdateExcludeAsync(propertyNames, ignoreNullValues);
    }

    /// <summary>
    /// 更新一条记录并排除属性
    /// </summary>
    /// <typeparam name="TEntity">实体</typeparam>
    /// <typeparam name="TDbContextLocator">数据库上下文定位器</typeparam>
    /// <param name="entity">实体</param>
    /// <param name="propertyPredicates">属性表达式</param>
    /// <param name="ignoreNullValues"></param>
    /// <returns>代理中的实体</returns>
    public static Task<EntityEntry<TEntity>> UpdateExcludeAsync<TEntity, TDbContextLocator>(this TEntity entity, IEnumerable<Expression<Func<TEntity, object>>> propertyPredicates, bool? ignoreNullValues = null)
        where TEntity : class, IPrivateEntity, new()
        where TDbContextLocator : class, IDbContextLocator
    {
        return new EntityExecutePart<TEntity>().SetEntity(entity).Change<TDbContextLocator>().UpdateExcludeAsync(propertyPredicates, ignoreNullValues);
    }

    /// <summary>
    /// 删除一条记录
    /// </summary>
    /// <typeparam name="TEntity">实体</typeparam>
    /// <typeparam name="TDbContextLocator">数据库上下文定位器</typeparam>
    /// <param name="entity">实体</param>
    /// <returns>代理中的实体</returns>
    public static EntityEntry<TEntity> Delete<TEntity, TDbContextLocator>(this TEntity entity)
        where TEntity : class, IPrivateEntity, new()
        where TDbContextLocator : class, IDbContextLocator
    {
        return new EntityExecutePart<TEntity>().SetEntity(entity).Change<TDbContextLocator>().Delete();
    }

    /// <summary>
    /// 删除一条记录
    /// </summary>
    /// <typeparam name="TEntity">实体</typeparam>
    /// <typeparam name="TDbContextLocator">数据库上下文定位器</typeparam>
    /// <param name="entity">实体</param>
    /// <returns>代理中的实体</returns>
    public static Task<EntityEntry<TEntity>> DeleteAsync<TEntity, TDbContextLocator>(this TEntity entity)
        where TEntity : class, IPrivateEntity, new()
        where TDbContextLocator : class, IDbContextLocator
    {
        return new EntityExecutePart<TEntity>().SetEntity(entity).Change<TDbContextLocator>().DeleteAsync();
    }
}
