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
using Air.Cloud.DataBase.Options;
using Air.Cloud.DataBase.Repositories;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Air.Cloud.DataBase.BackgroundServices
{
    public  class DatabaseStatusCheckBackgroundService : BackgroundService
    {

        private readonly IServiceProvider provider;
        private  DataSourceOptions options => AppCore.GetOptions<DataSourceOptions>();


        public DatabaseStatusCheckBackgroundService(IServiceProvider serviceProvider) { 
              this.provider=serviceProvider;
        }

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
                            var repository = services.GetService<IRepository>()?.Sql();
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
                                        {"StackTrace",ex.StackTrace }
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
                                {"StackTrace",ex.StackTrace }
                            }
                        );
                        await Task.Delay(TimeSpan.FromMilliseconds(options.ConnectionValidationIntervalMillis), stoppingToken);
                    }
                }
            }, stoppingToken);
        }
    }
}
