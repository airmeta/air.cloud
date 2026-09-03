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

using Microsoft.EntityFrameworkCore;

namespace Air.Cloud.UnitTest.Modules.DataBase;

/// <summary>
/// <para>zh-cn:数据库上下文池事务边界测试。</para>
/// <para>en-us:Tests for database context pool transaction boundaries.</para>
/// </summary>
public sealed class DbContextPoolTransactionTests
{
    /// <summary>
    /// <para>zh-cn:验证多个上下文异步保存按顺序执行，避免 DbContext 并发重入。</para>
    /// <para>en-us:Verifies asynchronous saves across contexts run sequentially and do not re-enter DbContext concurrently.</para>
    /// </summary>
    [Fact]
    public async Task SavePoolNowAsync_should_save_contexts_sequentially()
    {
        TrackingDbContext.ResetCounters();
        var first = new TrackingDbContext();
        var second = new TrackingDbContext();
        first.Add(new TestRow());
        second.Add(new TestRow());

        var pool = new DbContextPool(null!);
        pool.GetDbContexts()[first.ContextId.InstanceId] = first;
        pool.GetDbContexts()[second.ContextId.InstanceId] = second;

        var savedContextCount = await pool.SavePoolNowAsync();

        Assert.Equal(2, savedContextCount);
        Assert.Equal(1, TrackingDbContext.MaximumConcurrentSaveCalls);
    }

    /// <summary>
    /// <para>zh-cn:验证无显式事务时仍保持保存并接受实体状态的兼容语义。</para>
    /// <para>en-us:Verifies that saving without an explicit transaction keeps the compatibility behavior of accepting entity states.</para>
    /// </summary>
    [Fact]
    public void CommitTransaction_should_accept_entity_state_without_explicit_transaction()
    {
        using var context = new TrackingDbContext();
        context.Add(new TestRow());

        var pool = new DbContextPool(null!);
        pool.GetDbContexts()[context.ContextId.InstanceId] = context;

        pool.CommitTransaction();
        Assert.False(context.ChangeTracker.HasChanges());
    }

    /// <summary>
    /// <para>zh-cn:跟踪保存并发度的最小测试上下文。</para>
    /// <para>en-us:Minimal test context that tracks save concurrency.</para>
    /// </summary>
    private sealed class TrackingDbContext : DbContext
    {
        private static int concurrentSaveCalls;

        public static int MaximumConcurrentSaveCalls { get; private set; }

        public static void ResetCounters()
        {
            concurrentSaveCalls = 0;
            MaximumConcurrentSaveCalls = 0;
        }

        public TrackingDbContext()
            : base(new DbContextOptionsBuilder<TrackingDbContext>().Options)
        {
        }

        public DbSet<TestRow> Rows => Set<TestRow>();

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseInMemoryDatabase(Guid.NewGuid().ToString("N"));
        }

        public override async Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
        {
            var current = Interlocked.Increment(ref concurrentSaveCalls);
            MaximumConcurrentSaveCalls = Math.Max(MaximumConcurrentSaveCalls, current);
            try
            {
                await Task.Delay(10, cancellationToken);
                if (acceptAllChangesOnSuccess) ChangeTracker.AcceptAllChanges();
                return 1;
            }
            finally
            {
                Interlocked.Decrement(ref concurrentSaveCalls);
            }
        }
    }

    /// <summary>
    /// <para>zh-cn:用于触发 ChangeTracker 变更状态的测试实体。</para>
    /// <para>en-us:Test entity used to create a ChangeTracker pending state.</para>
    /// </summary>
    private sealed class TestRow
    {
        public int Id { get; set; }
    }
}
