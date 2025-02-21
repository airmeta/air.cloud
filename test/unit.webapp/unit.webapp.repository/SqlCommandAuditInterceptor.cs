
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

using System.Data.Common;

namespace unit.webapp.repository
{
    /// <summary>
    /// 执行 sql 审计
    /// </summary>
    public sealed class SqlCommandAuditInterceptor : DbCommandInterceptor
    {
        public override InterceptionResult<int> NonQueryExecuting(DbCommand command, CommandEventData eventData, InterceptionResult<int> result)
        {
            // 获取执行的 sql 语句
            var sql = command.CommandText;

            // 获取执行的 sql 类型，是 sql 语句，还是存储过程，还是其他
            var type = command.CommandType;

            // 获取 sql 传递的命令参数
            var parameters = command.Parameters;

            // 写日志~~~~
            //Trace.TraceInformation("NonQueryExecuting sql:" + sql);

            return base.NonQueryExecuting(command, eventData, result);
        }

        public override ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(DbCommand command, CommandEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            // 获取执行的 sql 语句
            var sql = command.CommandText;

            // 获取执行的 sql 类型，是 sql 语句，还是存储过程，还是其他
            var type = command.CommandType;

            // 获取 sql 传递的命令参数
            var parameters = command.Parameters;

            // 写日志~~~~
            //Trace.TraceInformation("NonQueryExecutingAsync sql:" + sql);

            return base.NonQueryExecutingAsync(command, eventData, result, cancellationToken);
        }

        public override InterceptionResult<DbDataReader> ReaderExecuting(DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result)
        {
            // 获取执行的 sql 语句
            var sql = command.CommandText;

            // 获取执行的 sql 类型，是 sql 语句，还是存储过程，还是其他
            var type = command.CommandType;

            // 获取 sql 传递的命令参数
            var parameters = command.Parameters;

            // 写日志~~~~
            //Trace.TraceInformation("ReaderExecuting sql:" + sql);

            return base.ReaderExecuting(command, eventData, result);
        }

        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result, CancellationToken cancellationToken = default)
        {
            // 获取执行的 sql 语句
            var sql = command.CommandText;

            // 获取执行的 sql 类型，是 sql 语句，还是存储过程，还是其他
            var type = command.CommandType;

            // 获取 sql 传递的命令参数
            var parameters = command.Parameters;

            // 写日志~~~~
            //Trace.TraceInformation("ReaderExecutingAsync sql:" + sql);

            return base.ReaderExecutingAsync(command, eventData, result, cancellationToken);
        }
    }
}
