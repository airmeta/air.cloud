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

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Air.Cloud.EntityFrameWork.Core.Entities.Dependencies;

/// <summary>
/// <para>zh-cn:数据库实体依赖基类</para>
/// <para>en-us:Describes the behavior provided by this member.</para>
/// </summary>
[IgnoreScanning]
public abstract class EntityBase : EntityBase<int, MasterDbContextLocator>
{
}

/// <summary>
/// <para>zh-cn:数据库实体依赖基类</para>
/// <para>en-us:Describes the behavior provided by this member.</para>
/// </summary>
/// <typeparam name="TKey"><para>zh-cn:主键类型</para><para>en-us:The generic parameter type.</para></typeparam>
[IgnoreScanning]
public abstract class EntityBase<TKey> : EntityBase<TKey, MasterDbContextLocator>
{
}

/// <summary>
/// <para>zh-cn:数据库实体依赖基类</para>
/// <para>en-us:Describes the behavior provided by this member.</para>
/// </summary>
/// <typeparam name="TKey"><para>zh-cn:主键类型</para><para>en-us:The generic parameter type.</para></typeparam>
/// <typeparam name="TDbContextLocator1"><para>zh-cn:数据库上下文定位器</para><para>en-us:The first database context locator type.</para></typeparam>
[IgnoreScanning]
public abstract class EntityBase<TKey, TDbContextLocator1> : PrivateEntityBase<TKey>
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
public abstract class EntityBase<TKey, TDbContextLocator1, TDbContextLocator2> : PrivateEntityBase<TKey>
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
public abstract class EntityBase<TKey, TDbContextLocator1, TDbContextLocator2, TDbContextLocator3> : PrivateEntityBase<TKey>
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
public abstract class EntityBase<TKey, TDbContextLocator1, TDbContextLocator2, TDbContextLocator3, TDbContextLocator4> : PrivateEntityBase<TKey>
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
public abstract class EntityBase<TKey, TDbContextLocator1, TDbContextLocator2, TDbContextLocator3, TDbContextLocator4, TDbContextLocator5> : PrivateEntityBase<TKey>
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
public abstract class EntityBase<TKey, TDbContextLocator1, TDbContextLocator2, TDbContextLocator3, TDbContextLocator4, TDbContextLocator5, TDbContextLocator6> : PrivateEntityBase<TKey>
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
public abstract class EntityBase<TKey, TDbContextLocator1, TDbContextLocator2, TDbContextLocator3, TDbContextLocator4, TDbContextLocator5, TDbContextLocator6, TDbContextLocator7> : PrivateEntityBase<TKey>
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
public abstract class EntityBase<TKey, TDbContextLocator1, TDbContextLocator2, TDbContextLocator3, TDbContextLocator4, TDbContextLocator5, TDbContextLocator6, TDbContextLocator7, TDbContextLocator8> : PrivateEntityBase<TKey>
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
public abstract class PrivateEntityBase<TKey> : IPrivateEntity
{
    /// <summary>
    /// <para>zh-cn:主键Id</para>
    /// <para>en-us:Describes the behavior provided by this member.</para>
    /// </summary>
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public virtual TKey Id { get; set; } = default!;
}
