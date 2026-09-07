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
/// 兼容自定义 IDbContextPool 实现的事务管理器适配器。
/// </summary>
internal sealed class LocalTransactionManagerAdapter : ILocalTransactionManager
{
    private readonly IDbContextPool _pool;
    private int _depth;
    private bool _rollbackOnly;

    public LocalTransactionManagerAdapter(IDbContextPool pool)
    {
        _pool = pool ?? throw new ArgumentNullException(nameof(pool));
    }

    public IDbContextTransaction? CurrentTransaction => _pool.DbContextTransaction;

    public bool IsActive => CurrentTransaction != null;

    public int Depth => _depth;

    public bool IsRollbackOnly => _rollbackOnly;

    public void Begin(bool ensureTransaction = false)
    {
        _pool.BeginTransaction(ensureTransaction);
        if (CurrentTransaction != null) _depth++;
    }

    public ILocalTransactionScope BeginScope(bool ensureTransaction = false, bool closeAllOnCompletion = false)
        => new LocalTransactionScope(this, ensureTransaction, closeAllOnCompletion);

    public void Commit(bool withCloseAll = false)
    {
        _pool.CommitTransaction(withCloseAll);
        if (CurrentTransaction == null)
        {
            _depth = 0;
            _rollbackOnly = false;
        }
        else if (_depth > 0)
        {
            _depth--;
        }
    }

    public void Rollback(bool withCloseAll = false)
    {
        _pool.RollbackTransaction(withCloseAll);
        if (CurrentTransaction == null)
        {
            _depth = 0;
            _rollbackOnly = false;
        }
        else
        {
            _rollbackOnly = true;
            if (_depth > 0) _depth--;
        }
    }
}
