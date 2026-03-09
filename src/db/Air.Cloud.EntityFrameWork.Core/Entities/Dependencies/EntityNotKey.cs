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

namespace Air.Cloud.EntityFrameWork.Core.Entities.Dependencies;

/// <summary>
/// 数据库无键实体依赖基接口
/// </summary>
[IgnoreScanning]
public abstract class EntityNotKey : EntityNotKey<MasterDbContextLocator>
{
    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="name">数据库中定义名</param>
    public EntityNotKey(string name) : base(name)
    {
    }
}

/// <summary>
/// 数据库无键实体依赖基接口
/// </summary>
/// <typeparam name="TDbContextLocator1">数据库上下文定位器</typeparam>
[IgnoreScanning]
public abstract class EntityNotKey<TDbContextLocator1> : PrivateEntityNotKey
    where TDbContextLocator1 : class, IDbContextLocator
{
    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="name">数据库中定义名</param>
    public EntityNotKey(string name) : base(name)
    {
    }
}

/// <summary>
/// 数据库无键实体依赖基接口
/// </summary>
/// <typeparam name="TDbContextLocator1">数据库上下文定位器</typeparam>
/// <typeparam name="TDbContextLocator2">数据库上下文定位器</typeparam>
[IgnoreScanning]
public abstract class EntityNotKey<TDbContextLocator1, TDbContextLocator2> : PrivateEntityNotKey
    where TDbContextLocator2 : class, IDbContextLocator
{
    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="name">数据库中定义名</param>
    public EntityNotKey(string name) : base(name)
    {
    }
}

/// <summary>
/// 数据库无键实体依赖基接口
/// </summary>
/// <typeparam name="TDbContextLocator1">数据库上下文定位器</typeparam>
/// <typeparam name="TDbContextLocator2">数据库上下文定位器</typeparam>
/// <typeparam name="TDbContextLocator3">数据库上下文定位器</typeparam>
[IgnoreScanning]
public abstract class EntityNotKey<TDbContextLocator1, TDbContextLocator2, TDbContextLocator3> : PrivateEntityNotKey
    where TDbContextLocator1 : class, IDbContextLocator
    where TDbContextLocator2 : class, IDbContextLocator
    where TDbContextLocator3 : class, IDbContextLocator
{
    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="name">数据库中定义名</param>
    public EntityNotKey(string name) : base(name)
    {
    }
}

/// <summary>
/// 数据库无键实体依赖基接口
/// </summary>
/// <typeparam name="TDbContextLocator1">数据库上下文定位器</typeparam>
/// <typeparam name="TDbContextLocator2">数据库上下文定位器</typeparam>
/// <typeparam name="TDbContextLocator3">数据库上下文定位器</typeparam>
/// <typeparam name="TDbContextLocator4">数据库上下文定位器</typeparam>
[IgnoreScanning]
public abstract class EntityNotKey<TDbContextLocator1, TDbContextLocator2, TDbContextLocator3, TDbContextLocator4> : PrivateEntityNotKey
    where TDbContextLocator1 : class, IDbContextLocator
    where TDbContextLocator2 : class, IDbContextLocator
    where TDbContextLocator3 : class, IDbContextLocator
    where TDbContextLocator4 : class, IDbContextLocator
{
    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="name">数据库中定义名</param>
    public EntityNotKey(string name) : base(name)
    {
    }
}

/// <summary>
/// 数据库无键实体依赖基接口
/// </summary>
/// <typeparam name="TDbContextLocator1">数据库上下文定位器</typeparam>
/// <typeparam name="TDbContextLocator2">数据库上下文定位器</typeparam>
/// <typeparam name="TDbContextLocator3">数据库上下文定位器</typeparam>
/// <typeparam name="TDbContextLocator4">数据库上下文定位器</typeparam>
/// <typeparam name="TDbContextLocator5">数据库上下文定位器</typeparam>
[IgnoreScanning]
public abstract class EntityNotKey<TDbContextLocator1, TDbContextLocator2, TDbContextLocator3, TDbContextLocator4, TDbContextLocator5> : PrivateEntityNotKey
    where TDbContextLocator1 : class, IDbContextLocator
    where TDbContextLocator2 : class, IDbContextLocator
    where TDbContextLocator3 : class, IDbContextLocator
    where TDbContextLocator4 : class, IDbContextLocator
    where TDbContextLocator5 : class, IDbContextLocator
{
    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="name">数据库中定义名</param>
    public EntityNotKey(string name) : base(name)
    {
    }
}

/// <summary>
/// 数据库无键实体依赖基接口
/// </summary>
/// <typeparam name="TDbContextLocator1">数据库上下文定位器</typeparam>
/// <typeparam name="TDbContextLocator2">数据库上下文定位器</typeparam>
/// <typeparam name="TDbContextLocator3">数据库上下文定位器</typeparam>
/// <typeparam name="TDbContextLocator4">数据库上下文定位器</typeparam>
/// <typeparam name="TDbContextLocator5">数据库上下文定位器</typeparam>
/// <typeparam name="TDbContextLocator6">数据库上下文定位器</typeparam>
[IgnoreScanning]
public abstract class EntityNotKey<TDbContextLocator1, TDbContextLocator2, TDbContextLocator3, TDbContextLocator4, TDbContextLocator5, TDbContextLocator6> : PrivateEntityNotKey
    where TDbContextLocator1 : class, IDbContextLocator
    where TDbContextLocator2 : class, IDbContextLocator
    where TDbContextLocator3 : class, IDbContextLocator
    where TDbContextLocator4 : class, IDbContextLocator
    where TDbContextLocator5 : class, IDbContextLocator
    where TDbContextLocator6 : class, IDbContextLocator
{
    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="name">数据库中定义名</param>
    public EntityNotKey(string name) : base(name)
    {
    }
}

/// <summary>
/// 数据库无键实体依赖基接口
/// </summary>
/// <typeparam name="TDbContextLocator1">数据库上下文定位器</typeparam>
/// <typeparam name="TDbContextLocator2">数据库上下文定位器</typeparam>
/// <typeparam name="TDbContextLocator3">数据库上下文定位器</typeparam>
/// <typeparam name="TDbContextLocator4">数据库上下文定位器</typeparam>
/// <typeparam name="TDbContextLocator5">数据库上下文定位器</typeparam>
/// <typeparam name="TDbContextLocator6">数据库上下文定位器</typeparam>
/// <typeparam name="TDbContextLocator7">数据库上下文定位器</typeparam>
[IgnoreScanning]
public abstract class EntityNotKey<TDbContextLocator1, TDbContextLocator2, TDbContextLocator3, TDbContextLocator4, TDbContextLocator5, TDbContextLocator6, TDbContextLocator7> : PrivateEntityNotKey
    where TDbContextLocator1 : class, IDbContextLocator
    where TDbContextLocator2 : class, IDbContextLocator
    where TDbContextLocator3 : class, IDbContextLocator
    where TDbContextLocator4 : class, IDbContextLocator
    where TDbContextLocator5 : class, IDbContextLocator
    where TDbContextLocator6 : class, IDbContextLocator
    where TDbContextLocator7 : class, IDbContextLocator
{
    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="name">数据库中定义名</param>
    public EntityNotKey(string name) : base(name)
    {
    }
}

/// <summary>
/// 数据库无键实体依赖基接口
/// </summary>
/// <typeparam name="TDbContextLocator1">数据库上下文定位器</typeparam>
/// <typeparam name="TDbContextLocator2">数据库上下文定位器</typeparam>
/// <typeparam name="TDbContextLocator3">数据库上下文定位器</typeparam>
/// <typeparam name="TDbContextLocator4">数据库上下文定位器</typeparam>
/// <typeparam name="TDbContextLocator5">数据库上下文定位器</typeparam>
/// <typeparam name="TDbContextLocator6">数据库上下文定位器</typeparam>
/// <typeparam name="TDbContextLocator7">数据库上下文定位器</typeparam>
/// <typeparam name="TDbContextLocator8">数据库上下文定位器</typeparam>
[IgnoreScanning]
public abstract class EntityNotKey<TDbContextLocator1, TDbContextLocator2, TDbContextLocator3, TDbContextLocator4, TDbContextLocator5, TDbContextLocator6, TDbContextLocator7, TDbContextLocator8> : PrivateEntityNotKey
    where TDbContextLocator1 : class, IDbContextLocator
    where TDbContextLocator2 : class, IDbContextLocator
    where TDbContextLocator3 : class, IDbContextLocator
    where TDbContextLocator4 : class, IDbContextLocator
    where TDbContextLocator5 : class, IDbContextLocator
    where TDbContextLocator6 : class, IDbContextLocator
    where TDbContextLocator7 : class, IDbContextLocator
    where TDbContextLocator8 : class, IDbContextLocator
{
    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="name">数据库中定义名</param>
    public EntityNotKey(string name) : base(name)
    {
    }
}

/// <summary>
/// 数据库无键实体基类（禁止外部继承）
/// </summary>
[IgnoreScanning]
public abstract class PrivateEntityNotKey : IPrivateEntityNotKey
{
    /// <summary>
    /// 无键实体名
    /// </summary>
    private readonly string _name;

    /// <summary>
    /// 无键实体 schema
    /// </summary>
    private readonly string _schema;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="name">数据库中定义名</param>
    /// <param name="schema"></param>
    public PrivateEntityNotKey(string name, string schema = default)
    {
        _name = name;
        _schema = schema;
    }

    /// <summary>
    /// 获取视图名称
    /// </summary>
    /// <returns></returns>
    public string GetName()
    {
        return _name;
    }

    /// <summary>
    ///  数据库中定义的 Schema
    /// </summary>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public string GetSchema()
    {
        return _schema;
    }
}