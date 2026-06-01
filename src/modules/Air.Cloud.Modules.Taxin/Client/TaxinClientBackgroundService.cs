
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
    /// <summary>
    /// <para>zh-cn:Taxin 客户端后台服务，负责客户端上线、推送服务信息、周期健康检查和下线处理。</para>
    /// <para>en-us:Taxin client background service that handles client online registration, service information push, periodic health checks, and offline cleanup.</para>
    /// </summary>
    public class TaxinClientBackgroundService : BackgroundService
    {
        private readonly ITaxinClientStandard TaxinClient;
        private static TaxinOptions Options => AppCore.GetOptions<TaxinOptions>();

        /// <summary>
        /// <para>zh-cn:初始化 Taxin 客户端后台服务。</para>
        /// <para>en-us:Initializes the Taxin client background service.</para>
        /// </summary>
        /// <param name="taxinClientStandard">
        /// <para>zh-cn:Taxin 客户端标准服务，用于执行上线、推送、检查和下线操作。</para>
        /// <para>en-us:The Taxin client standard service used to execute online, push, check, and offline operations.</para>
        /// </param>
        public TaxinClientBackgroundService(ITaxinClientStandard taxinClientStandard = null)
        {
            TaxinClient = taxinClientStandard;
        }

        /// <summary>
        /// <para>zh-cn:启动客户端上线流程，并在后台持续执行 Taxin 客户端健康检查。</para>
        /// <para>en-us:Starts the client online workflow and continuously runs Taxin client health checks in the background.</para>
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

        /// <summary>
        /// <para>zh-cn:停止 Taxin 客户端后台服务，并通知远程调用中心当前客户端已下线。</para>
        /// <para>en-us:Stops the Taxin client background service and notifies the remote call center that this client is offline.</para>
        /// </summary>
        /// <param name="cancellationToken">
        /// <para>zh-cn:停止操作的取消令牌。</para>
        /// <para>en-us>The cancellation token for the stop operation.</para>
        /// </param>
        /// <returns>
        /// <para>zh-cn:表示异步停止操作的任务。</para>
        /// <para>en-us:A task that represents the asynchronous stop operation.</para>
        /// </returns>
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
