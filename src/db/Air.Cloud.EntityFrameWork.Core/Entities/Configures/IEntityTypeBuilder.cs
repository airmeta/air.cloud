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

using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Air.Cloud.EntityFrameWork.Core.Entities.Configures;

/// <summary>
/// 数据库实体类型配置依赖接口
/// <para>zh-cn:该成员的数据库访问行为。</para>
/// <para>en-us:Database access behavior provided by this member.</para>
/// </summary>
/// <typeparam name="TEntity"><para>zh-cn:实体类型</para><para>en-us:The generic parameter type.</para></typeparam>
public interface IEntityTypeBuilder<TEntity> : IEntityTypeBuilder<TEntity, MasterDbContextLocator>
    where TEntity : class, IPrivateEntity, new()
{
}

/// <summary>
/// 数据库实体类型配置依赖接口
/// <para>zh-cn:该成员的数据库访问行为。</para>
/// <para>en-us:Database access behavior provided by this member.</para>
/// </summary>
/// <typeparam name="TEntity"><para>zh-cn:实体类型</para><para>en-us:The generic parameter type.</para></typeparam>
/// <typeparam name="TDbContextLocator1"><para>zh-cn:数据库上下文定位器</para><para>en-us:The generic parameter type.</para></typeparam>
public interface IEntityTypeBuilder<TEntity, TDbContextLocator1> : IPrivateEntityTypeBuilder<TEntity>
    where TEntity : class, IPrivateEntity, new()
    where TDbContextLocator1 : class, IDbContextLocator
{
}

/// <summary>
/// 数据库实体类型配置依赖接口
/// <para>zh-cn:该成员的数据库访问行为。</para>
/// <para>en-us:Database access behavior provided by this member.</para>
/// </summary>
/// <typeparam name="TEntity"><para>zh-cn:实体类型</para><para>en-us:The generic parameter type.</para></typeparam>
/// <typeparam name="TDbContextLocator1"><para>zh-cn:数据库上下文定位器</para><para>en-us:The generic parameter type.</para></typeparam>
/// <typeparam name="TDbContextLocator2"><para>zh-cn:数据库上下文定位器</para><para>en-us:The generic parameter type.</para></typeparam>
public interface IEntityTypeBuilder<TEntity, TDbContextLocator1, TDbContextLocator2> : IPrivateEntityTypeBuilder<TEntity>
    where TEntity : class, IPrivateEntity, new()
    where TDbContextLocator1 : class, IDbContextLocator
    where TDbContextLocator2 : class, IDbContextLocator
{
}

/// <summary>
/// 数据库实体类型配置依赖接口
/// <para>zh-cn:该成员的数据库访问行为。</para>
/// <para>en-us:Database access behavior provided by this member.</para>
/// </summary>
/// <typeparam name="TEntity"><para>zh-cn:实体类型</para><para>en-us:The generic parameter type.</para></typeparam>
/// <typeparam name="TDbContextLocator1"><para>zh-cn:数据库上下文定位器</para><para>en-us:The generic parameter type.</para></typeparam>
/// <typeparam name="TDbContextLocator2"><para>zh-cn:数据库上下文定位器</para><para>en-us:The generic parameter type.</para></typeparam>
/// <typeparam name="TDbContextLocator3"><para>zh-cn:数据库上下文定位器</para><para>en-us:The generic parameter type.</para></typeparam>
public interface IEntityTypeBuilder<TEntity, TDbContextLocator1, TDbContextLocator2, TDbContextLocator3> : IPrivateEntityTypeBuilder<TEntity>
    where TEntity : class, IPrivateEntity, new()
    where TDbContextLocator1 : class, IDbContextLocator
    where TDbContextLocator2 : class, IDbContextLocator
    where TDbContextLocator3 : class, IDbContextLocator
{
}

/// <summary>
/// 数据库实体类型配置依赖接口
/// <para>zh-cn:该成员的数据库访问行为。</para>
/// <para>en-us:Database access behavior provided by this member.</para>
/// </summary>
/// <typeparam name="TEntity"><para>zh-cn:实体类型</para><para>en-us:The generic parameter type.</para></typeparam>
/// <typeparam name="TDbContextLocator1"><para>zh-cn:数据库上下文定位器</para><para>en-us:The generic parameter type.</para></typeparam>
/// <typeparam name="TDbContextLocator2"><para>zh-cn:数据库上下文定位器</para><para>en-us:The generic parameter type.</para></typeparam>
/// <typeparam name="TDbContextLocator3"><para>zh-cn:数据库上下文定位器</para><para>en-us:The generic parameter type.</para></typeparam>
/// <typeparam name="TDbContextLocator4"><para>zh-cn:数据库上下文定位器</para><para>en-us:The generic parameter type.</para></typeparam>
public interface IEntityTypeBuilder<TEntity, TDbContextLocator1, TDbContextLocator2, TDbContextLocator3, TDbContextLocator4> : IPrivateEntityTypeBuilder<TEntity>
    where TEntity : class, IPrivateEntity, new()
    where TDbContextLocator1 : class, IDbContextLocator
    where TDbContextLocator2 : class, IDbContextLocator
    where TDbContextLocator3 : class, IDbContextLocator
    where TDbContextLocator4 : class, IDbContextLocator
{
}

/// <summary>
/// 数据库实体类型配置依赖接口
/// <para>zh-cn:该成员的数据库访问行为。</para>
/// <para>en-us:Database access behavior provided by this member.</para>
/// </summary>
/// <typeparam name="TEntity"><para>zh-cn:实体类型</para><para>en-us:The generic parameter type.</para></typeparam>
/// <typeparam name="TDbContextLocator1"><para>zh-cn:数据库上下文定位器</para><para>en-us:The generic parameter type.</para></typeparam>
/// <typeparam name="TDbContextLocator2"><para>zh-cn:数据库上下文定位器</para><para>en-us:The generic parameter type.</para></typeparam>
/// <typeparam name="TDbContextLocator3"><para>zh-cn:数据库上下文定位器</para><para>en-us:The generic parameter type.</para></typeparam>
/// <typeparam name="TDbContextLocator4"><para>zh-cn:数据库上下文定位器</para><para>en-us:The generic parameter type.</para></typeparam>
/// <typeparam name="TDbContextLocator5"><para>zh-cn:数据库上下文定位器</para><para>en-us:The generic parameter type.</para></typeparam>
public interface IEntityTypeBuilder<TEntity, TDbContextLocator1, TDbContextLocator2, TDbContextLocator3, TDbContextLocator4, TDbContextLocator5> : IPrivateEntityTypeBuilder<TEntity>
    where TEntity : class, IPrivateEntity, new()
    where TDbContextLocator1 : class, IDbContextLocator
    where TDbContextLocator2 : class, IDbContextLocator
    where TDbContextLocator3 : class, IDbContextLocator
    where TDbContextLocator4 : class, IDbContextLocator
    where TDbContextLocator5 : class, IDbContextLocator
{
}

/// <summary>
/// 数据库实体类型配置依赖接口
/// <para>zh-cn:该成员的数据库访问行为。</para>
/// <para>en-us:Database access behavior provided by this member.</para>
/// </summary>
/// <typeparam name="TEntity"><para>zh-cn:实体类型</para><para>en-us:The generic parameter type.</para></typeparam>
/// <typeparam name="TDbContextLocator1"><para>zh-cn:数据库上下文定位器</para><para>en-us:The generic parameter type.</para></typeparam>
/// <typeparam name="TDbContextLocator2"><para>zh-cn:数据库上下文定位器</para><para>en-us:The generic parameter type.</para></typeparam>
/// <typeparam name="TDbContextLocator3"><para>zh-cn:数据库上下文定位器</para><para>en-us:The generic parameter type.</para></typeparam>
/// <typeparam name="TDbContextLocator4"><para>zh-cn:数据库上下文定位器</para><para>en-us:The generic parameter type.</para></typeparam>
/// <typeparam name="TDbContextLocator5"><para>zh-cn:数据库上下文定位器</para><para>en-us:The generic parameter type.</para></typeparam>
/// <typeparam name="TDbContextLocator6"><para>zh-cn:数据库上下文定位器</para><para>en-us:The generic parameter type.</para></typeparam>
public interface IEntityTypeBuilder<TEntity, TDbContextLocator1, TDbContextLocator2, TDbContextLocator3, TDbContextLocator4, TDbContextLocator5, TDbContextLocator6> : IPrivateEntityTypeBuilder<TEntity>
    where TEntity : class, IPrivateEntity, new()
    where TDbContextLocator1 : class, IDbContextLocator
    where TDbContextLocator2 : class, IDbContextLocator
    where TDbContextLocator3 : class, IDbContextLocator
    where TDbContextLocator4 : class, IDbContextLocator
    where TDbContextLocator5 : class, IDbContextLocator
    where TDbContextLocator6 : class, IDbContextLocator
{
}

/// <summary>
/// 数据库实体类型配置依赖接口
/// <para>zh-cn:该成员的数据库访问行为。</para>
/// <para>en-us:Database access behavior provided by this member.</para>
/// </summary>
/// <typeparam name="TEntity"><para>zh-cn:实体类型</para><para>en-us:The generic parameter type.</para></typeparam>
/// <typeparam name="TDbContextLocator1"><para>zh-cn:数据库上下文定位器</para><para>en-us:The generic parameter type.</para></typeparam>
/// <typeparam name="TDbContextLocator2"><para>zh-cn:数据库上下文定位器</para><para>en-us:The generic parameter type.</para></typeparam>
/// <typeparam name="TDbContextLocator3"><para>zh-cn:数据库上下文定位器</para><para>en-us:The generic parameter type.</para></typeparam>
/// <typeparam name="TDbContextLocator4"><para>zh-cn:数据库上下文定位器</para><para>en-us:The generic parameter type.</para></typeparam>
/// <typeparam name="TDbContextLocator5"><para>zh-cn:数据库上下文定位器</para><para>en-us:The generic parameter type.</para></typeparam>
/// <typeparam name="TDbContextLocator6"><para>zh-cn:数据库上下文定位器</para><para>en-us:The generic parameter type.</para></typeparam>
/// <typeparam name="TDbContextLocator7"><para>zh-cn:数据库上下文定位器</para><para>en-us:The generic parameter type.</para></typeparam>
public interface IEntityTypeBuilder<TEntity, TDbContextLocator1, TDbContextLocator2, TDbContextLocator3, TDbContextLocator4, TDbContextLocator5, TDbContextLocator6, TDbContextLocator7> : IPrivateEntityTypeBuilder<TEntity>
    where TEntity : class, IPrivateEntity, new()
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
/// 数据库实体类型配置依赖接口
/// <para>zh-cn:该成员的数据库访问行为。</para>
/// <para>en-us:Database access behavior provided by this member.</para>
/// </summary>
/// <typeparam name="TEntity"><para>zh-cn:实体类型</para><para>en-us:The generic parameter type.</para></typeparam>
/// <typeparam name="TDbContextLocator1"><para>zh-cn:数据库上下文定位器</para><para>en-us:The generic parameter type.</para></typeparam>
/// <typeparam name="TDbContextLocator2"><para>zh-cn:数据库上下文定位器</para><para>en-us:The generic parameter type.</para></typeparam>
/// <typeparam name="TDbContextLocator3"><para>zh-cn:数据库上下文定位器</para><para>en-us:The generic parameter type.</para></typeparam>
/// <typeparam name="TDbContextLocator4"><para>zh-cn:数据库上下文定位器</para><para>en-us:The generic parameter type.</para></typeparam>
/// <typeparam name="TDbContextLocator5"><para>zh-cn:数据库上下文定位器</para><para>en-us:The generic parameter type.</para></typeparam>
/// <typeparam name="TDbContextLocator6"><para>zh-cn:数据库上下文定位器</para><para>en-us:The generic parameter type.</para></typeparam>
/// <typeparam name="TDbContextLocator7"><para>zh-cn:数据库上下文定位器</para><para>en-us:The generic parameter type.</para></typeparam>
/// <typeparam name="TDbContextLocator8"><para>zh-cn:数据库上下文定位器</para><para>en-us:The generic parameter type.</para></typeparam>
public interface IEntityTypeBuilder<TEntity, TDbContextLocator1, TDbContextLocator2, TDbContextLocator3, TDbContextLocator4, TDbContextLocator5, TDbContextLocator6, TDbContextLocator7, TDbContextLocator8> : IPrivateEntityTypeBuilder<TEntity>
    where TEntity : class, IPrivateEntity, new()
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
/// 数据库实体类型配置依赖接口（禁止外部继承）
/// <para>zh-cn:该成员的数据库访问行为。</para>
/// <para>en-us:Database access behavior provided by this member.</para>
/// </summary>
/// <typeparam name="TEntity"><para>zh-cn:泛型参数类型</para><para>en-us:The entity type.</para></typeparam>
public interface IPrivateEntityTypeBuilder<TEntity> : IPrivateModelBuilder
    where TEntity : class, IPrivateEntity, new()
{
    /// <summary>
    /// 实体类型配置
    /// <para>zh-cn:该成员的数据库访问行为。</para>
    /// <para>en-us:Database access behavior provided by this member.</para>
    /// </summary>
    /// <param name="entityBuilder"><para>zh-cn:实体类型构建器</para><para>en-us:The entityBuilder value.</para></param>
    /// <param name="dbContext"><para>zh-cn:数据库上下文</para><para>en-us:The dbContext value.</para></param>
    /// <param name="dbContextLocator"><para>zh-cn:数据库上下文定位器</para><para>en-us:The dbContextLocator value.</para></param>
    void Configure(EntityTypeBuilder<TEntity> entityBuilder, DbContext dbContext, Type dbContextLocator);
}
