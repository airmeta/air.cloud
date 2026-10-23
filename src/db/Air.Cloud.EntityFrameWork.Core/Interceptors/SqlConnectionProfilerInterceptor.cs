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
/// <para>zh-cn:该成员的数据库访问行为。</para>
/// <para>en-us:Database access behavior provided by this member.</para>
/// </summary>
internal sealed class SqlConnectionProfilerInterceptor : DbConnectionInterceptor
{
    /// <summary>
    /// 构造函数
    /// <para>zh-cn:该成员的数据库访问行为。</para>
    /// <para>en-us:Database access behavior provided by this member.</para>
    /// </summary>
    public SqlConnectionProfilerInterceptor()
    {
    }

    /// <summary>
    /// 拦截数据库连接
    /// <para>zh-cn:该成员的数据库访问行为。</para>
    /// <para>en-us:Database access behavior provided by this member.</para>
    /// </summary>
    /// <param name="connection"><para>zh-cn:数据库连接对象</para><para>en-us:The connection value.</para></param>
    /// <param name="eventData"><para>zh-cn:数据库连接事件数据</para><para>en-us:The eventData value.</para></param>
    /// <param name="result"><para>zh-cn:拦截结果</para><para>en-us:The result value.</para></param>
    /// <returns><para>zh-cn:操作结果。</para><para>en-us:The operation result.</para></returns>
    public override InterceptionResult ConnectionOpening(DbConnection connection, ConnectionEventData eventData, InterceptionResult result)
    {
        // 打印数据库连接信息到 MiniProfiler
        PrintConnectionToMiniProfiler(connection, eventData);

        return base.ConnectionOpening(connection, eventData, result);
    }

    /// <summary>
    /// 拦截数据库连接
    /// <para>zh-cn:该成员的数据库访问行为。</para>
    /// <para>en-us:Database access behavior provided by this member.</para>
    /// </summary>
    /// <param name="connection"><para>zh-cn:数据库连接对象</para><para>en-us:The connection value.</para></param>
    /// <param name="eventData"><para>zh-cn:数据库连接事件数据</para><para>en-us:The eventData value.</para></param>
    /// <param name="result"><para>zh-cn:拦截器结果</para><para>en-us:The result value.</para></param>
    /// <param name="cancellationToken"><para>zh-cn:取消异步Token</para><para>en-us:The cancellationToken value.</para></param>
    /// <returns><para>zh-cn:操作结果。</para><para>en-us:The operation result.</para></returns>
    public override ValueTask<InterceptionResult> ConnectionOpeningAsync(DbConnection connection, ConnectionEventData eventData, InterceptionResult result, CancellationToken cancellationToken = default)
    {
        // 打印数据库连接信息到 MiniProfiler
        PrintConnectionToMiniProfiler(connection, eventData);

        return base.ConnectionOpeningAsync(connection, eventData, result, cancellationToken);
    }

    /// <summary>
    /// 打印数据库连接信息到 MiniProfiler
    /// <para>zh-cn:该成员的数据库访问行为。</para>
    /// <para>en-us:Database access behavior provided by this member.</para>
    /// </summary>
    /// <param name="connection"><para>zh-cn:数据库连接对象</para><para>en-us:The connection value.</para></param>
    /// <param name="eventData"><para>zh-cn:数据库连接事件数据</para><para>en-us:The eventData value.</para></param>
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
