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

using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Air.Cloud.EntityFrameWork.Core.Interceptors;

/// <summary>
/// 数据库上下文提交拦截器
/// </summary>
[IgnoreScanning]
public class DbContextSaveChangesInterceptor : SaveChangesInterceptor
{
    /// <summary>
    /// 拦截保存数据库之前
    /// </summary>
    /// <param name="eventData"></param>
    /// <param name="result"></param>
    /// <returns></returns>
    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        if (eventData.Context is not null)
        {
            dynamic dbContext = eventData.Context;
            dbContext.SavingChangesEventInner(eventData, result);
        }

        return base.SavingChanges(eventData, result);
    }

    /// <summary>
    /// 拦截保存数据库之前
    /// </summary>
    /// <param name="eventData"></param>
    /// <param name="result"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        if (eventData.Context is not null)
        {
            dynamic dbContext = eventData.Context;
            dbContext.SavingChangesEventInner(eventData, result);
        }

        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    /// <summary>
    /// 拦截保存数据库成功
    /// </summary>
    /// <param name="eventData"></param>
    /// <param name="result"></param>
    /// <returns></returns>
    public override int SavedChanges(SaveChangesCompletedEventData eventData, int result)
    {
        if (eventData.Context is not null)
        {
            dynamic dbContext = eventData.Context;
            dbContext.SavedChangesEventInner(eventData, result);
        }

        return base.SavedChanges(eventData, result);
    }

    /// <summary>
    /// 拦截保存数据库成功
    /// </summary>
    /// <param name="eventData"></param>
    /// <param name="result"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public override ValueTask<int> SavedChangesAsync(SaveChangesCompletedEventData eventData, int result, CancellationToken cancellationToken = default)
    {
        if (eventData.Context is not null)
        {
            dynamic dbContext = eventData.Context;
            dbContext.SavedChangesEventInner(eventData, result);
        }

        return base.SavedChangesAsync(eventData, result, cancellationToken);
    }

    /// <summary>
    /// 拦截保存数据库失败
    /// </summary>
    /// <param name="eventData"></param>
    public override void SaveChangesFailed(DbContextErrorEventData eventData)
    {
        if (eventData.Context is not null)
        {
            dynamic dbContext = eventData.Context;
            dbContext.SaveChangesFailedEventInner(eventData);
        }

        base.SaveChangesFailed(eventData);
    }

    /// <summary>
    /// 拦截保存数据库失败
    /// </summary>
    /// <param name="eventData"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public override Task SaveChangesFailedAsync(DbContextErrorEventData eventData, CancellationToken cancellationToken = default)
    {
        if (eventData.Context is not null)
        {
            dynamic dbContext = eventData.Context;
            dbContext.SaveChangesFailedEventInner(eventData);
        }

        return base.SaveChangesFailedAsync(eventData, cancellationToken);
    }
}
