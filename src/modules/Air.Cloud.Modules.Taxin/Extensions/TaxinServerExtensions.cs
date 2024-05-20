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
using Air.Cloud.Core.Standard.Taxin.Server;
using Air.Cloud.Core.Standard.Taxin.Store;
using Air.Cloud.Modules.Taxin;
using Air.Cloud.Modules.Taxin.Model;
using Air.Cloud.Modules.Taxin.Server;
using Air.Cloud.Modules.Taxin.Store;

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
        /// TaxinServer注入
        /// </summary>
        /// <typeparam name="TTaxinServerDependency"></typeparam>
        /// <param name="services"></param>
        /// <returns></returns>
        public static IServiceCollection AddTaxinServer<TTaxinServerDependency>(this IServiceCollection services) where TTaxinServerDependency : class, ITaxinServerStandard, new()
        {
            services.AddSingleton<ITaxinStoreStandard, DefaultTaxinStoreDependency>();
            services.AddSingleton<ITaxinServerStandard, TTaxinServerDependency>();
            services.AddHostedService<TaxinServerBackgroundService>();
            ITaxinServerStandard client = new TTaxinServerDependency();
            client.OnLineAsync();
            return services;
        }

        /// <summary>
        /// TaxinServer注入
        /// </summary>
        /// <typeparam name="TTaxinServerDependency"></typeparam>
        /// <param name="services"></param>
        /// <returns></returns>
        public static IServiceCollection AddTaxinServer<TTaxinServerDependency, TTaxinStoreDependency>(this IServiceCollection services) where TTaxinServerDependency : class, ITaxinServerStandard, new() where TTaxinStoreDependency : class, ITaxinStoreStandard, new()
        {
            services.AddSingleton<ITaxinStoreStandard, TTaxinStoreDependency>();
            services.AddSingleton<ITaxinServerStandard, TTaxinServerDependency>();
            services.AddHostedService<TaxinServerBackgroundService>();
            ITaxinServerStandard client = new TTaxinServerDependency();
            client.OnLineAsync();
            return services;
        }

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
                            var result = await taxinServer.ReciveAsync(package);
                            await context.Response.WriteAsJsonAsync(result);
                        }
                        catch (Exception)
                        {

                            await context.Response.WriteAsJsonAsync(Array.Empty<string>());
                        }
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
