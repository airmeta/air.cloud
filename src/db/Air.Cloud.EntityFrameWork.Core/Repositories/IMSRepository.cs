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
using Air.Cloud.EntityFrameWork.Core.Repositories.Dependencies;

namespace Air.Cloud.EntityFrameWork.Core.Repositories;

/// <summary>
/// <para>zh-cn:随机主从库仓储（主库是默认数据库）</para>
/// <para>en-us:Describes the behavior provided by this member.</para>
/// </summary>
public partial interface IMSRepository : IMSRepository<MasterDbContextLocator>
{
}

/// <summary>
/// <para>zh-cn:随机主从库仓储</para>
/// <para>en-us:Describes the behavior provided by this member.</para>
/// </summary>
/// <typeparam name="TMasterDbContextLocator"><para>zh-cn:主库</para><para>en-us:The master database context locator type.</para></typeparam>
public partial interface IMSRepository<TMasterDbContextLocator>
    where TMasterDbContextLocator : class, IDbContextLocator
{
    /// <summary>
    /// <para>zh-cn:获取主库仓储</para>
    /// <para>en-us:Describes the behavior provided by this member.</para>
    /// </summary>
    /// <typeparam name="TEntity"><para>zh-cn:实体类型</para><para>en-us:The entity type.</para></typeparam>
    /// <returns><para>zh-cn:操作结果。</para><para>en-us:The operation result.</para></returns>
    IRepository<TEntity, TMasterDbContextLocator> Master<TEntity>()
        where TEntity : class, IPrivateEntity, new();

    /// <summary>
    /// <para>zh-cn:动态获取从库（随机）</para>
    /// <para>en-us:Describes the behavior provided by this member.</para>
    /// </summary>
    /// <typeparam name="TEntity"><para>zh-cn:实体类型</para><para>en-us:The entity type.</para></typeparam>
    /// <returns><para>zh-cn:操作结果。</para><para>en-us:The operation result.</para></returns>
    IPrivateReadableRepository<TEntity> Slave<TEntity>()
        where TEntity : class, IPrivateEntity, new();

    /// <summary>
    /// <para>zh-cn:动态获取从库（自定义）</para>
    /// <para>en-us:Describes the behavior provided by this member.</para>
    /// </summary>
    /// <typeparam name="TEntity"><para>zh-cn:实体类型</para><para>en-us:The entity type.</para></typeparam>
    /// <returns><para>zh-cn:操作结果。</para><para>en-us:The operation result.</para></returns>
    IPrivateReadableRepository<TEntity> Slave<TEntity>(Func<Type> locatorHandle)
        where TEntity : class, IPrivateEntity, new();
}

/// <summary>
/// <para>zh-cn:主从库仓储</para>
/// <para>en-us:Describes the behavior provided by this member.</para>
/// </summary>
/// <typeparam name="TMasterDbContextLocator"><para>zh-cn:主库</para><para>en-us:The master database context locator type.</para></typeparam>
/// <typeparam name="TSlaveDbContextLocator1"><para>zh-cn:从库</para><para>en-us:The first slave database context locator type.</para></typeparam>
public partial interface IMSRepository<TMasterDbContextLocator, TSlaveDbContextLocator1>
    where TMasterDbContextLocator : class, IDbContextLocator
    where TSlaveDbContextLocator1 : class, IDbContextLocator
{
    /// <summary>
    /// <para>zh-cn:获取主库仓储</para>
    /// <para>en-us:Describes the behavior provided by this member.</para>
    /// </summary>
    /// <typeparam name="TEntity"><para>zh-cn:实体类型</para><para>en-us:The entity type.</para></typeparam>
    /// <returns><para>zh-cn:操作结果。</para><para>en-us:The operation result.</para></returns>
    IRepository<TEntity, TMasterDbContextLocator> Master<TEntity>()
         where TEntity : class, IPrivateEntity, new();

    /// <summary>
    /// <para>zh-cn:获取从库仓储</para>
    /// <para>en-us:Describes the behavior provided by this member.</para>
    /// </summary>
    /// <typeparam name="TEntity"><para>zh-cn:实体类型</para><para>en-us:The entity type.</para></typeparam>
    /// <returns><para>zh-cn:操作结果。</para><para>en-us:The operation result.</para></returns>
    IReadableRepository<TEntity, TSlaveDbContextLocator1> Slave1<TEntity>()
        where TEntity : class, IPrivateEntity, new();
}

/// <summary>
/// <para>zh-cn:主从库仓储</para>
/// <para>en-us:Describes the behavior provided by this member.</para>
/// </summary>
/// <typeparam name="TMasterDbContextLocator"><para>zh-cn:主库</para><para>en-us:The master database context locator type.</para></typeparam>
/// <typeparam name="TSlaveDbContextLocator1"><para>zh-cn:从库</para><para>en-us:The first slave database context locator type.</para></typeparam>
/// <typeparam name="TSlaveDbContextLocator2"><para>zh-cn:从库</para><para>en-us:The second slave database context locator type.</para></typeparam>
public partial interface IMSRepository<TMasterDbContextLocator, TSlaveDbContextLocator1, TSlaveDbContextLocator2>
    : IMSRepository<TMasterDbContextLocator, TSlaveDbContextLocator1>
    where TMasterDbContextLocator : class, IDbContextLocator
    where TSlaveDbContextLocator1 : class, IDbContextLocator
    where TSlaveDbContextLocator2 : class, IDbContextLocator
{
    /// <summary>
    /// <para>zh-cn:获取从库仓储2</para>
    /// <para>en-us:Describes the behavior provided by this member.</para>
    /// </summary>
    /// <typeparam name="TEntity"><para>zh-cn:实体类型</para><para>en-us:The entity type.</para></typeparam>
    /// <returns><para>zh-cn:操作结果。</para><para>en-us:The operation result.</para></returns>
    IReadableRepository<TEntity, TSlaveDbContextLocator2> Slave2<TEntity>()
        where TEntity : class, IPrivateEntity, new();
}

/// <summary>
/// <para>zh-cn:主从库仓储</para>
/// <para>en-us:Describes the behavior provided by this member.</para>
/// </summary>
/// <typeparam name="TMasterDbContextLocator"><para>zh-cn:主库</para><para>en-us:The master database context locator type.</para></typeparam>
/// <typeparam name="TSlaveDbContextLocator1"><para>zh-cn:从库</para><para>en-us:The first slave database context locator type.</para></typeparam>
/// <typeparam name="TSlaveDbContextLocator2"><para>zh-cn:从库</para><para>en-us:The second slave database context locator type.</para></typeparam>
/// <typeparam name="TSlaveDbContextLocator3"><para>zh-cn:从库</para><para>en-us:The third slave database context locator type.</para></typeparam>
public partial interface IMSRepository<TMasterDbContextLocator, TSlaveDbContextLocator1, TSlaveDbContextLocator2, TSlaveDbContextLocator3>
    : IMSRepository<TMasterDbContextLocator, TSlaveDbContextLocator1, TSlaveDbContextLocator2>
    where TMasterDbContextLocator : class, IDbContextLocator
    where TSlaveDbContextLocator1 : class, IDbContextLocator
    where TSlaveDbContextLocator2 : class, IDbContextLocator
    where TSlaveDbContextLocator3 : class, IDbContextLocator
{
    /// <summary>
    /// <para>zh-cn:获取从库仓储3</para>
    /// <para>en-us:Describes the behavior provided by this member.</para>
    /// </summary>
    /// <typeparam name="TEntity"><para>zh-cn:实体类型</para><para>en-us:The entity type.</para></typeparam>
    /// <returns><para>zh-cn:操作结果。</para><para>en-us:The operation result.</para></returns>
    IReadableRepository<TEntity, TSlaveDbContextLocator3> Slave3<TEntity>()
        where TEntity : class, IPrivateEntity, new();
}

/// <summary>
/// <para>zh-cn:主从库仓储</para>
/// <para>en-us:Describes the behavior provided by this member.</para>
/// </summary>
/// <typeparam name="TMasterDbContextLocator"><para>zh-cn:主库</para><para>en-us:The master database context locator type.</para></typeparam>
/// <typeparam name="TSlaveDbContextLocator1"><para>zh-cn:从库</para><para>en-us:The first slave database context locator type.</para></typeparam>
/// <typeparam name="TSlaveDbContextLocator2"><para>zh-cn:从库</para><para>en-us:The second slave database context locator type.</para></typeparam>
/// <typeparam name="TSlaveDbContextLocator3"><para>zh-cn:从库</para><para>en-us:The third slave database context locator type.</para></typeparam>
/// <typeparam name="TSlaveDbContextLocator4"><para>zh-cn:从库</para><para>en-us:The fourth slave database context locator type.</para></typeparam>
public partial interface IMSRepository<TMasterDbContextLocator, TSlaveDbContextLocator1, TSlaveDbContextLocator2, TSlaveDbContextLocator3, TSlaveDbContextLocator4>
    : IMSRepository<TMasterDbContextLocator, TSlaveDbContextLocator1, TSlaveDbContextLocator2, TSlaveDbContextLocator3>
    where TMasterDbContextLocator : class, IDbContextLocator
    where TSlaveDbContextLocator1 : class, IDbContextLocator
    where TSlaveDbContextLocator2 : class, IDbContextLocator
    where TSlaveDbContextLocator3 : class, IDbContextLocator
    where TSlaveDbContextLocator4 : class, IDbContextLocator
{
    /// <summary>
    /// <para>zh-cn:获取从库仓储4</para>
    /// <para>en-us:Describes the behavior provided by this member.</para>
    /// </summary>
    /// <typeparam name="TEntity"><para>zh-cn:实体类型</para><para>en-us:The entity type.</para></typeparam>
    /// <returns><para>zh-cn:操作结果。</para><para>en-us:The operation result.</para></returns>
    IReadableRepository<TEntity, TSlaveDbContextLocator4> Slave4<TEntity>()
        where TEntity : class, IPrivateEntity, new();
}

/// <summary>
/// <para>zh-cn:主从库仓储</para>
/// <para>en-us:Describes the behavior provided by this member.</para>
/// </summary>
/// <typeparam name="TMasterDbContextLocator"><para>zh-cn:主库</para><para>en-us:The master database context locator type.</para></typeparam>
/// <typeparam name="TSlaveDbContextLocator1"><para>zh-cn:从库</para><para>en-us:The first slave database context locator type.</para></typeparam>
/// <typeparam name="TSlaveDbContextLocator2"><para>zh-cn:从库</para><para>en-us:The second slave database context locator type.</para></typeparam>
/// <typeparam name="TSlaveDbContextLocator3"><para>zh-cn:从库</para><para>en-us:The third slave database context locator type.</para></typeparam>
/// <typeparam name="TSlaveDbContextLocator4"><para>zh-cn:从库</para><para>en-us:The fourth slave database context locator type.</para></typeparam>
/// <typeparam name="TSlaveDbContextLocator5"><para>zh-cn:从库</para><para>en-us:The fifth slave database context locator type.</para></typeparam>
public partial interface IMSRepository<TMasterDbContextLocator, TSlaveDbContextLocator1, TSlaveDbContextLocator2, TSlaveDbContextLocator3, TSlaveDbContextLocator4, TSlaveDbContextLocator5>
    : IMSRepository<TMasterDbContextLocator, TSlaveDbContextLocator1, TSlaveDbContextLocator2, TSlaveDbContextLocator3, TSlaveDbContextLocator4>
    where TMasterDbContextLocator : class, IDbContextLocator
    where TSlaveDbContextLocator1 : class, IDbContextLocator
    where TSlaveDbContextLocator2 : class, IDbContextLocator
    where TSlaveDbContextLocator3 : class, IDbContextLocator
    where TSlaveDbContextLocator4 : class, IDbContextLocator
    where TSlaveDbContextLocator5 : class, IDbContextLocator
{
    /// <summary>
    /// <para>zh-cn:获取从库仓储5</para>
    /// <para>en-us:Describes the behavior provided by this member.</para>
    /// </summary>
    /// <typeparam name="TEntity"><para>zh-cn:实体类型</para><para>en-us:The entity type.</para></typeparam>
    /// <returns><para>zh-cn:操作结果。</para><para>en-us:The operation result.</para></returns>
    IReadableRepository<TEntity, TSlaveDbContextLocator5> Slave5<TEntity>()
        where TEntity : class, IPrivateEntity, new();
}

/// <summary>
/// <para>zh-cn:主从库仓储</para>
/// <para>en-us:Describes the behavior provided by this member.</para>
/// </summary>
/// <typeparam name="TMasterDbContextLocator"><para>zh-cn:主库</para><para>en-us:The master database context locator type.</para></typeparam>
/// <typeparam name="TSlaveDbContextLocator1"><para>zh-cn:从库</para><para>en-us:The first slave database context locator type.</para></typeparam>
/// <typeparam name="TSlaveDbContextLocator2"><para>zh-cn:从库</para><para>en-us:The second slave database context locator type.</para></typeparam>
/// <typeparam name="TSlaveDbContextLocator3"><para>zh-cn:从库</para><para>en-us:The third slave database context locator type.</para></typeparam>
/// <typeparam name="TSlaveDbContextLocator4"><para>zh-cn:从库</para><para>en-us:The fourth slave database context locator type.</para></typeparam>
/// <typeparam name="TSlaveDbContextLocator5"><para>zh-cn:从库</para><para>en-us:The fifth slave database context locator type.</para></typeparam>
/// <typeparam name="TSlaveDbContextLocator6"><para>zh-cn:从库</para><para>en-us:The sixth slave database context locator type.</para></typeparam>
public partial interface IMSRepository<TMasterDbContextLocator, TSlaveDbContextLocator1, TSlaveDbContextLocator2, TSlaveDbContextLocator3, TSlaveDbContextLocator4, TSlaveDbContextLocator5, TSlaveDbContextLocator6>
    : IMSRepository<TMasterDbContextLocator, TSlaveDbContextLocator1, TSlaveDbContextLocator2, TSlaveDbContextLocator3, TSlaveDbContextLocator4, TSlaveDbContextLocator5>
    where TMasterDbContextLocator : class, IDbContextLocator
    where TSlaveDbContextLocator1 : class, IDbContextLocator
    where TSlaveDbContextLocator2 : class, IDbContextLocator
    where TSlaveDbContextLocator3 : class, IDbContextLocator
    where TSlaveDbContextLocator4 : class, IDbContextLocator
    where TSlaveDbContextLocator5 : class, IDbContextLocator
    where TSlaveDbContextLocator6 : class, IDbContextLocator
{
    /// <summary>
    /// <para>zh-cn:获取从库仓储6</para>
    /// <para>en-us:Describes the behavior provided by this member.</para>
    /// </summary>
    /// <typeparam name="TEntity"><para>zh-cn:实体类型</para><para>en-us:The entity type.</para></typeparam>
    /// <returns><para>zh-cn:操作结果。</para><para>en-us:The operation result.</para></returns>
    IReadableRepository<TEntity, TSlaveDbContextLocator6> Slave6<TEntity>()
        where TEntity : class, IPrivateEntity, new();
}

/// <summary>
/// <para>zh-cn:主从库仓储</para>
/// <para>en-us:Describes the behavior provided by this member.</para>
/// </summary>
/// <typeparam name="TMasterDbContextLocator"><para>zh-cn:主库</para><para>en-us:The master database context locator type.</para></typeparam>
/// <typeparam name="TSlaveDbContextLocator1"><para>zh-cn:从库</para><para>en-us:The first slave database context locator type.</para></typeparam>
/// <typeparam name="TSlaveDbContextLocator2"><para>zh-cn:从库</para><para>en-us:The second slave database context locator type.</para></typeparam>
/// <typeparam name="TSlaveDbContextLocator3"><para>zh-cn:从库</para><para>en-us:The third slave database context locator type.</para></typeparam>
/// <typeparam name="TSlaveDbContextLocator4"><para>zh-cn:从库</para><para>en-us:The fourth slave database context locator type.</para></typeparam>
/// <typeparam name="TSlaveDbContextLocator5"><para>zh-cn:从库</para><para>en-us:The fifth slave database context locator type.</para></typeparam>
/// <typeparam name="TSlaveDbContextLocator6"><para>zh-cn:从库</para><para>en-us:The sixth slave database context locator type.</para></typeparam>
/// <typeparam name="TSlaveDbContextLocator7"><para>zh-cn:从库</para><para>en-us:The seventh slave database context locator type.</para></typeparam>
public partial interface IMSRepository<TMasterDbContextLocator, TSlaveDbContextLocator1, TSlaveDbContextLocator2, TSlaveDbContextLocator3, TSlaveDbContextLocator4, TSlaveDbContextLocator5, TSlaveDbContextLocator6, TSlaveDbContextLocator7>
    : IMSRepository<TMasterDbContextLocator, TSlaveDbContextLocator1, TSlaveDbContextLocator2, TSlaveDbContextLocator3, TSlaveDbContextLocator4, TSlaveDbContextLocator5, TSlaveDbContextLocator6>
    where TMasterDbContextLocator : class, IDbContextLocator
    where TSlaveDbContextLocator1 : class, IDbContextLocator
    where TSlaveDbContextLocator2 : class, IDbContextLocator
    where TSlaveDbContextLocator3 : class, IDbContextLocator
    where TSlaveDbContextLocator4 : class, IDbContextLocator
    where TSlaveDbContextLocator5 : class, IDbContextLocator
    where TSlaveDbContextLocator6 : class, IDbContextLocator
    where TSlaveDbContextLocator7 : class, IDbContextLocator
{
    /// <summary>
    /// <para>zh-cn:获取从库仓储7</para>
    /// <para>en-us:Describes the behavior provided by this member.</para>
    /// </summary>
    /// <typeparam name="TEntity"><para>zh-cn:实体类型</para><para>en-us:The entity type.</para></typeparam>
    /// <returns><para>zh-cn:操作结果。</para><para>en-us:The operation result.</para></returns>
    IReadableRepository<TEntity, TSlaveDbContextLocator7> Slave7<TEntity>()
        where TEntity : class, IPrivateEntity, new();
}
