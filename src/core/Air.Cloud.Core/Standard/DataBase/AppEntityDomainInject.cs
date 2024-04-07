using Air.Cloud.Core.Standard.DataBase.Domains;
using Air.Cloud.Core.Standard.Dependencies;
using Air.Cloud.Core.Standard.Dependencies.Extensions;

using Microsoft.Extensions.DependencyInjection;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.Loader;
using System.Text;
using System.Threading.Tasks;

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
