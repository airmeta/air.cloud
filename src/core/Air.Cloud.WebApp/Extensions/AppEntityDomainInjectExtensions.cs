// Copyright (c) 2020-2022 百小僧, Baiqian Co.,Ltd.
// Furion is licensed under Mulan PSL v2.
// You can use this software according to the terms and conditions of the Mulan PSL v2.
// You may obtain a copy of Mulan PSL v2 at:
//             https://gitee.com/dotnetchina/Furion/blob/master/LICENSE
// THIS SOFTWARE IS PROVIDED ON AN "AS IS" BASIS, WITHOUT WARRANTIES OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO NON-INFRINGEMENT, MERCHANTABILITY OR FIT FOR A PARTICULAR PURPOSE.
// See the Mulan PSL v2 for more details.

using Air.Cloud.Core.App;
using Air.Cloud.Core.Standard;
using Air.Cloud.Core.Standard.DataBase.Domains;
using Air.Cloud.Core.Standard.Dependencies;
using Air.Cloud.Core.Standard.Dependencies.Extensions;

using Microsoft.Extensions.DependencyInjection;

using System.Runtime.Loader;

namespace Air.Cloud.WebApp.Extensions
{
    public static class AppEntityDomainInjectExtensions
    {
        public static void AddEntityDomainInject(this IServiceCollection services)
        {
            var lifetimes = new[] { typeof(ITransient), typeof(IScoped), typeof(ISingleton) };
            //扫描所有的类
            foreach (var item in AppCore.Assemblies.ToList())
            {
                try
                {
                    var AllTypes = AssemblyLoadContext.Default.LoadFromAssemblyName(item).GetTypes().Where(s => s.IsClass && s.GetInterfaces().Contains(typeof(IEntityDomain))).ToList();
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
                    AppStandardRealization.PrintStandard.Print(new
                    {
                        Title = "domain-errors",
                        Type = "Information",
                        Content = $"注册Domain失败,异常信息:{ex.Message}",
                        State = true
                    });
                }
            }
        }
    }
}
