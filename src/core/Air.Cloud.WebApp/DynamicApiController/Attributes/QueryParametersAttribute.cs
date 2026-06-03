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
namespace Air.Cloud.WebApp.DynamicApiController.Attributes;

/// <summary>
/// 将 Action 所有参数 [FromQuery] 化
/// </summary>
[IgnoreScanning, AttributeUsage(AttributeTargets.Method)]
public sealed class QueryParametersAttribute : Attribute
{
    /// <summary>
    /// 默认构造函数
    /// </summary>
    public QueryParametersAttribute()
    {
    }
}