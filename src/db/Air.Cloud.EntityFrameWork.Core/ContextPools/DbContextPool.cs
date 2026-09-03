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
using Air.Cloud.Core;
using Air.Cloud.Core.Modules.AppPrint;
using Air.Cloud.EntityFrameWork.Core.Extensions.DatabaseProvider;

using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;

using System.Collections.Concurrent;
using System.Data.Common;
using System.Reflection;

namespace Air.Cloud.EntityFrameWork.Core.ContextPools;

/// <summary>
/// 数据库上下文池
/// </summary>
[IgnoreScanning]
public class DbContextPool : IDbContextPool
{
    /// <summary>
    /// 线程安全的数据库上下文集合
    /// </summary>
    private readonly ConcurrentDictionary<Guid, DbContext> _dbContexts;

    /// <summary>
    /// 服务提供器
    /// </summary>
    private readonly IServiceProvider _serviceProvider;

    /// <summary>
    /// 本地事务生命周期协调器。
    /// </summary>
    private readonly LocalTransactionCoordinator _transactionCoordinator;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="serviceProvider"></param>
    public DbContextPool(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;

        _dbContexts = new ConcurrentDictionary<Guid, DbContext>();
        _transactionCoordinator = new LocalTransactionCoordinator();
    }

    /// <summary>
    /// 数据库上下文事务
    /// </summary>
    public IDbContextTransaction? DbContextTransaction => _transactionCoordinator.Transaction;

    /// <summary>
    /// 获取所有数据库上下文
    /// </summary>
    /// <returns></returns>
    public ConcurrentDictionary<Guid, DbContext> GetDbContexts()
    {
        return _dbContexts;
    }

    /// <summary>
    /// <para>zh-cn:登记一个关系型数据库上下文，并在保存失败且上下文启用自动回滚时回滚当前本地事务。</para>
    /// <para>en-us:Registers a relational DbContext and rolls back the current local transaction on save failure when that context enables automatic rollback.</para>
    /// </summary>
    /// <param name="dbContext"></param>
    public void AddToPool(DbContext dbContext)
    {
        // 跳过非关系型数据库
        if (!dbContext.Database.IsRelational()) return;

        var instanceId = dbContext.ContextId.InstanceId;
        if (!_dbContexts.TryAdd(instanceId, dbContext)) return;

        // 订阅数据库上下文操作失败事件
        dbContext.SaveChangesFailed += (s, e) =>
        {
            // 当前事务
            if (s is not DbContext context) return;
            var database = context.Database;
            var currentTransaction = database.CurrentTransaction;

            // 只有事务不等于空且支持自动回滚
            if (currentTransaction == null || !IsFailedAutoRollbackEnabled(context)) return;
            // 获取数据库连接信息
            var connection = database.GetDbConnection();
            // 回滚事务
            currentTransaction.Rollback();
            // 自动回滚也必须同步清理池中的事务状态，否则下一次 BeginTransaction 可能复用已失效事务。
            if (ReferenceEquals(DbContextTransaction?.GetDbTransaction(), currentTransaction.GetDbTransaction()))
            {
                DetachTransactionContexts(currentTransaction.GetDbTransaction());
                _transactionCoordinator.Clear();
            }
            // 打印数据库关闭信息
            AppRealization.TraceLog.Write(AppRealization.JSON.Serialize(new AppPrintInformation
            {
                Title = "数据库事务状态更新",
                Level = AppPrintLevel.Error,
                Content = "已触发事务回滚",
                State = true,
                AdditionalParams=new Dictionary<string, object>()
                {
                    {"connection_id", context.ContextId},
                    {"connection_str", connection.ConnectionString},

                },
                Type = AppPrintConstType.ORM_EXEC_TYPE
            }), Db.TraceLogTags);
        };
    }

    /// <summary>
    /// 保存数据库上下文（异步）
    /// </summary>
    /// <param name="dbContext"></param>
    public Task AddToPoolAsync(DbContext dbContext)
    {
        AddToPool(dbContext);
        return Task.CompletedTask;
    }

    /// <summary>
    /// 保存数据库上下文池中所有已更改的数据库上下文
    /// </summary>
    /// <returns></returns>
    public int SavePoolNow()
    {
        // DbContext 不支持并发操作；即使池本身是线程安全集合，保存也必须串行执行。
        var savedContextCount = 0;
        foreach (var item in GetPendingDbContexts())
        {
            item.Value.SaveChanges();
            savedContextCount++;
        }

        return savedContextCount;
    }

    /// <summary>
    /// 保存数据库上下文池中所有已更改的数据库上下文
    /// </summary>
    /// <param name="acceptAllChangesOnSuccess"></param>
    /// <returns></returns>
    public int SavePoolNow(bool acceptAllChangesOnSuccess)
    {
        // DbContext 不支持并发操作；即使池本身是线程安全集合，保存也必须串行执行。
        var savedContextCount = 0;
        foreach (var item in GetPendingDbContexts())
        {
            item.Value.SaveChanges(acceptAllChangesOnSuccess);
            savedContextCount++;
        }

        return savedContextCount;
    }

    /// <summary>
    /// 保存数据库上下文池中所有已更改的数据库上下文
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<int> SavePoolNowAsync(CancellationToken cancellationToken = default)
    {
        // DbContext 不支持并发操作；多个上下文也必须按顺序保存，避免同一上下文重入。
        var savedContextCount = 0;
        foreach (var item in GetPendingDbContexts())
        {
            await item.Value.SaveChangesAsync(cancellationToken);
            savedContextCount++;
        }

        return savedContextCount;
    }

    /// <summary>
    /// 保存数据库上下文池中所有已更改的数据库上下文（异步）
    /// </summary>
    /// <param name="acceptAllChangesOnSuccess"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<int> SavePoolNowAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        // DbContext 不支持并发操作；多个上下文也必须按顺序保存，避免同一上下文重入。
        var savedContextCount = 0;
        foreach (var item in GetPendingDbContexts())
        {
            await item.Value.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
            savedContextCount++;
        }

        return savedContextCount;
    }

    /// <summary>
    /// <para>zh-cn:打开或接管当前上下文池的本地事务，并将兼容上下文绑定到同一连接事务。</para>
    /// <para>en-us:Starts or adopts the local transaction for this context pool and binds compatible contexts to the same connection transaction.</para>
    /// </summary>
    /// <param name="ensureTransaction"><para>zh-cn:池为空时是否解析默认上下文并确保事务存在。</para><para>en-us:Whether to resolve the default context and ensure a transaction when the pool is empty.</para></param>
    public void BeginTransaction(bool ensureTransaction = false)
    {
        var transactionContexts = GetUnitOfWorkContexts();

        // 没有已经解析的上下文时，按原有 ensureTransaction 语义创建默认上下文。
        if (transactionContexts.Count == 0 && ensureTransaction)
        {
            // 只允许通过明确的 MasterDbContextLocator 解析默认上下文，避免依赖描述器枚举顺序。
            var newDbContext = Db.GetDbContext(_serviceProvider);
            transactionContexts = GetUnitOfWorkContexts();
            if (!transactionContexts.Contains(newDbContext)) transactionContexts.Add(newDbContext);
        }

        if (transactionContexts.Count == 0) return;

        // 已经开启的事务只允许在相同 Provider、相同连接实例的上下文之间共享。
        if (DbContextTransaction != null)
        {
            ShareTransaction(DbContextTransaction.GetDbTransaction(), transactionContexts);
            return;
        }

        var transactionCreatedByPool = false;
        var transactionDbContext = transactionContexts.FirstOrDefault(u => u.Database.CurrentTransaction != null);
        if (transactionDbContext != null)
        {
            EnsureCompatibleConnections(transactionDbContext, transactionContexts);
            var existingTransaction = transactionDbContext.Database.CurrentTransaction
                ?? throw new InvalidOperationException("数据库上下文事务状态在读取期间发生变化。");
            _transactionCoordinator.Attach(existingTransaction, ownsTransaction: false);
        }
        else
        {
            EnsureCompatibleConnections(transactionContexts[0], transactionContexts);
            _transactionCoordinator.Attach(transactionContexts[0].Database.BeginTransaction(), ownsTransaction: true);
            transactionCreatedByPool = true;
        }

        if (DbContextTransaction == null) return;

        try
        {
            ShareTransaction(DbContextTransaction.GetDbTransaction(), transactionContexts);
        }
        catch
        {
            DetachTransactionContexts(DbContextTransaction.GetDbTransaction());
            if (transactionCreatedByPool)
            {
                try
                {
                    DbContextTransaction.Rollback();
                }
                catch
                {
                    // 保留绑定失败异常，清理动作本身不应覆盖根因。
                }
            }

            _transactionCoordinator.Clear();

            throw;
        }

        AppRealization.TraceLog.Write(AppRealization.JSON.Serialize(new AppPrintInformation
        {
            Title = "数据库事务状态更新",
            Level = AppPrintLevel.Information,
            Content = "事务已开启",
            State = true,
            Type = AppPrintConstType.ORM_EXEC_TYPE
        }), Db.TraceLogTags);
    }

    /// <summary>
    /// <para>zh-cn:保存池中变更并提交本地事务；事务提交成功后才接受实体状态。</para>
    /// <para>en-us:Saves pending changes and commits the local transaction; entity states are accepted only after the commit succeeds.</para>
    /// </summary>
    /// <param name="withCloseAll"><para>zh-cn:是否自动关闭所有连接。</para><para>en-us:Whether to close all connections after completion.</para></param>
    public void CommitTransaction(bool withCloseAll = false)
    {
        var transaction = DbContextTransaction;
        var committed = false;
        try
        {
            var hasChangesCount = 0;
            // 如果事务为空，则执行完毕后关闭连接
            if (transaction == null)
            {
                // 没有显式事务时保持 AutoSaveChanges 的历史语义。
                hasChangesCount = SavePoolNow();
                if (withCloseAll) CloseAll();
                return;
            }

            // 事务内延迟 AcceptAllChanges，避免后续上下文保存失败后内存状态与数据库回滚结果不一致。
            var pendingContexts = GetPendingDbContexts();
            hasChangesCount = SavePoolForTransaction(pendingContexts);
            // 提交共享事务
            transaction.Commit();
            committed = true;
            foreach (var item in pendingContexts) item.Value.ChangeTracker.AcceptAllChanges();
            // 打印数据库关闭信息
            AppRealization.TraceLog.Write(AppRealization.JSON.Serialize(new AppPrintInformation
            {
                Title = "数据库事务状态更新",
                Level = AppPrintLevel.Information,
                Content = $"事务已完成提交,检测到{hasChangesCount}项DbContext变化",
                State = true,
                Type = AppPrintConstType.ORM_EXEC_TYPE
            }), Db.TraceLogTags);
        }
        catch
        {
            // 只有数据库提交尚未完成时才尝试回滚；提交后的实体状态异常不能伪装成数据库回滚。
            if (!committed) TryRollback(transaction);
            // 打印数据库关闭信息
            AppRealization.TraceLog.Write(AppRealization.JSON.Serialize(new AppPrintInformation
            {
                Title = "数据库事务状态更新",
                Level = AppPrintLevel.Error,
                Content = committed ? "数据库已提交，但实体状态接受失败" : "已触发事务回滚",
                State = true,
                Type = AppPrintConstType.ORM_EXEC_TYPE
            }), Db.TraceLogTags);
            throw;
        }
        finally
        {
            DetachTransactionContexts(transaction?.GetDbTransaction());
            _transactionCoordinator.Clear();
        }
        // 关闭所有连接
        if (withCloseAll) CloseAll();
    }

    /// <summary>
    /// <para>zh-cn:回滚当前本地事务并清理参与上下文的事务绑定。</para>
    /// <para>en-us:Rolls back the current local transaction and clears transaction bindings from participating contexts.</para>
    /// </summary>
    /// <param name="withCloseAll"><para>zh-cn:是否自动关闭所有连接。</para><para>en-us:Whether to close all connections after rollback.</para></param>
    public void RollbackTransaction(bool withCloseAll = false)
    {
        var transaction = DbContextTransaction;
        try
        {
            // 回滚事务
            if (transaction?.GetDbTransaction()?.Connection != null) transaction.Rollback();
        }
        finally
        {
            DetachTransactionContexts(transaction?.GetDbTransaction());
            _transactionCoordinator.Clear();
        }
        // 打印数据库关闭信息
        AppRealization.TraceLog.Write(AppRealization.JSON.Serialize(new AppPrintInformation
        {
            Title = "数据库事务状态更新",
            Level = AppPrintLevel.Error,
            Content = "已触发事务回滚",
            State = true,
            Type = AppPrintConstType.ORM_EXEC_TYPE
        }), Db.TraceLogTags);
        // 关闭所有连接
        if (withCloseAll) CloseAll();
    }

    /// <summary>
    /// 释放所有数据库上下文
    /// </summary>
    public void CloseAll()
    {
        if (!_dbContexts.Any()) return;

        foreach (var item in _dbContexts)
        {
            var conn = item.Value.Database.GetDbConnection();
            if (conn.State != ConnectionState.Open) continue;
            conn.Close();
            // 打印数据库关闭信息
            AppRealization.TraceLog.Write(AppRealization.JSON.Serialize(new AppPrintInformation
            {
                Title = "数据库链接已经关闭",
                Level = AppPrintLevel.Information,
                Content = $"链接字符串为: {conn.ConnectionString}",
                State = true,
                Type = AppPrintConstType.ORM_EXEC_TYPE
            }),Db.TraceLogTags);
        }
    }

    /// <summary>
    /// 设置数据库上下文共享事务
    /// </summary>
    /// <param name="transaction"></param>
    /// <param name="transactionContexts">参与当前本地事务的数据库上下文</param>
    private void ShareTransaction(DbTransaction transaction, IReadOnlyCollection<DbContext> transactionContexts)
    {
        var conflictingTransaction = transactionContexts.FirstOrDefault(u =>
            u.Database.CurrentTransaction != null
            && !ReferenceEquals(u.Database.CurrentTransaction.GetDbTransaction(), transaction));
        if (conflictingTransaction != null)
        {
            throw new InvalidOperationException("同一工作单元内检测到多个不同的本地事务，无法合并提交。");
        }

        var owner = transactionContexts.FirstOrDefault(u => ReferenceEquals(u.Database.CurrentTransaction?.GetDbTransaction(), transaction));
        owner ??= transactionContexts.FirstOrDefault(u => ReferenceEquals(u.Database.GetDbConnection(), transaction.Connection));
        if (owner == null) throw new InvalidOperationException("无法确定本地事务所属的数据库上下文。");

        if (!ReferenceEquals(owner.Database.GetDbConnection(), transaction.Connection))
        {
            throw new InvalidOperationException("本地事务与数据库上下文连接实例不一致，无法共享。");
        }

        EnsureCompatibleConnections(owner, transactionContexts);

        // 跳过已经拥有该事务的上下文，其余上下文绑定到同一个本地事务。
        var boundContexts = new List<DbContext>();
        try
        {
            foreach (var dbContext in transactionContexts)
            {
                if (ReferenceEquals(dbContext.Database.CurrentTransaction?.GetDbTransaction(), transaction)) continue;
                dbContext.Database.UseTransaction(transaction);
                boundContexts.Add(dbContext);
            }
        }
        catch
        {
            foreach (var dbContext in boundContexts) TryDetachTransaction(dbContext, transaction);
            throw;
        }
    }

    private List<KeyValuePair<Guid, DbContext>> GetPendingDbContexts()
    {
        return _dbContexts
            .Where(u => u.Value != null && u.Value.ChangeTracker.HasChanges())
            .OrderBy(u => u.Key)
            .ToList();
    }

    private int SavePoolForTransaction(IReadOnlyCollection<KeyValuePair<Guid, DbContext>> pendingContexts)
    {
        var savedContextCount = 0;
        foreach (var item in pendingContexts)
        {
            item.Value.SaveChanges(acceptAllChangesOnSuccess: false);
            savedContextCount++;
        }

        return savedContextCount;
    }

    private void DetachTransactionContexts(DbTransaction? transaction)
    {
        if (transaction == null) return;

        foreach (var dbContext in _dbContexts.Values)
        {
            if (ReferenceEquals(dbContext.Database.CurrentTransaction?.GetDbTransaction(), transaction))
                TryDetachTransaction(dbContext, transaction);
        }
    }

    private static void TryDetachTransaction(DbContext dbContext, DbTransaction transaction)
    {
        try
        {
            dbContext.Database.UseTransaction(null);
        }
        catch
        {
            // 事务已经提交或回滚时，部分 Provider 会拒绝再次解绑；不覆盖原始异常。
        }
    }

    private static void TryRollback(IDbContextTransaction? transaction)
    {
        try
        {
            if (transaction?.GetDbTransaction()?.Connection != null) transaction.Rollback();
        }
        catch
        {
            // 回滚失败不能覆盖触发提交失败的原始异常。
        }
    }

    private List<DbContext> GetUnitOfWorkContexts()
    {
        return _dbContexts.Values
            .Where(IsUnitOfWorkEnabled)
            .OrderBy(u => u.ContextId.InstanceId)
            .ToList();
    }

    private static bool IsUnitOfWorkEnabled(DbContext dbContext)
    {
        // AppDbContext 的配置属性位于泛型基类，使用反射兼容自定义 DbContext 实现。
        var property = dbContext.GetType().GetProperty("UseUnitOfWork", BindingFlags.Instance | BindingFlags.Public);
        return property?.GetValue(dbContext) is not bool enabled || enabled;
    }

    private static bool IsFailedAutoRollbackEnabled(DbContext dbContext)
    {
        var property = dbContext.GetType().GetProperty("FailedAutoRollback", BindingFlags.Instance | BindingFlags.Public);
        return property?.GetValue(dbContext) is true;
    }

    private static void EnsureCompatibleConnections(DbContext owner, IEnumerable<DbContext> contexts)
    {
        var ownerConnection = owner.Database.GetDbConnection();
        var ownerProvider = owner.Database.ProviderName;

        foreach (var dbContext in contexts)
        {
            var connection = dbContext.Database.GetDbConnection();
            if (!string.Equals(ownerProvider, dbContext.Database.ProviderName, StringComparison.Ordinal))
            {
                throw new InvalidOperationException("同一工作单元不能共享不同数据库 Provider 的事务。请拆分为独立的本地事务或使用应用层最终一致性方案。");
            }

            if (!ReferenceEquals(ownerConnection, connection))
            {
                throw new InvalidOperationException("同一工作单元只能共享同一个 DbConnection 实例的本地事务；不同物理数据库或不同连接不能伪装为一个事务。");
            }
        }
    }
}
