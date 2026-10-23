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
/// <para>zh-cn:在保存更改生命周期中转发应用数据库上下文事件的 EF Core 拦截器。</para>
/// <para>en-us:EF Core interceptor that forwards application DbContext events during the save-changes lifecycle.</para>
/// </summary>
[IgnoreScanning]
public class DbContextSaveChangesInterceptor : SaveChangesInterceptor
{
    /// <summary>
    /// <para>zh-cn:在同步保存开始前转发上下文事件。</para>
    /// <para>en-us:Forwards the context event before a synchronous save starts.</para>
    /// </summary>
    /// <param name="eventData"><para>zh-cn:保存操作的上下文事件数据。</para><para>en-us:Context event data for the save operation.</para></param>
    /// <param name="result"><para>zh-cn:当前保存拦截结果。</para><para>en-us:The current save interception result.</para></param>
    /// <returns><para>zh-cn:基类返回的拦截结果。</para><para>en-us:The interception result returned by the base interceptor.</para></returns>
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
    /// <para>zh-cn:在异步保存开始前转发上下文事件。</para>
    /// <para>en-us:Forwards the context event before an asynchronous save starts.</para>
    /// </summary>
    /// <param name="eventData"><para>zh-cn:保存操作的上下文事件数据。</para><para>en-us:Context event data for the save operation.</para></param>
    /// <param name="result"><para>zh-cn:当前保存拦截结果。</para><para>en-us:The current save interception result.</para></param>
    /// <param name="cancellationToken"><para>zh-cn:取消令牌。</para><para>en-us:The cancellation token.</para></param>
    /// <returns><para>zh-cn:操作结果。</para><para>en-us:The operation result.</para></returns>
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
    /// <para>zh-cn:在同步保存成功后转发上下文事件。</para>
    /// <para>en-us:Forwards the context event after a successful synchronous save.</para>
    /// </summary>
    /// <param name="eventData"><para>zh-cn:已完成保存操作的事件数据。</para><para>en-us:Event data for the completed save operation.</para></param>
    /// <param name="result"><para>zh-cn:保存的实体数量。</para><para>en-us:The number of saved entities.</para></param>
    /// <returns><para>zh-cn:基类返回的保存实体数量。</para><para>en-us:The saved-entity count returned by the base interceptor.</para></returns>
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
    /// <para>zh-cn:在异步保存成功后转发上下文事件。</para>
    /// <para>en-us:Forwards the context event after a successful asynchronous save.</para>
    /// </summary>
    /// <param name="eventData"><para>zh-cn:已完成保存操作的事件数据。</para><para>en-us:Event data for the completed save operation.</para></param>
    /// <param name="result"><para>zh-cn:保存的实体数量。</para><para>en-us:The number of saved entities.</para></param>
    /// <param name="cancellationToken"><para>zh-cn:取消令牌。</para><para>en-us:The cancellation token.</para></param>
    /// <returns><para>zh-cn:操作结果。</para><para>en-us:The operation result.</para></returns>
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
    /// <para>zh-cn:在同步保存失败后转发上下文错误事件。</para>
    /// <para>en-us:Forwards the context error event after a synchronous save failure.</para>
    /// </summary>
    /// <param name="eventData"><para>zh-cn:保存失败的错误事件数据。</para><para>en-us:Error event data for the failed save.</para></param>
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
    /// <para>zh-cn:在异步保存失败后转发上下文错误事件。</para>
    /// <para>en-us:Forwards the context error event after an asynchronous save failure.</para>
    /// </summary>
    /// <param name="eventData"><para>zh-cn:保存失败的错误事件数据。</para><para>en-us:Error event data for the failed save.</para></param>
    /// <param name="cancellationToken"><para>zh-cn:取消令牌。</para><para>en-us:The cancellation token.</para></param>
    /// <returns><para>zh-cn:操作结果。</para><para>en-us:The operation result.</para></returns>
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
