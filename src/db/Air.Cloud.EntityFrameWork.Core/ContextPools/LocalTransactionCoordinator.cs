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
    /// <para>zh-cn:当前本地事务的嵌套工作单元深度。</para>
    /// <para>en-us:The nesting depth of the current local transaction work unit.</para>
    /// </summary>
    public int Depth { get; private set; }

    /// <summary>
    /// <para>zh-cn:当前工作单元是否已经被标记为只能回滚。</para>
    /// <para>en-us:Whether the current work unit has been marked rollback-only.</para>
    /// </summary>
    public bool IsRollbackOnly { get; private set; }

    /// <summary>
    /// <para>zh-cn:当前事务是否因自动回滚而中止；中止状态会阻止外层工作单元再次保存。</para>
    /// <para>en-us:Whether the current transaction was aborted by automatic rollback; an aborted state prevents an outer work unit from saving again.</para>
    /// </summary>
    public bool IsAborted { get; private set; }

    /// <summary>
    /// <para>zh-cn:登记一个本地事务；已有事务不能被静默替换。</para>
    /// <para>en-us:Attaches a local transaction; an existing transaction cannot be silently replaced.</para>
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
        if (Depth == 0) Depth = 1;
    }

    /// <summary>
    /// <para>zh-cn:进入一个嵌套工作单元；事务已经标记回滚时拒绝继续嵌套。</para>
    /// <para>en-us:Enters a nested work unit and rejects nesting after the transaction is marked rollback-only.</para>
    /// </summary>
    public void EnterScope()
    {
        if (Transaction == null) throw new InvalidOperationException("当前没有可以嵌套的本地事务。");
        if (IsRollbackOnly || IsAborted) throw new InvalidOperationException("当前本地事务已经标记为回滚，不能继续嵌套工作单元。");
        checked { Depth++; }
    }

    /// <summary>
    /// <para>zh-cn:将新加入的本地事务组对齐到当前工作单元深度。</para>
    /// <para>en-us:Aligns a newly joined local transaction group with the current work-unit depth.</para>
    /// </summary>
    /// <param name="depth"><para>zh-cn:目标深度，必须为正数。</para><para>en-us:The target depth, which must be positive.</para></param>
    public void AlignScopeDepth(int depth)
    {
        if (Transaction == null) throw new InvalidOperationException("当前没有可以对齐的本地事务。");
        if (depth < 1) throw new ArgumentOutOfRangeException(nameof(depth));
        if (IsRollbackOnly || IsAborted) throw new InvalidOperationException("当前本地事务已经标记为回滚，不能对齐工作单元深度。");
        Depth = depth;
    }

    /// <summary>
    /// <para>zh-cn:完成当前工作单元层级，并返回是否已经到达最外层。</para>
    /// <para>en-us:Completes the current work-unit level and returns whether the outermost level has been reached.</para>
    /// </summary>
    public bool CompleteScope()
    {
        if (Depth > 0) Depth--;
        return Depth == 0;
    }

    /// <summary>
    /// <para>zh-cn:将当前工作单元标记为只能回滚；实际数据库回滚由最外层统一执行。</para>
    /// <para>en-us:Marks the current work unit rollback-only; the outermost scope performs the actual database rollback.</para>
    /// </summary>
    public void MarkRollbackOnly() => IsRollbackOnly = true;

    /// <summary>
    /// <para>zh-cn:记录自动回滚导致的事务中止，并释放协调器持有的事务。</para>
    /// <para>en-us:Records an automatic-rollback abort and releases the transaction owned by the coordinator.</para>
    /// </summary>
    public void Abort()
    {
        var transaction = Transaction;
        var ownsTransaction = OwnsTransaction;
        Transaction = null;
        OwnsTransaction = false;
        Depth = 0;
        IsRollbackOnly = true;
        IsAborted = true;

        if (ownsTransaction) transaction?.Dispose();
    }

    /// <summary>
    /// <para>zh-cn:清除当前事务并按所有权释放它，同时重置嵌套和回滚状态。</para>
    /// <para>en-us:Clears the current transaction according to ownership and resets nesting and rollback state.</para>
    /// </summary>
    public void Clear()
    {
        var transaction = Transaction;
        var ownsTransaction = OwnsTransaction;
        Transaction = null;
        OwnsTransaction = false;
        Depth = 0;
        IsRollbackOnly = false;
        IsAborted = false;

        if (ownsTransaction) transaction?.Dispose();
    }

    /// <summary>
    /// <para>zh-cn:释放协调器持有的事务。</para>
    /// <para>en-us:Releases the transaction owned by the coordinator.</para>
    /// </summary>
    public void Dispose() => Clear();
}
