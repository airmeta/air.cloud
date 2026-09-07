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
/// 本地事务管理器作用域实现。
/// </summary>
internal sealed class LocalTransactionScope : ILocalTransactionScope
{
    private readonly ILocalTransactionManager _manager;
    private readonly bool _closeAll;
    private bool _disposed;

    public LocalTransactionScope(ILocalTransactionManager manager, bool ensureTransaction, bool closeAll)
    {
        _manager = manager ?? throw new ArgumentNullException(nameof(manager));
        _closeAll = closeAll;
        _manager.Begin(ensureTransaction);
    }

    public bool IsCompleted { get; private set; }

    public void Complete()
    {
        ThrowIfDisposed();
        if (IsCompleted) return;

        _manager.Commit(_closeAll);
        IsCompleted = true;
    }

    public void Rollback()
    {
        ThrowIfDisposed();
        if (IsCompleted) return;

        _manager.Rollback(_closeAll);
        IsCompleted = true;
    }

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
