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
using Air.Cloud.Core.Modules.AppPrint;
using Air.Cloud.Core.Standard.DataBase.Locators;
using Air.Cloud.Core.Standard.DataBase.Model;
using Air.Cloud.Core.Standard.DynamicServer;
using Air.Cloud.EntityFrameWork.Core.Extensions.DatabaseProvider;
using Air.Cloud.EntityFrameWork.Core.Repositories;

namespace Air.Cloud.EntityFrameWork.Core;

/// <summary>
/// <para>zh-cn:数据库访问静态入口，用于解析仓储和数据库上下文。</para>
/// <para>en-us:Static database access entry point for resolving repositories and database contexts.</para>
/// </summary>
[IgnoreScanning]
public static class Db
{
    /// <summary>
    /// <para>zh-cn:数据库迁移程序集名称。</para>
    /// <para>en-us:The database migration assembly name.</para>
    /// </summary>
    public static string MigrationAssemblyName = "Air.Database.Migrations";

    /// <summary>
    /// <para>zh-cn:跟踪日志标识</para>
    /// <para>en-us:Trace log tag</para>
    /// </summary>
    internal static Dictionary<string, string> TraceLogTags = new Dictionary<string, string>()
    {
        { "db_log",AppPrintConstType.ORM_EXEC_TYPE }
    };

    /// <summary>
    /// <para>zh-cn:获取非泛型数据库仓储。</para>
    /// <para>en-us:Gets the non-generic database repository.</para>
    /// </summary>
    /// <param name="serviceProvider"><para>zh-cn:可选的服务提供器；为空时使用全局服务。</para><para>en-us:An optional service provider; global services are used when null.</para></param>
    /// <returns><para>zh-cn:非泛型数据库仓储。</para><para>en-us:The non-generic database repository.</para></returns>
    public static IRepository GetRepository(IServiceProvider? serviceProvider = default)
    {
        return serviceProvider == null ? AppCore.GetService<IRepository>() : AppCore.GetService<IRepository>(serviceProvider);
    }

    /// <summary>
    /// <para>zh-cn:获取指定实体类型的数据库仓储。</para>
    /// <para>en-us:Gets the database repository for the specified entity type.</para>
    /// </summary>
    /// <typeparam name="TEntity"><para>zh-cn:实体类型。</para><para>en-us:The entity type.</para></typeparam>
    /// <param name="serviceProvider"><para>zh-cn:可选的服务提供器；为空时使用全局服务。</para><para>en-us:An optional service provider; global services are used when null.</para></param>
    /// <returns><para>zh-cn:指定实体类型的仓储。</para><para>en-us:The repository for the specified entity type.</para></returns>
    public static IRepository<TEntity> GetRepository<TEntity>(IServiceProvider? serviceProvider = default)
        where TEntity : class, IPrivateEntity, new()
    {
        return serviceProvider == null ? AppCore.GetService<IRepository<TEntity>>() : AppCore.GetService<IRepository<TEntity>>(serviceProvider);
    }

    /// <summary>
    /// <para>zh-cn:获取指定实体类型和上下文定位器的数据库仓储。</para>
    /// <para>en-us:Gets the repository for the specified entity type and context locator.</para>
    /// </summary>
    /// <typeparam name="TEntity"><para>zh-cn:实体类型。</para><para>en-us:The entity type.</para></typeparam>
    /// <typeparam name="TDbContextLocator"><para>zh-cn:数据库上下文定位器类型。</para><para>en-us:The database context locator type.</para></typeparam>
    /// <param name="serviceProvider"><para>zh-cn:可选的服务提供器；为空时使用全局服务。</para><para>en-us:An optional service provider; global services are used when null.</para></param>
    /// <returns><para>zh-cn:指定实体和定位器的仓储。</para><para>en-us:The repository for the specified entity and locator.</para></returns>
    public static IRepository<TEntity, TDbContextLocator> GetRepository<TEntity, TDbContextLocator>(IServiceProvider? serviceProvider = default)
        where TEntity : class, IPrivateEntity, new()
        where TDbContextLocator : class, IDbContextLocator
    {
        return serviceProvider == null ? AppCore.GetService<IRepository<TEntity, TDbContextLocator>>() : AppCore.GetService<IRepository<TEntity, TDbContextLocator>>(serviceProvider);
    }

    /// <summary>
    /// <para>zh-cn:根据定位器类型获取指定实体的数据库仓储。</para>
    /// <para>en-us:Gets the database repository for an entity using a locator type.</para>
    /// </summary>
    /// <typeparam name="TEntity"><para>zh-cn:实体类型。</para><para>en-us:The entity type.</para></typeparam>
    /// <param name="dbContextLocator"><para>zh-cn:数据库上下文定位器类型。</para><para>en-us:The database context locator type.</para></param>
    /// <param name="serviceProvider"><para>zh-cn:可选的服务提供器。</para><para>en-us:An optional service provider.</para></param>
    /// <returns><para>zh-cn:指定实体和定位器的私有仓储。</para><para>en-us:The private repository for the entity and locator.</para></returns>
    public static IPrivateRepository<TEntity> GetRepository<TEntity>(Type dbContextLocator, IServiceProvider? serviceProvider = default)
         where TEntity : class, IPrivateEntity, new()
    {
        var repositoryServiceType = typeof(IRepository<,>).MakeGenericType(typeof(TEntity), dbContextLocator);
        var repository = serviceProvider == null
            ? AppCore.GetService(repositoryServiceType)
            : AppCore.GetService(repositoryServiceType, serviceProvider);

        return repository as IPrivateRepository<TEntity>
            ?? throw new InvalidOperationException($"Repository for entity {typeof(TEntity).FullName} and locator {dbContextLocator.FullName} was not registered.");
    }

    /// <summary>
    /// <para>zh-cn:获取指定数据库上下文定位器的仓储。</para>
    /// <para>en-us:Gets the repository for the specified database-context locator.</para>
    /// </summary>
    /// <typeparam name="TDbContextLocator"><para>zh-cn:数据库上下文定位器类型。</para><para>en-us:The database context locator type.</para></typeparam>
    /// <param name="serviceProvider"><para>zh-cn:服务提供器。</para><para>en-us:The service provider.</para></param>
    /// <returns><para>zh-cn:指定定位器的数据库仓储。</para><para>en-us:The database repository for the specified locator.</para></returns>
    public static IDbRepository<TDbContextLocator> GetDbRepository<TDbContextLocator>(IServiceProvider serviceProvider = default)
        where TDbContextLocator : class, IDbContextLocator
    {
        return AppCore.GetService<IDbRepository<TDbContextLocator>>(serviceProvider);
    }

    /// <summary>
    /// <para>zh-cn:获取主从数据库仓储。</para>
    /// <para>en-us:Gets the master-slave database repository.</para>
    /// </summary>
    /// <param name="serviceProvider"><para>zh-cn:服务提供器。</para><para>en-us:The service provider.</para></param>
    /// <returns><para>zh-cn:主从数据库仓储。</para><para>en-us:The master-slave database repository.</para></returns>
    public static IMSRepository GetMSRepository(IServiceProvider serviceProvider = default)
    {
        return AppCore.GetService<IMSRepository>(serviceProvider);
    }

    /// <summary>
    /// <para>zh-cn:获取指定主库定位器的主从数据库仓储。</para>
    /// <para>en-us:Gets the master-slave repository for the specified master locator.</para>
    /// </summary>
    /// <typeparam name="TMasterDbContextLocator"><para>zh-cn:主库数据库上下文定位器类型。</para><para>en-us:The master database context locator type.</para></typeparam>
    /// <param name="serviceProvider"><para>zh-cn:服务提供器。</para><para>en-us:The service provider.</para></param>
    /// <returns><para>zh-cn:指定主库定位器的主从仓储。</para><para>en-us:The master-slave repository for the specified locator.</para></returns>
    public static IMSRepository<TMasterDbContextLocator> GetMSRepository<TMasterDbContextLocator>(IServiceProvider serviceProvider = default)
        where TMasterDbContextLocator : class, IDbContextLocator
    {
        return AppCore.GetService<IMSRepository<TMasterDbContextLocator>>(serviceProvider);
    }

    /// <summary>
    /// <para>zh-cn:获取默认数据库上下文。</para>
    /// <para>en-us:Gets the default database context.</para>
    /// </summary>
    /// <param name="serviceProvider"><para>zh-cn:可选的服务提供器。</para><para>en-us:An optional service provider.</para></param>
    /// <returns><para>zh-cn:默认数据库上下文。</para><para>en-us:The default database context.</para></returns>
    public static DbContext GetDbContext(IServiceProvider serviceProvider = default)
    {
        return GetDbContext(typeof(MasterDbContextLocator), serviceProvider);
    }

    /// <summary>
    /// <para>zh-cn:根据定位器类型获取数据库上下文。</para>
    /// <para>en-us:Gets a database context by locator type.</para>
    /// </summary>
    /// <param name="dbContextLocator"><para>zh-cn:数据库上下文定位器类型。</para><para>en-us:The database context locator type.</para></param>
    /// <param name="serviceProvider"><para>zh-cn:可选的服务提供器。</para><para>en-us:An optional service provider.</para></param>
    /// <returns><para>zh-cn:解析得到的数据库上下文。</para><para>en-us:The resolved database context.</para></returns>
    public static DbContext GetDbContext(Type dbContextLocator, IServiceProvider serviceProvider = default)
    {
        // 判断数据库上下文定位器是否绑定
        Penetrates.CheckDbContextLocator(dbContextLocator, out _);

        var dbContextResolve = AppCore.GetService<Func<Type, IScoped, DbContext>>(serviceProvider);
        return dbContextResolve(dbContextLocator, default);
    }

    /// <summary>
    /// <para>zh-cn:根据泛型定位器类型获取数据库上下文。</para>
    /// <para>en-us:Gets a database context by generic locator type.</para>
    /// </summary>
    /// <typeparam name="TDbContextLocator"><para>zh-cn:数据库上下文定位器类型。</para><para>en-us:The database context locator type.</para></typeparam>
    /// <param name="serviceProvider"><para>zh-cn:可选的服务提供器。</para><para>en-us:An optional service provider.</para></param>
    /// <returns><para>zh-cn:解析得到的数据库上下文。</para><para>en-us:The resolved database context.</para></returns>
    public static DbContext GetDbContext<TDbContextLocator>(IServiceProvider serviceProvider = default)
        where TDbContextLocator : class, IDbContextLocator
    {
        return GetDbContext(typeof(TDbContextLocator), serviceProvider);
    }
}
