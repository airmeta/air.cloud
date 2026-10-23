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

namespace Air.Cloud.EntityFrameWork.Core.Repositories;

/// <summary>
/// <para>zh-cn:多数据库仓储</para>
/// <para>en-us:Describes the behavior provided by this member.</para>
/// </summary>
/// <typeparam name="TDbContextLocator"><para>zh-cn:泛型参数类型</para><para>en-us:The database context locator type.</para></typeparam>
public partial interface IDbRepository<TDbContextLocator>
    where TDbContextLocator : class, IDbContextLocator
{
    /// <summary>
    /// <para>zh-cn:切换仓储</para>
    /// <para>en-us:Describes the behavior provided by this member.</para>
    /// </summary>
    /// <typeparam name="TEntity"><para>zh-cn:实体类型</para><para>en-us:The entity type.</para></typeparam>
    /// <returns><para>zh-cn:仓储</para><para>en-us:The operation result.</para></returns>
    IRepository<TEntity, TDbContextLocator> Change<TEntity>()
        where TEntity : class, IPrivateEntity, new();

    /// <summary>
    /// <para>zh-cn:解析服务</para>
    /// <para>en-us:Describes the behavior provided by this member.</para>
    /// </summary>
    /// <typeparam name="TService"><para>zh-cn:泛型参数类型</para><para>en-us:The generic parameter type.</para></typeparam>
    /// <returns><para>zh-cn:操作结果。</para><para>en-us:The operation result.</para></returns>
    TService GetService<TService>()
        where TService : notnull;

    /// <summary>
    /// <para>zh-cn:解析服务</para>
    /// <para>en-us:Describes the behavior provided by this member.</para>
    /// </summary>
    /// <typeparam name="TService"><para>zh-cn:泛型参数类型</para><para>en-us:The generic parameter type.</para></typeparam>
    /// <returns><para>zh-cn:操作结果。</para><para>en-us:The operation result.</para></returns>
    TService GetRequiredService<TService>()
        where TService : notnull;
}
