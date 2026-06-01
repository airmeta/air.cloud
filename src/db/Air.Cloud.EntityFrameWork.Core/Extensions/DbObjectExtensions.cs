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
using Air.Cloud.EntityFrameWork.Core.Helpers;

using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;

using System.Data.Common;
using System.Text;

namespace Air.Cloud.EntityFrameWork.Core.Extensions;

/// <summary>
/// DatabaseFacade 拓展类
/// </summary>
[IgnoreScanning]
public static class DbObjectExtensions
{
    /// <summary>
    /// 是否记录 EFCore 执行 sql 命令打印日志
    /// </summary>
    private static readonly bool IsLogEntityFrameworkCoreSqlExecuteCommand;

    /// <summary>
    /// 构造函数
    /// </summary>
    static DbObjectExtensions()
    {
        var appsettings = AppCore.Settings;
        IsLogEntityFrameworkCoreSqlExecuteCommand = appsettings.OutputOriginalSqlExecuteLog == true;
    }

    /// <summary>
    /// 初始化数据库命令对象
    /// </summary>
    /// <param name="databaseFacade">ADO.NET 数据库对象</param>
    /// <param name="sql">sql 语句</param>
    /// <param name="parameters">命令参数</param>
    /// <param name="commandType">命令类型</param>
    /// <returns>(DbConnection dbConnection, DbCommand dbCommand)</returns>
    public static (DbConnection dbConnection, DbCommand dbCommand) PrepareDbCommand(this DatabaseFacade databaseFacade, string sql, DbParameter[]? parameters = null, CommandType commandType = CommandType.Text)
    {
        // 创建数据库连接对象及数据库命令对象
        var (dbConnection, dbCommand) = databaseFacade.CreateDbCommand(sql, commandType);
        SetDbParameters(databaseFacade.ProviderName ?? string.Empty, ref dbCommand, parameters);

        // 打开数据库连接
        OpenConnection(databaseFacade, dbConnection, dbCommand);

        // 返回
        return (dbConnection, dbCommand);
    }

    /// <summary>
    /// 初始化数据库命令对象
    /// </summary>
    /// <param name="databaseFacade">ADO.NET 数据库对象</param>
    /// <param name="sql">sql 语句</param>
    /// <param name="model">命令模型</param>
    /// <param name="commandType">命令类型</param>
    /// <returns>(DbConnection dbConnection, DbCommand dbCommand, DbParameter[] dbParameters)</returns>
    public static (DbConnection dbConnection, DbCommand dbCommand, DbParameter[] dbParameters) PrepareDbCommand(this DatabaseFacade databaseFacade, string sql, object model, CommandType commandType = CommandType.Text)
    {
        // 创建数据库连接对象及数据库命令对象
        var (dbConnection, dbCommand) = databaseFacade.CreateDbCommand(sql, commandType);
        SetDbParameters(databaseFacade.ProviderName ?? string.Empty, ref dbCommand, model, out var dbParameters);

        // 打开数据库连接
        OpenConnection(databaseFacade, dbConnection, dbCommand);

        // 返回
        return (dbConnection, dbCommand, dbParameters);
    }

    /// <summary>
    /// 初始化数据库命令对象
    /// </summary>
    /// <param name="databaseFacade">ADO.NET 数据库对象</param>
    /// <param name="sql">sql 语句</param>
    /// <param name="parameters">命令参数</param>
    /// <param name="commandType">命令类型</param>
    /// <param name="cancellationToken">异步取消令牌</param>
    /// <returns>(DbConnection dbConnection, DbCommand dbCommand)</returns>
    public static async Task<(DbConnection dbConnection, DbCommand dbCommand)> PrepareDbCommandAsync(this DatabaseFacade databaseFacade, string sql, DbParameter[]? parameters = null, CommandType commandType = CommandType.Text, CancellationToken cancellationToken = default)
    {
        // 创建数据库连接对象及数据库命令对象
        var (dbConnection, dbCommand) = databaseFacade.CreateDbCommand(sql, commandType);
        SetDbParameters(databaseFacade.ProviderName ?? string.Empty, ref dbCommand, parameters);

        // 打开数据库连接
        await OpenConnectionAsync(databaseFacade, dbConnection, dbCommand, cancellationToken);

        // 返回
        return (dbConnection, dbCommand);
    }

    /// <summary>
    /// 初始化数据库命令对象
    /// </summary>
    /// <param name="databaseFacade">ADO.NET 数据库对象</param>
    /// <param name="sql">sql 语句</param>
    /// <param name="model">命令模型</param>
    /// <param name="commandType">命令类型</param>
    /// <param name="cancellationToken">异步取消令牌</param>
    /// <returns>(DbConnection dbConnection, DbCommand dbCommand, DbParameter[] dbParameters)</returns>
    public static async Task<(DbConnection dbConnection, DbCommand dbCommand, DbParameter[] dbParameters)> PrepareDbCommandAsync(this DatabaseFacade databaseFacade, string sql, object model, CommandType commandType = CommandType.Text, CancellationToken cancellationToken = default)
    {
        // 创建数据库连接对象及数据库命令对象
        var (dbConnection, dbCommand) = databaseFacade.CreateDbCommand(sql, commandType);
        SetDbParameters(databaseFacade.ProviderName ?? string.Empty, ref dbCommand, model, out var dbParameters);

        // 打开数据库连接
        await OpenConnectionAsync(databaseFacade, dbConnection, dbCommand, cancellationToken);

        // 返回
        return (dbConnection, dbCommand, dbParameters);
    }

    /// <summary>
    /// 创建数据库命令对象
    /// </summary>
    /// <param name="databaseFacade">ADO.NET 数据库对象</param>
    /// <param name="sql">sql 语句</param>
    /// <param name="commandType">命令类型</param>
    /// <returns>(DbConnection dbConnection, DbCommand dbCommand)</returns>
    private static (DbConnection dbConnection, DbCommand dbCommand) CreateDbCommand(this DatabaseFacade databaseFacade, string sql, CommandType commandType = CommandType.Text)
    {
        if (string.IsNullOrWhiteSpace(sql)) throw new ArgumentNullException(nameof(sql));

        // 支持读取配置渲染
        var realSql = sql.Render();

        // 判断是否启用 MiniProfiler 组件，如果有，则包装链接
        var dbConnection = databaseFacade.GetDbConnection();

        // 创建数据库命令对象
        var dbCommand = dbConnection.CreateCommand();

        // 设置基本参数
        dbCommand.Transaction = databaseFacade.CurrentTransaction?.GetDbTransaction();
        dbCommand.CommandType = commandType;
        dbCommand.CommandText = realSql;

        // 设置超时
        var commandTimeout = databaseFacade.GetCommandTimeout();
        if (commandTimeout != null) dbCommand.CommandTimeout = commandTimeout.Value;

        // 返回
        return (dbConnection, dbCommand);
    }

    /// <summary>
    /// 打开数据库连接
    /// </summary>
    /// <param name="databaseFacade">ADO.NET 数据库对象</param>
    /// <param name="dbConnection">数据库连接对象</param>
    /// <param name="dbCommand"></param>
    private static void OpenConnection(DatabaseFacade databaseFacade, DbConnection dbConnection, DbCommand dbCommand)
    {
        // 判断连接字符串是否关闭，如果是，则开启
        if (dbConnection.State == ConnectionState.Closed)
        {
            dbConnection.Open();
            // 打印数据库连接信息到 MiniProfiler
            PrintDataBaseConnectionInformation(databaseFacade, dbConnection, false);
        }

        // 记录 Sql 执行命令日志
        LogSqlExecuteCommand(databaseFacade, dbCommand);
    }

    /// <summary>
    /// 打开数据库连接
    /// </summary>
    /// <param name="databaseFacade">ADO.NET 数据库对象</param>
    /// <param name="dbConnection">数据库连接对象</param>
    /// <param name="dbCommand"></param>
    /// <param name="cancellationToken">异步取消令牌</param>
    /// <returns></returns>
    private static async Task OpenConnectionAsync(DatabaseFacade databaseFacade, DbConnection dbConnection, DbCommand dbCommand, CancellationToken cancellationToken = default)
    {
        // 判断连接字符串是否关闭，如果是，则开启
        if (dbConnection.State == ConnectionState.Closed)
        {
            await dbConnection.OpenAsync(cancellationToken);

            // 打印数据库连接信息到 MiniProfiler
            PrintDataBaseConnectionInformation(databaseFacade, dbConnection, true);
        }

        // 记录 Sql 执行命令日志
        LogSqlExecuteCommand(databaseFacade, dbCommand);
    }

    /// <summary>
    /// 设置数据库命令对象参数
    /// </summary>
    /// <param name="providerName"></param>
    /// <param name="dbCommand">数据库命令对象</param>
    /// <param name="parameters">命令参数</param>
    private static void SetDbParameters(string providerName, ref DbCommand dbCommand, DbParameter[]? parameters = null)
    {
        if (parameters == null || parameters.Length == 0) return;

        // 添加命令参数前缀
        foreach (var parameter in parameters)
        {
            parameter.ParameterName = DbHelpers.FixSqlParameterPlaceholder(providerName, parameter.ParameterName);
            dbCommand.Parameters.Add(parameter);
        }
    }

    /// <summary>
    /// 设置数据库命令对象参数
    /// </summary>
    /// <param name="providerName"></param>
    /// <param name="dbCommand">数据库命令对象</param>
    /// <param name="model">参数模型</param>
    /// <param name="dbParameters">命令参数</param>
    private static void SetDbParameters(string providerName, ref DbCommand dbCommand, object model, out DbParameter[] dbParameters)
    {
        dbParameters = DbHelpers.ConvertToDbParameters(model, dbCommand);
        SetDbParameters(providerName, ref dbCommand, dbParameters);
    }

    /// <summary>
    /// 打印数据库连接信息
    /// </summary>
    /// <param name="databaseFacade">ADO.NET 数据库对象</param>
    /// <param name="dbConnection">数据库连接对象</param>
    /// <param name="isAsync"></param>
    private static void PrintDataBaseConnectionInformation(DatabaseFacade databaseFacade, DbConnection dbConnection, bool isAsync)
    {
        var connectionId = databaseFacade.GetService<IRelationalConnection>()?.ConnectionId;
        if (AppEnvironment.IsDevelopment)
        {
            AppRealization.TraceLog.Write(new AppPrintInformation
            {
                Title = "数据库链接状态",
                Level = AppPrintLevel.Debug,
                Content = $"已读取到数据库链接信息",
                AdditionalParams = new Dictionary<string, object>()
                {
                      {"connection_id", connectionId?.ToString() ?? string.Empty},
                      {"connection_str", dbConnection.ConnectionString},
                },
                State = true,
                Type = AppPrintConstType.ORM_EXEC_TYPE
            }, Db.TraceLogTags);
        }
    }

    /// <summary>
    /// 输出原始 Sql 执行日志（ADO.NET）
    /// </summary>
    /// <param name="databaseFacade"></param>
    /// <param name="dbCommand"></param>
    private static void LogSqlExecuteCommand(DatabaseFacade databaseFacade, DbCommand dbCommand)
    {
        // 判断是否启用
        if (!IsLogEntityFrameworkCoreSqlExecuteCommand) return;
        // 构建日志内容
        var sqlLogBuilder = new StringBuilder();
        sqlLogBuilder.Append(@"Executed DbCommand (NaN) ");
        sqlLogBuilder.Append(@" [Parameters=[");
        // 拼接命令参数
        var parameters = dbCommand.Parameters;
        for (var i = 0; i < parameters.Count; i++)
        {
            var parameter = parameters[i];
            var parameterType = parameter.GetType();

            // 处理 OracleParameter 参数打印
            var dbType = string.Equals(parameterType.FullName, "Oracle.ManagedDataAccess.Client.OracleParameter", StringComparison.OrdinalIgnoreCase)
                ? parameterType.GetProperty("OracleDbType")?.GetValue(parameter) ?? parameter.DbType : parameter.DbType;

            sqlLogBuilder.Append($"{parameter.ParameterName}='{parameter.Value}' (Size = {parameter.Size}) (DbType = {dbType})");
            if (i < parameters.Count - 1) sqlLogBuilder.Append(", ");
        }
        sqlLogBuilder.Append(@$"], CommandType='{dbCommand.CommandType}', CommandTimeout='{dbCommand.CommandTimeout}']");
        sqlLogBuilder.Append("  ");
        sqlLogBuilder.Append(dbCommand.CommandType == CommandType.StoredProcedure ? "EXEC " + dbCommand.CommandText : dbCommand.CommandText);
        
        var connectionId = databaseFacade.GetService<IRelationalConnection>()?.ConnectionId;
        AppRealization.TraceLog.Write(new AppPrintInformation()
        {

            Title = "数据库语句执行监听",
            Level = AppPrintLevel.Debug,
            Content = $"已读取到数据库语句执行记录",
            AdditionalParams = new Dictionary<string, object>()
            {
                {"connection_id", connectionId?.ToString() ?? string.Empty},
                {"connection_str", dbCommand.Connection?.ConnectionString ?? string.Empty},
                {"sql_str",sqlLogBuilder.ToString() },

            },
            State = true,
            Type = AppPrintConstType.ORM_EXEC_TYPE
        }, Db.TraceLogTags);
    }
}
