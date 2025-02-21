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
using Air.Cloud.Core.Standard.Taxin;
using Air.Cloud.Core.Standard.Taxin.Client;
using Air.Cloud.Core.Standard.Taxin.Model;
using Air.Cloud.Core.Standard.Taxin.Server;
using Air.Cloud.Core.Standard.Taxin.Store;
using Air.Cloud.Modules.Taxin.Server;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Air.Cloud.Modules.Taxin.Extensions
{

    /// <summary>
    /// <para>zh-cn:Taxin 服务端扩展方法</para>
    /// <para>en-us:Taxin Server Extensions</para>
    /// </summary>
    public static class TaxinServerExtensions
    {

        /// <summary>
        /// <para>zh-cn:Taxin服务端注入</para>
        /// <para>en-us:Taxin server injection</para>
        /// </summary>
        /// <typeparam name="TTaxinServerDependency">
        /// <para>zh-cn:Taxin 服务端实现类</para>
        /// <para>en-us:Taxin server implementation type</para>
        /// </typeparam>
        /// <param name="services">
        /// <para>zh-cn:服务集合</para>
        /// <para>en-us: Services collection</para>
        /// </param>
        /// <returns>
        /// <para>zh-cn:服务集合</para>
        /// <para>en-us: Services collection</para>
        /// </returns>
        public static IServiceCollection AddTaxinServer<TTaxinServerDependency>(this IServiceCollection services) 
            where TTaxinServerDependency : class, ITaxinServerStandard, new()
        {
            services.AddSingleton<ITaxinStoreStandard, DefaultTaxinStoreDependency>();
            services.AddSingleton<ITaxinServerStandard, TTaxinServerDependency>();
            services.AddHostedService<TaxinServerBackgroundService>();
            ITaxinStoreStandard.IsTaxinServer = true;
            return services;
        }


        /// <summary>
        /// <para>zh-cn:Taxin服务端注入</para>
        /// <para>en-us:Taxin server injection</para>
        /// </summary>
        /// <typeparam name="TTaxinServerDependency">
        /// <para>zh-cn:Taxin 服务端实现类</para>
        /// <para>en-us:Taxin server implementation type</para>
        /// </typeparam>
        /// <typeparam name="TTaxinClientDependency">
        /// <para>zh-cn:Taxin 客户端实现类</para>
        /// <para>en-us:Taxin client implementation type</para>
        /// </typeparam>
        /// <param name="services">
        /// <para>zh-cn:服务集合</para>
        /// <para>en-us: Services collection</para>
        /// </param>
        /// <returns>
        /// <para>zh-cn:服务集合</para>
        /// <para>en-us: Services collection</para>
        /// </returns>
        public static IServiceCollection AddTaxinServer<TTaxinServerDependency, TTaxinClientDependency>(this IServiceCollection services)
            where TTaxinServerDependency : class, ITaxinServerStandard, new()
            where TTaxinClientDependency : class, ITaxinClientStandard, new()
        {
            services.AddSingleton<ITaxinStoreStandard, DefaultTaxinStoreDependency>();
            services.AddSingleton<ITaxinServerStandard, TTaxinServerDependency>();
            services.AddSingleton<ITaxinClientStandard, TTaxinClientDependency>();
            services.AddHostedService<TaxinServerBackgroundService>();
            ITaxinStoreStandard.IsTaxinServer = true;
            return services;
        }
        /// <summary>
        /// <para>zh-cn:Taxin服务端注入</para>
        /// <para>en-us:Taxin server injection</para>
        /// </summary>
        /// <typeparam name="TTaxinServerDependency">
        /// <para>zh-cn:Taxin 服务端实现类</para>
        /// <para>en-us:Taxin server implementation type</para>
        /// </typeparam>
        /// <typeparam name="TTaxinClientDependency">
        /// <para>zh-cn:Taxin 客户端实现类</para>
        /// <para>en-us:Taxin client implementation type</para>
        /// </typeparam>
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
        public static IServiceCollection AddTaxinServer<TTaxinServerDependency, TTaxinClientDependency, TTaxinStoreDependency>(this IServiceCollection services) 
            where TTaxinServerDependency : class, ITaxinServerStandard, new() 
            where TTaxinStoreDependency : class, ITaxinStoreStandard, new()
            where TTaxinClientDependency : class, ITaxinClientStandard, new()
            
        {
            services.AddSingleton<ITaxinStoreStandard, TTaxinStoreDependency>();
            services.AddSingleton<ITaxinServerStandard, TTaxinServerDependency>();
            services.AddSingleton<ITaxinClientStandard, TTaxinClientDependency>();
            services.AddHostedService<TaxinServerBackgroundService>();
            ITaxinStoreStandard.IsTaxinServer = true;
            return services;
        }
        /// <summary>
        /// <para>zh-cn:配置拦截Taxin请求</para>
        /// <para>en-us:</para>
        /// </summary>
        /// <typeparam name="TTaxinServerDependency">
        /// <para>zh-cn:Taxin 服务端实现类</para>
        /// <para>en-us:Taxin server implementation type</para>
        /// </typeparam>
        /// <param name="app">
        ///  <para>zh-cn:配置应用程序请求管道类</para>
        ///  <para>en-us:Defines a class that provides the mechanisms to configure an application's request pipeline.</para>
        /// </param>
        /// <returns>
        /// <para>zh-cn:配置应用程序请求管道</para>
        /// <para>en-us:Defines a class that provides the mechanisms to configure an application's request pipeline.</para>
        /// </returns>
        public static IApplicationBuilder UseTaxinServer<TTaxinServerDependency>(this IApplicationBuilder app) where TTaxinServerDependency : class, ITaxinServerStandard, new()
        {
            TaxinOptions Options = AppCore.GetOptions<TaxinOptions>();
            ITaxinServerStandard taxinServer = new TTaxinServerDependency();
            app.Map(new PathString(Options.PullRoute), application =>
            {
                //拉取数据请求
                application.Use(next =>
                {
                    return async (context) =>
                    {
                        var result = await taxinServer.DispatchAsync();
                        await context.Response.WriteAsJsonAsync(result);
                    };
                });
            });
            app.Map(new PathString(Options.CheckRoute), application =>
            {
                //检查数据版本请求
                application.Use(next =>
                {
                    return async (context) =>
                    {
                        var CheckTag = context.Request.Query.FirstOrDefault(s => s.Key.ToUpper().Equals("CHECKTAG"));
                        var result = await taxinServer.CheckAsync(CheckTag.Value);
                        await context.Response.WriteAsJsonAsync(result);
                    };
                });
            });
            app.Map(new PathString(Options.PushRoute), application =>
            {
                //推送数据请求
                application.Use(next =>
                {
                    return async (context) =>
                    {
                        try
                        {
                            var package = await context.Request.ReadFromJsonAsync<TaxinRouteDataPackage>();
                            var result = await taxinServer.ReceiveAsync(package);
                            await context.Response.WriteAsJsonAsync(result);
                        }
                        catch (Exception)
                        {
                            await context.Response.WriteAsJsonAsync(Array.Empty<string>());
                        }
                    };
                });
            });
            app.Map(new PathString(Options.OffLineRoute), application =>
            {
                //推送数据请求
                application.Use(next =>
                {
                    return async (context) =>
                    {
                        try
                        {
                            var package = await context.Request.ReadFromJsonAsync<TaxinRouteDataPackage>();
                            var result = await taxinServer.ClienOffLineAsync(package);
                            await context.Response.WriteAsJsonAsync(result);
                        }
                        catch (Exception)
                        {
                            await context.Response.WriteAsJsonAsync(Array.Empty<string>());
                        }
                    };
                });
            });
            return app;
        }
    }
}
