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
using Air.Cloud.Core.Standard.Taxin;
using Air.Cloud.Core.Standard.Taxin.Model;

using Microsoft.Extensions.DependencyInjection;

namespace Air.Cloud.Modules.Taxin.Extensions
{
    /// <summary>
    ///  <para>zh-cn:Taxin 存储 扩展方法</para>
    ///  <para>en-us:Taxin store extensions</para>
    /// </summary>
    public static class TaxinStoreExtensions
    {
        /// <summary>
        /// <para>zh-cn:设置存储</para>
        /// <para>en-us:Set store</para>
        /// </summary>
        /// <param name="taxinStore">
        /// <para>zh-cn:Taxin 存储实现</para>
        /// <para>en-us:Taxin store dependency</para>
        /// </param>
        /// <param name="pairs">
        /// <para>zh-cn:数据包</para>
        /// <para>en-us:data package</para>
        /// </param>
        public static async Task SetStoreAsync(this ITaxinStoreStandard taxinStore, IDictionary<string, IEnumerable<TaxinRouteDataPackage>> pairs)
        {
            await taxinStore.SetPersistenceAsync(ITaxinStoreStandard.Packages);
            ITaxinStoreStandard.Packages = pairs;
            await Task.CompletedTask;
        }

        /// <summary>
        /// <para>zh-cn:获取存储</para>
        /// <para>en-us:GetAsync store</para>
        /// </summary>
        /// <param name="taxinStore">
        /// <para>zh-cn:Taxin 存储实现</para>
        /// <para>en-us:Taxin store dependency</para>
        /// </param>
        public static async Task<IDictionary<string, IEnumerable<TaxinRouteDataPackage>>> GetStoreAsync(this ITaxinStoreStandard taxinStore)
        {
            IDictionary<string, IEnumerable<TaxinRouteDataPackage>> pairs = await taxinStore.GetPersistenceAsync();
            ITaxinStoreStandard.Packages = pairs;
            return ITaxinStoreStandard.Packages;
        }

        /// <summary>
        /// <para>zh-cn:Taxin 存储注入</para>
        /// <para>en-us:Taxin storage injection</para>
        /// </summary>
        /// <typeparam name="TTaxinStoreDependency">
        /// <para>zh-cn:Taxin 存储实现类</para>
        /// <para>en-us:Taxin storage implementation type</para>
        /// </typeparam>
        /// <param name="services">
        /// <para>zh-cn:服务集合</para>
        /// <para>en-us: Services collection</para>
        /// </param>
        /// <returns>
        /// <para>zh-cn:服务集合</para>
        /// <para>en-us: Services collection</para>
        /// </returns>
        public static IServiceCollection AddTaxinStore<TTaxinStoreDependency>(this IServiceCollection services)
            where TTaxinStoreDependency : class, ITaxinStoreStandard, new()
        {
            services.AddSingleton<ITaxinStoreStandard, TTaxinStoreDependency>();
            return services;
        }
    }
}
