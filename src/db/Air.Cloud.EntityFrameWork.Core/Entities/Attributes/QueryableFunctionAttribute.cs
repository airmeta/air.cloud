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
/// 实体函数配置特性
/// </summary>
[IgnoreScanning, AttributeUsage(AttributeTargets.Method)]
public class QueryableFunctionAttribute : DbFunctionAttribute
{
    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="name">函数名</param>
    /// <param name="schema">架构名</param>
    public QueryableFunctionAttribute(string name, string schema = null) : base(name, schema)
    {
        DbContextLocators = Array.Empty<Type>();
    }

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="name">函数名</param>
    /// <param name="schema">架构名</param>
    /// <param name="dbContextLocators">数据库上下文定位器</param>
    public QueryableFunctionAttribute(string name, string schema = null, params Type[] dbContextLocators) : base(name, schema)
    {
        DbContextLocators = dbContextLocators ?? Array.Empty<Type>();
    }

    /// <summary>
    /// 数据库上下文定位器
    /// </summary>
    public Type[] DbContextLocators { get; set; }
}