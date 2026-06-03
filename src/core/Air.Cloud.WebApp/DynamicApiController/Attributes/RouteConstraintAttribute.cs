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
/// 接口参数约束
/// </summary>
[IgnoreScanning, AttributeUsage(AttributeTargets.Parameter)]
public class RouteConstraintAttribute : Attribute
{
    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="constraint"></param>
    public RouteConstraintAttribute(string constraint)
    {
        Constraint = constraint;
    }

    /// <summary>
    /// 参数位置
    /// </summary>
    public string Constraint { get; set; }
}