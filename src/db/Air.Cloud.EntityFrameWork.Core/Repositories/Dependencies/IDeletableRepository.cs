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
/// <para>zh-cn:可删除仓储接口</para>
/// <para>en-us:Describes the behavior provided by this member.</para>
/// </summary>
/// <typeparam name="TEntity"><para>zh-cn:实体类型</para><para>en-us:The entity type.</para></typeparam>
public partial interface IDeletableRepository<TEntity> : IDeletableRepository<TEntity, MasterDbContextLocator>
    where TEntity : class, IPrivateEntity, new()
{
}

/// <summary>
/// <para>zh-cn:可删除仓储接口</para>
/// <para>en-us:Describes the behavior provided by this member.</para>
/// </summary>
/// <typeparam name="TEntity"><para>zh-cn:实体类型</para><para>en-us:The entity type.</para></typeparam>
/// <typeparam name="TDbContextLocator"><para>zh-cn:数据库上下文定位器</para><para>en-us:The database context locator type.</para></typeparam>
public partial interface IDeletableRepository<TEntity, TDbContextLocator> : IPrivateDeletableRepository<TEntity>
    where TEntity : class, IPrivateEntity, new()
    where TDbContextLocator : class, IDbContextLocator
{
}

/// <summary>
/// <para>zh-cn:可删除仓储接口</para>
/// <para>en-us:Describes the behavior provided by this member.</para>
/// </summary>
/// <typeparam name="TEntity"><para>zh-cn:实体类型</para><para>en-us:The entity type.</para></typeparam>
public interface IPrivateDeletableRepository<TEntity> : IPrivateRootRepository
    where TEntity : class, IPrivateEntity, new()
{
    /// <summary>
    /// <para>zh-cn:删除一条记录</para>
    /// <para>en-us:Describes the behavior provided by this member.</para>
    /// </summary>
    /// <param name="entity"><para>zh-cn:实体</para><para>en-us:The entity.</para></param>
    /// <returns><para>zh-cn:代理中的实体</para><para>en-us:The operation result.</para></returns>
    EntityEntry<TEntity> Delete(TEntity entity);

    /// <summary>
    /// <para>zh-cn:删除多条记录</para>
    /// <para>en-us:Describes the behavior provided by this member.</para>
    /// </summary>
    /// <param name="entities"><para>zh-cn:多个实体</para><para>en-us:The entities.</para></param>
    void Delete(params TEntity[] entities);

    /// <summary>
    /// <para>zh-cn:删除多条记录</para>
    /// <para>en-us:Describes the behavior provided by this member.</para>
    /// </summary>
    /// <param name="entities"><para>zh-cn:多个实体</para><para>en-us:The entities.</para></param>
    void Delete(IEnumerable<TEntity> entities);

    /// <summary>
    /// <para>zh-cn:删除一条记录</para>
    /// <para>en-us:Describes the behavior provided by this member.</para>
    /// </summary>
    /// <param name="entity"><para>zh-cn:实体</para><para>en-us:The entity.</para></param>
    /// <returns><para>zh-cn:代理中的实体</para><para>en-us:The operation result.</para></returns>
    Task<EntityEntry<TEntity>> DeleteAsync(TEntity entity);

    /// <summary>
    /// <para>zh-cn:删除多条记录</para>
    /// <para>en-us:Describes the behavior provided by this member.</para>
    /// </summary>
    /// <param name="entities"><para>zh-cn:多个实体</para><para>en-us:The entities.</para></param>
    /// <returns><para>zh-cn:Task</para><para>en-us:The operation result.</para></returns>
    Task DeleteAsync(params TEntity[] entities);

    /// <summary>
    /// <para>zh-cn:删除多条记录</para>
    /// <para>en-us:Describes the behavior provided by this member.</para>
    /// </summary>
    /// <param name="entities"><para>zh-cn:多个实体</para><para>en-us:The entities.</para></param>
    /// <returns><para>zh-cn:Task</para><para>en-us:The operation result.</para></returns>
    Task DeleteAsync(IEnumerable<TEntity> entities);

    /// <summary>
    /// <para>zh-cn:根据主键删除一条记录</para>
    /// <para>en-us:Describes the behavior provided by this member.</para>
    /// </summary>
    /// <param name="key"><para>zh-cn:主键</para><para>en-us:The key value.</para></param>
    void Delete(object key);

    /// <summary>
    /// <para>zh-cn:根据主键删除一条记录</para>
    /// <para>en-us:Describes the behavior provided by this member.</para>
    /// </summary>
    /// <param name="key"><para>zh-cn:主键</para><para>en-us:The key value.</para></param>
    /// <param name="cancellationToken"><para>zh-cn:异步取消令牌</para><para>en-us:The cancellation token.</para></param>
    /// <returns><para>zh-cn:Task</para><para>en-us:The operation result.</para></returns>
    Task DeleteAsync(object key, CancellationToken cancellationToken = default);
}
