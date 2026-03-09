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

namespace Air.Cloud.EntityFrameWork.Core.UnitOfWork.Attributes;

/// <summary>
/// 工作单元配置特性
/// </summary>
[IgnoreScanning, AttributeUsage(AttributeTargets.Method)]
public sealed class UnitOfWorkAttribute : Attribute
{
    /// <summary>
    /// 构造函数
    /// </summary>
    public UnitOfWorkAttribute()
    {
    }

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="ensureTransaction"></param>
    public UnitOfWorkAttribute(bool ensureTransaction)
    {
        EnsureTransaction = ensureTransaction;
    }

    /// <summary>
    /// 确保事务可用
    /// <para>此方法为了解决静态类方式操作数据库的问题</para>
    /// </summary>
    public bool EnsureTransaction { get; set; } = false;
}