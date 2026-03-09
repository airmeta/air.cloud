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
/// EFCore 工作单元实现
/// </summary>
[IgnoreScanning]
public sealed class EFCoreUnitOfWork : IUnitOfWork
{
    /// <summary>
    /// 数据库上下文池
    /// </summary>
    private readonly IDbContextPool _dbContextPool;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="dbContextPool"></param>
    public EFCoreUnitOfWork(IDbContextPool dbContextPool)
    {
        _dbContextPool = dbContextPool;
    }

    /// <summary>
    /// 开启工作单元处理
    /// </summary>
    /// <param name="context"></param>
    /// <param name="unitOfWork"></param>
    /// <exception cref="NotImplementedException"></exception>
    public void BeginTransaction(ActionExecutingContext context, UnitOfWorkAttribute unitOfWork)
    {
        // 开启事务
        _dbContextPool.BeginTransaction(unitOfWork.EnsureTransaction);
    }

    /// <summary>
    /// 提交工作单元处理
    /// </summary>
    /// <param name="resultContext"></param>
    /// <param name="unitOfWork"></param>
    /// <exception cref="NotImplementedException"></exception>
    public void CommitTransaction(ActionExecutedContext resultContext, UnitOfWorkAttribute unitOfWork)
    {
        // 提交事务
        _dbContextPool.CommitTransaction();
    }

    /// <summary>
    /// 回滚工作单元处理
    /// </summary>
    /// <param name="resultContext"></param>
    /// <param name="unitOfWork"></param>
    /// <exception cref="NotImplementedException"></exception>

    public void RollbackTransaction(ActionExecutedContext resultContext, UnitOfWorkAttribute unitOfWork)
    {
        // 回滚事务
        _dbContextPool.RollbackTransaction();
    }

    /// <summary>
    /// 执行完毕（无论成功失败）
    /// </summary>
    /// <param name="context"></param>
    /// <param name="resultContext"></param>
    /// <exception cref="NotImplementedException"></exception>
    public void OnCompleted(ActionExecutingContext context, ActionExecutedContext resultContext)
    {
        // 手动关闭
        _dbContextPool.CloseAll();
    }
}