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
using Air.Cloud.Core.Dependencies;
using Air.Cloud.Core.Standard.DataBase.Domains;
using Air.Cloud.Core.Standard.DynamicServer.Extensions;
using Air.Cloud.Core.Standard.Print;

using Microsoft.Extensions.DependencyInjection;

using System.Runtime.Loader;

namespace Air.Cloud.Core.Standard.DataBase
{
    public static  class AppEntityDomainInject
    {
        public static void AddEntityDomainInject(this IServiceCollection services)
        {
            var lifetimes = new[] { typeof(ITransient), typeof(IScoped), typeof(ISingleton) };
            //扫描所有的类
            foreach (var item in AppCore.Assemblies.ToList())
            {
                try
                {
                    var AllTypes = AssemblyLoadContext.Default.LoadFromAssemblyName(item).GetTypes().Where(s => s.IsClass&&s.GetInterfaces().Contains(typeof(IEntityDomain))).ToList();
                    foreach (var t in AllTypes)
                    {
                        var instances = t.GetInterfaces();
                        var regType = instances.First(a => a.GetInterfaces().Contains(typeof(IEntityDomain)));
                        var lifeTime = instances.First(s => lifetimes.Contains(s));
                        if (instances.Contains(typeof(IEntityDomain)) && t.IsPublic)
                        {
                            services.Add(ServiceDescriptor.Describe(regType, t, DependencyInjectionServiceCollectionExtensions.TryGetServiceLifetime(lifeTime)));
                        }
                    }
                }
                catch (Exception ex)
                {
                    AppRealization.Output.Print(new AppPrintInformation
                    {
                        Title = "domain-errors",
                        Level = AppPrintInformation.AppPrintLevel.Error,
                        Content = $"注册Domain失败,异常信息:{ex.Message}",
                        State = true
                    });
                }
            }
        }
    }
}
