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
using Air.Cloud.EntityFrameWork.Core.UnitOfWork.Attributes;

using Microsoft.AspNetCore.Mvc.Filters;

namespace Air.Cloud.EntityFrameWork.Core.UnitOfWork;

/// <summary>
/// 工作单元依赖接口
/// </summary>
public interface IUnitOfWork
{
    /// <summary>
    /// 开启工作单元处理
    /// </summary>
    /// <param name="context"></param>
    /// <param name="unitOfWork"></param>
    void BeginTransaction(ActionExecutingContext context, UnitOfWorkAttribute unitOfWork);

    /// <summary>
    /// 提交工作单元处理
    /// </summary>
    /// <param name="resultContext"></param>
    /// <param name="unitOfWork"></param>
    void CommitTransaction(ActionExecutedContext resultContext, UnitOfWorkAttribute unitOfWork);

    /// <summary>
    /// 回滚工作单元处理
    /// </summary>
    /// <param name="resultContext"></param>
    /// <param name="unitOfWork"></param>
    void RollbackTransaction(ActionExecutedContext resultContext, UnitOfWorkAttribute unitOfWork);

    /// <summary>
    /// 执行完毕（无论成功失败）
    /// </summary>
    /// <param name="context"></param>
    /// <param name="resultContext"></param>
    void OnCompleted(ActionExecutingContext context, ActionExecutedContext resultContext);
}