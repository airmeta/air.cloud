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

using System.ComponentModel;

namespace Air.Cloud.Core.Dependencies.Enums;

/// <summary>
/// 注册范围
/// </summary>
[IgnoreScanning]
public enum InjectionPatterns
{
    /// <summary>
    /// 只注册自己
    /// </summary>
    [Description("只注册自己")]
    Self,

    /// <summary>
    /// 第一个接口
    /// </summary>
    [Description("只注册第一个接口")]
    FirstInterface,

    /// <summary>
    /// 自己和第一个接口，默认值
    /// </summary>
    [Description("自己和第一个接口")]
    SelfWithFirstInterface,

    /// <summary>
    /// 所有接口
    /// </summary>
    [Description("所有接口")]
    ImplementedInterfaces,

    /// <summary>
    /// 注册自己包括所有接口
    /// </summary>
    [Description("自己包括所有接口")]
    All
}