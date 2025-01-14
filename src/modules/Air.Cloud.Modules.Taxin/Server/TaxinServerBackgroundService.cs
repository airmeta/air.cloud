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
using Air.Cloud.Core.Standard.Print;
using Air.Cloud.Core.Standard.Taxin;
using Air.Cloud.Core.Standard.Taxin.Server;
using Air.Cloud.Modules.Taxin.Extensions;

using Microsoft.Extensions.Hosting;

namespace Air.Cloud.Modules.Taxin.Server
{
    /// <summary>
    /// <para>zh-cn:Taxin服务端后台服务</para>
    /// <para>en-us:Taxin server-side backend services </para>
    /// </summary>
    public class TaxinServerBackgroundService : BackgroundService
    {
        private readonly ITaxinServerStandard TaxinServer;
        private readonly ITaxinStoreStandard StoreStandard;
        private static TaxinOptions Options => AppCore.GetOptions<TaxinOptions>();
        /// <summary>
        /// <para>zh-en:构造服务端后台服务</para>
        /// <para>en-us:Construct server-side backend services</para>
        /// </summary>
        /// <param name="TaxinServer"></param>
        /// <param name="StoreStandard"></param>
        public TaxinServerBackgroundService(ITaxinServerStandard TaxinServer, ITaxinStoreStandard StoreStandard)
        {
            this.TaxinServer = TaxinServer;
            this.StoreStandard = StoreStandard;
        }
        /// <summary>
        /// <para>zh-cn:执行内容</para>
        /// <para>en-us:Execute</para>
        /// </summary>
        /// <param name="stoppingToken"></param>
        /// <returns></returns>
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            AppRealization.Output.Print(new AppPrintInformation()
            {
                Title = "The Taxin service is being launched",
                Content = "The Taxin service is being launched"
            });
            await TaxinServer.OnLineAsync();
            if (Options.Persistence)
            {
                await Task.Factory.StartNew(async () =>
                {
                    AppRealization.Output.Print(new AppPrintInformation()
                    {
                        Title = "The Taxin service is successfully launched",
                        Content = "Start loading the instance state and transferring it"
                    });
                    while (!stoppingToken.IsCancellationRequested)
                    {
                        try
                        {
                            //整理数据(该方法暂时未定义 后续需要增加一个数据整理方法)
                            await StoreStandard.SetStoreAsync(ITaxinStoreStandard.Packages);
                            await Task.Delay(TimeSpan.FromSeconds(Options.PersistenceRate), stoppingToken);
                        }
                        catch (OperationCanceledException) { }
                    }
                }, stoppingToken);
            }
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
