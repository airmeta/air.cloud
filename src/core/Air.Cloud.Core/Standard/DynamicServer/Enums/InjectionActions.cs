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
/// 服务注册方式
/// </summary>
[IgnoreScanning]
public enum InjectionActions
{
    /// <summary>
    /// 如果存在则覆盖
    /// </summary>
    [Description("存在则覆盖")]
    Add,

    /// <summary>
    /// 如果存在则跳过，默认方式
    /// </summary>
    [Description("存在则跳过")]
    TryAdd
}