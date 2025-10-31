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

namespace Air.Cloud.Core.Standard.DynamicServer.Enums;

/// <summary>
/// 注册类型
/// </summary>
[IgnoreScanning]
public enum RegisterType
{
    /// <summary>
    /// 瞬时
    /// </summary>
    [Description("瞬时")]
    Transient,

    /// <summary>
    /// 作用域
    /// </summary>
    [Description("作用域")]
    Scoped,

    /// <summary>
    /// 单例
    /// </summary>
    [Description("单例")]
    Singleton
}