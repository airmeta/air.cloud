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

namespace Air.Cloud.EntityFrameWork.Core.Entities.Dependencies;

/// <summary>
/// <para>zh-cn:数据库实体依赖基类</para>
/// <para>en-us:Describes the behavior provided by this member.</para>
/// </summary>
[IgnoreScanning]
public abstract class Entity : Entity<int, MasterDbContextLocator>
{
}

/// <summary>
/// <para>zh-cn:数据库实体依赖基类</para>
/// <para>en-us:Describes the behavior provided by this member.</para>
/// </summary>
/// <typeparam name="TKey"><para>zh-cn:主键类型</para><para>en-us:The generic parameter type.</para></typeparam>
[IgnoreScanning]
public abstract class Entity<TKey> : Entity<TKey, MasterDbContextLocator>
{
}

/// <summary>
/// <para>zh-cn:数据库实体依赖基类</para>
/// <para>en-us:Describes the behavior provided by this member.</para>
/// </summary>
/// <typeparam name="TKey"><para>zh-cn:主键类型</para><para>en-us:The generic parameter type.</para></typeparam>
/// <typeparam name="TDbContextLocator1"><para>zh-cn:数据库上下文定位器</para><para>en-us:The first database context locator type.</para></typeparam>
[IgnoreScanning]
public abstract class Entity<TKey, TDbContextLocator1> : PrivateEntity<TKey>
    where TDbContextLocator1 : class, IDbContextLocator
{
}

/// <summary>
/// <para>zh-cn:数据库实体依赖基类</para>
/// <para>en-us:Describes the behavior provided by this member.</para>
/// </summary>
/// <typeparam name="TKey"><para>zh-cn:主键类型</para><para>en-us:The generic parameter type.</para></typeparam>
/// <typeparam name="TDbContextLocator1"><para>zh-cn:数据库上下文定位器</para><para>en-us:The first database context locator type.</para></typeparam>
/// <typeparam name="TDbContextLocator2"><para>zh-cn:数据库上下文定位器</para><para>en-us:The second database context locator type.</para></typeparam>
[IgnoreScanning]
public abstract class Entity<TKey, TDbContextLocator1, TDbContextLocator2> : PrivateEntity<TKey>
    where TDbContextLocator1 : class, IDbContextLocator
    where TDbContextLocator2 : class, IDbContextLocator
{
}

/// <summary>
/// <para>zh-cn:数据库实体依赖基类</para>
/// <para>en-us:Describes the behavior provided by this member.</para>
/// </summary>
/// <typeparam name="TKey"><para>zh-cn:主键类型</para><para>en-us:The generic parameter type.</para></typeparam>
/// <typeparam name="TDbContextLocator1"><para>zh-cn:数据库上下文定位器</para><para>en-us:The first database context locator type.</para></typeparam>
/// <typeparam name="TDbContextLocator2"><para>zh-cn:数据库上下文定位器</para><para>en-us:The second database context locator type.</para></typeparam>
/// <typeparam name="TDbContextLocator3"><para>zh-cn:数据库上下文定位器</para><para>en-us:The third database context locator type.</para></typeparam>
[IgnoreScanning]
public abstract class Entity<TKey, TDbContextLocator1, TDbContextLocator2, TDbContextLocator3> : PrivateEntity<TKey>
    where TDbContextLocator1 : class, IDbContextLocator
    where TDbContextLocator2 : class, IDbContextLocator
    where TDbContextLocator3 : class, IDbContextLocator
{
}

/// <summary>
/// <para>zh-cn:数据库实体依赖基类</para>
/// <para>en-us:Describes the behavior provided by this member.</para>
/// </summary>
/// <typeparam name="TKey"><para>zh-cn:主键类型</para><para>en-us:The generic parameter type.</para></typeparam>
/// <typeparam name="TDbContextLocator1"><para>zh-cn:数据库上下文定位器</para><para>en-us:The first database context locator type.</para></typeparam>
/// <typeparam name="TDbContextLocator2"><para>zh-cn:数据库上下文定位器</para><para>en-us:The second database context locator type.</para></typeparam>
/// <typeparam name="TDbContextLocator3"><para>zh-cn:数据库上下文定位器</para><para>en-us:The third database context locator type.</para></typeparam>
/// <typeparam name="TDbContextLocator4"><para>zh-cn:数据库上下文定位器</para><para>en-us:The fourth database context locator type.</para></typeparam>
[IgnoreScanning]
public abstract class Entity<TKey, TDbContextLocator1, TDbContextLocator2, TDbContextLocator3, TDbContextLocator4> : PrivateEntity<TKey>
    where TDbContextLocator1 : class, IDbContextLocator
    where TDbContextLocator2 : class, IDbContextLocator
    where TDbContextLocator3 : class, IDbContextLocator
    where TDbContextLocator4 : class, IDbContextLocator
{
}

/// <summary>
/// <para>zh-cn:数据库实体依赖基类</para>
/// <para>en-us:Describes the behavior provided by this member.</para>
/// </summary>
/// <typeparam name="TKey"><para>zh-cn:主键类型</para><para>en-us:The generic parameter type.</para></typeparam>
/// <typeparam name="TDbContextLocator1"><para>zh-cn:数据库上下文定位器</para><para>en-us:The first database context locator type.</para></typeparam>
/// <typeparam name="TDbContextLocator2"><para>zh-cn:数据库上下文定位器</para><para>en-us:The second database context locator type.</para></typeparam>
/// <typeparam name="TDbContextLocator3"><para>zh-cn:数据库上下文定位器</para><para>en-us:The third database context locator type.</para></typeparam>
/// <typeparam name="TDbContextLocator4"><para>zh-cn:数据库上下文定位器</para><para>en-us:The fourth database context locator type.</para></typeparam>
/// <typeparam name="TDbContextLocator5"><para>zh-cn:数据库上下文定位器</para><para>en-us:The fifth database context locator type.</para></typeparam>
[IgnoreScanning]
public abstract class Entity<TKey, TDbContextLocator1, TDbContextLocator2, TDbContextLocator3, TDbContextLocator4, TDbContextLocator5> : PrivateEntity<TKey>
    where TDbContextLocator1 : class, IDbContextLocator
    where TDbContextLocator2 : class, IDbContextLocator
    where TDbContextLocator3 : class, IDbContextLocator
    where TDbContextLocator4 : class, IDbContextLocator
    where TDbContextLocator5 : class, IDbContextLocator
{
}

/// <summary>
/// <para>zh-cn:数据库实体依赖基类</para>
/// <para>en-us:Describes the behavior provided by this member.</para>
/// </summary>
/// <typeparam name="TKey"><para>zh-cn:主键类型</para><para>en-us:The generic parameter type.</para></typeparam>
/// <typeparam name="TDbContextLocator1"><para>zh-cn:数据库上下文定位器</para><para>en-us:The first database context locator type.</para></typeparam>
/// <typeparam name="TDbContextLocator2"><para>zh-cn:数据库上下文定位器</para><para>en-us:The second database context locator type.</para></typeparam>
/// <typeparam name="TDbContextLocator3"><para>zh-cn:数据库上下文定位器</para><para>en-us:The third database context locator type.</para></typeparam>
/// <typeparam name="TDbContextLocator4"><para>zh-cn:数据库上下文定位器</para><para>en-us:The fourth database context locator type.</para></typeparam>
/// <typeparam name="TDbContextLocator5"><para>zh-cn:数据库上下文定位器</para><para>en-us:The fifth database context locator type.</para></typeparam>
/// <typeparam name="TDbContextLocator6"><para>zh-cn:数据库上下文定位器</para><para>en-us:The sixth database context locator type.</para></typeparam>
[IgnoreScanning]
public abstract class Entity<TKey, TDbContextLocator1, TDbContextLocator2, TDbContextLocator3, TDbContextLocator4, TDbContextLocator5, TDbContextLocator6> : PrivateEntity<TKey>
    where TDbContextLocator1 : class, IDbContextLocator
    where TDbContextLocator2 : class, IDbContextLocator
    where TDbContextLocator3 : class, IDbContextLocator
    where TDbContextLocator4 : class, IDbContextLocator
    where TDbContextLocator5 : class, IDbContextLocator
    where TDbContextLocator6 : class, IDbContextLocator
{
}

/// <summary>
/// <para>zh-cn:数据库实体依赖基类</para>
/// <para>en-us:Describes the behavior provided by this member.</para>
/// </summary>
/// <typeparam name="TKey"><para>zh-cn:主键类型</para><para>en-us:The generic parameter type.</para></typeparam>
/// <typeparam name="TDbContextLocator1"><para>zh-cn:数据库上下文定位器</para><para>en-us:The first database context locator type.</para></typeparam>
/// <typeparam name="TDbContextLocator2"><para>zh-cn:数据库上下文定位器</para><para>en-us:The second database context locator type.</para></typeparam>
/// <typeparam name="TDbContextLocator3"><para>zh-cn:数据库上下文定位器</para><para>en-us:The third database context locator type.</para></typeparam>
/// <typeparam name="TDbContextLocator4"><para>zh-cn:数据库上下文定位器</para><para>en-us:The fourth database context locator type.</para></typeparam>
/// <typeparam name="TDbContextLocator5"><para>zh-cn:数据库上下文定位器</para><para>en-us:The fifth database context locator type.</para></typeparam>
/// <typeparam name="TDbContextLocator6"><para>zh-cn:数据库上下文定位器</para><para>en-us:The sixth database context locator type.</para></typeparam>
/// <typeparam name="TDbContextLocator7"><para>zh-cn:数据库上下文定位器</para><para>en-us:The seventh database context locator type.</para></typeparam>
[IgnoreScanning]
public abstract class Entity<TKey, TDbContextLocator1, TDbContextLocator2, TDbContextLocator3, TDbContextLocator4, TDbContextLocator5, TDbContextLocator6, TDbContextLocator7> : PrivateEntity<TKey>
    where TDbContextLocator1 : class, IDbContextLocator
    where TDbContextLocator2 : class, IDbContextLocator
    where TDbContextLocator3 : class, IDbContextLocator
    where TDbContextLocator4 : class, IDbContextLocator
    where TDbContextLocator5 : class, IDbContextLocator
    where TDbContextLocator6 : class, IDbContextLocator
    where TDbContextLocator7 : class, IDbContextLocator
{
}

/// <summary>
/// <para>zh-cn:数据库实体依赖基类</para>
/// <para>en-us:Describes the behavior provided by this member.</para>
/// </summary>
/// <typeparam name="TKey"><para>zh-cn:主键类型</para><para>en-us:The generic parameter type.</para></typeparam>
/// <typeparam name="TDbContextLocator1"><para>zh-cn:数据库上下文定位器</para><para>en-us:The first database context locator type.</para></typeparam>
/// <typeparam name="TDbContextLocator2"><para>zh-cn:数据库上下文定位器</para><para>en-us:The second database context locator type.</para></typeparam>
/// <typeparam name="TDbContextLocator3"><para>zh-cn:数据库上下文定位器</para><para>en-us:The third database context locator type.</para></typeparam>
/// <typeparam name="TDbContextLocator4"><para>zh-cn:数据库上下文定位器</para><para>en-us:The fourth database context locator type.</para></typeparam>
/// <typeparam name="TDbContextLocator5"><para>zh-cn:数据库上下文定位器</para><para>en-us:The fifth database context locator type.</para></typeparam>
/// <typeparam name="TDbContextLocator6"><para>zh-cn:数据库上下文定位器</para><para>en-us:The sixth database context locator type.</para></typeparam>
/// <typeparam name="TDbContextLocator7"><para>zh-cn:数据库上下文定位器</para><para>en-us:The seventh database context locator type.</para></typeparam>
/// <typeparam name="TDbContextLocator8"><para>zh-cn:数据库上下文定位器</para><para>en-us:The eighth database context locator type.</para></typeparam>
[IgnoreScanning]
public abstract class Entity<TKey, TDbContextLocator1, TDbContextLocator2, TDbContextLocator3, TDbContextLocator4, TDbContextLocator5, TDbContextLocator6, TDbContextLocator7, TDbContextLocator8> : PrivateEntity<TKey>
    where TDbContextLocator1 : class, IDbContextLocator
    where TDbContextLocator2 : class, IDbContextLocator
    where TDbContextLocator3 : class, IDbContextLocator
    where TDbContextLocator4 : class, IDbContextLocator
    where TDbContextLocator5 : class, IDbContextLocator
    where TDbContextLocator6 : class, IDbContextLocator
    where TDbContextLocator7 : class, IDbContextLocator
    where TDbContextLocator8 : class, IDbContextLocator
{
}

/// <summary>
/// <para>zh-cn:数据库实体依赖基类（禁止外部继承）</para>
/// <para>en-us:Describes the behavior provided by this member.</para>
/// </summary>
/// <typeparam name="TKey"><para>zh-cn:主键类型</para><para>en-us:The generic parameter type.</para></typeparam>
[IgnoreScanning]
public abstract class PrivateEntity<TKey> : PrivateEntityBase<TKey>
{
    /// <summary>
    /// <para>zh-cn:创建时间</para>
    /// <para>en-us:Describes the behavior provided by this member.</para>
    /// </summary>
    public virtual DateTimeOffset CreatedTime { get; set; }

    /// <summary>
    /// <para>zh-cn:更新时间</para>
    /// <para>en-us:Describes the behavior provided by this member.</para>
    /// </summary>
    public virtual DateTimeOffset? UpdatedTime { get; set; }
}
