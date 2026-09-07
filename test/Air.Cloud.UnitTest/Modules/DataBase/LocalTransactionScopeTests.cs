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
using Air.Cloud.EntityFrameWork.Core.Extensions;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;

using System.Collections.Concurrent;
using System.Data.Common;

namespace Air.Cloud.UnitTest.Modules.DataBase;

/// <summary>
/// <para>zh-cn:事务作用域自动完成和自动回滚测试。</para>
/// <para>en-us:Tests for transaction-scope completion and automatic rollback.</para>
/// </summary>
public sealed class LocalTransactionScopeTests
{
    /// <summary>
    /// <para>zh-cn:未调用 Complete 的作用域释放时必须回滚。</para>
    /// <para>en-us:A scope that is disposed without Complete must roll back.</para>
    /// </summary>
    [Fact]
    public void Incomplete_scope_should_rollback_on_dispose()
    {
        var pool = new TrackingPool();
        using var provider = new ServiceCollection()
            .AddSingleton<IDbContextPool>(pool)
            .AddDatabaseAccessor()
            .BuildServiceProvider();

        using (var scope = provider.CreateScope())
        {
            var manager = scope.ServiceProvider.GetRequiredService<ILocalTransactionManager>();
            using var transactionScope = manager.BeginScope();

            Assert.True(transactionScope.IsCompleted == false);
            Assert.Equal(1, pool.BeginCount);
        }

        Assert.Equal(1, pool.RollbackCount);
        Assert.Equal(0, pool.CommitCount);
    }

    /// <summary>
    /// <para>zh-cn:完成作用域后释放不能重复回滚。</para>
    /// <para>en-us:Disposing a completed scope must not roll it back again.</para>
    /// </summary>
    [Fact]
    public void Completed_scope_should_not_rollback_on_dispose()
    {
        var pool = new TrackingPool();
        using var provider = new ServiceCollection()
            .AddSingleton<IDbContextPool>(pool)
            .AddDatabaseAccessor()
            .BuildServiceProvider();

        using (var scope = provider.CreateScope())
        {
            var manager = scope.ServiceProvider.GetRequiredService<ILocalTransactionManager>();
            using var transactionScope = manager.BeginScope();
            transactionScope.Complete();

            Assert.True(transactionScope.IsCompleted);
        }

        Assert.Equal(1, pool.CommitCount);
        Assert.Equal(0, pool.RollbackCount);
    }

    private sealed class TrackingPool : IDbContextPool
    {
        private readonly TestTransaction _transaction = new();
        private bool _active;

        public int BeginCount { get; private set; }
        public int CommitCount { get; private set; }
        public int RollbackCount { get; private set; }
        public IDbContextTransaction? DbContextTransaction => _active ? _transaction : null;

        public ConcurrentDictionary<Guid, DbContext> GetDbContexts() => new();
        public void AddToPool(DbContext dbContext) { }
        public Task AddToPoolAsync(DbContext dbContext) => Task.CompletedTask;
        public int SavePoolNow() => 0;
        public int SavePoolNow(bool acceptAllChangesOnSuccess) => 0;
        public Task<int> SavePoolNowAsync(CancellationToken cancellationToken = default) => Task.FromResult(0);
        public Task<int> SavePoolNowAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default) => Task.FromResult(0);

        public void BeginTransaction(bool ensureTransaction = false)
        {
            BeginCount++;
            _active = true;
        }

        public void CommitTransaction(bool withCloseAll = false)
        {
            CommitCount++;
            _active = false;
        }

        public void RollbackTransaction(bool withCloseAll = false)
        {
            RollbackCount++;
            _active = false;
        }

        public void CloseAll() { }
    }

    private sealed class TestTransaction : IDbContextTransaction
    {
        public Guid TransactionId { get; } = Guid.NewGuid();
        public DbTransaction GetDbTransaction() => throw new NotSupportedException();
        public void Commit() => throw new NotSupportedException();
        public Task CommitAsync(CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public void Rollback() => throw new NotSupportedException();
        public Task RollbackAsync(CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public void Dispose() { }
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}
