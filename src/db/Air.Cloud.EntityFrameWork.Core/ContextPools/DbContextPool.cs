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
/// <para>zh-cn:按请求作用域管理多个 Entity Framework Core 数据库上下文及其本地事务。</para>
/// <para>en-us:Manages multiple Entity Framework Core contexts and their local transactions within a request scope.</para>
/// </summary>
[IgnoreScanning]
public class DbContextPool : IDbContextPool, ILocalTransactionManager
{
    /// <summary>
    /// 线程安全的数据库上下文集合
    /// <para>zh-cn:按实例标识保存当前作用域中已解析的数据库上下文。</para>
    /// <para>en-us:Stores database contexts resolved in the current scope by instance identifier.</para>
    /// </summary>
    private readonly ConcurrentDictionary<Guid, DbContext> _dbContexts;

    /// <summary>
    /// 服务提供器
    /// <para>zh-cn:用于解析上下文和相关服务的作用域服务提供器。</para>
    /// <para>en-us:Scoped service provider used to resolve contexts and related services.</para>
    /// </summary>
    private readonly IServiceProvider _serviceProvider;

    /// <summary>
    /// 按物理连接实例隔离的本地事务生命周期协调器。
    /// <para>zh-cn:按物理连接实例关联本地事务的生命周期协调器。</para>
    /// <para>en-us:Coordinates local transaction lifetimes by physical connection instance.</para>
    /// </summary>
    private readonly Dictionary<DbConnection, LocalTransactionCoordinator> _transactionCoordinators;

    /// <summary>
    /// <para>zh-cn:创建上下文池并初始化上下文集合和事务协调器。</para>
    /// <para>en-us:Creates the context pool and initializes its context collection and transaction coordinators.</para>
    /// </summary>
    /// <param name="serviceProvider"><para>zh-cn:用于解析默认数据库上下文的服务提供器。</para><para>en-us:The service provider used to resolve the default database context.</para></param>
    public DbContextPool(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;

        _dbContexts = new ConcurrentDictionary<Guid, DbContext>();
        _transactionCoordinators = new Dictionary<DbConnection, LocalTransactionCoordinator>(ReferenceEqualityComparer.Instance);
    }

    /// <summary>
    /// <para>zh-cn:数据库上下文事务；多连接场景返回首个本地事务，仅用于兼容旧调用。</para>
    /// <para>en-us:The context transaction; in multi-connection scenarios, the first local transaction is returned for legacy callers only.</para>
    /// </summary>
    public IDbContextTransaction? DbContextTransaction => _transactionCoordinators.Values
        .Select(u => u.Transaction)
        .FirstOrDefault(u => u != null);

    /// <summary>
    /// <para>zh-cn:当前可观察到的首个本地事务，供公共事务管理器使用。</para>
    /// <para>en-us:The first observable local transaction exposed to the public transaction manager.</para>
    /// </summary>
    public IDbContextTransaction? CurrentTransaction => DbContextTransaction;

    /// <summary>
    /// <para>zh-cn:当前是否存在活动事务组。</para>
    /// <para>en-us:Whether an active local transaction group exists.</para>
    /// </summary>
    public bool IsActive => GetActiveCoordinators().Count > 0;

    /// <summary>
    /// <para>zh-cn:当前事务组的嵌套深度；多事务组必须保持同一深度，不一致时返回最大值用于诊断。</para>
    /// <para>en-us:The current transaction-group nesting depth; multiple groups must stay aligned, and the maximum is returned for diagnostics when they are not.</para>
    /// </summary>
    public int Depth => GetActiveCoordinators().Select(u => u.Depth).DefaultIfEmpty(0).Max();

    /// <summary>
    /// <para>zh-cn:当前事务是否已经标记为只能回滚。</para>
    /// <para>en-us:Whether the current transaction is marked rollback-only.</para>
    /// </summary>
    public bool IsRollbackOnly => _transactionCoordinators.Values.Any(u => u.IsRollbackOnly || u.IsAborted);

    /// <summary>
    /// <para>zh-cn:公共事务管理器的开始入口。</para>
    /// <para>en-us:Begins a scope through the public transaction manager.</para>
    /// </summary>
    public void Begin(bool ensureTransaction = false) => BeginTransaction(ensureTransaction);

    /// <summary>
    /// <para>zh-cn:创建一个自动回滚的本地事务作用域。</para>
    /// <para>en-us:Creates a local transaction scope that automatically rolls back when not completed.</para>
    /// </summary>
    public ILocalTransactionScope BeginScope(bool ensureTransaction = false, bool closeAllOnCompletion = false)
        => new LocalTransactionScope(this, ensureTransaction, closeAllOnCompletion);

    /// <summary>
    /// <para>zh-cn:公共事务管理器的提交入口。</para>
    /// <para>en-us:Commits a scope through the public transaction manager.</para>
    /// </summary>
    public void Commit(bool withCloseAll = false) => CommitTransaction(withCloseAll);

    /// <summary>
    /// <para>zh-cn:公共事务管理器的回滚入口。</para>
    /// <para>en-us:Rolls back a scope through the public transaction manager.</para>
    /// </summary>
    public void Rollback(bool withCloseAll = false) => RollbackTransaction(withCloseAll);

    /// <summary>
    /// <para>zh-cn:获取当前作用域中已经登记的所有数据库上下文。</para>
    /// <para>en-us:Gets all database contexts registered in the current scope.</para>
    /// </summary>
    /// <returns><para>zh-cn:按上下文实例标识索引的线程安全集合。</para><para>en-us:A thread-safe collection indexed by context instance identifier.</para></returns>
    public ConcurrentDictionary<Guid, DbContext> GetDbContexts()
    {
        return _dbContexts;
    }

    /// <summary>
    /// <para>zh-cn:登记一个关系型数据库上下文，并在保存失败且上下文启用自动回滚时回滚当前本地事务。</para>
    /// <para>en-us:Registers a relational DbContext and rolls back the current local transaction on save failure when that context enables automatic rollback.</para>
    /// </summary>
    /// <param name="dbContext"><para>zh-cn:要登记到当前池中的数据库上下文。</para><para>en-us:The database context to register in the current pool.</para></param>
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
            var transaction = currentTransaction.GetDbTransaction();
            if (transaction != null && TryGetCoordinator(transaction, out var coordinator))
            {
                DetachTransactionContexts(transaction);
                coordinator.Abort();
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
    /// <para>zh-cn:异步登记数据库上下文；当前实现不执行额外异步 I/O。</para>
    /// <para>en-us:Asynchronously registers a database context; the current implementation performs no additional asynchronous I/O.</para>
    /// </summary>
    /// <param name="dbContext"><para>zh-cn:要登记到当前池中的数据库上下文。</para><para>en-us:The database context to register in the current pool.</para></param>
    /// <returns><para>zh-cn:表示登记操作完成的任务。</para><para>en-us:A task representing completion of the registration.</para></returns>
    public Task AddToPoolAsync(DbContext dbContext)
    {
        AddToPool(dbContext);
        return Task.CompletedTask;
    }

    /// <summary>
    /// <para>zh-cn:顺序保存池中所有有待处理变更的数据库上下文。</para>
    /// <para>en-us:Sequentially saves all contexts with pending changes in the pool.</para>
    /// </summary>
    /// <returns><para>zh-cn:成功执行保存的上下文数量。</para><para>en-us:The number of contexts successfully saved.</para></returns>
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
    /// <para>zh-cn:按指定策略顺序保存池中所有有待处理变更的上下文。</para>
    /// <para>en-us:Sequentially saves all contexts with pending changes using the specified policy.</para>
    /// </summary>
    /// <param name="acceptAllChangesOnSuccess"><para>zh-cn:保存成功后是否调用 AcceptAllChanges。</para><para>en-us:Whether to call AcceptAllChanges after a successful save.</para></param>
    /// <returns><para>zh-cn:成功执行保存的上下文数量。</para><para>en-us:The number of contexts successfully saved.</para></returns>
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
    /// <para>zh-cn:异步顺序保存池中所有有待处理变更的上下文。</para>
    /// <para>en-us:Asynchronously and sequentially saves all contexts with pending changes in the pool.</para>
    /// </summary>
    /// <param name="cancellationToken"><para>zh-cn:用于取消保存操作的令牌。</para><para>en-us:The token used to cancel the save operation.</para></param>
    /// <returns><para>zh-cn:成功执行保存的上下文数量。</para><para>en-us:The number of contexts successfully saved.</para></returns>
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
    /// <para>zh-cn:按指定实体状态接受策略异步保存池中所有有待处理变更的上下文。</para>
    /// <para>en-us:Asynchronously saves all contexts with pending changes using the specified entity-state acceptance policy.</para>
    /// </summary>
    /// <param name="acceptAllChangesOnSuccess"><para>zh-cn:保存成功后是否调用 AcceptAllChanges。</para><para>en-us:Whether to call AcceptAllChanges after a successful save.</para></param>
    /// <param name="cancellationToken"><para>zh-cn:用于取消保存操作的令牌。</para><para>en-us:The token used to cancel the save operation.</para></param>
    /// <returns><para>zh-cn:成功执行保存的上下文数量。</para><para>en-us:The number of contexts successfully saved.</para></returns>
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
    /// <para>zh-cn:按物理连接实例打开或接管本地事务；同一连接的上下文共享事务，不同连接分别维护本地事务。</para>
    /// <para>en-us:Starts or adopts local transactions per physical connection; contexts sharing one connection share a transaction, while different connections keep independent local transactions.</para>
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

        var groups = GroupContextsByConnection(transactionContexts);
        var activeDepths = GetActiveCoordinators().Select(u => u.Depth).Distinct().ToArray();
        if (activeDepths.Length > 1)
            throw new InvalidOperationException("多个本地事务组的嵌套工作单元深度不一致，无法加入新的事务组。");
        var targetDepth = activeDepths.Length == 0 ? 1 : activeDepths[0] + 1;
        try
        {
            foreach (var group in groups)
            {
                var coordinator = GetOrCreateCoordinator(group.Connection);
                if (coordinator.IsAborted)
                    throw new InvalidOperationException("当前本地事务已因保存失败而中止，不能在同一工作单元中重新开启事务。");
                var transactionContext = group.Contexts.FirstOrDefault(u => u.Database.CurrentTransaction != null);
                if (coordinator.Transaction != null)
                {
                    ShareTransaction(coordinator.Transaction.GetDbTransaction(), group.Contexts);
                    coordinator.EnterScope();
                    continue;
                }

                if (transactionContext != null)
                {
                    var existingTransaction = transactionContext.Database.CurrentTransaction
                        ?? throw new InvalidOperationException("数据库上下文事务状态在读取期间发生变化。");
                    coordinator.Attach(existingTransaction, ownsTransaction: false);
                    coordinator.AlignScopeDepth(targetDepth);
                }
                else
                {
                    coordinator.Attach(group.Contexts[0].Database.BeginTransaction(), ownsTransaction: true);
                    coordinator.AlignScopeDepth(targetDepth);
                }

                var transaction = coordinator.Transaction?.GetDbTransaction()
                    ?? throw new InvalidOperationException("数据库上下文未能建立本地事务。");
                ShareTransaction(transaction, group.Contexts);
            }
        }
        catch
        {
            // 加入新事务组失败时，当前工作单元中的已有事务也不能继续悬挂；全部回滚并清理。
            foreach (var coordinator in GetActiveCoordinators())
            {
                var transaction = coordinator.Transaction?.GetDbTransaction();
                TryRollback(coordinator.Transaction);
                DetachTransactionContexts(transaction);
            }
            ClearTransactionCoordinators();

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
    /// <para>zh-cn:保存池中变更并按事务组顺序提交本地事务；每个事务提交成功后才接受对应实体状态。多个连接组之间不提供原子性。</para>
    /// <para>en-us:Saves pending changes and commits local transactions in group order; entity states are accepted only after their group commits. No atomicity is provided across connection groups.</para>
    /// </summary>
    /// <param name="withCloseAll"><para>zh-cn:是否自动关闭所有连接。</para><para>en-us:Whether to close all connections after completion.</para></param>
    public void CommitTransaction(bool withCloseAll = false)
    {
        var coordinators = GetActiveCoordinators();
        var committed = new HashSet<DbTransaction>(ReferenceEqualityComparer.Instance);
        var shouldClear = false;
        try
        {
            var hasChangesCount = 0;
            if (coordinators.Count == 0 && _transactionCoordinators.Values.Any(u => u.IsRollbackOnly || u.IsAborted))
                throw new InvalidOperationException("当前本地事务已经回滚，不能在外层工作单元中继续提交。");

            // 如果事务为空，则执行完毕后关闭连接
            if (coordinators.Count == 0)
            {
                // 没有显式事务时保持 AutoSaveChanges 的历史语义。
                hasChangesCount = SavePoolNow();
                if (withCloseAll) CloseAll();
                return;
            }

            if (coordinators.Any(u => u.IsRollbackOnly || u.IsAborted))
            {
                foreach (var coordinator in coordinators) TryRollback(coordinator.Transaction);
                throw new InvalidOperationException("当前本地事务已标记为回滚，外层工作单元不能提交。");
            }

            // 所有事务组必须处于同一嵌套层级，避免一组提前提交而另一组仍在内层。
            var depths = coordinators.Select(u => u.Depth).Distinct().ToArray();
            if (depths.Length != 1)
                throw new InvalidOperationException("多个本地事务组的嵌套工作单元深度不一致，无法安全提交。");

            // 嵌套工作单元只完成自身层级，真正的保存和提交延迟到最外层。
            if (!coordinators.All(u => u.CompleteScope()))
            {
                return;
            }
            shouldClear = true;

            // 每个物理连接组独立保存并提交；跨组不提供原子性，应由业务使用 Outbox/补偿保证最终一致。
            foreach (var coordinator in coordinators)
            {
                var transaction = coordinator.Transaction?.GetDbTransaction()
                    ?? throw new InvalidOperationException("本地事务状态在提交期间丢失。");
                var pendingContexts = GetPendingDbContexts(transaction.Connection);
                hasChangesCount += SavePoolForTransaction(pendingContexts, coordinator);
                coordinator.Transaction!.Commit();
                committed.Add(transaction);
                foreach (var item in pendingContexts) item.Value.ChangeTracker.AcceptAllChanges();
            }

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
            shouldClear = true;
            // 已提交的组无法再回滚；其余组仍尝试回滚，并将部分提交事实留给上层补偿。
            foreach (var coordinator in coordinators)
            {
                var transaction = coordinator.Transaction?.GetDbTransaction();
                if (transaction != null && !committed.Contains(transaction)) TryRollback(coordinator.Transaction);
            }
            // 打印数据库关闭信息
            AppRealization.TraceLog.Write(AppRealization.JSON.Serialize(new AppPrintInformation
            {
                Title = "数据库事务状态更新",
                Level = AppPrintLevel.Error,
                Content = committed.Count > 0 ? "部分本地事务已提交，其余事务已回滚或提交失败" : "已触发事务回滚",
                State = true,
                Type = AppPrintConstType.ORM_EXEC_TYPE
            }), Db.TraceLogTags);
            throw;
        }
        finally
        {
            if (shouldClear)
            {
                foreach (var coordinator in coordinators)
                {
                    var transaction = coordinator.Transaction?.GetDbTransaction();
                    DetachTransactionContexts(transaction);
                }
                ClearTransactionCoordinators();
            }
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
        var coordinators = GetActiveCoordinators();

        // EF Core 本地事务没有跨上下文嵌套事务；内层回滚标记整个工作单元，交由最外层实际回滚。
        if (coordinators.Count > 0 && coordinators.All(u => u.Depth > 1))
        {
            foreach (var coordinator in coordinators)
            {
                coordinator.MarkRollbackOnly();
                coordinator.CompleteScope();
            }
            return;
        }

        try
        {
            foreach (var coordinator in coordinators)
            {
                // 回滚事务
                if (coordinator.Transaction?.GetDbTransaction()?.Connection != null) coordinator.Transaction.Rollback();
            }
        }
        finally
        {
            foreach (var coordinator in coordinators)
                DetachTransactionContexts(coordinator.Transaction?.GetDbTransaction());
            ClearTransactionCoordinators();
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
    /// <para>zh-cn:关闭并清理当前池中的所有数据库上下文及事务状态。</para>
    /// <para>en-us:Closes and cleans up all database contexts and transaction state in the current pool.</para>
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
    /// <para>zh-cn:将同一物理连接上的数据库上下文绑定到共享事务。</para>
    /// <para>en-us:Binds database contexts on the same physical connection to a shared transaction.</para>
    /// </summary>
    /// <param name="transaction"><para>zh-cn:要绑定到上下文的数据库事务。</para><para>en-us:The database transaction to bind to the contexts.</para></param>
    /// <param name="transactionContexts"><para>zh-cn:参与当前本地事务的数据库上下文集合。</para><para>en-us:The database contexts participating in the current local transaction.</para></param>
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

    private List<KeyValuePair<Guid, DbContext>> GetPendingDbContexts(DbConnection? connection = null)
    {
        return _dbContexts
            .Where(u => u.Value != null
                && u.Value.ChangeTracker.HasChanges()
                && (connection == null || ReferenceEquals(u.Value.Database.GetDbConnection(), connection)))
            .OrderBy(u => u.Key)
            .ToList();
    }

    private int SavePoolForTransaction(IReadOnlyCollection<KeyValuePair<Guid, DbContext>> pendingContexts, LocalTransactionCoordinator coordinator)
    {
        var savedContextCount = 0;
        foreach (var item in pendingContexts)
        {
            if (coordinator.IsAborted)
                throw new InvalidOperationException("当前本地事务已因保存失败而中止，不能继续保存其他上下文。");

            item.Value.SaveChanges(acceptAllChangesOnSuccess: false);
            savedContextCount++;
        }

        return savedContextCount;
    }

    private LocalTransactionCoordinator GetOrCreateCoordinator(DbConnection connection)
    {
        if (!_transactionCoordinators.TryGetValue(connection, out var coordinator))
        {
            coordinator = new LocalTransactionCoordinator();
            _transactionCoordinators.Add(connection, coordinator);
        }

        return coordinator;
    }

    private bool TryGetCoordinator(DbTransaction transaction, out LocalTransactionCoordinator coordinator)
    {
        if (transaction.Connection != null
            && _transactionCoordinators.TryGetValue(transaction.Connection, out coordinator!)
            && ReferenceEquals(coordinator.Transaction?.GetDbTransaction(), transaction))
            return true;

        coordinator = null!;
        return false;
    }

    private List<LocalTransactionCoordinator> GetActiveCoordinators() => _transactionCoordinators.Values
        .Where(u => u.IsActive)
        .ToList();

    private void ClearTransactionCoordinators()
    {
        foreach (var coordinator in _transactionCoordinators.Values) coordinator.Clear();
        _transactionCoordinators.Clear();
    }

    private static List<TransactionContextGroup> GroupContextsByConnection(IReadOnlyCollection<DbContext> contexts)
    {
        var groups = new Dictionary<DbConnection, TransactionContextGroup>(ReferenceEqualityComparer.Instance);
        foreach (var context in contexts)
        {
            var connection = context.Database.GetDbConnection();
            if (!groups.TryGetValue(connection, out var group))
            {
                group = new TransactionContextGroup(connection);
                groups.Add(connection, group);
            }

            group.Contexts.Add(context);
        }

        return groups.Values.ToList();
    }

    private sealed class TransactionContextGroup
    {
        /// <summary><para>zh-cn:创建按物理连接分组的事务上下文集合。</para><para>en-us:Creates a transaction-context group for a physical connection.</para></summary>
        /// <param name="connection"><para>zh-cn:该分组对应的物理数据库连接。</para><para>en-us:The physical database connection represented by the group.</para></param>
        public TransactionContextGroup(DbConnection connection) => Connection = connection;

        /// <summary><para>zh-cn:该事务分组使用的物理数据库连接。</para><para>en-us:The physical database connection used by this transaction group.</para></summary>
        public DbConnection Connection { get; }

        /// <summary><para>zh-cn:绑定到该物理连接的数据库上下文集合。</para><para>en-us:The database contexts bound to this physical connection.</para></summary>
        public List<DbContext> Contexts { get; } = new();
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
