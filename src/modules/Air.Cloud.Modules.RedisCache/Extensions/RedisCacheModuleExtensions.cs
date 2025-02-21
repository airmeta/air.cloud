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
using Air.Cloud.Core.Standard.Cache;
using Air.Cloud.Core.Standard.Cache.Redis;

using Microsoft.Extensions.DependencyInjection;

namespace Air.Cloud.Modules.RedisCache.Extensions
{
    public static class RedisCacheModuleExtensions
    {
        public static IServiceCollection AddRedisCacheService<TRedisCacheModuleDependency>(this IServiceCollection services) where TRedisCacheModuleDependency : class, IRedisCacheStandard, new()
        {
            services.AddSingleton<IAppCacheStandard, TRedisCacheModuleDependency>();
            services.AddSingleton<IRedisCacheStandard, TRedisCacheModuleDependency>();
            return services;
        }

    }
}
