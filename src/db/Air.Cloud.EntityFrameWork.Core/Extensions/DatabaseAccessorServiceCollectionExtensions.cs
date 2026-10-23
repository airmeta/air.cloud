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
using Air.Cloud.Core.Standard.DataRepository;
using Air.Cloud.Core.Standard.DynamicServer;
using Air.Cloud.EntityFrameWork.Core.Configure;
using Air.Cloud.EntityFrameWork.Core.ContextPools;
using Air.Cloud.EntityFrameWork.Core.Contexts.Dynamic;
using Air.Cloud.EntityFrameWork.Core.Contexts.Enums;
using Air.Cloud.EntityFrameWork.Core.DataRepository;
using Air.Cloud.EntityFrameWork.Core.Extensions.DatabaseProvider;
using Air.Cloud.EntityFrameWork.Core.Internal;
using Air.Cloud.EntityFrameWork.Core.Options;
using Air.Cloud.EntityFrameWork.Core.Repositories;
using Air.Cloud.EntityFrameWork.Core.Repositories.Implantations;
using Air.Cloud.EntityFrameWork.Core.UnitOfWork;
using Air.Cloud.EntityFrameWork.Core.UnitOfWork.Extensions;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
namespace Air.Cloud.EntityFrameWork.Core.Extensions
{
    /// <summary>
    /// <para>zh-cn:数据库访问基础设施和上下文注册扩展。</para>
    /// <para>en-us:Extensions for registering database access infrastructure and database contexts.</para>
    /// </summary>
    [IgnoreScanning]
    public static class DatabaseAccessorServiceCollectionExtensions
    {
        /// <summary>
        /// <para>zh-cn:添加数据库访问基础设施，并注册 DataSourceSettings 的统一选项绑定。</para>
        /// <para>en-us:Adds the database access infrastructure and registers the unified DataSourceSettings options binding.</para>
        /// </summary>
        /// <param name="services"><para>zh-cn:服务集合。</para><para>en-us:The service collection.</para></param>
        /// <param name="configure"><para>zh-cn:可选的附加服务注册回调。</para><para>en-us:An optional callback for additional service registrations.</para></param>
        /// <param name="migrationAssemblyName"><para>zh-cn:可选的迁移程序集名称。</para><para>en-us:The optional migration assembly name.</para></param>
        /// <returns>
        /// <para>zh-cn:已注册数据库访问服务的服务集合。</para>
        /// <para>en-us:The service collection with database access services registered.</para>
        /// </returns>
        public static IServiceCollection AddDatabaseAccessor(this IServiceCollection services, Action<IServiceCollection> configure = null, string migrationAssemblyName = default)
        {
            // Core owns the canonical DataSourceSettings binding. Providers consume the resulting raw string.
            services.AddOptions<DataSourceOptions>()
                .BindConfiguration("DataSourceSettings")
                .ValidateDataAnnotations();

            // 设置迁移类库名称
            if (!string.IsNullOrWhiteSpace(migrationAssemblyName)) Db.MigrationAssemblyName = migrationAssemblyName;

            // 配置数据库上下文
            configure?.Invoke(services);

            // 注册数据库上下文池
            services.TryAddScoped<IDbContextPool, DbContextPool>();

            // 对外公开同一作用域内的本地事务管理器；默认实现与上下文池使用同一个实例。
            services.TryAddScoped<ILocalTransactionManager>(provider =>
            {
                var pool = provider.GetRequiredService<IDbContextPool>();
                return pool as ILocalTransactionManager
                    ?? new LocalTransactionManagerAdapter(pool);
            });

            // 注册全局数据库 Provider 配置器解析器。Provider 包会在自身 Startup 中登记实现。
            services.TryAddSingleton<IDatabaseConfigureResolver, DatabaseConfigureResolver>();

            // 注册多数据库上下文仓储
            services.TryAddScoped(typeof(IRepository<,>), typeof(EFCoreRepository<,>));

            // 注册泛型仓储
            services.TryAddScoped(typeof(IRepository<>), typeof(EFCoreRepository<>));

            // 注册主从库仓储
            services.TryAddScoped(typeof(IMSRepository), typeof(MSRepository));
            services.TryAddScoped(typeof(IMSRepository<>), typeof(MSRepository<>));
            services.TryAddScoped(typeof(IMSRepository<,>), typeof(MSRepository<,>));
            services.TryAddScoped(typeof(IMSRepository<,,>), typeof(MSRepository<,,>));
            services.TryAddScoped(typeof(IMSRepository<,,,>), typeof(MSRepository<,,,>));
            services.TryAddScoped(typeof(IMSRepository<,,,,>), typeof(MSRepository<,,,,>));
            services.TryAddScoped(typeof(IMSRepository<,,,,,>), typeof(MSRepository<,,,,,>));
            services.TryAddScoped(typeof(IMSRepository<,,,,,,>), typeof(MSRepository<,,,,,,>));
            services.TryAddScoped(typeof(IMSRepository<,,,,,,,>), typeof(MSRepository<,,,,,,,>));

            // 注册非泛型仓储
            services.TryAddScoped<IRepository, EFCoreRepository>();

            // 注册多数据库仓储
            services.TryAddScoped(typeof(IDbRepository<>), typeof(DbRepository<>));

            // 注册 Core 通用数据仓储抽象适配器
            services.TryAddScoped<IDataRepositoryAccessor, EntityFrameworkDataRepositoryAccessor>();
            services.TryAddScoped(typeof(IDataRepository<>), typeof(EntityFrameworkDataRepository<>));

            // 注册解析数据库上下文委托
            services.TryAddScoped(provider =>
            {
                DbContext dbContextResolve(Type locator, IScoped transient)
                {
                    return ResolveDbContext(provider, locator);
                }
                return (Func<Type, IScoped, DbContext>)dbContextResolve;
            });
            // 注册全局工作单元过滤器
            services.AddUnitOfWork<EFCoreUnitOfWork>();

            return services;
        }

        /// <summary>
        /// <para>zh-cn:注册默认数据库上下文与主定位器。</para>
        /// <para>en-us:Registers the default database context with the master locator.</para>
        /// </summary>
        /// <typeparam name="TDbContext"><para>zh-cn:数据库上下文类型。</para><para>en-us:The database context type.</para></typeparam>
        /// <param name="services"><para>zh-cn:服务集合。</para><para>en-us:The service collection.</para></param>
        /// <returns><para>zh-cn:已注册上下文的服务集合。</para><para>en-us:The service collection with the context registered.</para></returns>
        public static IServiceCollection RegisterDbContext<TDbContext>(this IServiceCollection services)
            where TDbContext : DbContext
        {
            return services.RegisterDbContext<TDbContext, MasterDbContextLocator>();
        }

        /// <summary>
        /// <para>zh-cn:注册数据库上下文与指定定位器。</para>
        /// <para>en-us:Registers a database context with the specified locator.</para>
        /// </summary>
        /// <typeparam name="TDbContext"><para>zh-cn:数据库上下文类型。</para><para>en-us:The database context type.</para></typeparam>
        /// <typeparam name="TDbContextLocator"><para>zh-cn:数据库上下文定位器类型。</para><para>en-us:The database context locator type.</para></typeparam>
        /// <param name="services"><para>zh-cn:服务集合。</para><para>en-us:The service collection.</para></param>
        /// <returns><para>zh-cn:已注册上下文的服务集合。</para><para>en-us:The service collection with the context registered.</para></returns>
        public static IServiceCollection RegisterDbContext<TDbContext, TDbContextLocator>(this IServiceCollection services)
            where TDbContext : DbContext
            where TDbContextLocator : class, IDbContextLocator
        {
            // 存储数据库上下文和定位器关系
            var locatorType = typeof(TDbContextLocator);
            var contextType = typeof(TDbContext);
            if (Penetrates.DbContextDescriptors.TryGetValue(locatorType, out var registeredContextType)
                && registeredContextType != contextType)
            {
                throw new InvalidOperationException($"Database locator `{locatorType.FullName}` is already bound to `{registeredContextType.FullName}` and cannot be rebound to `{contextType.FullName}`.");
            }

            Penetrates.DbContextDescriptors.TryAdd(locatorType, contextType);

            // 注册数据库上下文
            services.TryAddScoped<TDbContext>();

            return services;
        }

        /// <summary>
        /// 通过定位器解析上下文
        /// <para>zh-cn:该成员的数据库访问行为。</para>
        /// <para>en-us:Database access behavior provided by this member.</para>
        /// </summary>
        /// <param name="provider"><para>zh-cn:参数值。</para><para>en-us:The provider value.</para></param>
        /// <param name="dbContextLocator"><para>zh-cn:参数值。</para><para>en-us:The dbContextLocator value.</para></param>
        /// <returns><para>zh-cn:操作结果。</para><para>en-us:The operation result.</para></returns>
        private static DbContext ResolveDbContext(IServiceProvider provider, Type dbContextLocator)
        {
            // 判断数据库上下文定位器是否绑定
            Penetrates.CheckDbContextLocator(dbContextLocator, out var dbContextType);

            // 动态解析数据库上下文
            var dbContext = provider.GetService(dbContextType) as DbContext;
            if (dbContext == null)
            {
                throw new InvalidOperationException($"DbContext {dbContextType.FullName} was not registered.");
            }

            // 实现动态数据库上下文功能，刷新 OnModelCreating
            var dbContextAttribute = DbProvider.GetAppDbContextAttribute(dbContextType);
            if (dbContextAttribute?.Mode == DbContextMode.Dynamic)
            {
                DynamicModelCacheKeyFactory.RebuildModels();
            }

            // 添加数据库上下文到池中
            var dbContextPool = provider.GetService<IDbContextPool>();
            dbContextPool?.AddToPool(dbContext);

            return dbContext;
        }
    }
}

