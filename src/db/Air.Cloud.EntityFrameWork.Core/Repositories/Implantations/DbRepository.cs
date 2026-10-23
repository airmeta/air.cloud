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

using Microsoft.Extensions.DependencyInjection;

namespace Air.Cloud.EntityFrameWork.Core.Repositories.Implantations;

/// <summary>
/// 多数据库仓储
/// <para>zh-cn:该成员的数据库访问行为。</para>
/// <para>en-us:Database access behavior provided by this member.</para>
/// </summary>
/// <typeparam name="TDbContextLocator"><para>zh-cn:泛型参数类型</para><para>en-us:The database context locator type.</para></typeparam>
public partial class DbRepository<TDbContextLocator> : IDbRepository<TDbContextLocator>
    where TDbContextLocator : class, IDbContextLocator
{
    /// <summary>
    /// 服务提供器
    /// <para>zh-cn:该成员的数据库访问行为。</para>
    /// <para>en-us:Database access behavior provided by this member.</para>
    /// </summary>
    private readonly IServiceProvider _serviceProvider;

    /// <summary>
    /// 构造函数
    /// <para>zh-cn:该成员的数据库访问行为。</para>
    /// <para>en-us:Database access behavior provided by this member.</para>
    /// </summary>
    /// <param name="serviceProvider"><para>zh-cn:服务提供器</para><para>en-us:The serviceProvider value.</para></param>
    public DbRepository(
        IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    /// <summary>
    /// 切换实体
    /// <para>zh-cn:该成员的数据库访问行为。</para>
    /// <para>en-us:Database access behavior provided by this member.</para>
    /// </summary>
    /// <typeparam name="TEntity"><para>zh-cn:泛型参数类型</para><para>en-us:The entity type.</para></typeparam>
    /// <returns><para>zh-cn:操作结果。</para><para>en-us:The operation result.</para></returns>
    public virtual IRepository<TEntity, TDbContextLocator> Change<TEntity>()
         where TEntity : class, IPrivateEntity, new()
    {
        return _serviceProvider.GetRequiredService<IRepository<TEntity, TDbContextLocator>>();
    }

    /// <summary>
    /// 解析服务
    /// <para>zh-cn:该成员的数据库访问行为。</para>
    /// <para>en-us:Database access behavior provided by this member.</para>
    /// </summary>
    /// <typeparam name="TService"><para>zh-cn:泛型参数类型</para><para>en-us:The generic parameter type.</para></typeparam>
    /// <returns><para>zh-cn:操作结果。</para><para>en-us:The operation result.</para></returns>
    public virtual TService GetService<TService>()
        where TService : notnull
    {
        return _serviceProvider.GetRequiredService<TService>();
    }

    /// <summary>
    /// 解析服务
    /// <para>zh-cn:该成员的数据库访问行为。</para>
    /// <para>en-us:Database access behavior provided by this member.</para>
    /// </summary>
    /// <typeparam name="TService"><para>zh-cn:泛型参数类型</para><para>en-us:The generic parameter type.</para></typeparam>
    /// <returns><para>zh-cn:操作结果。</para><para>en-us:The operation result.</para></returns>
    public virtual TService GetRequiredService<TService>()
        where TService : notnull
    {
        return _serviceProvider.GetRequiredService<TService>();
    }
}
