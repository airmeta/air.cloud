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
/// <para>zh-cn:工作单元生命周期抽象，协调请求内事务的开始、提交、回滚和清理。</para>
/// <para>en-us:Work-unit lifecycle abstraction coordinating transaction start, commit, rollback, and cleanup within a request.</para>
/// </summary>
public interface IUnitOfWork
{
    /// <summary>
    /// <para>zh-cn:开始当前请求的工作单元事务。</para>
    /// <para>en-us:Begins the work-unit transaction for the current request.</para>
    /// </summary>
    /// <param name="context"><para>zh-cn:当前 MVC 动作执行上下文。</para><para>en-us:The current MVC action execution context.</para></param>
    /// <param name="unitOfWork"><para>zh-cn:工作单元配置特性。</para><para>en-us:The work-unit configuration attribute.</para></param>
    void BeginTransaction(ActionExecutingContext context, UnitOfWorkAttribute unitOfWork);

    /// <summary>
    /// <para>zh-cn:在动作成功后提交当前工作单元事务。</para>
    /// <para>en-us:Commits the current work-unit transaction after successful action execution.</para>
    /// </summary>
    /// <param name="resultContext"><para>zh-cn:当前动作执行结果上下文。</para><para>en-us:The current action execution result context.</para></param>
    /// <param name="unitOfWork"><para>zh-cn:工作单元配置特性。</para><para>en-us:The work-unit configuration attribute.</para></param>
    void CommitTransaction(ActionExecutedContext resultContext, UnitOfWorkAttribute unitOfWork);

    /// <summary>
    /// <para>zh-cn:在动作失败后回滚当前工作单元事务。</para>
    /// <para>en-us:Rolls back the current work-unit transaction after action failure.</para>
    /// </summary>
    /// <param name="resultContext"><para>zh-cn:当前动作执行结果上下文。</para><para>en-us:The current action execution result context.</para></param>
    /// <param name="unitOfWork"><para>zh-cn:工作单元配置特性。</para><para>en-us:The work-unit configuration attribute.</para></param>
    void RollbackTransaction(ActionExecutedContext resultContext, UnitOfWorkAttribute unitOfWork);

    /// <summary>
    /// <para>zh-cn:在工作单元成功或失败后执行清理。</para>
    /// <para>en-us:Runs cleanup after the work unit succeeds or fails.</para>
    /// </summary>
    /// <param name="context"><para>zh-cn:当前 MVC 动作执行上下文。</para><para>en-us:The current MVC action execution context.</para></param>
    /// <param name="resultContext"><para>zh-cn:当前动作执行结果上下文。</para><para>en-us:The current action execution result context.</para></param>
    void OnCompleted(ActionExecutingContext context, ActionExecutedContext resultContext);
}
