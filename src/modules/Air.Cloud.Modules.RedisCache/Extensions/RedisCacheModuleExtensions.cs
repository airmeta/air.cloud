using Air.Cloud.Core.App;
using Air.Cloud.Core.Standard;
using Air.Cloud.Core.Standard.Cache;
using Air.Cloud.Core.Standard.Cache.Redis;
using Air.Cloud.Modules.RedisCache.Dependencies;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Air.Cloud.Modules.RedisCache.Extensions
{
    public static class RedisCacheModuleExtensions
    {
        public static IServiceCollection WebRedisCacheInject<TRedisCacheModuleDependency>(this IServiceCollection services) where TRedisCacheModuleDependency : class, IRedisCacheStandard, new()
        {
            services.AddSingleton<IAppCacheStandard, TRedisCacheModuleDependency>();
            services.AddSingleton<IRedisCacheStandard, TRedisCacheModuleDependency>();
            return services;
        }

        //public static void HostRedisCacheInject<TRedisCacheModuleDependency>() where TRedisCacheModuleDependency : class, IRedisCacheStandard, new()
        //{
        //    AppStandardRealization.SetDependency<IAppCacheStandard>(new TRedisCacheModuleDependency());
        //    AppStandardRealization.SetDependency<IRedisCacheStandard>(new TRedisCacheModuleDependency());
        //}
    }
}
