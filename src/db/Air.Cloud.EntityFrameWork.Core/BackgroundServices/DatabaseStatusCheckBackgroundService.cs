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
    public  class DatabaseStatusCheckBackgroundService : BackgroundService
    {

        private readonly IServiceProvider provider;
        private DataSourceOptions options => AppCore.GetOptions<DataSourceOptions>()
            ?? throw new InvalidOperationException($"{nameof(DataSourceOptions)} was not configured.");


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
