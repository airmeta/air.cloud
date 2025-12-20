
﻿/*
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
using Air.Cloud.Core.Standard.Security;
using Air.Cloud.Core.Standard.Security.Model;
using Air.Cloud.Core.Standard.Security.Options;

using Microsoft.Extensions.Hosting;

namespace Air.Cloud.Extensions.Security.BackgroundServices
{
    public class AuthenticationStoreBackgroundService : BackgroundService
    {

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await Task.Factory.StartNew(async () =>
            {
                var options = AppCore.GetOptions<AuthenticaOptions>();
                while (!stoppingToken.IsCancellationRequested)
                {
                    try
                    {
                        //收集一下所有的端点数据 并进行存储
                        IList<string> AllowAnonymousPaths = new List<string>();

                        IList<string> RequireAuthorization = new List<string>();

                        IList<EndpointData> AuthorizeData = new List<EndpointData>();

                        foreach (var item in ISecurityServerStandard.ServerEndpointDatas)
                        {
                            if (item.IsAllowAnonymous)
                            {
                                AllowAnonymousPaths.Add(item.Path);
                                continue;
                            }
                            if (item.RequiresAuthorization)
                            {
                                RequireAuthorization.Add(item.Path);
                                continue;
                            }
                            if (item.AuthorizeData != null)
                            {
                                AuthorizeData.Add(item);
                            }
                        }
                        File.WriteAllText("security/AuthData.json", AppRealization.JSON.Serialize(AuthorizeData));
                        File.WriteAllText("security/AllowAnonymousPaths.json", AppRealization.JSON.Serialize(AllowAnonymousPaths));
                        File.WriteAllText("security/RequireAuthorization.json", AppRealization.JSON.Serialize(RequireAuthorization));
                        await Task.Delay(TimeSpan.FromMilliseconds(options.StoreIntervalMillis), stoppingToken);
                    }
                    catch (Exception ex)
                    {
                        AppRealization.Output.Print("服务安全数据存储", "服务安全数据存储异常" + ex.Message,
                            AppPrintLevel.Error,
                            AdditionalParams: new Dictionary<string, object>()
                            {
                                {"message",ex.Message },
                                {"StackTrace",ex.StackTrace }
                            }
                        );
                        await Task.Delay(TimeSpan.FromMilliseconds(options.StoreIntervalMillis), stoppingToken);
                    }
                }
            }, stoppingToken);
           

        }
    }
}
