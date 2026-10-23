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
/// <para>zh-cn:Entity Framework Core 查询扩展。</para>
/// <para>en-us:Entity Framework Core query extensions.</para>
/// </summary>
[IgnoreScanning]
public static class EFCoreExtensions
{
    /// <summary>
    /// <para>zh-cn:仅当条件为 true 时向查询添加 Include；否则返回原查询。</para>
    /// <para>en-us:Adds Include only when condition is true; otherwise returns the original query.</para>
    /// </summary>
    /// <typeparam name="TSource"><para>zh-cn:泛型类型</para><para>en-us:The generic parameter type.</para></typeparam>
    /// <typeparam name="TProperty"><para>zh-cn:泛型属性类型</para><para>en-us:The generic parameter type.</para></typeparam>
    /// <param name="sources"><para>zh-cn:要扩展的实体查询。</para><para>en-us:The entity query to extend.</para></param>
    /// <param name="condition"><para>zh-cn:是否应用 Include。</para><para>en-us:Whether Include should be applied.</para></param>
    /// <param name="expression"><para>zh-cn:要加载的导航属性表达式。</para><para>en-us:Expression for the navigation property to load.</para></param>
    /// <returns><para>zh-cn:添加 Include 后的查询，或条件不满足时的原查询。</para><para>en-us:The query with Include applied, or the original query when the condition is false.</para></returns>
    public static IIncludableQueryable<TSource, TProperty> Include<TSource, TProperty>(this IQueryable<TSource> sources, bool condition, Expression<Func<TSource, TProperty>> expression) where TSource : class
    {
        return condition ? sources.Include(expression) : (IIncludableQueryable<TSource, TProperty>)sources;
    }
}
