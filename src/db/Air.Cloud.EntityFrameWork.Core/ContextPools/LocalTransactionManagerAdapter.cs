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
/// <para>zh-cn:兼容自定义 IDbContextPool 实现的事务管理器适配器。</para>
/// <para>en-us:Transaction-manager adapter for custom IDbContextPool implementations.</para>
/// </summary>
internal sealed class LocalTransactionManagerAdapter : ILocalTransactionManager
{
    private readonly IDbContextPool _pool;
    private int _depth;
    private bool _rollbackOnly;

    /// <summary><para>zh-cn:创建兼容自定义上下文池的事务管理器适配器。</para><para>en-us:Creates a transaction-manager adapter for a custom context pool.</para></summary>
    /// <param name="pool"><para>zh-cn:要适配的数据库上下文池。</para><para>en-us:The database context pool to adapt.</para></param>
    public LocalTransactionManagerAdapter(IDbContextPool pool)
    {
        _pool = pool ?? throw new ArgumentNullException(nameof(pool));
    }

    /// <summary><para>zh-cn:当前可观察到的本地事务。</para><para>en-us:The currently observable local transaction.</para></summary>
    public IDbContextTransaction? CurrentTransaction => _pool.DbContextTransaction;

    /// <summary><para>zh-cn:指示当前是否存在活动事务。</para><para>en-us:Indicates whether an active transaction exists.</para></summary>
    public bool IsActive => CurrentTransaction != null;

    /// <summary><para>zh-cn:当前事务嵌套深度。</para><para>en-us:The current transaction nesting depth.</para></summary>
    public int Depth => _depth;

    /// <summary><para>zh-cn:指示当前事务是否只能回滚。</para><para>en-us:Indicates whether the current transaction is rollback-only.</para></summary>
    public bool IsRollbackOnly => _rollbackOnly;

    /// <summary><para>zh-cn:开始或加入当前本地事务。</para><para>en-us:Starts or joins the current local transaction.</para></summary>
    /// <param name="ensureTransaction"><para>zh-cn:池为空时是否确保解析上下文并创建事务。</para><para>en-us:Whether to resolve a context and create a transaction when the pool is empty.</para></param>
    public void Begin(bool ensureTransaction = false)
    {
        _pool.BeginTransaction(ensureTransaction);
        if (CurrentTransaction != null) _depth++;
    }

    /// <summary><para>zh-cn:创建自动回滚的事务作用域。</para><para>en-us:Creates a transaction scope with automatic rollback.</para></summary>
    /// <param name="ensureTransaction"><para>zh-cn:池为空时是否确保创建事务。</para><para>en-us:Whether to ensure a transaction when the pool is empty.</para></param>
    /// <param name="closeAllOnCompletion"><para>zh-cn:完成后是否关闭全部连接。</para><para>en-us:Whether to close all connections on completion.</para></param>
    /// <returns><para>zh-cn:事务作用域。</para><para>en-us:The transaction scope.</para></returns>
    public ILocalTransactionScope BeginScope(bool ensureTransaction = false, bool closeAllOnCompletion = false)
        => new LocalTransactionScope(this, ensureTransaction, closeAllOnCompletion);

    /// <summary><para>zh-cn:提交当前事务作用域。</para><para>en-us:Commits the current transaction scope.</para></summary>
    /// <param name="withCloseAll"><para>zh-cn:提交后是否关闭全部连接。</para><para>en-us:Whether to close all connections after commit.</para></param>
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

    /// <summary><para>zh-cn:回滚当前事务作用域。</para><para>en-us:Rolls back the current transaction scope.</para></summary>
    /// <param name="withCloseAll"><para>zh-cn:回滚后是否关闭全部连接。</para><para>en-us:Whether to close all connections after rollback.</para></param>
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
