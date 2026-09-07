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

using Air.Cloud.EntityFrameWork.Core.ContextPools;

using Microsoft.EntityFrameworkCore.Storage;

using System.Data.Common;

namespace Air.Cloud.UnitTest.Modules.DataBase;

/// <summary>
/// <para>zh-cn:本地事务协调器的嵌套深度和中止状态测试。</para>
/// <para>en-us:Tests for local transaction coordinator nesting depth and abort state.</para>
/// </summary>
public sealed class LocalTransactionCoordinatorTests
{
    /// <summary>
    /// <para>zh-cn:验证嵌套提交只退出当前层级，最外层完成后才归零。</para>
    /// <para>en-us:Verifies that nested completion exits only the current level and reaches zero only at the outermost scope.</para>
    /// </summary>
    [Fact]
    public void Complete_scope_should_decrement_depth_without_replacing_transaction()
    {
        using var coordinator = new LocalTransactionCoordinator();
        using var transaction = new TestTransaction();

        coordinator.Attach(transaction, ownsTransaction: false);
        coordinator.EnterScope();

        Assert.Equal(2, coordinator.Depth);
        Assert.False(coordinator.CompleteScope());
        Assert.Equal(1, coordinator.Depth);
        Assert.Same(transaction, coordinator.Transaction);
        Assert.True(coordinator.CompleteScope());
        Assert.Equal(0, coordinator.Depth);
    }

    /// <summary>
    /// <para>zh-cn:验证回滚标记禁止继续嵌套，并且清理后可以重新建立事务状态。</para>
    /// <para>en-us:Verifies that rollback-only blocks further nesting and that clearing allows a fresh transaction state.</para>
    /// </summary>
    [Fact]
    public void Rollback_only_should_block_new_scope_until_clear()
    {
        using var coordinator = new LocalTransactionCoordinator();
        using var transaction = new TestTransaction();

        coordinator.Attach(transaction, ownsTransaction: false);
        coordinator.EnterScope();
        coordinator.MarkRollbackOnly();

        Assert.Throws<InvalidOperationException>(() => coordinator.EnterScope());
        coordinator.Clear();

        Assert.Equal(0, coordinator.Depth);
        Assert.False(coordinator.IsRollbackOnly);
        Assert.False(coordinator.IsAborted);
    }

    /// <summary>
    /// <para>zh-cn:验证自动中止状态不会被误认为无事务，从而避免外层重新保存。</para>
    /// <para>en-us:Verifies that an automatic abort is not mistaken for no transaction, preventing an outer scope from saving again.</para>
    /// </summary>
    [Fact]
    public void Abort_should_clear_transaction_and_remain_rollback_only()
    {
        using var coordinator = new LocalTransactionCoordinator();
        var transaction = new TestTransaction();

        coordinator.Attach(transaction, ownsTransaction: true);
        coordinator.EnterScope();
        coordinator.Abort();

        Assert.Null(coordinator.Transaction);
        Assert.Equal(0, coordinator.Depth);
        Assert.True(coordinator.IsRollbackOnly);
        Assert.True(coordinator.IsAborted);
        Assert.Equal(1, transaction.DisposeCount);

        coordinator.Clear();
        Assert.False(coordinator.IsAborted);
    }

    /// <summary>
    /// <para>zh-cn:验证在工作单元执行过程中加入的新事务组可以继承当前嵌套深度。</para>
    /// <para>en-us:Verifies that a transaction group joined during a work unit can inherit the current nesting depth.</para>
    /// </summary>
    [Fact]
    public void Align_scope_depth_should_update_new_transaction_group()
    {
        using var coordinator = new LocalTransactionCoordinator();
        using var transaction = new TestTransaction();

        coordinator.Attach(transaction, ownsTransaction: false);
        coordinator.AlignScopeDepth(3);

        Assert.Equal(3, coordinator.Depth);
        Assert.Throws<ArgumentOutOfRangeException>(() => coordinator.AlignScopeDepth(0));
    }

    private sealed class TestTransaction : IDbContextTransaction
    {
        public int DisposeCount { get; private set; }

        public Guid TransactionId { get; } = Guid.NewGuid();

        public DbTransaction GetDbTransaction() => throw new NotSupportedException();

        public void Commit() => throw new NotSupportedException();

        public Task CommitAsync(CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public void Rollback() => throw new NotSupportedException();

        public Task RollbackAsync(CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public void Dispose() => DisposeCount++;

        public ValueTask DisposeAsync()
        {
            Dispose();
            return ValueTask.CompletedTask;
        }
    }
}
