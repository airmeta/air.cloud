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
using Air.Cloud.Core;
using Air.Cloud.Core.App;
using Air.Cloud.Core.Plugins.Http.Extensions;
using Air.Cloud.Core.Standard.Taxin;
using Air.Cloud.Core.Standard.Taxin.Client;
using Air.Cloud.Core.Standard.Taxin.Model;
using Air.Cloud.Core.Standard.Taxin.Result;
using Air.Cloud.Core.Standard.Taxin.Tools;
using Air.Cloud.Modules.Taxin.Extensions;

using System.Data;

namespace Air.Cloud.Modules.Taxin.Client
{
    public class TaxinClientDependency : ITaxinClientStandard
    {
        private ITaxinStoreStandard ITaxinStoreStandard => AppCore.GetService<ITaxinStoreStandard>();
        private IHttpClientFactory HttpClientFactory => AppCore.GetService<IHttpClientFactory>();

        private IDictionary<string, string> Headers = new Dictionary<string, string>();
        /// <summary>
        /// <para>zh-cn:Taxin 配置选项</para>
        /// <para>en-us:Taxin options </para>
        /// </summary>
        private TaxinOptions Options => AppCore.GetOptions<TaxinOptions>();
        /// <summary>
        /// <para>zh-cn:构造函数</para>
        /// <para>en-us:Constractor  method</para>
        /// </summary>
        public TaxinClientDependency()
        {
            this.Headers.Add("client", AppRealization.PID.Get());
        }
        /// <inheritdoc/>
        public async Task OnLineAsync()
        {
            //加载存储的数据
            await this.ITaxinStoreStandard.GetStoreAsync();
            TaxinTools.Scanning();
        }
        /// <inheritdoc/>
        public async Task OffLineAsync()
        {
            try
            {
                using (var client = HttpClientFactory.CreateClient())
                {
                    client.Timeout = new TimeSpan(0, 3, 0);
                    string Url = (new Uri(new Uri(Options.ServerAddress), Options.OffLineRoute)).ToString();
                    var result = await client.PostAsync(Url, client.SetHeaders(Headers).SetBody(new { UniqueKey = ITaxinStoreStandard.Current.UniqueKey, InstancePId=ITaxinStoreStandard.Current.InstancePId }));
                    string Content = await result.Content.ReadAsStringAsync();
                    var Result = AppRealization.JSON.Deserialize<TaxinActionResult>(Content);
                    if (!Result.IsSuccess)
                    {
                        AppRealization.Output.Print(new Core.Standard.Print.AppPrintInformation()
                        {
                            Level = Core.Standard.Print.AppPrintInformation.AppPrintLevel.Error,
                            State = true,
                            Content = "Taxin client offline failed",
                            Title = "Taxin client offline failed"
                        });
                    }
                    await this.ITaxinStoreStandard.SetStoreAsync(ITaxinStoreStandard.Packages);
                }
            }
            catch (Exception ex)
            {
                AppRealization.Output.Print(new Core.Standard.Print.AppPrintInformation()
                {
                    Title = "Taxin client error",
                    Content = ex.Message,
                    AdditionalParams = new Dictionary<string, object>()
                    {
                        {"error",ex }
                    }
                });
            }
        }
        /// <inheritdoc/>
        public async Task PullAsync()
        {
            using (var client = HttpClientFactory.CreateClient())
            {
                client.Timeout = new TimeSpan(0, 5, 0);
                //拉取
                string Url = (new Uri(new Uri(Options.ServerAddress), Options.PullRoute)).ToString();
                var result = await client.PostAsync(Url, client.SetHeaders(Headers).SetBody(string.Empty));
                string Content = await result.Content.ReadAsStringAsync();
                var Result = AppRealization.JSON.Deserialize<IEnumerable<IEnumerable<TaxinRouteDataPackage>>>(Content);
                await TaxinTools.SetPackages(Result);
                await ITaxinStoreStandard.SetStoreAsync(ITaxinStoreStandard.Packages);
            }
        }
        /// <inheritdoc/>
        public async Task PushAsync()
        {
            try
            {
                using (var client = HttpClientFactory.CreateClient())
                {
                    client.Timeout = new TimeSpan(0,5, 0);
                    string Url = (new Uri(new Uri(Options.ServerAddress), Options.PushRoute)).ToString();
                    var result = await client.PostAsync(Url, client.SetHeaders(Headers).SetBody(AppRealization.JSON.Serialize(ITaxinStoreStandard.Current)));
                    string Content = await result.Content.ReadAsStringAsync();
                    var Result = AppRealization.JSON.Deserialize<IEnumerable<IEnumerable<TaxinRouteDataPackage>>>(Content);
                    await TaxinTools.SetPackages(Result);
                    await ITaxinStoreStandard.SetStoreAsync(ITaxinStoreStandard.Packages);
                }
            }
            catch (Exception ex)
            {
                AppRealization.Output.Print(new Core.Standard.Print.AppPrintInformation()
                {
                    Title = "Taxin client error",
                    Content = ex.Message,
                    AdditionalParams = new Dictionary<string, object>()
                    {
                        {"error",ex }
                    }
                });
            }
        }
        /// <inheritdoc/>
        public async Task CheckAsync()
        {
            using (var client = HttpClientFactory.CreateClient())
            {
                client.Timeout = new TimeSpan(0, 3, 0);
                string Route = $"{Options.CheckRoute}?CheckTag={ITaxinStoreStandard.CheckTag}";
                string Url = (new Uri(new Uri(Options.ServerAddress), Route)).ToString();
                try
                {
                    string Content = await client.GetStringAsync(Url);
                    var Result = AppRealization.JSON.Deserialize<TaxinActionResult>(Content);
                    if (Result.IsSuccess)
                    {
                        if (Result.IsChange)
                        {
                            //pull server data
                            await PullAsync();
                            //标志已经被修改
                            ITaxinStoreStandard.CheckTag = Result.NewTag;
                        }
                    }
                }
                catch (Exception ex)
                {
                    AppRealization.Output.Print(new Core.Standard.Print.AppPrintInformation()
                    {
                        Title = "Taxin client error",
                        Content = ex.Message,
                        AdditionalParams = new Dictionary<string, object>()
                            {
                                {"error",ex }
                            }
                    });
                }

            }
        }
        /// <inheritdoc/>
        public async Task<TResult> SendAsync<TResult>(string Route, object Data=null,Tuple<Version,Version> Version=null) where TResult : class
        {
            var Routes=ITaxinStoreStandard.Routes[Route];
            if (Routes == null)
                return null;
            #region 版本定位
            TaxinRouteInformation? RouteInfo;
            if (Version == null)
            {
                RouteInfo =Routes.Values.FirstOrDefault();
            }
            else
            {
                Func<Version, bool> func = (s) =>
                {
                    Version StartVersion = Version.Item1;
                    Version EndVersion = Version.Item2;
                    if((EndVersion == default || EndVersion == null)&&(StartVersion == default || StartVersion == null))
                        return s.CompareTo(EndVersion) <= 0 && s.CompareTo(StartVersion) >= 0;
                    if (StartVersion == default || StartVersion == null)
                        return s.CompareTo(EndVersion) <= 0;
                    if(EndVersion == default || EndVersion == null)
                        return s.CompareTo(StartVersion) >= 0;
                    return true;
                };
                Func<IEnumerable<Version>, Version> LocateVersion = (versions) =>
                {
                    switch (Options.BalanceType)
                    {
                        case Core.Standard.Taxin.Enums.BalanceTypeEnum.Random:
                            Random ran = new Random();
                            int n = ran.Next(versions.Count()+1);
                            return versions.ToArray()[n];
                        case Core.Standard.Taxin.Enums.BalanceTypeEnum.Low:
                            return versions.OrderBy(s => s.ToString()).FirstOrDefault(); 
                           
                        case Core.Standard.Taxin.Enums.BalanceTypeEnum.High:
                            return versions.OrderByDescending(s => s.ToString()).FirstOrDefault();
                    }
                    return versions.FirstOrDefault();
                }; 
                var TargetVersion = LocateVersion(Routes.Keys.Where(func));
                RouteInfo = Routes[TargetVersion];
            }
            #endregion
            #region  发起请求
            try
            {
                using (HttpClient client = HttpClientFactory.CreateClient())
                {
                    Data = Data ??new { };
                    HttpRequestMessage httpRequestMessage = new HttpRequestMessage()
                    {
                        RequestUri = new Uri(new Uri(Options.ServerAddress), RouteInfo.Route),
                        Method = RouteInfo.HttpMethod,
                        Content = client.SetBody(Data)
                    };
                    var response = await client.SetHeaders(Headers).SendAsync(httpRequestMessage);
                    string Content = await response.Content.ReadAsStringAsync();
                    if (response.StatusCode == System.Net.HttpStatusCode.OK)
                    {
                        var Result = AppRealization.JSON.Deserialize<TResult>(Content);
                        return Result;
                    }
                    AppRealization.Output.Print(new Core.Standard.Print.AppPrintInformation()
                    {
                        Title = "Taxin client request error",
                        Content = $"Taxin client request Url:{httpRequestMessage.RequestUri}StatusCode:{response.StatusCode} error",
                        AdditionalParams = new Dictionary<string, object>()
                        {
                            {"error",Content }
                        }
                    });
                    throw new HttpRequestException("请求下游服务出现异常,请稍后再试");
                }
            }
            catch (Exception ex)
            {
                AppRealization.Output.Print(new Core.Standard.Print.AppPrintInformation()
                {
                    Title = "Taxin client request error",
                    Content = ex.Message,
                    AdditionalParams = new Dictionary<string, object>()
                    {
                        {"error",ex }
                    }
                });
                throw new HttpRequestException("请求下游服务出现异常,请稍后再试");
            }
            #endregion
        }
    }
}
