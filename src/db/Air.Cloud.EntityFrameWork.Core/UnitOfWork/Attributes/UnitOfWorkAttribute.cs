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
/// <para>zh-cn:标记需要工作单元事务处理的 MVC 动作。</para>
/// <para>en-us:Marks an MVC action that requires work-unit transaction handling.</para>
/// </summary>
[IgnoreScanning, AttributeUsage(AttributeTargets.Method)]
public sealed class UnitOfWorkAttribute : Attribute
{
    /// <summary>
    /// <para>zh-cn:创建使用默认事务策略的工作单元特性。</para>
    /// <para>en-us:Creates the attribute with the default transaction policy.</para>
    /// </summary>
    public UnitOfWorkAttribute()
    {
    }

    /// <summary>
    /// <para>zh-cn:创建指定事务策略的工作单元特性。</para>
    /// <para>en-us:Creates the attribute with the specified transaction policy.</para>
    /// </summary>
    /// <param name="ensureTransaction"><para>zh-cn:没有已解析上下文时是否确保创建事务。</para><para>en-us:Whether to ensure a transaction is created when no context has been resolved.</para></param>
    public UnitOfWorkAttribute(bool ensureTransaction)
    {
        EnsureTransaction = ensureTransaction;
    }

    /// <summary>
    /// <para>zh-cn:指示没有已解析上下文时是否确保事务可用。</para>
    /// <para>en-us:Indicates whether a transaction must be available when no context has been resolved.</para>
    /// </summary>
    public bool EnsureTransaction { get; set; } = false;
}
