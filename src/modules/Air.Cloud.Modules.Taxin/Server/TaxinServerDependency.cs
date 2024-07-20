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
using Air.Cloud.Core.App;
using Air.Cloud.Core.Standard.Taxin;
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
            /*
             * 后续需要增加选举插件 这样就能在服务节点宕机的时候进行数据的迁移
             * 设想: 
             *      1. 服务端启动的时候当前节点就是服务端
             *      2. 第二个服务端进来的时候 还是当前节点为服务端
             *      3. 第三个服务端进来的时候 会进行选举 选举出一个服务端 一般为当前节点 另外两个服务端自动降级为备用服务端
             *      4. 当前服务端宕机的时候 会自动选举出一个服务端为服务端
             *      5. 服务端恢复的时候 会自动降级为备用服务端
             *      6. 客户端进来的时候 直接将数据传送到主服务端 然后主服务端进行数据的分发到备用服务端
             *      7. 服务端之间的数据交互 采用TCP协议进行数据的传输 服务端之间使用心跳进行相互监测
             *      8. 为了避免暂时网络不可访问导致脑裂问题 需要给予开发者一个确认项 以启用Cache,Query等中间件协助通信
             *         不过这种通信可能会延迟一些,但是可以解决脑裂问题
             */
            await this.ITaxinStoreStandard.SetStoreAsync(ITaxinStoreStandard.Packages);
        }
        /// <inheritdoc/>
        public async Task OnLineAsync()
        {
            await this.ITaxinStoreStandard.GetStoreAsync();
            var current=TaxinTools.Scanning();
            var AllRoutes= await this.ReceiveAsync(current);
            await TaxinTools.SetPackages(AllRoutes);
        }

    }
}
