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
using Microsoft.EntityFrameworkCore.Storage;

namespace Air.Cloud.EntityFrameWork.Core.ContextPools;

/// <summary>
/// <para>zh-cn:保存本地事务的生命周期和所有权，避免上下文池同时承担事务状态管理。</para>
/// <para>en-us:Tracks local transaction lifetime and ownership so the context pool does not also own transaction state management.</para>
/// </summary>
internal sealed class LocalTransactionCoordinator : IDisposable
{
    /// <summary>
    /// 当前事务。
    /// </summary>
    public IDbContextTransaction? Transaction { get; private set; }

    /// <summary>
    /// 当前事务是否由协调器创建并负责释放。
    /// </summary>
    public bool OwnsTransaction { get; private set; }

    /// <summary>
    /// 事务是否处于活动状态。
    /// </summary>
    public bool IsActive => Transaction != null;

    /// <summary>
    /// 登记一个本地事务；已有事务不能被静默替换。
    /// </summary>
    /// <param name="transaction">要登记的事务。</param>
    /// <param name="ownsTransaction">是否由协调器负责释放。</param>
    public void Attach(IDbContextTransaction transaction, bool ownsTransaction)
    {
        ArgumentNullException.ThrowIfNull(transaction);

        if (Transaction != null && !ReferenceEquals(Transaction, transaction))
            throw new InvalidOperationException("当前工作单元已经绑定了另一个本地事务。");

        Transaction = transaction;
        OwnsTransaction |= ownsTransaction;
    }

    /// <summary>
    /// 清除当前事务并按所有权释放它。
    /// </summary>
    public void Clear()
    {
        var transaction = Transaction;
        var ownsTransaction = OwnsTransaction;
        Transaction = null;
        OwnsTransaction = false;

        if (ownsTransaction) transaction?.Dispose();
    }

    /// <summary>
    /// 释放协调器持有的事务。
    /// </summary>
    public void Dispose() => Clear();
}
