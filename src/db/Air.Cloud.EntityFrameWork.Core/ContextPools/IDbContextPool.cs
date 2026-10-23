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

using System.Collections.Concurrent;

namespace Air.Cloud.EntityFrameWork.Core.ContextPools;

/// <summary>
/// <para>zh-cn:数据库上下文池抽象，负责登记上下文、保存变更和管理本地事务。</para>
/// <para>en-us:Database context pool abstraction for registering contexts, saving changes, and managing local transactions.</para>
/// </summary>
public interface IDbContextPool
{
    /// <summary>
    /// <para>zh-cn:当前可观察到的数据库事务；多连接时仅代表首个本地事务。</para>
    /// <para>en-us:The currently observable database transaction; with multiple connections it represents only the first local transaction.</para>
    /// </summary>
    IDbContextTransaction? DbContextTransaction { get; }

    /// <summary>
    /// <para>zh-cn:获取池中已登记的所有数据库上下文。</para>
    /// <para>en-us:Gets all database contexts registered in the pool.</para>
    /// </summary>
    /// <returns><para>zh-cn:按上下文实例标识索引的线程安全集合。</para><para>en-us:A thread-safe collection indexed by context instance identifier.</para></returns>
    ConcurrentDictionary<Guid, DbContext> GetDbContexts();

    /// <summary>
    /// <para>zh-cn:登记一个数据库上下文并参与当前池的生命周期管理。</para>
    /// <para>en-us:Registers a database context for lifecycle management by the current pool.</para>
    /// </summary>
    /// <param name="dbContext"><para>zh-cn:要登记的数据库上下文。</para><para>en-us:The database context to register.</para></param>
    void AddToPool(DbContext dbContext);

    /// <summary>
    /// <para>zh-cn:异步登记一个数据库上下文。</para>
    /// <para>en-us:Asynchronously registers a database context.</para>
    /// </summary>
    /// <param name="dbContext"><para>zh-cn:要登记的数据库上下文。</para><para>en-us:The database context to register.</para></param>
    /// <returns><para>zh-cn:表示登记操作完成的任务。</para><para>en-us:A task representing completion of the registration.</para></returns>
    Task AddToPoolAsync(DbContext dbContext);

    /// <summary>
    /// <para>zh-cn:保存池中所有有待处理变更的数据库上下文。</para>
    /// <para>en-us:Saves all database contexts with pending changes in the pool.</para>
    /// </summary>
    /// <returns><para>zh-cn:成功保存的上下文数量。</para><para>en-us:The number of contexts successfully saved.</para></returns>
    int SavePoolNow();

    /// <summary>
    /// <para>zh-cn:按指定策略保存池中所有有待处理变更的上下文。</para>
    /// <para>en-us:Saves all contexts with pending changes using the specified policy.</para>
    /// </summary>
    /// <param name="acceptAllChangesOnSuccess"><para>zh-cn:保存成功后是否接受所有实体状态变更。</para><para>en-us:Whether to accept all entity-state changes after a successful save.</para></param>
    /// <returns><para>zh-cn:成功保存的上下文数量。</para><para>en-us:The number of contexts successfully saved.</para></returns>
    int SavePoolNow(bool acceptAllChangesOnSuccess);

    /// <summary>
    /// <para>zh-cn:异步保存池中所有有待处理变更的上下文。</para>
    /// <para>en-us:Asynchronously saves all contexts with pending changes in the pool.</para>
    /// </summary>
    /// <param name="cancellationToken"><para>zh-cn:用于取消保存操作的令牌。</para><para>en-us:The token used to cancel the save operation.</para></param>
    /// <returns><para>zh-cn:成功保存的上下文数量。</para><para>en-us:The number of contexts successfully saved.</para></returns>
    Task<int> SavePoolNowAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// <para>zh-cn:按指定策略异步保存池中所有有待处理变更的上下文。</para>
    /// <para>en-us:Asynchronously saves all contexts with pending changes using the specified policy.</para>
    /// </summary>
    /// <param name="acceptAllChangesOnSuccess"><para>zh-cn:保存成功后是否接受所有实体状态变更。</para><para>en-us:Whether to accept all entity-state changes after a successful save.</para></param>
    /// <param name="cancellationToken"><para>zh-cn:用于取消保存操作的令牌。</para><para>en-us:The token used to cancel the save operation.</para></param>
    /// <returns><para>zh-cn:成功保存的上下文数量。</para><para>en-us:The number of contexts successfully saved.</para></returns>
    Task<int> SavePoolNowAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default);

    /// <summary>
    /// <para>zh-cn:打开或加入当前本地事务。</para>
    /// <para>en-us:Opens or joins the current local transaction.</para>
    /// </summary>
    /// <param name="ensureTransaction"><para>zh-cn:池为空时是否解析默认上下文并确保事务存在。</para><para>en-us:Whether to resolve the default context and ensure a transaction when the pool is empty.</para></param>
    /// <returns><para>zh-cn:操作结果。</para><para>en-us:The operation result.</para></returns>
    void BeginTransaction(bool ensureTransaction = false);

    /// <summary>
    /// <para>zh-cn:保存变更并提交当前本地事务。</para>
    /// <para>en-us:Saves changes and commits the current local transaction.</para>
    /// </summary>
    /// <param name="withCloseAll"><para>zh-cn:是否自动关闭所有连接</para><para>en-us:The withCloseAll value.</para></param>
    void CommitTransaction(bool withCloseAll = false);

    /// <summary>
    /// <para>zh-cn:回滚当前本地事务并清理事务绑定。</para>
    /// <para>en-us:Rolls back the current local transaction and clears transaction bindings.</para>
    /// </summary>
    /// <param name="withCloseAll"><para>zh-cn:是否自动关闭所有连接</para><para>en-us:The withCloseAll value.</para></param>
    void RollbackTransaction(bool withCloseAll = false);

    /// <summary>
    /// <para>zh-cn:关闭并清理池中的所有数据库连接。</para>
    /// <para>en-us:Closes and cleans up all database connections in the pool.</para>
    /// </summary>
    void CloseAll();
}
