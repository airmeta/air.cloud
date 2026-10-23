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
    /// <para>zh-cn:注册 Entity Framework Core 数据库上下文和上下文池的扩展方法。</para>
    /// <para>en-us:Extension methods for registering Entity Framework Core database contexts and context pools.</para>
    /// </summary>
    [IgnoreScanning]
    public static class DatabaseProviderServiceCollectionExtensions
    {
        /// <summary>
        /// <para>zh-cn:注册使用主数据库定位器的 EF Core 上下文池。</para>
        /// <para>en-us:Registers an EF Core context pool using the master database locator.</para>
        /// </summary>
        /// <typeparam name="TDbContext"><para>zh-cn:数据库上下文</para><para>en-us:The generic parameter type.</para></typeparam>
        /// <param name="services"><para>zh-cn:服务集合。</para><para>en-us:The service collection.</para></param>
        /// <param name="optionBuilder"><para>zh-cn:可选的上下文选项配置回调。</para><para>en-us:An optional callback for configuring context options.</para></param>
        /// <param name="connectionMetadata"><para>zh-cn:连接名称，读取自 DataSourceSettings:ConnectionStrings；旧连接字符串和配置路径调用保持兼容。</para><para>en-us:Connection name read from DataSourceSettings:ConnectionStrings; legacy raw strings and configuration paths remain compatible.</para></param>
        /// <param name="poolSize"><para>zh-cn:上下文池最大容量，默认 100。</para><para>en-us:The maximum context-pool size; defaults to 100.</para></param>
        /// <param name="interceptors"><para>zh-cn:可选的 EF Core 拦截器。</para><para>en-us:Optional EF Core interceptors.</para></param>
        /// <returns><para>zh-cn:已注册上下文池的服务集合。</para><para>en-us:The service collection with the context pool registered.</para></returns>
        public static IServiceCollection AddDbPool<TDbContext>(this IServiceCollection services, Action<IServiceProvider, DbContextOptionsBuilder> optionBuilder = null, string connectionMetadata = default, int poolSize = 100, params IInterceptor[] interceptors)
            where TDbContext : DbContext
        {
            // 注册数据库上下文
            return services.AddDbPool<TDbContext, MasterDbContextLocator>(optionBuilder, connectionMetadata, poolSize, interceptors);
        }
        /// <summary>
        /// <para>zh-cn:注册使用指定数据库定位器的 EF Core 上下文池。</para>
        /// <para>en-us:Registers an EF Core context pool using the specified database locator.</para>
        /// </summary>
        /// <typeparam name="TDbContext"><para>zh-cn:数据库上下文</para><para>en-us:The generic parameter type.</para></typeparam>
        /// <typeparam name="TDbContextLocator"><para>zh-cn:数据库上下文定位器</para><para>en-us:The generic parameter type.</para></typeparam>
        /// <param name="services"><para>zh-cn:服务集合。</para><para>en-us:The service collection.</para></param>
        /// <param name="optionBuilder"><para>zh-cn:可选的上下文选项配置回调。</para><para>en-us:An optional callback for configuring context options.</para></param>
        /// <param name="connectionMetadata"><para>zh-cn:连接名称，读取自 DataSourceSettings:ConnectionStrings；旧连接字符串和配置路径调用保持兼容。</para><para>en-us:Connection name read from DataSourceSettings:ConnectionStrings; legacy raw strings and configuration paths remain compatible.</para></param>
        /// <param name="poolSize"><para>zh-cn:上下文池最大容量，默认 100。</para><para>en-us:The maximum context-pool size; defaults to 100.</para></param>
        /// <param name="interceptors"><para>zh-cn:可选的 EF Core 拦截器。</para><para>en-us:Optional EF Core interceptors.</para></param>
        /// <returns><para>zh-cn:已注册上下文池的服务集合。</para><para>en-us:The service collection with the context pool registered.</para></returns>
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
        /// <para>zh-cn:注册使用调用方选项配置回调的 EF Core 上下文池。</para>
        /// <para>en-us:Registers an EF Core context pool configured by the caller's options callback.</para>
        /// </summary>
        /// <typeparam name="TDbContext"><para>zh-cn:数据库上下文</para><para>en-us:The generic parameter type.</para></typeparam>
        /// <typeparam name="TDbContextLocator"><para>zh-cn:数据库上下文定位器</para><para>en-us:The generic parameter type.</para></typeparam>
        /// <param name="services"><para>zh-cn:服务</para><para>en-us:The services value.</para></param>
        /// <param name="optionBuilder"><para>zh-cn:自定义配置</para><para>en-us:The optionBuilder value.</para></param>
        /// <param name="poolSize"><para>zh-cn:池大小</para><para>en-us:The poolSize value.</para></param>
        /// <param name="interceptors"><para>zh-cn:拦截器</para><para>en-us:The interceptors value.</para></param>
        /// <returns><para>zh-cn:服务集合</para><para>en-us:The operation result.</para></returns>
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
        /// <para>zh-cn:注册使用主数据库定位器的 EF Core 上下文。</para>
        /// <para>en-us:Registers an EF Core context using the master database locator.</para>
        /// </summary>
        /// <typeparam name="TDbContext"><para>zh-cn:数据库上下文</para><para>en-us:The generic parameter type.</para></typeparam>
        /// <param name="services"><para>zh-cn:服务</para><para>en-us:The services value.</para></param>
        /// <param name="optionBuilder"><para>zh-cn:自定义配置</para><para>en-us:The optionBuilder value.</para></param>
        /// <param name="connectionMetadata"><para>zh-cn:连接名称，读取自 DataSourceSettings:ConnectionStrings；旧连接字符串和配置路径调用保持兼容。</para><para>en-us:Connection name read from DataSourceSettings:ConnectionStrings; legacy raw strings and configuration paths remain compatible.</para></param>
        /// <param name="interceptors"><para>zh-cn:拦截器</para><para>en-us:The interceptors value.</para></param>
        /// <returns><para>zh-cn:服务集合</para><para>en-us:The operation result.</para></returns>
        public static IServiceCollection AddDb<TDbContext>(this IServiceCollection services,Action<IServiceProvider, DbContextOptionsBuilder> optionBuilder = null, string connectionMetadata = default, params IInterceptor[] interceptors)
            where TDbContext : DbContext
        {
            // 注册数据库上下文
            return services.AddDb<TDbContext, MasterDbContextLocator>(optionBuilder, connectionMetadata, interceptors);
        }

        /// <summary>
        /// <para>zh-cn:注册使用调用方选项配置回调的主数据库上下文。</para>
        /// <para>en-us:Registers the master database context configured by the caller's options callback.</para>
        /// </summary>
        /// <typeparam name="TDbContext"><para>zh-cn:数据库上下文</para><para>en-us:The generic parameter type.</para></typeparam>
        /// <param name="services"><para>zh-cn:服务</para><para>en-us:The services value.</para></param>
        /// <param name="optionBuilder"><para>zh-cn:自定义配置</para><para>en-us:The optionBuilder value.</para></param>
        /// <param name="interceptors"><para>zh-cn:拦截器</para><para>en-us:The interceptors value.</para></param>
        /// <returns><para>zh-cn:服务集合</para><para>en-us:The operation result.</para></returns>
        public static IServiceCollection AddDb<TDbContext>(this IServiceCollection services, Action<IServiceProvider, DbContextOptionsBuilder> optionBuilder, params IInterceptor[] interceptors)
            where TDbContext : DbContext
        {
            // 注册数据库上下文
            return services.AddDb<TDbContext, MasterDbContextLocator>(optionBuilder, interceptors);
        }

        /// <summary>
        /// <para>zh-cn:注册使用指定定位器和连接元数据的 EF Core 上下文。</para>
        /// <para>en-us:Registers an EF Core context using the specified locator and connection metadata.</para>
        /// </summary>
        /// <typeparam name="TDbContext"><para>zh-cn:数据库上下文</para><para>en-us:The generic parameter type.</para></typeparam>
        /// <typeparam name="TDbContextLocator"><para>zh-cn:数据库上下文定位器</para><para>en-us:The generic parameter type.</para></typeparam>
        /// <param name="services"><para>zh-cn:服务</para><para>en-us:The services value.</para></param>
        /// <param name="optionBuilder"><para>zh-cn:上下文选项配置回调。</para><para>en-us:The context-options configuration callback.</para></param>
        /// <param name="connectionMetadata"><para>zh-cn:连接名称，读取自 DataSourceSettings:ConnectionStrings；旧连接字符串和配置路径调用保持兼容。</para><para>en-us:Connection name read from DataSourceSettings:ConnectionStrings; legacy raw strings and configuration paths remain compatible.</para></param>
        /// <param name="interceptors"><para>zh-cn:拦截器</para><para>en-us:The interceptors value.</para></param>
        /// <returns><para>zh-cn:服务集合</para><para>en-us:The operation result.</para></returns>
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
        /// <para>zh-cn:注册使用调用方选项配置回调的指定数据库上下文。</para>
        /// <para>en-us:Registers the specified database context configured by the caller's options callback.</para>
        /// </summary>
        /// <typeparam name="TDbContext"><para>zh-cn:数据库上下文</para><para>en-us:The generic parameter type.</para></typeparam>
        /// <typeparam name="TDbContextLocator"><para>zh-cn:数据库上下文定位器</para><para>en-us:The generic parameter type.</para></typeparam>
        /// <param name="services"><para>zh-cn:服务</para><para>en-us:The services value.</para></param>
        /// <param name="optionBuilder"><para>zh-cn:自定义配置</para><para>en-us:The optionBuilder value.</para></param>
        /// <param name="interceptors"><para>zh-cn:拦截器</para><para>en-us:The interceptors value.</para></param>
        /// <returns><para>zh-cn:服务集合</para><para>en-us:The operation result.</para></returns>
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
        /// <para>zh-cn:解析连接并应用数据库 Provider 的上下文配置。</para>
        /// <para>en-us:Resolves the connection and applies the database-provider context configuration.</para>
        /// </summary>
        /// <typeparam name="TDbContext"><para>zh-cn:要配置的数据库上下文类型。</para><para>en-us:The database context type to configure.</para></typeparam>
        /// <typeparam name="TDbContextLocator"><para>zh-cn:用于定位该上下文的定位器类型。</para><para>en-us:The locator type used to identify the context.</para></typeparam>
        /// <param name="serviceProvider"><para>zh-cn:当前数据库上下文配置使用的服务提供器。</para><para>en-us:The service provider used for the current context configuration.</para></param>
        /// <param name="connectionMetadata"><para>zh-cn:由 Core 解析后的原始连接字符串。</para><para>en-us:The raw connection string resolved by Core.</para></param>
        /// <param name="options"><para>zh-cn:待配置的数据库上下文选项构建器。</para><para>en-us:The database-context options builder to configure.</para></param>
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

