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
using Air.Cloud.Core.App;
using Air.Cloud.Core.Modules.AppPrint;
using Air.Cloud.EntityFrameWork.Core.Options;
using Air.Cloud.EntityFrameWork.Core.Repositories;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Air.Cloud.EntityFrameWork.Core.BackgroundServices
{
    /// <summary>
    /// <para>zh-cn:数据库状态检查后台服务，周期性执行连接验证 SQL 并输出数据库状态日志。</para>
    /// <para>en-us:Database status check background service that periodically executes connection validation SQL and writes database status logs.</para>
    /// </summary>
    public  class DatabaseStatusCheckBackgroundService : BackgroundService
    {

        private readonly IServiceProvider provider;
        private DataSourceOptions options => AppCore.GetOptions<DataSourceOptions>()
            ?? throw new InvalidOperationException($"{nameof(DataSourceOptions)} was not configured.");


        /// <summary>
        /// <para>zh-cn:初始化数据库状态检查后台服务。</para>
        /// <para>en-us:Initializes the database status check background service.</para>
        /// </summary>
        /// <param name="serviceProvider">
        /// <para>zh-cn:应用服务提供器，用于创建作用域并解析数据库仓储。</para>
        /// <para>en-us:The application service provider used to create scopes and resolve database repositories.</para>
        /// </param>
        public DatabaseStatusCheckBackgroundService(IServiceProvider serviceProvider) { 
              this.provider=serviceProvider;
        }

        /// <summary>
        /// <para>zh-cn:启动数据库连接状态检查循环，直到后台服务被取消。</para>
        /// <para>en-us:Starts the database connection status check loop until the background service is cancelled.</para>
        /// </summary>
        /// <param name="stoppingToken">
        /// <para>zh-cn:用于停止后台检查循环的取消令牌。</para>
        /// <para>en-us:The cancellation token used to stop the background check loop.</para>
        /// </param>
        /// <returns>
        /// <para>zh-cn:表示后台执行过程的任务。</para>
        /// <para>en-us:A task that represents the background execution process.</para>
        /// </returns>
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await Task.Factory.StartNew(async () =>
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    try
                    {
                        using (var scope = provider.CreateScope())
                        {
                            var services = scope.ServiceProvider;
                            var repository = services.GetRequiredService<IRepository>().Sql();
                            try
                            {
                                await repository.SqlNonQueryAsync(options.ConnectionValidationSQL);
                                AppRealization.Output.Print("数据库状态检查", "数据库连接验证成功，数据库状态正常。", AppPrintLevel.Debug);
                            }
                            catch (Exception ex)
                            {
                                AppRealization.Output.Print("数据库状态检查", "数据库连接验证失败，数据库状态异常。",
                                    AppPrintLevel.Error,
                                    AdditionalParams: new Dictionary<string, object>()
                                    {
                                        {"message",ex.Message },
                                        {"StackTrace",ex.StackTrace ?? string.Empty }
                                    }
                                );
                            }
                            await Task.Delay(TimeSpan.FromMilliseconds(options.ConnectionValidationIntervalMillis), stoppingToken);
                        }
                    }
                    catch (Exception ex) {

                        AppRealization.Output.Print("数据库状态检查", "数据库连接验证失败，数据库状态异常。" + ex.Message,
                            AppPrintLevel.Error,
                            AdditionalParams:new Dictionary<string, object>()
                            {
                                {"message",ex.Message },
                                {"StackTrace",ex.StackTrace ?? string.Empty }
                            }
                        );
                        await Task.Delay(TimeSpan.FromMilliseconds(options.ConnectionValidationIntervalMillis), stoppingToken);
                    }
                }
            }, stoppingToken);
        }
    }
}
