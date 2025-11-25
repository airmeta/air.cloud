
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
using Air.Cloud.Core.Standard.Print;
using Air.Cloud.Core.Standard.Taxin.Client;
using Air.Cloud.Core.Standard.Taxin.Options;

using Microsoft.Extensions.Hosting;

namespace Air.Cloud.Modules.Taxin.Client
{
    public class TaxinClientBackgroundService : BackgroundService
    {
        private readonly ITaxinClientStandard TaxinClient;
        private static TaxinOptions Options => AppCore.GetOptions<TaxinOptions>();
        public TaxinClientBackgroundService(ITaxinClientStandard taxinClientStandard = null)
        {
            TaxinClient = taxinClientStandard;
        }
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            string A = AppCore.Configuration["AppSettings:GateWayAddress"];
            //client online
            await TaxinClient.OnLineAsync();
            AppRealization.Output.Print(new AppPrintInformation()
            {
                Title = "Taxin远程调用",
                Content = "Taxin远程调用客户端已检查完毕,正在上线..."
            });
            //push current client data
            await TaxinClient.PushAsync();
            AppRealization.Output.Print(new AppPrintInformation()
            {
                Title = "Taxin远程调用",
                Content = "Taxin客户端开始接收来自其他服务的网络调用."
            });
            await Task.Factory.StartNew(async () =>
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    try
                    {
                        //client check
                        await TaxinClient.CheckAsync();
                     
                        await Task.Delay(TimeSpan.FromSeconds(Options.CheckRate), stoppingToken);
                    }
                    catch (OperationCanceledException) { }
                }
            }, stoppingToken);
        }
        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            AppRealization.Output.Print(new AppPrintInformation()
            {
                Title = "Taxin 远程调用",
                Content = "Taxin 客户端已下线",
            });
            try
            {
                await TaxinClient.OffLineAsync();
            }
            catch (Exception)
            {
                throw;
            }
        }
    }
}
