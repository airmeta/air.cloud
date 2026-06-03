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

namespace Air.Cloud.EntityFrameWork.Core.Internal;

/// <summary>
/// 构建 Sql 字符串执行部件
/// </summary>
public sealed partial class SqlExecutePart
{
    /// <summary>
    /// 设置 Sql 字符串
    /// </summary>
    /// <param name="sql"></param>
    /// <returns></returns>
    public SqlExecutePart SetSqlString(string sql)
    {
        if (!string.IsNullOrWhiteSpace(sql)) SqlString = sql;
        return this;
    }

    /// <summary>
    /// 设置 ADO.NET 超时时间
    /// </summary>
    /// <param name="timeout">单位秒</param>
    /// <returns></returns>
    public SqlExecutePart SetCommandTimeout(int timeout)
    {
        if (timeout > 0) Timeout = timeout;
        return this;
    }

    /// <summary>
    /// 设置数据库执行作用域
    /// </summary>
    /// <param name="serviceProvider"></param>
    /// <returns></returns>
    public SqlExecutePart SetContextScoped(IServiceProvider serviceProvider)
    {
        if (serviceProvider != null) ContextScoped = serviceProvider;
        return this;
    }

    /// <summary>
    /// 设置数据库上下文定位器
    /// </summary>
    /// <typeparam name="TDbContextLocator"></typeparam>
    /// <returns></returns>
    public SqlExecutePart Change<TDbContextLocator>()
        where TDbContextLocator : class, IDbContextLocator
    {
        return Change(typeof(TDbContextLocator));
    }

    /// <summary>
    /// 设置数据库上下文定位器
    /// </summary>
    /// <returns></returns>
    public SqlExecutePart Change(Type dbContextLocator)
    {
        if (dbContextLocator != null) DbContextLocator = dbContextLocator;
        return this;
    }
}