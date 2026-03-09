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

using Air.Cloud.EntityFrameWork.Core.Extensions.Linq;

using Microsoft.EntityFrameworkCore.Query;

using System.Linq.Expressions;

namespace Air.Cloud.EntityFrameWork.Core.Extensions.Linq;

/// <summary>
/// EntityFramework Core 拓展
/// </summary>
[IgnoreScanning]
public static class EFCoreExtensions
{
    /// <summary>
    /// 根据条件成立再构建 Include 查询
    /// </summary>
    /// <typeparam name="TSource">泛型类型</typeparam>
    /// <typeparam name="TProperty">泛型属性类型</typeparam>
    /// <param name="sources">集合对象</param>
    /// <param name="condition">布尔条件</param>
    /// <param name="expression">新的集合对象表达式</param>
    /// <returns></returns>
    public static IIncludableQueryable<TSource, TProperty> Include<TSource, TProperty>(this IQueryable<TSource> sources, bool condition, Expression<Func<TSource, TProperty>> expression) where TSource : class
    {
        return condition ? sources.Include(expression) : (IIncludableQueryable<TSource, TProperty>)sources;
    }
}