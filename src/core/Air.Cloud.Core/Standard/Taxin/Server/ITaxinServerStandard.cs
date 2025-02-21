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
using Air.Cloud.Core.Standard.Taxin.Result;
using Air.Cloud.Core.Standard.Taxin.Model;

namespace Air.Cloud.Core.Standard.Taxin.Server
{
    /// <summary>
    /// <para>zh-cn:Taxin服务端标准</para>
    /// <para>en-us:Taxin server standard</para>
    /// </summary>
    public interface ITaxinServerStandard: ITaxinStandard
    {
        /// <summary>
        /// <para>zh-cn:接收路由数据包</para>
        /// <para>en-us:Receive route data package</para>
        /// </summary>
        /// <returns>
        /// <para>zh-cn:返回标识信息,供客户端存储</para>
        /// <para>en-us:Return the identification information for the client to store</para>
        /// </returns>
        public Task<IEnumerable<IEnumerable<TaxinRouteDataPackage>>> ReceiveAsync(TaxinRouteDataPackage package);
        /// <summary>
        /// <para>zh-cn:派发路由数据包</para>
        /// <para>en-us:Pull route data package</para>
        /// </summary>
        /// <returns>
        /// <para>zh-cn:路由数据包</para>
        /// <para>en-us:Route data package</para>
        /// </returns>
        public Task<IEnumerable<IEnumerable<TaxinRouteDataPackage>>> DispatchAsync();
        /// <summary>
        /// <para>zh-cn:检查路由数据包</para>
        /// <para>en-us:Check route data package</para>
        /// </summary>
        /// <returns>
        /// <para>zh-cn:Taxin 动作结果</para>
        /// <para>en-us:Taxin action result</para>
        /// </returns>
        public Task<TaxinActionResult> CheckAsync(string CheckTag);
        /// <summary>
        /// <para>zh-cn:客户端下线</para>
        /// <para>en-us:The client goes offline</para>
        /// </summary>
        /// <returns>
        /// <para>zh-cn:Taxin 动作结果</para>
        /// <para>en-us:Taxin action result</para>
        /// </returns>
        public Task<TaxinActionResult> ClienOffLineAsync(TaxinRouteDataPackage package);

    }
}
