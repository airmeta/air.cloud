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
using Air.Cloud.Core.Modules.AppPrint;
using Air.Cloud.Core.Standard.DataBase.Model;
using Air.Cloud.Core.Standard.DataBase.Repositories;
using Air.Cloud.Core.Standard.DynamicServer;
using Air.Cloud.Core.Standard.DynamicServer.Extensions;
using Air.Cloud.DataBase.ElasticSearch.Attributes;
using Air.Cloud.DataBase.ElasticSearch.Connections;
using Air.Cloud.DataBase.ElasticSearch.Implantations;

using Microsoft.Extensions.DependencyInjection;

using System.Reflection;
using System.Runtime.Loader;

namespace Air.Cloud.DataBase.ElasticSearch.Extensions
{
    /// <summary>
    /// <para>zh-cn:ElasticSearch 服务注册扩展，扫描 ElasticSearch 实体并注册对应的 NoSQL 仓储实现。</para>
    /// <para>en-us:ElasticSearch service registration extensions that scan ElasticSearch entities and register the corresponding NoSQL repository implementations.</para>
    /// </summary>
    public static class ElasticSearchInjectExtensions
    {

        /// <summary>
        /// <para>zh-cn:注册ES服务</para>
        /// <para>en-us:Inject ElasticSearch repository services</para>
        /// </summary>
        /// <param name="services">
        /// 服务集合
        /// </param>
        public static IServiceCollection AddElasticSearch(this IServiceCollection services)
        {
            foreach (var item in AppCore.Assemblies.ToList())
            {
                try
                {
                    var allEntities = AssemblyLoadContext.Default.LoadFromAssemblyName(item).GetTypes()
                        .Where(s => s.IsClass
                                    && !s.IsAbstract
                                    && s.GetInterfaces().Contains(typeof(INoSqlEntity))
                                    && s.GetCustomAttribute<ElasticSearchIndexAttribute>() != null)
                        .ToList();

                    foreach (var t in allEntities)
                    {
                        Type genericType = typeof(ESNoSqlRepository<>);
                        Type constructedType = genericType.MakeGenericType(t);
                        AppCore.SetService(
                            typeof(INoSqlRepository<>).MakeGenericType(t),
                            constructedType,
                            DependencyInjectionServiceCollectionExtensions.TryGetServiceLifetime(typeof(ITransient)));
                    }
                }
                catch (Exception ex)
                {
                    AppRealization.Output.Print(new AppPrintInformation
                    {
                        Title = "domain-errors",
                        Level = AppPrintLevel.Error,
                        Content = $"注册ElasticSearch连接池异常,异常信息:{ex.Message}",
                        State = true
                    });
                }
            }

            return services;
        }
    }
}
