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
using Air.Cloud.Core.App;
using Air.Cloud.Core.Standard.Store;
using Air.Cloud.Core.Standard.Taxin.Model;
using Air.Cloud.Core.Standard.Taxin.Result;
using Air.Cloud.Core.Standard.Taxin.Server;
using Air.Cloud.Core.Standard.Taxin.Tools;
using Air.Cloud.Modules.Taxin.Extensions;

namespace Air.Cloud.Modules.Taxin.Server
{
    /// <summary>
    /// <para>zh-cn:Taxin服务端实现</para>
    /// <para>en-us:Taxin server implementation</para>
    /// </summary>
    public  class TaxinServerDependency : ITaxinServerStandard
    {
        /// <summary>
        /// <para>zh-cn:Taxin 存储标准实现</para>
        /// <para>en-us:Taxin store standard dependency</para>
        /// </summary>
        public ITaxinStoreStandard ITaxinStoreStandard =>AppCore.GetService<ITaxinStoreStandard>();
        /// <inheritdoc/>
        public Task<TaxinActionResult> CheckAsync(string CheckTag)
        {
            ITaxinStoreStandard.CheckTag = ITaxinStoreStandard.CheckTag ?? Guid.NewGuid().ToString();
            return Task.FromResult(new TaxinActionResult()
            {
                IsSuccess = true,
                IsChange = !(ITaxinStoreStandard.CheckTag==CheckTag),
                Message="ok",
                NewTag= ITaxinStoreStandard.CheckTag,
                OldTag=CheckTag
            });
        }
        /// <inheritdoc/>
        public Task<TaxinActionResult> ClienOffLineAsync(TaxinRouteDataPackage package)
        {
            if (ITaxinStoreStandard.Packages.Keys.Contains(package.UniqueKey))
            {
                var Packages = ITaxinStoreStandard.Packages[package.UniqueKey].Where(s=>s.InstancePId!=package.InstancePId).ToList();
                ITaxinStoreStandard.Packages[package.UniqueKey]= Packages;
                string NewTag = Guid.NewGuid().ToString();
                string OldTag= ITaxinStoreStandard.CheckTag;
                ITaxinStoreStandard.CheckTag = NewTag;
                return Task.FromResult(new TaxinActionResult()
                { 
                    IsSuccess = true,
                    IsChange = true,
                    Message = "ok",
                    NewTag = NewTag,
                    OldTag = OldTag
                });
            }

            return Task.FromResult(new TaxinActionResult()
            {
                IsSuccess = true,
                IsChange = false,
                Message = "ok",
                NewTag = ITaxinStoreStandard.CheckTag,
                OldTag = ITaxinStoreStandard.CheckTag
            });
        }
        /// <inheritdoc/>
        public Task<IEnumerable<IEnumerable<TaxinRouteDataPackage>>> DispatchAsync()
        {
            return Task.FromResult(ITaxinStoreStandard.Packages.Values.AsEnumerable());
        }
        /// <inheritdoc/>
        public Task<IEnumerable<IEnumerable<TaxinRouteDataPackage>>> ReceiveAsync(TaxinRouteDataPackage package)
        {
            if (ITaxinStoreStandard.Packages.Keys.Contains(package.UniqueKey))
            {
                var Packages = ITaxinStoreStandard.Packages[package.UniqueKey].Where(s => s.InstancePId != package.InstancePId).ToList();
                Packages.Add(package);
                ITaxinStoreStandard.Packages[package.UniqueKey]=Packages;
                string NewTag = Guid.NewGuid().ToString();
                string OldTag = ITaxinStoreStandard.CheckTag;
                ITaxinStoreStandard.CheckTag = NewTag;
            }
            else
            {
                ITaxinStoreStandard.Packages.Add(package.UniqueKey, new List<TaxinRouteDataPackage>() { package});
            }

            return Task.FromResult(ITaxinStoreStandard.Packages.Values.AsEnumerable());

        }
        /// <inheritdoc/>
        public async Task OffLineAsync()
        {
            //停机前进行数据的存储
            await this.ITaxinStoreStandard.SetStoreAsync(ITaxinStoreStandard.Packages);
        }
        /// <inheritdoc/>
        public async Task OnLineAsync()
        {
            await this.ITaxinStoreStandard.GetStoreAsync();
            var current=TaxinTools.Scanning();
            var AllRoutes= await this.ReceiveAsync(current);
            TaxinTools.SetPackages(AllRoutes);
        }

    }
}
