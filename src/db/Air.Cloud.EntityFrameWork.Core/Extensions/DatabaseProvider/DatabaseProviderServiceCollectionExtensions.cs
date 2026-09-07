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
using Air.Cloud.EntityFrameWork.Core.Options;

using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

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
        /// <param name="connectionMetadata"><para>zh-cn:连接名称，读取自 DataSourceSettings:ConnectionStrings；旧连接字符串和配置路径调用保持兼容。</para><para>en-us:Connection name read from DataSourceSettings:ConnectionStrings; legacy raw strings and configuration paths remain compatible.</para></param>
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
        /// <param name="connectionMetadata"><para>zh-cn:连接名称，读取自 DataSourceSettings:ConnectionStrings；旧连接字符串和配置路径调用保持兼容。</para><para>en-us:Connection name read from DataSourceSettings:ConnectionStrings; legacy raw strings and configuration paths remain compatible.</para></param>
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
            var connectionMetadataValue = DbProvider.GetConnectionMetadata<TDbContext>(connectionMetadata);
            services.AddDbContextPool<TDbContext>(Penetrates.ConfigureDbContext((serviceProvider, options) =>
            {
                var connStr = DataSourceConnectionStringResolver.Resolve(
                    AppConfiguration.Configuration,
                    serviceProvider.GetService<IOptionsMonitor<DataSourceOptions>>()?.CurrentValue,
                    connectionMetadataValue);
                var _options = ConfigureDatabase<TDbContext, TDbContextLocator>(serviceProvider, connStr, options);
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
        /// <param name="connectionMetadata"><para>zh-cn:连接名称，读取自 DataSourceSettings:ConnectionStrings；旧连接字符串和配置路径调用保持兼容。</para><para>en-us:Connection name read from DataSourceSettings:ConnectionStrings; legacy raw strings and configuration paths remain compatible.</para></param>
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
        /// <param name="optionBuilder"></param>
        /// <param name="connectionMetadata"><para>zh-cn:连接名称，读取自 DataSourceSettings:ConnectionStrings；旧连接字符串和配置路径调用保持兼容。</para><para>en-us:Connection name read from DataSourceSettings:ConnectionStrings; legacy raw strings and configuration paths remain compatible.</para></param>
        /// <param name="interceptors">拦截器</param>
        /// <returns>服务集合</returns>
        public static IServiceCollection AddDb<TDbContext, TDbContextLocator>(this IServiceCollection services, Action<IServiceProvider, DbContextOptionsBuilder> optionBuilder = null, string connectionMetadata = default, params IInterceptor[] interceptors)
            where TDbContext : DbContext
            where TDbContextLocator : class, IDbContextLocator
        {
            // 注册数据库上下文
            services.RegisterDbContext<TDbContext, TDbContextLocator>();

            // 配置数据库上下文
            var connectionMetadataValue = DbProvider.GetConnectionMetadata<TDbContext>(connectionMetadata);
            services.AddDbContext<TDbContext>(Penetrates.ConfigureDbContext((serviceProvider, options) =>
            {
                var connStr = DataSourceConnectionStringResolver.Resolve(
                    AppConfiguration.Configuration,
                    serviceProvider.GetService<IOptionsMonitor<DataSourceOptions>>()?.CurrentValue,
                    connectionMetadataValue);
                var _options = ConfigureDatabase<TDbContext, TDbContextLocator>(serviceProvider, connStr, options);
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
        /// <typeparam name="TDbContextLocator"></typeparam>
        /// <param name="serviceProvider">当前数据库上下文配置所使用的服务提供器。</param>
        /// <param name="connectionMetadata"><para>zh-cn:由 Core 解析后的原始连接字符串。</para><para>en-us:The raw connection string resolved by Core.</para></param>
        /// <param name="options">数据库上下文选项构建器</param>
        private static DbContextOptionsBuilder ConfigureDatabase<TDbContext, TDbContextLocator>(IServiceProvider serviceProvider, string connectionMetadata, DbContextOptionsBuilder options)
             where TDbContext : DbContext
             where TDbContextLocator : class, IDbContextLocator
        {
            var resolver = serviceProvider.GetRequiredService<IDatabaseConfigureResolver>();
            var configure = resolver.Resolve(typeof(TDbContext), typeof(TDbContextLocator));
            var dbContextOptionsBuilder = configure.Configure<TDbContext>(options, connectionMetadata);
            // 解决分表分库
            var dbContextAttribute = DbProvider.GetAppDbContextAttribute(typeof(TDbContext));
            if (dbContextAttribute?.Mode == DbContextMode.Dynamic) 
                dbContextOptionsBuilder.ReplaceService<IModelCacheKeyFactory, DynamicModelCacheKeyFactory>();

            return dbContextOptionsBuilder;
        }
    }
}

