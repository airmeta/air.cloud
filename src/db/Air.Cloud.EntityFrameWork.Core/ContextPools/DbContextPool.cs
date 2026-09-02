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
    /// 登记错误的数据库上下文
    /// </summary>
    private readonly ConcurrentDictionary<Guid, DbContext> _failedDbContexts;

    /// <summary>
    /// 服务提供器
    /// </summary>
    private readonly IServiceProvider _serviceProvider;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="serviceProvider"></param>
    public DbContextPool(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;

        _dbContexts = new ConcurrentDictionary<Guid, DbContext>();
        _failedDbContexts = new ConcurrentDictionary<Guid, DbContext>();
    }

    /// <summary>
    /// 数据库上下文事务
    /// </summary>
    public IDbContextTransaction? DbContextTransaction { get; private set; }

    /// <summary>
    /// 获取所有数据库上下文
    /// </summary>
    /// <returns></returns>
    public ConcurrentDictionary<Guid, DbContext> GetDbContexts()
    {
        return _dbContexts;
    }

    /// <summary>
    /// 保存数据库上下文
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
            // 排除已经存在的数据库上下文
            if (!_failedDbContexts.TryAdd(instanceId, dbContext)) return;

            // 当前事务
            if (s is not DbContext context) return;
            dynamic dynamicContext = context;
            var database = context.Database;
            var currentTransaction = database.CurrentTransaction;

            // 只有事务不等于空且支持自动回滚
            if (!(currentTransaction != null && dynamicContext.FailedAutoRollback == true)) return;
            // 获取数据库连接信息
            var connection = database.GetDbConnection();
            // 回滚事务
            currentTransaction?.Rollback();
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
    /// 打开事务
    /// </summary>
    /// <param name="ensureTransaction"></param>
    /// <returns></returns>
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
            DbContextTransaction = transactionDbContext.Database.CurrentTransaction;
        }
        else
        {
            EnsureCompatibleConnections(transactionContexts[0], transactionContexts);
            DbContextTransaction = transactionContexts[0].Database.BeginTransaction();
            transactionCreatedByPool = true;
        }

        if (DbContextTransaction == null) return;

        try
        {
            ShareTransaction(DbContextTransaction.GetDbTransaction(), transactionContexts);
        }
        catch
        {
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
                finally
                {
                    DbContextTransaction.Dispose();
                    DbContextTransaction = null;
                }
            }

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
    /// 提交事务
    /// </summary>
    /// <param name="withCloseAll">是否自动关闭所有连接</param>
    public void CommitTransaction(bool withCloseAll = false)
    {
        var transaction = DbContextTransaction;
        try
        {
            // 将所有数据库上下文修改 SaveChanges();，这里另外判断是否需要手动提交。
            var hasChangesCount = SavePoolNow();
            // 如果事务为空，则执行完毕后关闭连接
            if (transaction == null)
            {
                if (withCloseAll) CloseAll();
                return;
            }
            // 提交共享事务
            transaction.Commit();
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
            // 回滚事务
            if (transaction?.GetDbTransaction()?.Connection != null) transaction.Rollback();
            // 打印数据库关闭信息
            AppRealization.TraceLog.Write(AppRealization.JSON.Serialize(new AppPrintInformation
            {
                Title = "数据库事务状态更新",
                Level = AppPrintLevel.Error,
                Content = "已触发事务回滚",
                State = true,
                Type = AppPrintConstType.ORM_EXEC_TYPE
            }), Db.TraceLogTags);
            throw;
        }
        finally
        {
            DbContextTransaction = null;
            transaction?.Dispose();
        }
        // 关闭所有连接
        if (withCloseAll) CloseAll();
    }

    /// <summary>
    /// 回滚事务
    /// </summary>
    /// <param name="withCloseAll">是否自动关闭所有连接</param>
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
            DbContextTransaction = null;
            transaction?.Dispose();
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
        foreach (var dbContext in transactionContexts)
        {
            if (ReferenceEquals(dbContext.Database.CurrentTransaction?.GetDbTransaction(), transaction)) continue;
            dbContext.Database.UseTransaction(transaction);
        }
    }

    private List<KeyValuePair<Guid, DbContext>> GetPendingDbContexts()
    {
        return _dbContexts
            .Where(u => u.Value != null && u.Value.ChangeTracker.HasChanges() && !_failedDbContexts.ContainsKey(u.Key))
            .OrderBy(u => u.Key)
            .ToList();
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
