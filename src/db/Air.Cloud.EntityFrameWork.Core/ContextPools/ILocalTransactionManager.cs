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

namespace Air.Cloud.EntityFrameWork.Core.ContextPools;

/// <summary>
/// <para>zh-cn:应用层可直接使用的本地事务管理器。每次 Begin/Commit 按工作单元深度配对，只有最外层提交才会真正写入数据库。</para>
/// <para>en-us:Application-facing local transaction manager. Each Begin/Commit pair participates in work-unit nesting, and only the outermost commit writes to the database.</para>
/// </summary>
public interface ILocalTransactionManager
{
    /// <summary>
    /// <para>zh-cn:当前可观察到的首个本地事务。多物理连接场景下仅用于兼容和诊断，不能代表所有事务组。</para>
    /// <para>en-us:The first observable local transaction. In multi-connection scenarios this is for compatibility and diagnostics only and does not represent every transaction group.</para>
    /// </summary>
    IDbContextTransaction? CurrentTransaction { get; }

    /// <summary>
    /// <para>zh-cn:当前是否存在活动的本地事务组。</para>
    /// <para>en-us:Whether an active local transaction group exists.</para>
    /// </summary>
    bool IsActive { get; }

    /// <summary>
    /// <para>zh-cn:当前嵌套工作单元深度。没有活动事务时为 0。</para>
    /// <para>en-us:The current nested work-unit depth. Returns 0 when no transaction is active.</para>
    /// </summary>
    int Depth { get; }

    /// <summary>
    /// <para>zh-cn:当前事务是否只能回滚。</para>
    /// <para>en-us:Whether the current transaction is rollback-only.</para>
    /// </summary>
    bool IsRollbackOnly { get; }

    /// <summary>
    /// <para>zh-cn:开始或加入当前作用域的本地事务。</para>
    /// <para>en-us:Starts or joins the local transaction for the current scope.</para>
    /// </summary>
    /// <param name="ensureTransaction"><para>zh-cn:没有已解析上下文时是否解析默认上下文。</para><para>en-us:Whether to resolve the default context when no context has been resolved.</para></param>
    void Begin(bool ensureTransaction = false);

    /// <summary>
    /// <para>zh-cn:创建一个事务作用域；作用域未 Complete 时释放会自动回滚。</para>
    /// <para>en-us:Creates a transaction scope; disposing the scope without Complete automatically rolls it back.</para>
    /// </summary>
    /// <param name="ensureTransaction"><para>zh-cn:没有已解析上下文时是否解析默认上下文。</para><para>en-us:Whether to resolve the default context when no context has been resolved.</para></param>
    /// <param name="closeAllOnCompletion"><para>zh-cn:作用域完成后是否关闭池中连接。</para><para>en-us:Whether to close pooled connections when the scope completes.</para></param>
    /// <returns><para>zh-cn:可提交或回滚的事务作用域。</para><para>en-us:A transaction scope that can be completed or rolled back.</para></returns>
    ILocalTransactionScope BeginScope(bool ensureTransaction = false, bool closeAllOnCompletion = false);

    /// <summary>
    /// <para>zh-cn:完成当前事务作用域；最外层作用域才保存并提交。</para>
    /// <para>en-us:Completes the current transaction scope; only the outermost scope saves and commits.</para>
    /// </summary>
    /// <param name="withCloseAll"><para>zh-cn:完成后是否关闭池中连接。</para><para>en-us:Whether to close pooled connections after completion.</para></param>
    void Commit(bool withCloseAll = false);

    /// <summary>
    /// <para>zh-cn:回滚当前作用域。嵌套作用域会标记外层事务为 rollback-only。</para>
    /// <para>en-us:Rolls back the current scope. A nested scope marks the outer transaction rollback-only.</para>
    /// </summary>
    /// <param name="withCloseAll"><para>zh-cn:完成后是否关闭池中连接。</para><para>en-us:Whether to close pooled connections after rollback.</para></param>
    void Rollback(bool withCloseAll = false);
}
