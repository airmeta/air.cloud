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

namespace Air.Cloud.EntityFrameWork.Core.Internal;

/// <summary>
/// 实体执行部件
/// <para>zh-cn:该成员的数据库访问行为。</para>
/// <para>en-us:Database access behavior provided by this member.</para>
/// </summary>
public sealed partial class EntityExecutePart<TEntity>
    where TEntity : class, IPrivateEntity, new()
{
    /// <summary>
    /// 设置实体
    /// <para>zh-cn:该成员的数据库访问行为。</para>
    /// <para>en-us:Database access behavior provided by this member.</para>
    /// </summary>
    /// <param name="entity"><para>zh-cn:实体对象。</para><para>en-us:The entity.</para></param>
    /// <returns><para>zh-cn:操作结果。</para><para>en-us:The operation result.</para></returns>
    public EntityExecutePart<TEntity> SetEntity(TEntity entity)
    {
        Entity = entity;
        return this;
    }

    /// <summary>
    /// 设置数据库执行作用域
    /// <para>zh-cn:该成员的数据库访问行为。</para>
    /// <para>en-us:Database access behavior provided by this member.</para>
    /// </summary>
    /// <param name="serviceProvider"><para>zh-cn:服务提供器。</para><para>en-us:The service provider.</para></param>
    /// <returns><para>zh-cn:操作结果。</para><para>en-us:The operation result.</para></returns>
    public EntityExecutePart<TEntity> SetContextScoped(IServiceProvider serviceProvider)
    {
        if (serviceProvider != null) ContextScoped = serviceProvider;
        return this;
    }

    /// <summary>
    /// 设置数据库上下文定位器
    /// <para>zh-cn:该成员的数据库访问行为。</para>
    /// <para>en-us:Database access behavior provided by this member.</para>
    /// </summary>
    /// <typeparam name="TDbContextLocator"><para>zh-cn:泛型参数类型</para><para>en-us:The database context locator type.</para></typeparam>
    /// <returns><para>zh-cn:操作结果。</para><para>en-us:The operation result.</para></returns>
    public EntityExecutePart<TEntity> Change<TDbContextLocator>()
        where TDbContextLocator : class, IDbContextLocator
    {
        return Change(typeof(TDbContextLocator));
    }

    /// <summary>
    /// 设置数据库上下文定位器
    /// <para>zh-cn:该成员的数据库访问行为。</para>
    /// <para>en-us:Database access behavior provided by this member.</para>
    /// </summary>
    /// <returns><para>zh-cn:操作结果。</para><para>en-us:The operation result.</para></returns>
    public EntityExecutePart<TEntity> Change(Type dbContextLocator)
    {
        if (dbContextLocator != null) DbContextLocator = dbContextLocator;
        return this;
    }
}
