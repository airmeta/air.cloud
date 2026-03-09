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

namespace Air.Cloud.EntityFrameWork.Core.Contexts.Enums;

/// <summary>
/// 数据库上下文模式
/// </summary>
[IgnoreScanning]
public enum DbContextMode
{
    /// <summary>
    /// 缓存模型数据库上下文
    /// <para>
    /// OnModelCreating 只会初始化一次
    /// </para>
    /// </summary>
    [Description("缓存模型数据库上下文")]
    Cached,

    /// <summary>
    /// 动态模型数据库上下文
    /// <para>
    /// OnModelCreating 每次都会调用
    /// </para>
    /// </summary>
    [Description("动态模型数据库上下文")]
    Dynamic
}