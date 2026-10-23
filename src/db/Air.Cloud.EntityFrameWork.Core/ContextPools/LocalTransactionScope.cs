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
namespace Air.Cloud.EntityFrameWork.Core.ContextPools;

/// <summary>
/// <para>zh-cn:本地事务管理器作用域实现，未完成时释放会自动回滚。</para>
/// <para>en-us:Local transaction manager scope that rolls back automatically when disposed without completion.</para>
/// </summary>
internal sealed class LocalTransactionScope : ILocalTransactionScope
{
    private readonly ILocalTransactionManager _manager;
    private readonly bool _closeAll;
    private bool _disposed;

    /// <summary><para>zh-cn:创建并立即开始本地事务作用域。</para><para>en-us:Creates and immediately begins a local transaction scope.</para></summary>
    /// <param name="manager"><para>zh-cn:负责事务生命周期的管理器。</para><para>en-us:The manager responsible for the transaction lifecycle.</para></param>
    /// <param name="ensureTransaction"><para>zh-cn:作用域开始时是否确保事务存在。</para><para>en-us:Whether to ensure a transaction exists when the scope starts.</para></param>
    /// <param name="closeAll"><para>zh-cn:作用域完成后是否关闭全部连接。</para><para>en-us:Whether to close all connections when the scope completes.</para></param>
    /// <exception cref="ArgumentNullException"><para>zh-cn:manager 为空时抛出。</para><para>en-us:Thrown when manager is null.</para></exception>
    public LocalTransactionScope(ILocalTransactionManager manager, bool ensureTransaction, bool closeAll)
    {
        _manager = manager ?? throw new ArgumentNullException(nameof(manager));
        _closeAll = closeAll;
        _manager.Begin(ensureTransaction);
    }

    /// <summary><para>zh-cn:指示作用域是否已经完成提交或回滚。</para><para>en-us:Indicates whether the scope has completed by commit or rollback.</para></summary>
    public bool IsCompleted { get; private set; }

    /// <summary><para>zh-cn:完成当前作用域并提交事务。</para><para>en-us:Completes the current scope and commits the transaction.</para></summary>
    public void Complete()
    {
        ThrowIfDisposed();
        if (IsCompleted) return;

        _manager.Commit(_closeAll);
        IsCompleted = true;
    }

    /// <summary><para>zh-cn:显式回滚当前作用域。</para><para>en-us:Explicitly rolls back the current scope.</para></summary>
    public void Rollback()
    {
        ThrowIfDisposed();
        if (IsCompleted) return;

        _manager.Rollback(_closeAll);
        IsCompleted = true;
    }

    /// <summary><para>zh-cn:释放作用域；未完成时自动回滚。</para><para>en-us:Disposes the scope and automatically rolls back when it is incomplete.</para></summary>
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        if (!IsCompleted)
        {
            _manager.Rollback(_closeAll);
            IsCompleted = true;
        }
    }

    /// <summary><para>zh-cn:异步释放作用域。</para><para>en-us:Asynchronously disposes the scope.</para></summary>
    /// <returns><para>zh-cn:表示释放完成的任务。</para><para>en-us:A task representing disposal completion.</para></returns>
    public ValueTask DisposeAsync()
    {
        Dispose();
        return ValueTask.CompletedTask;
    }

    private void ThrowIfDisposed()
    {
        if (_disposed) throw new ObjectDisposedException(nameof(LocalTransactionScope));
    }
}
