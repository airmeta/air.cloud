
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
using Air.Cloud.Core.Plugins.Http.Extensions;
using Air.Cloud.Core.Plugins.InternalAccess;
using Air.Cloud.Core.Standard.SkyMirror;
using Air.Cloud.Core.Standard.SkyMirror.Model;
using Air.Cloud.Core.Standard.SkyMirror.Options;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Air.Cloud.Core.Standard.SkyMirror.BackgroundServices
{
    /// <summary>
    /// <para>zh-cn:安全数据推送后台服务</para>
    /// <para>en-us:Security Data Push Background Service</para>
    /// </summary>
    public class SecurityDataPushBackgroundService : BackgroundService
    {
        private readonly IServiceProvider provider;
        private AuthenticaOptions Options => AppCore.GetOptions<AuthenticaOptions>();

        /// <summary>
        /// <para>zh-cn:内部调用请求头</para>
        /// <para>en-us:Internal request headers</para>
        /// </summary>
        private IDictionary<string, string> Headers = new Dictionary<string, string>();
        /// <summary>
        /// <para>zh-cn:构造函数</para>
        /// <para>en-us:Constructor</para>ssssss
        /// </summary>
        /// <param name="serviceProvider"></param>
        public SecurityDataPushBackgroundService(IServiceProvider serviceProvider)
        {
            this.provider = serviceProvider;
            //装载内部访问令牌
            IInternalAccessValidPlugin internalAccessValidPlugin = AppRealization.AppPlugin.GetPlugin<IInternalAccessValidPlugin>();
            if (internalAccessValidPlugin != null)
            {
                var AccessToken = internalAccessValidPlugin.CreateInternalAccessToken();
                this.Headers.Add(AccessToken.Item1, AccessToken.Item2);
            }
        }
        /// <inheritdoc/>
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
                            var HttpClientFactory = scope.ServiceProvider.GetService<IHttpClientFactory>();
                            using (var client = HttpClientFactory.CreateClient())
                            {
                                client.Timeout = new TimeSpan(0, 5, 0);
                                string Url = (new Uri(new Uri(Options.GetServerAddress()), Options.PushRoute)).ToString();
                                var result = await client.PostAsync(Url, client.SetHeaders(Headers).SetBody(AppRealization.JSON.Serialize(ISkyMirrorShieldClientStandard.ClientEndpointDatas)));
                                string Content = await result.Content.ReadAsStringAsync();
                                var Result = AppRealization.JSON.Deserialize<SecurityPushResult>(Content);
                                if (!Result.IsSuccess)
                                {
                                    AppRealization.Output.Print("服务安全数据存储", "服务安全数据存储失败",
                                        AppPrintLevel.Error,
                                        AdditionalParams: new Dictionary<string, object>()
                                        {
                                            {"ResponseContent",Content }
                                        }
                                    );
                                    await Task.Delay(TimeSpan.FromMilliseconds(Options.RetryIntervalMillis), stoppingToken);
                                }
                                else
                                {
                                    AppRealization.Output.Print("服务安全数据存储", "服务安全数据存储成功",
                                        AppPrintLevel.Information
                                    );
                                }
                            }
                        }
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
                        await Task.Delay(TimeSpan.FromMilliseconds(Options.RetryIntervalMillis), stoppingToken);
                    }
                }
            }, stoppingToken);
        }
    }
}