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
using Air.Cloud.Core.Plugins.Http;
using Air.Cloud.Core.Standard.Taxin;
using Air.Cloud.Core.Standard.Taxin.Client;
using Air.Cloud.Modules.Taxin.Extensions;
using Air.Cloud.Core.Standard.Taxin.Result;
using Air.Cloud.Core.Standard.Taxin.Tools;
using Air.Cloud.Modules.Taxin.Model;

using System.Data;

namespace Air.Cloud.Modules.Taxin.Client
{
    public class TaxinClientDependency : ITaxinClientStandard
    {
        public ITaxinStoreStandard ITaxinStoreStandard { get; set; }
        public TaxinClientDependency(ITaxinStoreStandard taxinStore=null) {
            this.ITaxinStoreStandard = taxinStore;
        }
        public async  Task OnLineAsync()
        {
            //加载存储的数据
            await this.ITaxinStoreStandard.GetStoreAsync();
            TaxinTools.Scanning();
        }
        public async Task OffLineAsync()
        {
            TaxinOptions options = AppCore.GetOptions<TaxinOptions>();
            //下线
            string Url = (new Uri(new Uri(options.ServerAddress), options.OffLineRoute)).ToString();
            var Result = HttpClientPlugin.Do<TaxinActionResult>(Url, HttpClientPlugin.POST, AppRealization.JSON.Serialize(new { Key=ITaxinStoreStandard.Current.UniqueKey }));
            if (!Result.IsSuccess)
            {
                AppRealization.Output.Print(new Core.Standard.Print.AppPrintInformation()
                {
                    Level=Core.Standard.Print.AppPrintInformation.AppPrintLevel.Error,
                    State=true,
                    Content= "Taxin client offline failed",
                    Title="Taxin client offline failed"
                });
            }
            await this.ITaxinStoreStandard.SetStoreAsync(ITaxinStoreStandard.Packages);
        }
        public async  Task PullAsync()
        {
            TaxinOptions options = AppCore.GetOptions<TaxinOptions>();
            //拉取
            string Url = (new Uri(new Uri(options.ServerAddress), options.PullRoute)).ToString();
            var Result = HttpClientPlugin.Do<List<TaxinRouteDataPackage>>(Url,HttpClientPlugin.POST,string.Empty);
            IList<TaxinRouteDataPackage> ItemValues = null;
            foreach (var s in Result) {
                string Key = s.UniqueKey;
                if (ITaxinStoreStandard.Packages.ContainsKey(Key)) ItemValues = ITaxinStoreStandard.Packages[Key].ToList();
                if (ItemValues.Any(x => x.InstancePId == s.InstancePId))
                {
                    ItemValues = ItemValues.Where(x => x.InstancePId != s.InstancePId).ToList();
                }
                ItemValues.Add(s);
                ITaxinStoreStandard.Packages.Add(Key,ItemValues);
            }
            await this.ITaxinStoreStandard.SetStoreAsync(ITaxinStoreStandard.Packages);
        }
        public async Task PushAsync()
        {
            //推送
            TaxinOptions options = AppCore.GetOptions<TaxinOptions>();
            string Url = (new Uri(new Uri(options.ServerAddress), options.PushRoute)).ToString();
            var Result = HttpClientPlugin.Do<List<IEnumerable<TaxinRouteDataPackage>>>(Url, HttpClientPlugin.POST, AppRealization.JSON.Serialize(ITaxinStoreStandard.Current));
            IList<TaxinRouteDataPackage> ItemValues = null;
            foreach (var instancePackages in Result)
            {
                foreach (var s in instancePackages)
                {
                    string Key = s.UniqueKey;
                    if (ITaxinStoreStandard.Packages.ContainsKey(Key)) ItemValues = ITaxinStoreStandard.Packages[Key].ToList();
                    if (ItemValues.Any(x => x.InstancePId == s.InstancePId))
                    {
                        ItemValues = ItemValues.Where(x => x.InstancePId != s.InstancePId).ToList();
                    }
                    ItemValues.Add(s);
                    ITaxinStoreStandard.Packages.Add(Key, ItemValues);
                }
            }
            await this.ITaxinStoreStandard.SetStoreAsync(ITaxinStoreStandard.Packages);
        }
        public async Task CheckAsync()
        {
            //检查
            TaxinOptions options = AppCore.GetOptions<TaxinOptions>();
            string Url = (new Uri(new Uri(options.ServerAddress), options.PushRoute)).ToString();
            var Result = HttpClientPlugin.Do<TaxinActionResult>(Url, HttpClientPlugin.POST,AppRealization.JSON.Serialize(new { ITaxinStoreStandard.CheckTag }));
            if (Result.IsSuccess)
            {
                if (Result.IsChange)
                {
                    //标志已经被修改
                    ITaxinStoreStandard.CheckTag = Result.NewTag;
                    //拉取新数据
                    await PullAsync();
                }
            }
        }


    }
}
