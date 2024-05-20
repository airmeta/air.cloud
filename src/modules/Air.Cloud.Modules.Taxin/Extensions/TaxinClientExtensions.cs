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
using Air.Cloud.Core.Standard.Taxin.Client;
using Air.Cloud.Core.Standard.Taxin.Store;
using Air.Cloud.Modules.Taxin.Client;
using Air.Cloud.Modules.Taxin.Store;

using Microsoft.Extensions.DependencyInjection;

namespace Air.Cloud.Modules.Taxin.Extensions
{
    /// <summary>
    /// <para>zh-cn:Taxin 客户端扩展方法</para>
    /// <para>en-us:Taxin Client Extensions</para>
    /// </summary>
    public static class TaxinClientExtensions
    {   /// <summary>
        /// <para>zh-cn:Taxin 客户端注入</para>
        /// <para>zh-cn:Taxin Client Inject</para>
        /// </summary>
        /// <typeparam name="TTaxinClientDependency">
        /// <para>zh-cn:Taxin 客户端实现</para>
        /// <para>en-us:Taxin Client dependency</para>
        /// </typeparam>
        /// <param name="services">
        /// <para>zh-cn:服务集合</para>
        /// <para>en-us: Services collection</para>
        /// </param>
        /// <returns>
        /// <para>zh-cn:服务集合</para>
        /// <para>en-us: Services collection</para>    
        /// </returns>
        public static IServiceCollection AddTaxinClient<TTaxinClientDependency>(this IServiceCollection services) where TTaxinClientDependency : class, ITaxinClientStandard, new()
        {
            services.AddSingleton<ITaxinClientStandard, TTaxinClientDependency>();
            services.AddSingleton<ITaxinStoreStandard, DefaultTaxinStoreDependency>();
            services.AddHostedService<TaxinClientBackgroundService>();
            ITaxinClientStandard client = new TTaxinClientDependency();
            client.OnLineAsync();
            return services;
        }
        /// <summary>
        /// <para>zh-cn:Taxin 客户端注入</para>
        /// <para>zh-cn:Taxin Client Inject</para>
        /// </summary>
        /// <typeparam name="TTaxinClientDependency">
        /// <para>zh-cn:Taxin 客户端实现</para>
        /// <para>en-us:Taxin Client dependency</para>
        /// </typeparam>
        /// <typeparam name="TTaxinStoreDependency">
        /// <para>zh-cn:Taxin存储实现</para>
        /// <para>en-us:Taxin store dependency</para>
        /// </typeparam>
        /// <param name="services">
        /// <para>zh-cn:服务集合</para>
        /// <para>en-us: Services collection</para>
        /// </param>
        /// <returns>
        /// <para>zh-cn:服务集合</para>
        /// <para>en-us: Services collection</para>
        /// </returns>
        public static IServiceCollection AddTaxinClient<TTaxinClientDependency, TTaxinStoreDependency>(this IServiceCollection services) 
            where TTaxinClientDependency : class, ITaxinClientStandard, new() 
            where TTaxinStoreDependency : class, ITaxinStoreStandard, new()
        {
            services.AddSingleton<ITaxinClientStandard, TTaxinClientDependency>();
            services.AddSingleton<ITaxinStoreStandard, TTaxinStoreDependency>();
            services.AddHostedService<TaxinClientBackgroundService>();
            ITaxinClientStandard client = new TTaxinClientDependency();
            client.OnLineAsync();
            return services;
        }
    }
}
