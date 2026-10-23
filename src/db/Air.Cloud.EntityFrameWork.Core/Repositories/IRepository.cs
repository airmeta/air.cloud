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
using Air.Cloud.Core.Standard.DataBase.Model;
using Air.Cloud.EntityFrameWork.Core.Repositories.Dependencies;

using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.DependencyInjection;

using System.Collections.Concurrent;
using System.Linq.Expressions;

namespace Air.Cloud.EntityFrameWork.Core.Repositories;

/// <summary>
/// <para>zh-cn:非泛型仓储</para>
/// <para>en-us:Describes the behavior provided by this member.</para>
/// </summary>
public partial interface IRepository
{
    /// <summary>
    /// <para>zh-cn:切换仓储</para>
    /// <para>en-us:Describes the behavior provided by this member.</para>
    /// </summary>
    /// <typeparam name="TEntity"><para>zh-cn:实体类型</para><para>en-us:The entity type.</para></typeparam>
    /// <returns><para>zh-cn:仓储</para><para>en-us:The operation result.</para></returns>
    IRepository<TEntity> Change<TEntity>()
        where TEntity : class, IPrivateEntity, new();

    /// <summary>
    /// <para>zh-cn:切换多数据库上下文仓储</para>
    /// <para>en-us:Describes the behavior provided by this member.</para>
    /// </summary>
    /// <typeparam name="TEntity"><para>zh-cn:实体类型</para><para>en-us:The entity type.</para></typeparam>
    /// <typeparam name="TDbContextLocator"><para>zh-cn:数据库上下文定位器</para><para>en-us:The database context locator type.</para></typeparam>
    /// <returns><para>zh-cn:仓储</para><para>en-us:The operation result.</para></returns>
    IRepository<TEntity, TDbContextLocator> Change<TEntity, TDbContextLocator>()
        where TEntity : class, IPrivateEntity, new()
        where TDbContextLocator : class, IDbContextLocator;

    /// <summary>
    /// <para>zh-cn:重新构建并切换仓储</para>
    /// <para>en-us:Describes the behavior provided by this member.</para>
    /// </summary>
    /// <remarks>特别注意，Scoped 必须手动释放</remarks>
    /// <typeparam name="TEntity"><para>zh-cn:实体类型</para><para>en-us:The entity type.</para></typeparam>
    /// <returns><para>zh-cn:仓储</para><para>en-us:The operation result.</para></returns>
    (IRepository<TEntity> Repository, IServiceScope Scoped) BuildChange<TEntity>()
        where TEntity : class, IPrivateEntity, new();

    /// <summary>
    /// <para>zh-cn:重新构建并切换多数据库上下文仓储</para>
    /// <para>en-us:Describes the behavior provided by this member.</para>
    /// </summary>
    /// <remarks>特别注意，Scoped 必须手动释放</remarks>
    /// <typeparam name="TEntity"><para>zh-cn:实体类型</para><para>en-us:The entity type.</para></typeparam>
    /// <typeparam name="TDbContextLocator"><para>zh-cn:数据库上下文定位器</para><para>en-us:The database context locator type.</para></typeparam>
    /// <returns><para>zh-cn:仓储</para><para>en-us:The operation result.</para></returns>
    (IRepository<TEntity, TDbContextLocator> Repository, IServiceScope Scoped) BuildChange<TEntity, TDbContextLocator>()
        where TEntity : class, IPrivateEntity, new()
        where TDbContextLocator : class, IDbContextLocator;

    /// <summary>
    /// <para>zh-cn:解析服务</para>
    /// <para>en-us:Describes the behavior provided by this member.</para>
    /// </summary>
    /// <typeparam name="TService"><para>zh-cn:泛型参数类型</para><para>en-us:The generic parameter type.</para></typeparam>
    /// <returns><para>zh-cn:操作结果。</para><para>en-us:The operation result.</para></returns>
    TService GetService<TService>()
        where TService : notnull;

    /// <summary>
    /// <para>zh-cn:解析服务</para>
    /// <para>en-us:Describes the behavior provided by this member.</para>
    /// </summary>
    /// <typeparam name="TService"><para>zh-cn:泛型参数类型</para><para>en-us:The generic parameter type.</para></typeparam>
    /// <returns><para>zh-cn:操作结果。</para><para>en-us:The operation result.</para></returns>
    TService GetRequiredService<TService>()
        where TService : notnull;
}

/// <summary>
/// <para>zh-cn:仓储接口</para>
/// <para>en-us:Describes the behavior provided by this member.</para>
/// </summary>
/// <typeparam name="TEntity"><para>zh-cn:实体类型</para><para>en-us:The entity type.</para></typeparam>
public partial interface IRepository<TEntity> : IRepository<TEntity, MasterDbContextLocator>
    , IWritableRepository<TEntity>
    , IReadableRepository<TEntity>
    where TEntity : class, IPrivateEntity, new()
{
}

/// <summary>
/// <para>zh-cn:多数据库上下文仓储</para>
/// <para>en-us:Describes the behavior provided by this member.</para>
/// </summary>
/// <typeparam name="TEntity"><para>zh-cn:实体类型</para><para>en-us:The entity type.</para></typeparam>
/// <typeparam name="TDbContextLocator"><para>zh-cn:数据库上下文定位器</para><para>en-us:The database context locator type.</para></typeparam>
public partial interface IRepository<TEntity, TDbContextLocator> : IPrivateRepository<TEntity>
    , IWritableRepository<TEntity, TDbContextLocator>
    , IReadableRepository<TEntity, TDbContextLocator>
    where TEntity : class, IPrivateEntity, new()
    where TDbContextLocator : class, IDbContextLocator
{
}

/// <summary>
/// <para>zh-cn:私有公共实体</para>
/// <para>en-us:Describes the behavior provided by this member.</para>
/// </summary>
/// <typeparam name="TEntity"><para>zh-cn:泛型参数类型</para><para>en-us:The entity type.</para></typeparam>
public interface IPrivateRepository<TEntity>
    : IPrivateWritableRepository<TEntity>
    , IPrivateReadableRepository<TEntity>
    , IPrivateRootRepository
    where TEntity : class, IPrivateEntity, new()
{
    /// <summary>
    /// <para>zh-cn:EF Core 数据库操作对象；原生 SQL 文本执行请直接使用 EF Core API。</para>
    /// <para>en-us:The EF Core database facade; execute raw SQL through EF Core APIs directly.</para>
    /// </summary>
    DatabaseFacade Database { get; }

    /// <summary>
    /// <para>zh-cn:当前数据库上下文。</para>
    /// <para>en-us:The current EF Core database context.</para>
    /// </summary>
    DbContext Context { get; }

    /// <summary>
    /// <para>zh-cn:确保当前请求已登记本地事务上下文。</para>
    /// <para>en-us:Ensures that the current request context is registered for a local transaction.</para>
    /// </summary>
    void EnsureTransaction();

    /// <summary>
    /// <para>zh-cn:将仓储约束为特定的 EF Core 仓储接口。</para>
    /// <para>en-us:Constrains the repository to a specific EF Core repository interface.</para>
    /// </summary>
    TRestrainRepository Constraint<TRestrainRepository>()
        where TRestrainRepository : class, IPrivateRootRepository;

    /// <summary>
    /// <para>zh-cn:实体集合</para>
    /// <para>en-us:Describes the behavior provided by this member.</para>
    /// </summary>
    DbSet<TEntity> Entities { get; }

    /// <summary>
    /// <para>zh-cn:不跟踪的（脱轨）实体</para>
    /// <para>en-us:Describes the behavior provided by this member.</para>
    /// </summary>
    IQueryable<TEntity> DetachedEntities { get; }

    /// <summary>
    /// <para>zh-cn:查看实体类型</para>
    /// <para>en-us:Describes the behavior provided by this member.</para>
    /// </summary>
    IEntityType EntityType { get; }

    /// <summary>
    /// <para>zh-cn:实体追综器</para>
    /// <para>en-us:Describes the behavior provided by this member.</para>
    /// </summary>
    ChangeTracker ChangeTracker { get; }

    /// <summary>
    /// <para>zh-cn:实体模型</para>
    /// <para>en-us:Describes the behavior provided by this member.</para>
    /// </summary>
    IModel Model { get; }
    /// <summary>
    /// <para>zh-cn:数据库提供器名</para>
    /// <para>en-us:Describes the behavior provided by this member.</para>
    /// </summary>
    string ProviderName { get; }

    /// <summary>
    /// <para>zh-cn:服务提供器</para>
    /// <para>en-us:Describes the behavior provided by this member.</para>
    /// </summary>
    IServiceProvider ServiceProvider { get; }

    /// <summary>
    /// <para>zh-cn:租户Id</para>
    /// <para>en-us:Describes the behavior provided by this member.</para>
    /// </summary>
    Guid? TenantId { get; }

    /// <summary>
    /// <para>zh-cn:判断上下文是否更改</para>
    /// <para>en-us:Describes the behavior provided by this member.</para>
    /// </summary>
    /// <returns><para>zh-cn:bool</para><para>en-us:The operation result.</para></returns>
    bool HasChanges();

    /// <summary>
    /// <para>zh-cn:将实体加入数据上下文托管</para>
    /// <para>en-us:Describes the behavior provided by this member.</para>
    /// </summary>
    /// <param name="entity"><para>zh-cn:实体</para><para>en-us:The entity.</para></param>
    /// <returns><para>zh-cn:EntityEntry</para><para>en-us:The operation result.</para></returns>
    EntityEntry Entry(object entity);

    /// <summary>
    /// <para>zh-cn:将实体加入数据上下文托管</para>
    /// <para>en-us:Describes the behavior provided by this member.</para>
    /// </summary>
    /// <param name="entity"><para>zh-cn:实体</para><para>en-us:The entity.</para></param>
    /// <returns><para>zh-cn:操作结果。</para><para>en-us:The operation result.</para></returns>
    EntityEntry<TEntity> Entry(TEntity entity);

    /// <summary>
    /// <para>zh-cn:获取实体状态</para>
    /// <para>en-us:Describes the behavior provided by this member.</para>
    /// </summary>
    /// <param name="entity"><para>zh-cn:实体</para><para>en-us:The entity.</para></param>
    /// <returns><para>zh-cn:操作结果。</para><para>en-us:The operation result.</para></returns>
    EntityState EntityEntryState(object entity);

    /// <summary>
    /// <para>zh-cn:获取实体状态</para>
    /// <para>en-us:Describes the behavior provided by this member.</para>
    /// </summary>
    /// <param name="entity"><para>zh-cn:实体</para><para>en-us:The entity.</para></param>
    /// <returns><para>zh-cn:EntityState</para><para>en-us:The operation result.</para></returns>
    EntityState EntityEntryState(TEntity entity);

    /// <summary>
    /// <para>zh-cn:将实体属性加入托管</para>
    /// <para>en-us:Describes the behavior provided by this member.</para>
    /// </summary>
    /// <param name="entity"><para>zh-cn:实体</para><para>en-us:The entity.</para></param>
    /// <param name="propertyName"><para>zh-cn:属性名</para><para>en-us:The property name.</para></param>
    /// <returns><para>zh-cn:PropertyEntry</para><para>en-us:The operation result.</para></returns>
    PropertyEntry EntityPropertyEntry(object entity, string propertyName);

    /// <summary>
    /// <para>zh-cn:将实体属性加入托管</para>
    /// <para>en-us:Describes the behavior provided by this member.</para>
    /// </summary>
    /// <param name="entity"><para>zh-cn:实体</para><para>en-us:The entity.</para></param>
    /// <param name="propertyName"><para>zh-cn:属性名</para><para>en-us:The property name.</para></param>
    /// <returns><para>zh-cn:PropertyEntry</para><para>en-us:The operation result.</para></returns>
    PropertyEntry EntityPropertyEntry(TEntity entity, string propertyName);

    /// <summary>
    /// <para>zh-cn:将实体属性加入托管</para>
    /// <para>en-us:Describes the behavior provided by this member.</para>
    /// </summary>
    /// <typeparam name="TProperty"><para>zh-cn:属性类型</para><para>en-us:The generic parameter type.</para></typeparam>
    /// <param name="entity"><para>zh-cn:实体</para><para>en-us:The entity.</para></param>
    /// <param name="propertyPredicate"><para>zh-cn:属性表达式</para><para>en-us:The property predicate.</para></param>
    /// <returns><para>zh-cn:PropertyEntry</para><para>en-us:The operation result.</para></returns>
    PropertyEntry<TEntity, TProperty> EntityPropertyEntry<TProperty>(TEntity entity, Expression<Func<TEntity, TProperty>> propertyPredicate);

    /// <summary>
    /// <para>zh-cn:改变实体状态</para>
    /// <para>en-us:Describes the behavior provided by this member.</para>
    /// </summary>
    /// <param name="entity"><para>zh-cn:实体</para><para>en-us:The entity.</para></param>
    /// <param name="entityState"><para>zh-cn:实体状态</para><para>en-us:The entityState value.</para></param>
    /// <returns><para>zh-cn:EntityEntry</para><para>en-us:The operation result.</para></returns>
    EntityEntry ChangeEntityState(object entity, EntityState entityState);

    /// <summary>
    /// <para>zh-cn:改变实体状态</para>
    /// <para>en-us:Describes the behavior provided by this member.</para>
    /// </summary>
    /// <param name="entity"><para>zh-cn:实体</para><para>en-us:The entity.</para></param>
    /// <param name="entityState"><para>zh-cn:实体状态</para><para>en-us:The entityState value.</para></param>
    /// <returns><para>zh-cn:EntityEntry{TEntity}</para><para>en-us:The operation result.</para></returns>
    EntityEntry<TEntity> ChangeEntityState(TEntity entity, EntityState entityState);

    /// <summary>
    /// <para>zh-cn:改变实体状态</para>
    /// <para>en-us:Describes the behavior provided by this member.</para>
    /// </summary>
    /// <param name="entityEntry"><para>zh-cn:实体条目</para><para>en-us:The entity entry.</para></param>
    /// <param name="entityState"><para>zh-cn:实体状态</para><para>en-us:The entityState value.</para></param>
    /// <returns><para>zh-cn:EntityEntry</para><para>en-us:The operation result.</para></returns>
    EntityEntry ChangeEntityState(EntityEntry entityEntry, EntityState entityState);

    /// <summary>
    /// <para>zh-cn:改变实体状态</para>
    /// <para>en-us:Describes the behavior provided by this member.</para>
    /// </summary>
    /// <param name="entityEntry"><para>zh-cn:实体条目</para><para>en-us:The entity entry.</para></param>
    /// <param name="entityState"><para>zh-cn:实体状态</para><para>en-us:The entityState value.</para></param>
    /// <returns><para>zh-cn:EntityEntry{TEntity}</para><para>en-us:The operation result.</para></returns>
    EntityEntry<TEntity> ChangeEntityState(EntityEntry<TEntity> entityEntry, EntityState entityState);

    /// <summary>
    /// <para>zh-cn:检查实体跟踪状态</para>
    /// <para>en-us:Describes the behavior provided by this member.</para>
    /// </summary>
    /// <param name="id"><para>zh-cn:参数值。</para><para>en-us:The id value.</para></param>
    /// <param name="entityEntry"><para>zh-cn:参数值。</para><para>en-us:The entityEntry value.</para></param>
    /// <param name="keyName"><para>zh-cn:参数值。</para><para>en-us:The keyName value.</para></param>
    /// <returns><para>zh-cn:操作结果。</para><para>en-us:The operation result.</para></returns>
    bool CheckTrackState(object id, out EntityEntry entityEntry, string? keyName = default);

    /// <summary>
    /// <para>zh-cn:检查实体跟踪状态</para>
    /// <para>en-us:Describes the behavior provided by this member.</para>
    /// </summary>
    /// <typeparam name="TTrackEntity"><para>zh-cn:泛型参数类型</para><para>en-us:The generic parameter type.</para></typeparam>
    /// <param name="id"><para>zh-cn:参数值。</para><para>en-us:The id value.</para></param>
    /// <param name="entityEntry"><para>zh-cn:参数值。</para><para>en-us:The entityEntry value.</para></param>
    /// <param name="keyName"><para>zh-cn:参数值。</para><para>en-us:The keyName value.</para></param>
    /// <returns><para>zh-cn:操作结果。</para><para>en-us:The operation result.</para></returns>
    bool CheckTrackState<TTrackEntity>(object id, out EntityEntry entityEntry, string? keyName = default)
         where TTrackEntity : class, IPrivateEntity, new();

    /// <summary>
    /// <para>zh-cn:判断是否被附加</para>
    /// <para>en-us:Describes the behavior provided by this member.</para>
    /// </summary>
    /// <param name="entity"><para>zh-cn:实体</para><para>en-us:The entity.</para></param>
    /// <returns><para>zh-cn:bool</para><para>en-us:The operation result.</para></returns>
    bool IsAttached(object entity);

    /// <summary>
    /// <para>zh-cn:判断是否被附加</para>
    /// <para>en-us:Describes the behavior provided by this member.</para>
    /// </summary>
    /// <param name="entity"><para>zh-cn:实体</para><para>en-us:The entity.</para></param>
    /// <returns><para>zh-cn:bool</para><para>en-us:The operation result.</para></returns>
    bool IsAttached(TEntity entity);

    /// <summary>
    /// <para>zh-cn:附加实体</para>
    /// <para>en-us:Describes the behavior provided by this member.</para>
    /// </summary>
    /// <param name="entity"><para>zh-cn:实体</para><para>en-us:The entity.</para></param>
    /// <returns><para>zh-cn:EntityEntry</para><para>en-us:The operation result.</para></returns>
    EntityEntry Attach(object entity);

    /// <summary>
    /// <para>zh-cn:附加实体</para>
    /// <para>en-us:Describes the behavior provided by this member.</para>
    /// </summary>
    /// <param name="entity"><para>zh-cn:实体</para><para>en-us:The entity.</para></param>
    /// <returns><para>zh-cn:EntityEntry</para><para>en-us:The operation result.</para></returns>
    EntityEntry<TEntity> Attach(TEntity entity);

    /// <summary>
    /// <para>zh-cn:附加多个实体</para>
    /// <para>en-us:Describes the behavior provided by this member.</para>
    /// </summary>
    /// <param name="entities"><para>zh-cn:多个实体</para><para>en-us:The entities.</para></param>
    void AttachRange(params object[] entities);

    /// <summary>
    /// <para>zh-cn:附加多个实体</para>
    /// <para>en-us:Describes the behavior provided by this member.</para>
    /// </summary>
    /// <param name="entities"><para>zh-cn:多个实体</para><para>en-us:The entities.</para></param>
    void AttachRange(IEnumerable<TEntity> entities);

    /// <summary>
    /// <para>zh-cn:取消附加实体</para>
    /// <para>en-us:Describes the behavior provided by this member.</para>
    /// </summary>
    /// <param name="entity"><para>zh-cn:实体</para><para>en-us:The entity.</para></param>
    void Detach(object entity);

    /// <summary>
    /// <para>zh-cn:取消附加实体</para>
    /// <para>en-us:Describes the behavior provided by this member.</para>
    /// </summary>
    /// <param name="entity"><para>zh-cn:实体</para><para>en-us:The entity.</para></param>
    void Detach(TEntity entity);

    /// <summary>
    /// <para>zh-cn:取消附加实体</para>
    /// <para>en-us:Describes the behavior provided by this member.</para>
    /// </summary>
    /// <param name="entityEntry"><para>zh-cn:实体条目</para><para>en-us:The entity entry.</para></param>
    void Detach(EntityEntry entityEntry);

    /// <summary>
    /// <para>zh-cn:取消附加实体</para>
    /// <para>en-us:Describes the behavior provided by this member.</para>
    /// </summary>
    /// <param name="entityEntry"><para>zh-cn:实体条目</para><para>en-us:The entity entry.</para></param>
    void Detach(EntityEntry<TEntity> entityEntry);

    /// <summary>
    /// <para>zh-cn:获取所有数据库上下文</para>
    /// <para>en-us:Describes the behavior provided by this member.</para>
    /// </summary>
    /// <returns><para>zh-cn:ConcurrentBag{DbContext}</para><para>en-us:The operation result.</para></returns>
    public ConcurrentDictionary<Guid, DbContext> GetDbContexts();

    /// <summary>
    /// <para>zh-cn:判断实体是否设置了主键</para>
    /// <para>en-us:Describes the behavior provided by this member.</para>
    /// </summary>
    /// <param name="entity"><para>zh-cn:实体</para><para>en-us:The entity.</para></param>
    /// <returns><para>zh-cn:bool</para><para>en-us:The operation result.</para></returns>
    bool IsKeySet(TEntity entity);

    /// <summary>
    /// <para>zh-cn:删除数据库</para>
    /// <para>en-us:Describes the behavior provided by this member.</para>
    /// </summary>
    void EnsureDeleted();

    /// <summary>
    /// <para>zh-cn:删除数据库</para>
    /// <para>en-us:Describes the behavior provided by this member.</para>
    /// </summary>
    Task EnsureDeletedAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// <para>zh-cn:创建数据库</para>
    /// <para>en-us:Describes the behavior provided by this member.</para>
    /// </summary>
    void EnsureCreated();

    /// <summary>
    /// <para>zh-cn:创建数据库</para>
    /// <para>en-us:Describes the behavior provided by this member.</para>
    /// </summary>
    Task EnsureCreatedAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// <para>zh-cn:判断是否是关系型数据库</para>
    /// <para>en-us:Describes the behavior provided by this member.</para>
    /// </summary>
    /// <returns><para>zh-cn:bool</para><para>en-us:The operation result.</para></returns>
    bool IsRelational();

    /// <summary>
    /// <para>zh-cn:切换仓储</para>
    /// <para>en-us:Describes the behavior provided by this member.</para>
    /// </summary>
    /// <typeparam name="TChangeEntity"><para>zh-cn:实体类型</para><para>en-us:The generic parameter type.</para></typeparam>
    /// <returns><para>zh-cn:仓储</para><para>en-us:The operation result.</para></returns>
    new IRepository<TChangeEntity> Change<TChangeEntity>()
            where TChangeEntity : class, IPrivateEntity, new();

    /// <summary>
    /// <para>zh-cn:切换多数据库上下文仓储</para>
    /// <para>en-us:Describes the behavior provided by this member.</para>
    /// </summary>
    /// <typeparam name="TChangeEntity"><para>zh-cn:实体类型</para><para>en-us:The generic parameter type.</para></typeparam>
    /// <typeparam name="TChangeDbContextLocator"><para>zh-cn:数据库上下文定位器</para><para>en-us:The generic parameter type.</para></typeparam>
    /// <returns><para>zh-cn:仓储</para><para>en-us:The operation result.</para></returns>
    IRepository<TChangeEntity, TChangeDbContextLocator> Change<TChangeEntity, TChangeDbContextLocator>()
        where TChangeEntity : class, IPrivateEntity, new()
        where TChangeDbContextLocator : class, IDbContextLocator;

    /// <summary>
    /// <para>zh-cn:重新构建并切换仓储</para>
    /// <para>en-us:Describes the behavior provided by this member.</para>
    /// </summary>
    /// <remarks>特别注意，Scoped 必须手动释放</remarks>
    /// <typeparam name="TChangeEntity"><para>zh-cn:实体类型</para><para>en-us:The generic parameter type.</para></typeparam>
    /// <returns><para>zh-cn:仓储</para><para>en-us:The operation result.</para></returns>
    (IRepository<TChangeEntity> Repository, IServiceScope Scoped) BuildChange<TChangeEntity>()
        where TChangeEntity : class, IPrivateEntity, new();

    /// <summary>
    /// <para>zh-cn:重新构建并切换多数据库上下文仓储</para>
    /// <para>en-us:Describes the behavior provided by this member.</para>
    /// </summary>
    /// <remarks>特别注意，Scoped 必须手动释放</remarks>
    /// <typeparam name="TChangeEntity"><para>zh-cn:实体类型</para><para>en-us:The generic parameter type.</para></typeparam>
    /// <typeparam name="TChangeDbContextLocator"><para>zh-cn:数据库上下文定位器</para><para>en-us:The generic parameter type.</para></typeparam>
    /// <returns><para>zh-cn:仓储</para><para>en-us:The operation result.</para></returns>
    (IRepository<TChangeEntity, TChangeDbContextLocator> Repository, IServiceScope Scoped) BuildChange<TChangeEntity, TChangeDbContextLocator>()
        where TChangeEntity : class, IPrivateEntity, new()
        where TChangeDbContextLocator : class, IDbContextLocator;
}
