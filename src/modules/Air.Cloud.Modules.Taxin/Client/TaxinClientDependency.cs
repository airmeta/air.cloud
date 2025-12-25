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
using Air.Cloud.Core.Extensions;
using Air.Cloud.Core.Modules.AppPrint;
using Air.Cloud.Core.Plugins.Http.Extensions;
using Air.Cloud.Core.Plugins.InternalAccess;
using Air.Cloud.Core.Standard.KVCenter;
using Air.Cloud.Core.Standard.Store;
using Air.Cloud.Core.Standard.Taxin.Client;
using Air.Cloud.Core.Standard.Taxin.Enums;
using Air.Cloud.Core.Standard.Taxin.Model;
using Air.Cloud.Core.Standard.Taxin.Options;
using Air.Cloud.Core.Standard.Taxin.Result;
using Air.Cloud.Core.Standard.Taxin.Tools;
using Air.Cloud.Modules.Taxin.Extensions;

using System.Data;

namespace Air.Cloud.Modules.Taxin.Client
{
    /// <summary>
    /// <para>zh-cn:默认的客户端实现</para>
    /// <para>en-us:Default Taxin client dependency</para>
    /// </summary>
    public class TaxinClientDependency : ITaxinClientStandard
    {
        /// <summary>
        /// <para>zh-cn:Taxin存储标准实现</para>
        /// <para>en-us:Taxin store standard dependency</para>
        /// </summary>
        private ITaxinStoreStandard ITaxinStoreStandard => AppCore.GetService<ITaxinStoreStandard>();
        
        private IHttpClientFactory HttpClientFactory => AppCore.GetService<IHttpClientFactory>();

        /// <summary>
        /// <para>zh-cn:内部调用请求头</para>
        /// <para>en-us:Internal request headers</para>
        /// </summary>
        private IDictionary<string, string> Headers = new Dictionary<string, string>();
        /// <summary>
        /// <para>zh-cn:Taxin 配置选项</para>
        /// <para>en-us:Taxin options </para>
        /// </summary>
        private TaxinOptions Options => AppCore.GetOptions<TaxinOptions>();

        /// <summary>
        /// <para>zh-cn:默认构造函数</para>
        /// <para>en-us:Default constructor</para>
        /// </summary>
        public TaxinClientDependency()
        {
            //装载内部访问令牌
            IInternalAccessValidPlugin internalAccessValidPlugin = AppRealization.AppPlugin.GetPlugin<IInternalAccessValidPlugin>();
            if (internalAccessValidPlugin!=null)
            {
               var AccessToken= internalAccessValidPlugin.CreateInternalAccessToken();
               this.Headers.Add(AccessToken.Item1, AccessToken.Item2);
            }
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
            switch (Options.PersistenceMethod)
            {
                case PersistenceMethodEnum.Folder:
                case PersistenceMethodEnum.Cache:
                    try
                    {
                        using (var client = HttpClientFactory.CreateClient())
                        {
                            client.Timeout = new TimeSpan(0, 3, 0);
                            string Url = (new Uri(new Uri(Options.GetServerAddress()), Options.OffLineRoute)).ToString();
                            var result = await client.PostAsync(Url, client.SetHeaders(Headers).SetBody(new { UniqueKey = ITaxinStoreStandard.Current.UniqueKey, InstancePId = ITaxinStoreStandard.Current.InstancePId }));
                            string Content = await result.Content.ReadAsStringAsync();
                            var Result = AppRealization.JSON.Deserialize<TaxinActionResult>(Content);
                            if (!Result.IsSuccess)
                            {
                                AppRealization.Output.Print(new AppPrintInformation()
                                {
                                    Level = AppPrintLevel.Error,
                                    State = true,
                                    Content = "Taxin client offline failed",
                                    Title = "Taxin Notice"
                                });
                            }
                            await this.ITaxinStoreStandard.SetStoreAsync(ITaxinStoreStandard.Packages);
                        }
                    }
                    catch (Exception ex)
                    {
                        AppRealization.Output.Print(new AppPrintInformation()
                        {
                            Title = "Taxin Notice",
                            Content = ex.Message,
                            AdditionalParams = new Dictionary<string, object>()
                            {
                                {"error",ex }
                            }
                        });
                    }
                    break;
                case PersistenceMethodEnum.KVCenter:
                    AppRealization.Output.Print(new AppPrintInformation()
                    {
                        Title = "Taxin Notice",
                        Content = "采用KVCenter模式,将会自动去中心化,OffLine方法已跳过",
                        Level=AppPrintLevel.Information
                    });
                    break;
            }
        }
        /// <inheritdoc/>
        public async Task PullAsync()
        {
            switch (Options.PersistenceMethod)
            {
                case PersistenceMethodEnum.Folder:
                case PersistenceMethodEnum.Cache:
                    using (var client = HttpClientFactory.CreateClient())
                    {
                        client.Timeout = new TimeSpan(0, 0, 30);
                        string Url = (new Uri(new Uri(Options.GetServerAddress()), Options.PullRoute)).ToString();
                        var result = await client.PostAsync(Url, client.SetHeaders(Headers).SetBody(string.Empty));
                        string Content = await result.Content.ReadAsStringAsync();
                        var Result = AppRealization.JSON.Deserialize<IEnumerable<IEnumerable<TaxinRouteDataPackage>>>(Content);
                        TaxinTools.SetPackages(Result);
                        await ITaxinStoreStandard.SetStoreAsync(ITaxinStoreStandard.Packages);
                    }
                    break;
                case PersistenceMethodEnum.KVCenter:
                    var AllData = await AppRealization.KVCenter.QueryAsync<DefaultKVCenterServiceOptions>(ITaxinStoreStandard.GetPersistenceKVDataBasePath(Options.PersistencePath));
                    foreach (var item in AllData)
                    {
                        var package = AppRealization.JSON.Deserialize<TaxinRouteDataPackage>(item.Value);
                        if (ITaxinStoreStandard.Packages.ContainsKey(package.UniqueKey))
                        {
                            var Packages = ITaxinStoreStandard.Packages[package.UniqueKey]?.Where(s => s.InstancePId != package.InstancePId).ToList();
                            Packages.Add(package);
                            ITaxinStoreStandard.Packages[package.UniqueKey] = Packages;
                        }
                        else
                        {
                            var Packages = new List<TaxinRouteDataPackage>(){
                                package
                            };
                            ITaxinStoreStandard.Packages[package.UniqueKey] = Packages;
                        }
                    }
                    TaxinTools.SetPackages(ITaxinStoreStandard.Packages.SelectMany(s=>s.Value));
                    await ITaxinStoreStandard.SetStoreAsync(ITaxinStoreStandard.Packages);
                    break;
            }
        }
        /// <inheritdoc/>
        public async Task PushAsync()
        {
            switch (Options.PersistenceMethod)
            {
                case PersistenceMethodEnum.Folder:
                case PersistenceMethodEnum.Cache:
                    try
                    {
                        using (var client = HttpClientFactory.CreateClient())
                        {
                            client.Timeout = new TimeSpan(0, 5, 0);
                            string Url = (new Uri(new Uri(Options.GetServerAddress()), Options.PushRoute)).ToString();
                            var result = await client.PostAsync(Url, client.SetHeaders(Headers).SetBody(ITaxinStoreStandard.Current));
                            string Content = await result.Content.ReadAsStringAsync();
                            var Result = AppRealization.JSON.Deserialize<IEnumerable<IEnumerable<TaxinRouteDataPackage>>>(Content);
                            TaxinTools.SetPackages(Result);
                            await ITaxinStoreStandard.SetStoreAsync(ITaxinStoreStandard.Packages);
                        }
                    }
                    catch (Exception ex)
                    {
                        AppRealization.Output.Print(new AppPrintInformation()
                        {
                            Title = "Taxin Notice",
                            Content = ex.Message,
                            AdditionalParams = new Dictionary<string, object>()
                            {
                                {"error",ex }
                            }
                        });
                    }
                    break;
                case PersistenceMethodEnum.KVCenter:
                     await AppRealization.KVCenter.AddOrUpdateAsync(ITaxinStoreStandard.GetPersistenceKVDataPath(Options.PersistencePath),AppRealization.JSON.Serialize(ITaxinStoreStandard.Current));
                    break;
            }
            
        }
        /// <inheritdoc/>
        public async Task CheckAsync()
        {
            switch (Options.PersistenceMethod)
            {
                case PersistenceMethodEnum.Folder:
                case PersistenceMethodEnum.Cache:
                    using (var client = HttpClientFactory.CreateClient())
                    {
                        client.Timeout = new TimeSpan(0, 3, 0);
                        string Route = $"{Options.CheckRoute}?CheckTag={ITaxinStoreStandard.CheckTag}";
                        string Url = (new Uri(new Uri(Options.GetServerAddress()), Route)).ToString();
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
                            AppRealization.Output.Print(new AppPrintInformation()
                            {
                                Title = "Taxin Notice",
                                Content = ex.Message,
                                AdditionalParams = new Dictionary<string, object>()
                                        {
                                            {"error",ex }
                                        }
                            });
                        }
                    }
                    break;
                case PersistenceMethodEnum.KVCenter:
                    await PullAsync();
                    break;

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
                        RequestUri = new Uri(new Uri(Options.GetServerAddress()), RouteInfo.Route),
                        Method = RouteInfo.HttpMethod,
                        Content = client.SetBody(Data)
                    };
                    var response = await client.SetHeaders(Headers).SendAsync(httpRequestMessage);
                    string Content = await response.Content.ReadAsStringAsync();
                    AppRealization.Output.Print(new AppPrintInformation()
                    {
                        Title = "Taxin Notice",
                        Content = $"Taxin client request Url:{httpRequestMessage.RequestUri} StatusCode:{response.StatusCode}",
                        AdditionalParams = new Dictionary<string, object>()
                        {
                            {"content",Content }
                        }
                    });
                    if (response.StatusCode == System.Net.HttpStatusCode.OK)
                    {
                        var Result = AppRealization.JSON.Deserialize<TResult>(Content);
                        return Result;
                    }
                    throw new HttpRequestException("请求下游服务出现异常,请稍后再试");
                }
            }
            catch (Exception ex)
            {
                AppRealization.Output.Print(new AppPrintInformation()
                {
                    Title = "Taxin Notice",
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
