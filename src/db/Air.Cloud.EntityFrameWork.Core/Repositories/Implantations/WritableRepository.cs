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

using Air.Cloud.Core.Standard.DataBase.Model;

namespace Air.Cloud.EntityFrameWork.Core.Repositories.Implantations;

/// <summary>
/// <para>zh-cn:可写仓储实现。</para>
/// <para>en-us:Repository implementation for context save and state operations.</para>
/// </summary>
public partial class PrivateRepository<TEntity>
    where TEntity : class, IPrivateEntity, new()
{
    /// <summary>
    /// <para>zh-cn:接受所有更改</para>
    /// <para>en-us:Performs the operation described by this member.</para>
    /// </summary>
    public virtual void AcceptAllChanges()
    {
        ChangeTracker.AcceptAllChanges();
    }

    /// <summary>
    /// <para>zh-cn:保存数据库上下文池中所有已更改的数据库上下文</para>
    /// <para>en-us:Sets or selects the database context for this operation.</para>
    /// </summary>
    /// <returns><para>zh-cn:操作结果。</para><para>en-us:The operation result.</para></returns>
    public int SavePoolNow()
    {
        return _dbContextPool.SavePoolNow();
    }

    /// <summary>
    /// <para>zh-cn:保存数据库上下文池中所有已更改的数据库上下文</para>
    /// <para>en-us:Sets or selects the database context for this operation.</para>
    /// </summary>
    /// <param name="acceptAllChangesOnSuccess"><para>zh-cn:acceptAllChangesOnSuccess 参数。</para><para>en-us:The acceptAllChangesOnSuccess value.</para></param>
    /// <returns><para>zh-cn:操作结果。</para><para>en-us:The operation result.</para></returns>
    public int SavePoolNow(bool acceptAllChangesOnSuccess)
    {
        return _dbContextPool.SavePoolNow(acceptAllChangesOnSuccess);
    }

    /// <summary>
    /// <para>zh-cn:保存数据库上下文池中所有已更改的数据库上下文</para>
    /// <para>en-us:Sets or selects the database context for this operation.</para>
    /// </summary>
    /// <param name="cancellationToken"><para>zh-cn:cancellationToken 参数。</para><para>en-us:The cancellationToken value.</para></param>
    /// <returns><para>zh-cn:操作结果。</para><para>en-us:The operation result.</para></returns>
    public Task<int> SavePoolNowAsync(CancellationToken cancellationToken = default)
    {
        return _dbContextPool.SavePoolNowAsync(cancellationToken);
    }

    /// <summary>
    /// <para>zh-cn:保存数据库上下文池中所有已更改的数据库上下文</para>
    /// <para>en-us:Sets or selects the database context for this operation.</para>
    /// </summary>
    /// <param name="acceptAllChangesOnSuccess"><para>zh-cn:acceptAllChangesOnSuccess 参数。</para><para>en-us:The acceptAllChangesOnSuccess value.</para></param>
    /// <param name="cancellationToken"><para>zh-cn:cancellationToken 参数。</para><para>en-us:The cancellationToken value.</para></param>
    /// <returns><para>zh-cn:操作结果。</para><para>en-us:The operation result.</para></returns>
    public Task<int> SavePoolNowAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        return _dbContextPool.SavePoolNowAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

}
