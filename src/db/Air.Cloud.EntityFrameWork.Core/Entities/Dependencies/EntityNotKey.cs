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
/// <para>zh-cn:数据库无键实体依赖基接口</para>
/// <para>en-us:Describes the behavior provided by this member.</para>
/// </summary>
[IgnoreScanning]
public abstract class EntityNotKey : EntityNotKey<MasterDbContextLocator>
{
    /// <summary>
    /// <para>zh-cn:构造函数</para>
    /// <para>en-us:Describes the behavior provided by this member.</para>
    /// </summary>
    /// <param name="name"><para>zh-cn:数据库中定义名</para><para>en-us:The name value.</para></param>
    public EntityNotKey(string name) : base(name)
    {
    }
}

/// <summary>
/// <para>zh-cn:数据库无键实体依赖基接口</para>
/// <para>en-us:Describes the behavior provided by this member.</para>
/// </summary>
/// <typeparam name="TDbContextLocator1"><para>zh-cn:数据库上下文定位器</para><para>en-us:The first database context locator type.</para></typeparam>
[IgnoreScanning]
public abstract class EntityNotKey<TDbContextLocator1> : PrivateEntityNotKey
    where TDbContextLocator1 : class, IDbContextLocator
{
    /// <summary>
    /// <para>zh-cn:构造函数</para>
    /// <para>en-us:Describes the behavior provided by this member.</para>
    /// </summary>
    /// <param name="name"><para>zh-cn:数据库中定义名</para><para>en-us:The name value.</para></param>
    public EntityNotKey(string name) : base(name)
    {
    }
}

/// <summary>
/// <para>zh-cn:数据库无键实体依赖基接口</para>
/// <para>en-us:Describes the behavior provided by this member.</para>
/// </summary>
/// <typeparam name="TDbContextLocator1"><para>zh-cn:数据库上下文定位器</para><para>en-us:The first database context locator type.</para></typeparam>
/// <typeparam name="TDbContextLocator2"><para>zh-cn:数据库上下文定位器</para><para>en-us:The second database context locator type.</para></typeparam>
[IgnoreScanning]
public abstract class EntityNotKey<TDbContextLocator1, TDbContextLocator2> : PrivateEntityNotKey
    where TDbContextLocator2 : class, IDbContextLocator
{
    /// <summary>
    /// <para>zh-cn:构造函数</para>
    /// <para>en-us:Describes the behavior provided by this member.</para>
    /// </summary>
    /// <param name="name"><para>zh-cn:数据库中定义名</para><para>en-us:The name value.</para></param>
    public EntityNotKey(string name) : base(name)
    {
    }
}

/// <summary>
/// <para>zh-cn:数据库无键实体依赖基接口</para>
/// <para>en-us:Describes the behavior provided by this member.</para>
/// </summary>
/// <typeparam name="TDbContextLocator1"><para>zh-cn:数据库上下文定位器</para><para>en-us:The first database context locator type.</para></typeparam>
/// <typeparam name="TDbContextLocator2"><para>zh-cn:数据库上下文定位器</para><para>en-us:The second database context locator type.</para></typeparam>
/// <typeparam name="TDbContextLocator3"><para>zh-cn:数据库上下文定位器</para><para>en-us:The third database context locator type.</para></typeparam>
[IgnoreScanning]
public abstract class EntityNotKey<TDbContextLocator1, TDbContextLocator2, TDbContextLocator3> : PrivateEntityNotKey
    where TDbContextLocator1 : class, IDbContextLocator
    where TDbContextLocator2 : class, IDbContextLocator
    where TDbContextLocator3 : class, IDbContextLocator
{
    /// <summary>
    /// <para>zh-cn:构造函数</para>
    /// <para>en-us:Describes the behavior provided by this member.</para>
    /// </summary>
    /// <param name="name"><para>zh-cn:数据库中定义名</para><para>en-us:The name value.</para></param>
    public EntityNotKey(string name) : base(name)
    {
    }
}

/// <summary>
/// <para>zh-cn:数据库无键实体依赖基接口</para>
/// <para>en-us:Describes the behavior provided by this member.</para>
/// </summary>
/// <typeparam name="TDbContextLocator1"><para>zh-cn:数据库上下文定位器</para><para>en-us:The first database context locator type.</para></typeparam>
/// <typeparam name="TDbContextLocator2"><para>zh-cn:数据库上下文定位器</para><para>en-us:The second database context locator type.</para></typeparam>
/// <typeparam name="TDbContextLocator3"><para>zh-cn:数据库上下文定位器</para><para>en-us:The third database context locator type.</para></typeparam>
/// <typeparam name="TDbContextLocator4"><para>zh-cn:数据库上下文定位器</para><para>en-us:The fourth database context locator type.</para></typeparam>
[IgnoreScanning]
public abstract class EntityNotKey<TDbContextLocator1, TDbContextLocator2, TDbContextLocator3, TDbContextLocator4> : PrivateEntityNotKey
    where TDbContextLocator1 : class, IDbContextLocator
    where TDbContextLocator2 : class, IDbContextLocator
    where TDbContextLocator3 : class, IDbContextLocator
    where TDbContextLocator4 : class, IDbContextLocator
{
    /// <summary>
    /// <para>zh-cn:构造函数</para>
    /// <para>en-us:Describes the behavior provided by this member.</para>
    /// </summary>
    /// <param name="name"><para>zh-cn:数据库中定义名</para><para>en-us:The name value.</para></param>
    public EntityNotKey(string name) : base(name)
    {
    }
}

/// <summary>
/// <para>zh-cn:数据库无键实体依赖基接口</para>
/// <para>en-us:Describes the behavior provided by this member.</para>
/// </summary>
/// <typeparam name="TDbContextLocator1"><para>zh-cn:数据库上下文定位器</para><para>en-us:The first database context locator type.</para></typeparam>
/// <typeparam name="TDbContextLocator2"><para>zh-cn:数据库上下文定位器</para><para>en-us:The second database context locator type.</para></typeparam>
/// <typeparam name="TDbContextLocator3"><para>zh-cn:数据库上下文定位器</para><para>en-us:The third database context locator type.</para></typeparam>
/// <typeparam name="TDbContextLocator4"><para>zh-cn:数据库上下文定位器</para><para>en-us:The fourth database context locator type.</para></typeparam>
/// <typeparam name="TDbContextLocator5"><para>zh-cn:数据库上下文定位器</para><para>en-us:The fifth database context locator type.</para></typeparam>
[IgnoreScanning]
public abstract class EntityNotKey<TDbContextLocator1, TDbContextLocator2, TDbContextLocator3, TDbContextLocator4, TDbContextLocator5> : PrivateEntityNotKey
    where TDbContextLocator1 : class, IDbContextLocator
    where TDbContextLocator2 : class, IDbContextLocator
    where TDbContextLocator3 : class, IDbContextLocator
    where TDbContextLocator4 : class, IDbContextLocator
    where TDbContextLocator5 : class, IDbContextLocator
{
    /// <summary>
    /// <para>zh-cn:构造函数</para>
    /// <para>en-us:Describes the behavior provided by this member.</para>
    /// </summary>
    /// <param name="name"><para>zh-cn:数据库中定义名</para><para>en-us:The name value.</para></param>
    public EntityNotKey(string name) : base(name)
    {
    }
}

/// <summary>
/// <para>zh-cn:数据库无键实体依赖基接口</para>
/// <para>en-us:Describes the behavior provided by this member.</para>
/// </summary>
/// <typeparam name="TDbContextLocator1"><para>zh-cn:数据库上下文定位器</para><para>en-us:The first database context locator type.</para></typeparam>
/// <typeparam name="TDbContextLocator2"><para>zh-cn:数据库上下文定位器</para><para>en-us:The second database context locator type.</para></typeparam>
/// <typeparam name="TDbContextLocator3"><para>zh-cn:数据库上下文定位器</para><para>en-us:The third database context locator type.</para></typeparam>
/// <typeparam name="TDbContextLocator4"><para>zh-cn:数据库上下文定位器</para><para>en-us:The fourth database context locator type.</para></typeparam>
/// <typeparam name="TDbContextLocator5"><para>zh-cn:数据库上下文定位器</para><para>en-us:The fifth database context locator type.</para></typeparam>
/// <typeparam name="TDbContextLocator6"><para>zh-cn:数据库上下文定位器</para><para>en-us:The sixth database context locator type.</para></typeparam>
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
    /// <para>zh-cn:构造函数</para>
    /// <para>en-us:Describes the behavior provided by this member.</para>
    /// </summary>
    /// <param name="name"><para>zh-cn:数据库中定义名</para><para>en-us:The name value.</para></param>
    public EntityNotKey(string name) : base(name)
    {
    }
}

/// <summary>
/// <para>zh-cn:数据库无键实体依赖基接口</para>
/// <para>en-us:Describes the behavior provided by this member.</para>
/// </summary>
/// <typeparam name="TDbContextLocator1"><para>zh-cn:数据库上下文定位器</para><para>en-us:The first database context locator type.</para></typeparam>
/// <typeparam name="TDbContextLocator2"><para>zh-cn:数据库上下文定位器</para><para>en-us:The second database context locator type.</para></typeparam>
/// <typeparam name="TDbContextLocator3"><para>zh-cn:数据库上下文定位器</para><para>en-us:The third database context locator type.</para></typeparam>
/// <typeparam name="TDbContextLocator4"><para>zh-cn:数据库上下文定位器</para><para>en-us:The fourth database context locator type.</para></typeparam>
/// <typeparam name="TDbContextLocator5"><para>zh-cn:数据库上下文定位器</para><para>en-us:The fifth database context locator type.</para></typeparam>
/// <typeparam name="TDbContextLocator6"><para>zh-cn:数据库上下文定位器</para><para>en-us:The sixth database context locator type.</para></typeparam>
/// <typeparam name="TDbContextLocator7"><para>zh-cn:数据库上下文定位器</para><para>en-us:The seventh database context locator type.</para></typeparam>
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
    /// <para>zh-cn:构造函数</para>
    /// <para>en-us:Describes the behavior provided by this member.</para>
    /// </summary>
    /// <param name="name"><para>zh-cn:数据库中定义名</para><para>en-us:The name value.</para></param>
    public EntityNotKey(string name) : base(name)
    {
    }
}

/// <summary>
/// <para>zh-cn:数据库无键实体依赖基接口</para>
/// <para>en-us:Describes the behavior provided by this member.</para>
/// </summary>
/// <typeparam name="TDbContextLocator1"><para>zh-cn:数据库上下文定位器</para><para>en-us:The first database context locator type.</para></typeparam>
/// <typeparam name="TDbContextLocator2"><para>zh-cn:数据库上下文定位器</para><para>en-us:The second database context locator type.</para></typeparam>
/// <typeparam name="TDbContextLocator3"><para>zh-cn:数据库上下文定位器</para><para>en-us:The third database context locator type.</para></typeparam>
/// <typeparam name="TDbContextLocator4"><para>zh-cn:数据库上下文定位器</para><para>en-us:The fourth database context locator type.</para></typeparam>
/// <typeparam name="TDbContextLocator5"><para>zh-cn:数据库上下文定位器</para><para>en-us:The fifth database context locator type.</para></typeparam>
/// <typeparam name="TDbContextLocator6"><para>zh-cn:数据库上下文定位器</para><para>en-us:The sixth database context locator type.</para></typeparam>
/// <typeparam name="TDbContextLocator7"><para>zh-cn:数据库上下文定位器</para><para>en-us:The seventh database context locator type.</para></typeparam>
/// <typeparam name="TDbContextLocator8"><para>zh-cn:数据库上下文定位器</para><para>en-us:The eighth database context locator type.</para></typeparam>
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
    /// <para>zh-cn:构造函数</para>
    /// <para>en-us:Describes the behavior provided by this member.</para>
    /// </summary>
    /// <param name="name"><para>zh-cn:数据库中定义名</para><para>en-us:The name value.</para></param>
    public EntityNotKey(string name) : base(name)
    {
    }
}

/// <summary>
/// <para>zh-cn:数据库无键实体基类（禁止外部继承）</para>
/// <para>en-us:Describes the behavior provided by this member.</para>
/// </summary>
[IgnoreScanning]
public abstract class PrivateEntityNotKey : IPrivateEntityNotKey
{
    /// <summary>
    /// <para>zh-cn:无键实体名</para>
    /// <para>en-us:Describes the behavior provided by this member.</para>
    /// </summary>
    private readonly string _name;

    /// <summary>
    /// <para>zh-cn:无键实体 schema</para>
    /// <para>en-us:Describes the behavior provided by this member.</para>
    /// </summary>
    private readonly string _schema;

    /// <summary>
    /// <para>zh-cn:构造函数</para>
    /// <para>en-us:Describes the behavior provided by this member.</para>
    /// </summary>
    /// <param name="name"><para>zh-cn:数据库中定义名</para><para>en-us:The name value.</para></param>
    /// <param name="schema"><para>zh-cn:参数值。</para><para>en-us:The schema value.</para></param>
    public PrivateEntityNotKey(string name, string schema = default)
    {
        _name = name;
        _schema = schema;
    }

    /// <summary>
    /// <para>zh-cn:获取视图名称</para>
    /// <para>en-us:Describes the behavior provided by this member.</para>
    /// </summary>
    /// <returns><para>zh-cn:操作结果。</para><para>en-us:The operation result.</para></returns>
    public string GetName()
    {
        return _name;
    }

    /// <summary>
    /// <para>zh-cn:数据库中定义的 Schema</para>
    /// <para>en-us:Describes the behavior provided by this member.</para>
    /// </summary>
    /// <returns><para>zh-cn:操作结果。</para><para>en-us:The operation result.</para></returns>
    /// <exception cref="NotImplementedException"><para>zh-cn:参数无效或当前操作状态不允许时抛出。</para><para>en-us:Thrown when an argument is invalid or the current operation state is not allowed.</para></exception>
    public string GetSchema()
    {
        return _schema;
    }
}
