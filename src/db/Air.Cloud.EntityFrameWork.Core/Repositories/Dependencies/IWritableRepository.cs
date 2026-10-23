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

namespace Air.Cloud.EntityFrameWork.Core.Repositories.Dependencies;

/// <summary>
/// <para>zh-cn:可写仓储接口</para>
/// <para>en-us:Describes the behavior provided by this member.</para>
/// </summary>
/// <typeparam name="TEntity"><para>zh-cn:实体类型</para><para>en-us:The entity type.</para></typeparam>
public partial interface IWritableRepository<TEntity> : IWritableRepository<TEntity, MasterDbContextLocator>
    where TEntity : class, IPrivateEntity, new()
{
}

/// <summary>
/// <para>zh-cn:可写仓储接口</para>
/// <para>en-us:Describes the behavior provided by this member.</para>
/// </summary>
/// <typeparam name="TEntity"><para>zh-cn:实体类型</para><para>en-us:The entity type.</para></typeparam>
/// <typeparam name="TDbContextLocator"><para>zh-cn:数据库上下文定位器</para><para>en-us:The database context locator type.</para></typeparam>
public partial interface IWritableRepository<TEntity, TDbContextLocator> : IPrivateWritableRepository<TEntity>
    , IInsertableRepository<TEntity>
    , IUpdateableRepository<TEntity>
    , IDeletableRepository<TEntity>
    where TEntity : class, IPrivateEntity, new()
    where TDbContextLocator : class, IDbContextLocator
{
}

/// <summary>
/// <para>zh-cn:可写仓储接口</para>
/// <para>en-us:Describes the behavior provided by this member.</para>
/// </summary>
/// <typeparam name="TEntity"><para>zh-cn:实体类型</para><para>en-us:The entity type.</para></typeparam>
public partial interface IPrivateWritableRepository<TEntity>
    : IPrivateInsertableRepository<TEntity>
    , IPrivateUpdateableRepository<TEntity>
    , IPrivateDeletableRepository<TEntity>
    , IPrivateRootRepository
    where TEntity : class, IPrivateEntity, new()
{
    /// <summary>
    /// <para>zh-cn:接受所有更改</para>
    /// <para>en-us:Describes the behavior provided by this member.</para>
    /// </summary>
    void AcceptAllChanges();

    /// <summary>
    /// <para>zh-cn:保存数据库上下文池中所有已更改的数据库上下文</para>
    /// <para>en-us:Describes the behavior provided by this member.</para>
    /// </summary>
    /// <returns><para>zh-cn:操作结果。</para><para>en-us:The operation result.</para></returns>
    int SavePoolNow();

    /// <summary>
    /// <para>zh-cn:保存数据库上下文池中所有已更改的数据库上下文</para>
    /// <para>en-us:Describes the behavior provided by this member.</para>
    /// </summary>
    /// <param name="acceptAllChangesOnSuccess"><para>zh-cn:保存成功后是否接受所有更改。</para><para>en-us:Whether to accept all changes after saving.</para></param>
    /// <returns><para>zh-cn:操作结果。</para><para>en-us:The operation result.</para></returns>
    int SavePoolNow(bool acceptAllChangesOnSuccess);

    /// <summary>
    /// <para>zh-cn:保存数据库上下文池中所有已更改的数据库上下文</para>
    /// <para>en-us:Describes the behavior provided by this member.</para>
    /// </summary>
    /// <param name="cancellationToken"><para>zh-cn:取消令牌。</para><para>en-us:The cancellation token.</para></param>
    /// <returns><para>zh-cn:操作结果。</para><para>en-us:The operation result.</para></returns>
    Task<int> SavePoolNowAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// <para>zh-cn:保存数据库上下文池中所有已更改的数据库上下文</para>
    /// <para>en-us:Describes the behavior provided by this member.</para>
    /// </summary>
    /// <param name="acceptAllChangesOnSuccess"><para>zh-cn:保存成功后是否接受所有更改。</para><para>en-us:Whether to accept all changes after saving.</para></param>
    /// <param name="cancellationToken"><para>zh-cn:取消令牌。</para><para>en-us:The cancellation token.</para></param>
    /// <returns><para>zh-cn:操作结果。</para><para>en-us:The operation result.</para></returns>
    Task<int> SavePoolNowAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default);

}
