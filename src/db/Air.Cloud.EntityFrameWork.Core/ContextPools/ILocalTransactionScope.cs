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
namespace Air.Cloud.EntityFrameWork.Core.ContextPools;

/// <summary>
/// <para>zh-cn:本地事务作用域。只有调用 Complete 后才会提交，未完成作用域释放时自动回滚。</para>
/// <para>en-us:Local transaction scope. It commits only after Complete is called and automatically rolls back when an incomplete scope is disposed.</para>
/// </summary>
public interface ILocalTransactionScope : IDisposable, IAsyncDisposable
{
    /// <summary>
    /// <para>zh-cn:当前作用域是否已经完成提交或回滚。</para>
    /// <para>en-us:Whether this scope has already completed by commit or rollback.</para>
    /// </summary>
    bool IsCompleted { get; }

    /// <summary>
    /// <para>zh-cn:完成当前作用域并提交；嵌套作用域只退出当前深度，最外层才写入数据库。</para>
    /// <para>en-us:Completes and commits this scope; nested scopes only exit their depth, while the outermost scope writes to the database.</para>
    /// </summary>
    void Complete();

    /// <summary>
    /// <para>zh-cn:显式回滚当前作用域。</para>
    /// <para>en-us:Explicitly rolls back this scope.</para>
    /// </summary>
    void Rollback();
}
