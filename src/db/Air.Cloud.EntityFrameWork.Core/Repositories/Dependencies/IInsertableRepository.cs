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
/// <para>zh-cn:可插入仓储接口</para>
/// <para>en-us:Describes the behavior provided by this member.</para>
/// </summary>
/// <typeparam name="TEntity"><para>zh-cn:实体类型</para><para>en-us:The entity type.</para></typeparam>
public partial interface IInsertableRepository<TEntity> : IInsertableRepository<TEntity, MasterDbContextLocator>
    where TEntity : class, IPrivateEntity, new()
{
}

/// <summary>
/// <para>zh-cn:可插入仓储接口</para>
/// <para>en-us:Describes the behavior provided by this member.</para>
/// </summary>
/// <typeparam name="TEntity"><para>zh-cn:实体类型</para><para>en-us:The entity type.</para></typeparam>
/// <typeparam name="TDbContextLocator"><para>zh-cn:数据库上下文定位器</para><para>en-us:The database context locator type.</para></typeparam>
public partial interface IInsertableRepository<TEntity, TDbContextLocator> : IPrivateInsertableRepository<TEntity>
    where TEntity : class, IPrivateEntity, new()
    where TDbContextLocator : class, IDbContextLocator
{
}

/// <summary>
/// <para>zh-cn:可插入仓储接口</para>
/// <para>en-us:Describes the behavior provided by this member.</para>
/// </summary>
/// <typeparam name="TEntity"><para>zh-cn:实体类型</para><para>en-us:The entity type.</para></typeparam>
public interface IPrivateInsertableRepository<TEntity> : IPrivateRootRepository
    where TEntity : class, IPrivateEntity, new()
{
    /// <summary>
    /// <para>zh-cn:新增一条记录</para>
    /// <para>en-us:Describes the behavior provided by this member.</para>
    /// </summary>
    /// <param name="entity"><para>zh-cn:实体</para><para>en-us:The entity.</para></param>
    /// <param name="ignoreNullValues"><para>zh-cn:是否忽略空值。</para><para>en-us:Whether to ignore null values.</para></param>
    /// <returns><para>zh-cn:代理的实体</para><para>en-us:The operation result.</para></returns>
    EntityEntry<TEntity> Insert(TEntity entity, bool? ignoreNullValues = null);

    /// <summary>
    /// <para>zh-cn:新增多条记录</para>
    /// <para>en-us:Describes the behavior provided by this member.</para>
    /// </summary>
    /// <param name="entities"><para>zh-cn:多个实体</para><para>en-us:The entities.</para></param>
    void Insert(params TEntity[] entities);

    /// <summary>
    /// <para>zh-cn:新增多条记录</para>
    /// <para>en-us:Describes the behavior provided by this member.</para>
    /// </summary>
    /// <param name="entities"><para>zh-cn:多个实体</para><para>en-us:The entities.</para></param>
    void Insert(IEnumerable<TEntity> entities);

    /// <summary>
    /// <para>zh-cn:新增一条记录</para>
    /// <para>en-us:Describes the behavior provided by this member.</para>
    /// </summary>
    /// <param name="entity"><para>zh-cn:实体</para><para>en-us:The entity.</para></param>
    /// <param name="ignoreNullValues"><para>zh-cn:是否忽略空值。</para><para>en-us:Whether to ignore null values.</para></param>
    /// <param name="cancellationToken"><para>zh-cn:取消异步令牌</para><para>en-us:The cancellation token.</para></param>
    /// <returns><para>zh-cn:代理的实体</para><para>en-us:The operation result.</para></returns>
    Task<EntityEntry<TEntity>> InsertAsync(TEntity entity, bool? ignoreNullValues = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// <para>zh-cn:新增多条记录</para>
    /// <para>en-us:Describes the behavior provided by this member.</para>
    /// </summary>
    /// <param name="entities"><para>zh-cn:多个实体</para><para>en-us:The entities.</para></param>
    /// <returns><para>zh-cn:Task</para><para>en-us:The operation result.</para></returns>
    Task InsertAsync(params TEntity[] entities);

    /// <summary>
    /// <para>zh-cn:新增多条记录</para>
    /// <para>en-us:Describes the behavior provided by this member.</para>
    /// </summary>
    /// <param name="entities"><para>zh-cn:多个实体</para><para>en-us:The entities.</para></param>
    /// <param name="cancellationToken"><para>zh-cn:取消异步令牌</para><para>en-us:The cancellation token.</para></param>
    /// <returns><para>zh-cn:操作结果。</para><para>en-us:The operation result.</para></returns>
    Task InsertAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default);

}
