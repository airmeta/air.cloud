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

using Air.Cloud.Core.Standard.DataBase.Locators;
using Air.Cloud.EntityFrameWork.Core.Configure;
using Air.Cloud.EntityFrameWork.Core.Contexts.Dynamic;
using Air.Cloud.EntityFrameWork.Core.Contexts.Enums;
using Air.Cloud.EntityFrameWork.Core.Internal;

using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace Air.Cloud.EntityFrameWork.Core.Extensions.DatabaseProvider
{
    /// <summary>
    /// Sqlite 数据库服务拓展
    /// </summary>
    [IgnoreScanning]
    public static class DatabaseProviderServiceCollectionExtensions
    {
        /// <summary>
        /// 添加默认数据库上下文
        /// </summary>
        /// <typeparam name="TDbContext">数据库上下文</typeparam>
        /// <param name="services">服务</param>
        /// <param name="optionBuilder"></param>
        /// <param name="connectionMetadata">支持数据库连接字符串，配置文件的 ConnectionStrings 中的Key或 配置文件的完整的配置路径，如果是内存数据库，则为数据库名称</param>
        /// <param name="poolSize">池大小</param>
        /// <param name="interceptors">拦截器</param>
        /// <returns>服务集合</returns>
        public static IServiceCollection AddDbPool<TDbContext>(this IServiceCollection services, Action<IServiceProvider, DbContextOptionsBuilder> optionBuilder = null, string connectionMetadata = default, int poolSize = 100, params IInterceptor[] interceptors)
            where TDbContext : DbContext
        {
            // 注册数据库上下文
            return services.AddDbPool<TDbContext, MasterDbContextLocator>(optionBuilder, connectionMetadata, poolSize, interceptors);
        }
        /// <summary>
        /// 添加其他数据库上下文
        /// </summary>
        /// <typeparam name="TDbContext">数据库上下文</typeparam>
        /// <typeparam name="TDbContextLocator">数据库上下文定位器</typeparam>
        /// <param name="services">服务</param>
        /// <param name="optionBuilder"></param>
        /// <param name="connectionMetadata">支持数据库连接字符串，配置文件的 ConnectionStrings 中的Key或 配置文件的完整的配置路径，如果是内存数据库，则为数据库名称</param>
        /// <param name="poolSize">池大小</param>
        /// <param name="interceptors">拦截器</param>
        /// <returns>服务集合</returns>
        public static IServiceCollection AddDbPool<TDbContext, TDbContextLocator>(this IServiceCollection services, Action<IServiceProvider, DbContextOptionsBuilder> optionBuilder = null, string connectionMetadata = default, int poolSize = 100, params IInterceptor[] interceptors)
            where TDbContext : DbContext
            where TDbContextLocator : class, IDbContextLocator
        {
            // 注册数据库上下文
            services.RegisterDbContext<TDbContext, TDbContextLocator>();

            // 配置数据库上下文
            var connStr = DbProvider.GetConnectionString<TDbContext>(connectionMetadata);
            services.AddDbContextPool<TDbContext>(Penetrates.ConfigureDbContext((serviceProvider, options) =>
            {
                var _options = ConfigureDatabase<TDbContext>(connStr, options);
                optionBuilder?.Invoke(serviceProvider, _options);
            }, interceptors), poolSize: poolSize);

            return services;
        }

        /// <summary>
        /// 添加其他数据库上下文
        /// </summary>
        /// <typeparam name="TDbContext">数据库上下文</typeparam>
        /// <typeparam name="TDbContextLocator">数据库上下文定位器</typeparam>
        /// <param name="services">服务</param>
        /// <param name="optionBuilder">自定义配置</param>
        /// <param name="poolSize">池大小</param>
        /// <param name="interceptors">拦截器</param>
        /// <returns>服务集合</returns>
        public static IServiceCollection AddDbPool<TDbContext, TDbContextLocator>(this IServiceCollection services, Action<IServiceProvider, DbContextOptionsBuilder> optionBuilder, int poolSize = 100, params IInterceptor[] interceptors)
            where TDbContext : DbContext
            where TDbContextLocator : class, IDbContextLocator
        {
            // 注册数据库上下文
            services.RegisterDbContext<TDbContext, TDbContextLocator>();

            // 配置数据库上下文
            services.AddDbContextPool<TDbContext>(Penetrates.ConfigureDbContext(optionBuilder, interceptors), poolSize: poolSize);

            return services;
        }

        /// <summary>
        ///  添加默认数据库上下文
        /// </summary>
        /// <typeparam name="TDbContext">数据库上下文</typeparam>
        /// <param name="services">服务</param>
        /// <param name="optionBuilder">自定义配置</param>
        /// <param name="connectionMetadata">支持数据库连接字符串，配置文件的 ConnectionStrings 中的Key或 配置文件的完整的配置路径，如果是内存数据库，则为数据库名称</param>
        /// <param name="interceptors">拦截器</param>
        /// <returns>服务集合</returns>
        public static IServiceCollection AddDb<TDbContext>(this IServiceCollection services,Action<IServiceProvider, DbContextOptionsBuilder> optionBuilder = null, string connectionMetadata = default, params IInterceptor[] interceptors)
            where TDbContext : DbContext
        {
            // 注册数据库上下文
            return services.AddDb<TDbContext, MasterDbContextLocator>(optionBuilder, connectionMetadata, interceptors);
        }

        /// <summary>
        ///  添加默认数据库上下文
        /// </summary>
        /// <typeparam name="TDbContext">数据库上下文</typeparam>
        /// <param name="services">服务</param>
        /// <param name="optionBuilder">自定义配置</param>
        /// <param name="interceptors">拦截器</param>
        /// <returns>服务集合</returns>
        public static IServiceCollection AddDb<TDbContext>(this IServiceCollection services, Action<IServiceProvider, DbContextOptionsBuilder> optionBuilder, params IInterceptor[] interceptors)
            where TDbContext : DbContext
        {
            // 注册数据库上下文
            return services.AddDb<TDbContext, MasterDbContextLocator>(optionBuilder, interceptors);
        }

        /// <summary>
        /// 添加数据库上下文
        /// </summary>
        /// <typeparam name="TDbContext">数据库上下文</typeparam>
        /// <typeparam name="TDbContextLocator">数据库上下文定位器</typeparam>
        /// <param name="services">服务</param>
        /// <param name="providerName">数据库提供器</param>
        /// <param name="optionBuilder"></param>
        /// <param name="connectionMetadata">支持数据库连接字符串，配置文件的 ConnectionStrings 中的Key或 配置文件的完整的配置路径，如果是内存数据库，则为数据库名称</param>
        /// <param name="interceptors">拦截器</param>
        /// <returns>服务集合</returns>
        public static IServiceCollection AddDb<TDbContext, TDbContextLocator>(this IServiceCollection services, Action<IServiceProvider, DbContextOptionsBuilder> optionBuilder = null, string connectionMetadata = default, params IInterceptor[] interceptors)
            where TDbContext : DbContext
            where TDbContextLocator : class, IDbContextLocator
        {
            // 注册数据库上下文
            services.RegisterDbContext<TDbContext, TDbContextLocator>();

            // 配置数据库上下文
            var connStr = DbProvider.GetConnectionString<TDbContext>(connectionMetadata);
            services.AddDbContext<TDbContext>(Penetrates.ConfigureDbContext((serviceProvider, options) =>
            {
                var _options = ConfigureDatabase<TDbContext>(connStr, options);
                optionBuilder?.Invoke(serviceProvider, _options);
            }, interceptors));

            return services;
        }

        /// <summary>
        /// 添加数据库上下文
        /// </summary>
        /// <typeparam name="TDbContext">数据库上下文</typeparam>
        /// <typeparam name="TDbContextLocator">数据库上下文定位器</typeparam>
        /// <param name="services">服务</param>
        /// <param name="optionBuilder">自定义配置</param>
        /// <param name="interceptors">拦截器</param>
        /// <returns>服务集合</returns>
        public static IServiceCollection AddDb<TDbContext, TDbContextLocator>(this IServiceCollection services, Action<IServiceProvider, DbContextOptionsBuilder> optionBuilder, params IInterceptor[] interceptors)
            where TDbContext : DbContext
            where TDbContextLocator : class, IDbContextLocator
        {
            // 注册数据库上下文
            services.RegisterDbContext<TDbContext, TDbContextLocator>();

            // 配置数据库上下文
            services.AddDbContext<TDbContext>(Penetrates.ConfigureDbContext(optionBuilder, interceptors));

            return services;
        }

        /// <summary>
        /// 配置数据库
        /// </summary>
        /// <typeparam name="TDbContext"></typeparam>
        /// <param name="connectionMetadata">支持数据库连接字符串，配置文件的 ConnectionStrings 中的Key或 配置文件的完整的配置路径，如果是内存数据库，则为数据库名称</param>
        /// <param name="options">数据库上下文选项构建器</param>
        private static DbContextOptionsBuilder ConfigureDatabase<TDbContext>(string connectionMetadata, DbContextOptionsBuilder options)
             where TDbContext : DbContext
        {
            var configure = AppCore.GetService<IDatabaseConfigure>();
            var dbContextOptionsBuilder = configure.Configure<TDbContext>(options, connectionMetadata);
            // 解决分表分库
            var dbContextAttribute = DbProvider.GetAppDbContextAttribute(typeof(TDbContext));
            if (dbContextAttribute?.Mode == DbContextMode.Dynamic) 
                dbContextOptionsBuilder.ReplaceService<IModelCacheKeyFactory, DynamicModelCacheKeyFactory>();

            return dbContextOptionsBuilder;
        }
    }
}

