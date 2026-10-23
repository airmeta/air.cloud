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

namespace Air.Cloud.EntityFrameWork.Core.Entities.Attributes;

/// <summary>
/// <para>zh-cn:将实体方法映射为可查询数据库函数的配置特性。</para>
/// <para>en-us:Attribute that maps an entity method to a queryable database function.</para>
/// </summary>
[IgnoreScanning, AttributeUsage(AttributeTargets.Method)]
public class QueryableFunctionAttribute : DbFunctionAttribute
{
    /// <summary>
    /// <para>zh-cn:创建指定函数名和架构名的数据库函数特性。</para>
    /// <para>en-us:Creates the database-function attribute with the specified name and schema.</para>
    /// </summary>
    /// <param name="name"><para>zh-cn:函数名</para><para>en-us:The name value.</para></param>
    /// <param name="schema"><para>zh-cn:架构名</para><para>en-us:The schema value.</para></param>
    public QueryableFunctionAttribute(string name, string schema = null) : base(name, schema)
    {
        DbContextLocators = Array.Empty<Type>();
    }

    /// <summary>
    /// <para>zh-cn:创建指定函数名、架构名和上下文定位器的数据库函数特性。</para>
    /// <para>en-us:Creates the database-function attribute with the specified name, schema, and context locators.</para>
    /// </summary>
    /// <param name="name"><para>zh-cn:函数名</para><para>en-us:The name value.</para></param>
    /// <param name="schema"><para>zh-cn:架构名</para><para>en-us:The schema value.</para></param>
    /// <param name="dbContextLocators"><para>zh-cn:数据库上下文定位器</para><para>en-us:The dbContextLocators value.</para></param>
    public QueryableFunctionAttribute(string name, string schema = null, params Type[] dbContextLocators) : base(name, schema)
    {
        DbContextLocators = dbContextLocators ?? Array.Empty<Type>();
    }

    /// <summary>
    /// <para>zh-cn:限制该函数可用的数据库上下文定位器类型。</para>
    /// <para>en-us:Limits the database-context locator types where this function is available.</para>
    /// </summary>
    public Type[] DbContextLocators { get; set; }
}
