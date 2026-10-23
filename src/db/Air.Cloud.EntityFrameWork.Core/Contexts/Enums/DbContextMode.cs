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
/// <para>zh-cn:数据库上下文模型缓存策略。</para>
/// <para>en-us:Database-context model caching strategy.</para>
/// </summary>
[IgnoreScanning]
public enum DbContextMode
{
    /// <summary>
    /// <para>zh-cn:缓存模型数据库上下文；OnModelCreating 通常只初始化一次。</para>
    /// <para>en-us:Uses a cached model; OnModelCreating is normally initialized once.</para>
    /// </summary>
    [Description("缓存模型数据库上下文")]
    Cached,

    /// <summary>
    /// <para>zh-cn:动态模型数据库上下文；每次创建上下文时都会调用 OnModelCreating。</para>
    /// <para>en-us:Uses a dynamic model; OnModelCreating is called whenever the context is created.</para>
    /// </summary>
    [Description("动态模型数据库上下文")]
    Dynamic
}
