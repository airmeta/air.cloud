
/*
 * Copyright (c) 2024 星曳数据
 *
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at https://mozilla.org/MPL/2.0/.
 *
 * This file is provided under the Mozilla Public License Version 2.0,
 * and the "NO WARRANTY" clause of the MPL is hereby expressly
 * acknowledged.
 */
using Microsoft.Extensions.Hosting;

namespace Air.Cloud.Core.Standard.Taxin.Client
{
    public class TaxinClientBackgroundService : BackgroundService
    {
        public readonly ITaxinClientStandard TaxinClient;
        public TaxinClientBackgroundService(ITaxinClientStandard taxinClientStandard = null)
        {
            TaxinClient = taxinClientStandard;
        }
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            AppRealization.Output.Print(new AppPrintInformation()
            {
                Title = "The Taxin service is being launched",
                Content = "The Taxin service is being launched"
            });
            await Task.Factory.StartNew(async () =>
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    AppRealization.Output.Print(new AppPrintInformation()
                    {
                        Title = "The Taxin service is successfully launched",
                        Content = "Start loading the instance state and transferring it"
                    });
                    try
                    {
                        //先推送
                        await TaxinClient.CheckAsync();
                        await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
                    }
                    catch (OperationCanceledException) { }
                }
            }, stoppingToken);
        }


        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            AppRealization.Output.Print(new AppPrintInformation()
            {
                Title = "The Taxin service is stopped",
                Content = "The Taxin service is stopped"
            });
            await Task.CompletedTask;
        }
    }
}
