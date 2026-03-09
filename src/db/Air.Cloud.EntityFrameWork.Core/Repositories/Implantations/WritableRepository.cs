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
/// 可写仓储分部类
/// </summary>
public partial class PrivateRepository<TEntity>
    where TEntity : class, IPrivateEntity, new()
{
    /// <summary>
    /// 接受所有更改
    /// </summary>
    public virtual void AcceptAllChanges()
    {
        ChangeTracker.AcceptAllChanges();
    }

    /// <summary>
    /// 保存数据库上下文池中所有已更改的数据库上下文
    /// </summary>
    /// <returns></returns>
    public int SavePoolNow()
    {
        return _dbContextPool.SavePoolNow();
    }

    /// <summary>
    /// 保存数据库上下文池中所有已更改的数据库上下文
    /// </summary>
    /// <param name="acceptAllChangesOnSuccess"></param>
    /// <returns></returns>
    public int SavePoolNow(bool acceptAllChangesOnSuccess)
    {
        return _dbContextPool.SavePoolNow(acceptAllChangesOnSuccess);
    }

    /// <summary>
    /// 保存数据库上下文池中所有已更改的数据库上下文
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public Task<int> SavePoolNowAsync(CancellationToken cancellationToken = default)
    {
        return _dbContextPool.SavePoolNowAsync(cancellationToken);
    }

    /// <summary>
    /// 保存数据库上下文池中所有已更改的数据库上下文
    /// </summary>
    /// <param name="acceptAllChangesOnSuccess"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public Task<int> SavePoolNowAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        return _dbContextPool.SavePoolNowAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    /// <summary>
    /// 提交更改操作
    /// </summary>
    /// <returns></returns>
    public virtual int SaveNow()
    {
        return Context.SaveChanges();
    }

    /// <summary>
    /// 提交更改操作
    /// </summary>
    /// <param name="acceptAllChangesOnSuccess"></param>
    /// <returns></returns>
    public virtual int SaveNow(bool acceptAllChangesOnSuccess)
    {
        return Context.SaveChanges(acceptAllChangesOnSuccess);
    }

    /// <summary>
    /// 提交更改操作（异步）
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public virtual Task<int> SaveNowAsync(CancellationToken cancellationToken = default)
    {
        return Context.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// 提交更改操作（异步）
    /// </summary>
    /// <param name="acceptAllChangesOnSuccess"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public virtual Task<int> SaveNowAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        return Context.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }
}