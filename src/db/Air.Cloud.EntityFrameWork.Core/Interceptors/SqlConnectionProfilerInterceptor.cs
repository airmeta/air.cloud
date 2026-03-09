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

using Air.Cloud.Core;
using Air.Cloud.Core.Modules.AppPrint;

using Microsoft.EntityFrameworkCore.Diagnostics;

using System.Data.Common;

namespace Air.Cloud.EntityFrameWork.Core.Interceptors;

/// <summary>
/// 数据库连接拦截分析器
/// </summary>
internal sealed class SqlConnectionProfilerInterceptor : DbConnectionInterceptor
{
    /// <summary>
    /// 构造函数
    /// </summary>
    public SqlConnectionProfilerInterceptor()
    {
    }

    /// <summary>
    /// 拦截数据库连接
    /// </summary>
    /// <param name="connection">数据库连接对象</param>
    /// <param name="eventData">数据库连接事件数据</param>
    /// <param name="result">拦截结果</param>
    /// <returns></returns>
    public override InterceptionResult ConnectionOpening(DbConnection connection, ConnectionEventData eventData, InterceptionResult result)
    {
        // 打印数据库连接信息到 MiniProfiler
        PrintConnectionToMiniProfiler(connection, eventData);

        return base.ConnectionOpening(connection, eventData, result);
    }

    /// <summary>
    /// 拦截数据库连接
    /// </summary>
    /// <param name="connection">数据库连接对象</param>
    /// <param name="eventData">数据库连接事件数据</param>
    /// <param name="result">拦截器结果</param>
    /// <param name="cancellationToken">取消异步Token</param>
    /// <returns></returns>
    public override ValueTask<InterceptionResult> ConnectionOpeningAsync(DbConnection connection, ConnectionEventData eventData, InterceptionResult result, CancellationToken cancellationToken = default)
    {
        // 打印数据库连接信息到 MiniProfiler
        PrintConnectionToMiniProfiler(connection, eventData);

        return base.ConnectionOpeningAsync(connection, eventData, result, cancellationToken);
    }

    /// <summary>
    /// 打印数据库连接信息到 MiniProfiler
    /// </summary>
    /// <param name="connection">数据库连接对象</param>
    /// <param name="eventData">数据库连接事件数据</param>
    private void PrintConnectionToMiniProfiler(DbConnection connection, ConnectionEventData eventData)
    {
        if (AppEnvironment.IsDevelopment)
        {
            AppRealization.TraceLog.Write(new AppPrintInformation
            {
                Title = "数据库链接状态",
                Level = AppPrintLevel.Information,
                Content = $"已读取到数据库链接信息",
                AdditionalParams = new Dictionary<string, object>()
                {
                      {"connection_id", eventData.ConnectionId},
                      {"connection_str", connection.ConnectionString},
                },
                State = true,
                Type = AppPrintConstType.ORM_EXEC_TYPE
            }, Db.TraceLogTags);
        }
       
    }
}