using Air.Cloud.Core.Standard.Cache.Redis;
using Air.Cloud.Core.Standard.Taxin.Client;
using Air.Cloud.Core.Standard.Taxin.Server;
using Air.Cloud.Modules.Taxin.Client;
using Air.Cloud.Modules.Taxin.Server;

using Microsoft.Extensions.DependencyInjection;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Air.Cloud.Modules.Taxin.Extensions
{
    public static  class TaxinModuleExtensions
    {
        /// <summary>
        /// TaxinClient注入
        /// </summary>
        /// <typeparam name="TTaxinClientDependency"></typeparam>
        /// <param name="services"></param>
        /// <returns></returns>
        public static IServiceCollection TaxinClientInject<TTaxinClientDependency>(this IServiceCollection services) where TTaxinClientDependency : class, ITaxinClientStandard, new()
        {
            services.AddSingleton<ITaxinClientStandard, TTaxinClientDependency>();
            ITaxinClientStandard client=new TTaxinClientDependency();
            //client.Start();
            return services;    
        }
        /// <summary>
        /// TaxinClient注入
        /// </summary>
        /// <param name="services"></param>
        /// <returns></returns>
        public static IServiceCollection TaxinClientInject(this IServiceCollection services)
        {
            services.AddSingleton<ITaxinClientStandard, TaxinClientDependency>();
            return services;
        }
        /// <summary>
        /// TaxinServer注入
        /// </summary>
        /// <typeparam name="TTaxinServerDependency"></typeparam>
        /// <param name="services"></param>
        /// <returns></returns>
        public static IServiceCollection TaxinServerInject<TTaxinServerDependency>(this IServiceCollection services) where TTaxinServerDependency : class, ITaxinServerStandard
        {
            services.AddSingleton<ITaxinServerStandard, TTaxinServerDependency>();
            return services;
        }
        /// <summary>
        /// TaxinServer注入
        /// </summary>
        /// <param name="services"></param>
        /// <returns></returns>
        public static IServiceCollection TaxinServerInject(this IServiceCollection services)
        {
            services.AddSingleton<ITaxinServerStandard, TaxinServerDependency>();
            return services;
        }

    }
}
