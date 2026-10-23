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
using Air.Cloud.EntityFrameWork.Core.UnitOfWork.Attributes;

using Microsoft.AspNetCore.Mvc.Filters;

namespace Air.Cloud.EntityFrameWork.Core.UnitOfWork;

/// <summary>
/// <para>zh-cn:基于 EF Core 上下文池实现的工作单元生命周期协调器。</para>
/// <para>en-us:Work-unit lifecycle coordinator built on the EF Core context pool.</para>
/// </summary>
[IgnoreScanning]
public sealed class EFCoreUnitOfWork : IUnitOfWork
{
    /// <summary>
    /// 数据库上下文池
    /// <para>zh-cn:该成员的数据库访问行为。</para>
    /// <para>en-us:Database access behavior provided by this member.</para>
    /// </summary>
    private readonly IDbContextPool _dbContextPool;

    /// <summary>
    /// <para>zh-cn:创建 EF Core 工作单元。</para>
    /// <para>en-us:Creates an EF Core work unit.</para>
    /// </summary>
    /// <param name="dbContextPool"><para>zh-cn:当前作用域的数据库上下文池。</para><para>en-us:The database context pool for the current scope.</para></param>
    public EFCoreUnitOfWork(IDbContextPool dbContextPool)
    {
        _dbContextPool = dbContextPool;
    }

    /// <summary>
    /// <para>zh-cn:根据工作单元配置开始事务。</para>
    /// <para>en-us:Begins a transaction according to the work-unit configuration.</para>
    /// </summary>
    /// <param name="context"><para>zh-cn:当前 MVC 动作上下文。</para><para>en-us:The current MVC action context.</para></param>
    /// <param name="unitOfWork"><para>zh-cn:工作单元配置。</para><para>en-us:The work-unit configuration.</para></param>
    /// <exception cref="NotImplementedException"><para>zh-cn:参数无效或当前操作状态不允许时抛出。</para><para>en-us:Thrown when an argument is invalid or the current operation state is not allowed.</para></exception>
    public void BeginTransaction(ActionExecutingContext context, UnitOfWorkAttribute unitOfWork)
    {
        // 开启事务
        _dbContextPool.BeginTransaction(unitOfWork.EnsureTransaction);
    }

    /// <summary>
    /// <para>zh-cn:保存并提交工作单元事务。</para>
    /// <para>en-us:Saves changes and commits the work-unit transaction.</para>
    /// </summary>
    /// <param name="resultContext"><para>zh-cn:动作执行结果上下文。</para><para>en-us:The action execution result context.</para></param>
    /// <param name="unitOfWork"><para>zh-cn:工作单元配置。</para><para>en-us:The work-unit configuration.</para></param>
    /// <exception cref="NotImplementedException"><para>zh-cn:参数无效或当前操作状态不允许时抛出。</para><para>en-us:Thrown when an argument is invalid or the current operation state is not allowed.</para></exception>
    public void CommitTransaction(ActionExecutedContext resultContext, UnitOfWorkAttribute unitOfWork)
    {
        // 提交事务
        _dbContextPool.CommitTransaction();
    }

    /// <summary>
    /// <para>zh-cn:回滚工作单元事务。</para>
    /// <para>en-us:Rolls back the work-unit transaction.</para>
    /// </summary>
    /// <param name="resultContext"><para>zh-cn:动作执行结果上下文。</para><para>en-us:The action execution result context.</para></param>
    /// <param name="unitOfWork"><para>zh-cn:工作单元配置。</para><para>en-us:The work-unit configuration.</para></param>
    /// <exception cref="NotImplementedException"><para>zh-cn:参数无效或当前操作状态不允许时抛出。</para><para>en-us:Thrown when an argument is invalid or the current operation state is not allowed.</para></exception>

    public void RollbackTransaction(ActionExecutedContext resultContext, UnitOfWorkAttribute unitOfWork)
    {
        // 回滚事务
        _dbContextPool.RollbackTransaction();
    }

    /// <summary>
    /// <para>zh-cn:在工作单元完成后执行统一清理。</para>
    /// <para>en-us:Performs unified cleanup after the work unit completes.</para>
    /// </summary>
    /// <param name="context"><para>zh-cn:当前 MVC 动作上下文。</para><para>en-us:The current MVC action context.</para></param>
    /// <param name="resultContext"><para>zh-cn:动作执行结果上下文。</para><para>en-us:The action execution result context.</para></param>
    /// <exception cref="NotImplementedException"><para>zh-cn:参数无效或当前操作状态不允许时抛出。</para><para>en-us:Thrown when an argument is invalid or the current operation state is not allowed.</para></exception>
    public void OnCompleted(ActionExecutingContext context, ActionExecutedContext resultContext)
    {
        // 手动关闭
        _dbContextPool.CloseAll();
    }
}
