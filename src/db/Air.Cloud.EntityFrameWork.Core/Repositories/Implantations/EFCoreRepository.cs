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
using Air.Cloud.EntityFrameWork.Core.ContextPools;
using Air.Cloud.EntityFrameWork.Core.Repositories;

using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.DependencyInjection;

using System.Collections.Concurrent;
using System.Linq.Expressions;

namespace Air.Cloud.EntityFrameWork.Core.Repositories.Implantations;

/// <summary>
/// <para>zh-cn:非泛型EF Core仓储实现</para>
/// <para>en-us:Provides the corresponding database repository operation.</para>
/// </summary>
[IgnoreScanning]
public partial class EFCoreRepository : IRepository
{
    /// <summary>
    /// <para>zh-cn:服务提供器</para>
    /// <para>en-us:Provides the corresponding database repository operation.</para>
    /// </summary>
    private readonly IServiceProvider _serviceProvider;

    /// <summary>
    /// <para>zh-cn:构造函数</para>
    /// <para>en-us:Provides the corresponding database repository operation.</para>
    /// </summary>
    /// <param name="serviceProvider"><para>zh-cn:服务提供器</para><para>en-us:The service provider.</para></param>
    public EFCoreRepository(
        IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    /// <summary>
    /// <para>zh-cn:切换仓储</para>
    /// <para>en-us:Provides the corresponding database repository operation.</para>
    /// </summary>
    /// <typeparam name="TEntity"><para>zh-cn:实体类型</para><para>en-us:The entity type.</para></typeparam>
    /// <returns><para>zh-cn:仓储</para><para>en-us:The operation result.</para></returns>
    public virtual IRepository<TEntity> Change<TEntity>()
        where TEntity : class, IPrivateEntity, new()
    {
        return _serviceProvider.GetRequiredService<IRepository<TEntity>>();
    }

    /// <summary>
    /// <para>zh-cn:切换多数据库上下文仓储</para>
    /// <para>en-us:Provides the corresponding database repository operation.</para>
    /// </summary>
    /// <typeparam name="TEntity"><para>zh-cn:实体类型</para><para>en-us:The entity type.</para></typeparam>
    /// <typeparam name="TDbContextLocator"><para>zh-cn:数据库上下文定位器</para><para>en-us:The database context locator type.</para></typeparam>
    /// <returns><para>zh-cn:仓储</para><para>en-us:The operation result.</para></returns>
    public virtual IRepository<TEntity, TDbContextLocator> Change<TEntity, TDbContextLocator>()
        where TEntity : class, IPrivateEntity, new()
        where TDbContextLocator : class, IDbContextLocator
    {
        return _serviceProvider.GetRequiredService<IRepository<TEntity, TDbContextLocator>>();
    }

    /// <summary>
    /// <para>zh-cn:重新构建并切换仓储</para>
    /// <para>en-us:Provides the corresponding database repository operation.</para>
    /// </summary>
    /// <remarks>特别注意，Scoped 必须手动释放</remarks>
    /// <typeparam name="TEntity"><para>zh-cn:实体类型</para><para>en-us:The entity type.</para></typeparam>
    /// <returns><para>zh-cn:仓储</para><para>en-us:The operation result.</para></returns>
    public virtual (IRepository<TEntity> Repository, IServiceScope Scoped) BuildChange<TEntity>()
        where TEntity : class, IPrivateEntity, new()
    {
        var scoped = _serviceProvider.CreateScope();
        var repository = scoped.ServiceProvider.GetRequiredService<IRepository<TEntity>>();

        // 添加未托管对象
        AppCore.UnmanagedObjects.Add(scoped);

        return (repository, scoped);
    }

    /// <summary>
    /// <para>zh-cn:重新构建并切换多数据库上下文仓储</para>
    /// <para>en-us:Provides the corresponding database repository operation.</para>
    /// </summary>
    /// <remarks>特别注意，Scoped 必须手动释放</remarks>
    /// <typeparam name="TEntity"><para>zh-cn:实体类型</para><para>en-us:The entity type.</para></typeparam>
    /// <typeparam name="TDbContextLocator"><para>zh-cn:数据库上下文定位器</para><para>en-us:The database context locator type.</para></typeparam>
    /// <returns><para>zh-cn:仓储</para><para>en-us:The operation result.</para></returns>
    public virtual (IRepository<TEntity, TDbContextLocator> Repository, IServiceScope Scoped) BuildChange<TEntity, TDbContextLocator>()
        where TEntity : class, IPrivateEntity, new()
        where TDbContextLocator : class, IDbContextLocator
    {
        var scoped = _serviceProvider.CreateScope();
        var repository = scoped.ServiceProvider.GetRequiredService<IRepository<TEntity, TDbContextLocator>>();

        // 添加未托管对象
        AppCore.UnmanagedObjects.Add(scoped);

        return (repository, scoped);
    }

    /// <summary>
    /// <para>zh-cn:解析服务</para>
    /// <para>en-us:Provides the corresponding database repository operation.</para>
    /// </summary>
    /// <typeparam name="TService"><para>zh-cn:泛型参数类型</para><para>en-us:The generic parameter type.</para></typeparam>
    /// <returns><para>zh-cn:操作结果。</para><para>en-us:The operation result.</para></returns>
    public virtual TService GetService<TService>()
        where TService : notnull
    {
        return _serviceProvider.GetRequiredService<TService>();
    }

    /// <summary>
    /// <para>zh-cn:解析服务</para>
    /// <para>en-us:Provides the corresponding database repository operation.</para>
    /// </summary>
    /// <typeparam name="TService"><para>zh-cn:泛型参数类型</para><para>en-us:The generic parameter type.</para></typeparam>
    /// <returns><para>zh-cn:操作结果。</para><para>en-us:The operation result.</para></returns>
    public virtual TService GetRequiredService<TService>()
        where TService : notnull
    {
        return _serviceProvider.GetRequiredService<TService>();
    }
}

/// <summary>
/// <para>zh-cn:EF Core仓储实现</para>
/// <para>en-us:Provides the corresponding database repository operation.</para>
/// </summary>
/// <typeparam name="TEntity"><para>zh-cn:实体类型</para><para>en-us:The entity type.</para></typeparam>
[IgnoreScanning]
public partial class EFCoreRepository<TEntity> : EFCoreRepository<TEntity, MasterDbContextLocator>
    , IRepository<TEntity>
    where TEntity : class, IPrivateEntity, new()
{
    /// <summary>
    /// <para>zh-cn:构造函数</para>
    /// <para>en-us:Provides the corresponding database repository operation.</para>
    /// </summary>
    /// <param name="serviceProvider"><para>zh-cn:服务提供器</para><para>en-us:The service provider.</para></param>
    public EFCoreRepository(IServiceProvider serviceProvider) : base(serviceProvider)
    {
    }
}

/// <summary>
/// <para>zh-cn:多数据库上下文仓储</para>
/// <para>en-us:Provides the corresponding database repository operation.</para>
/// </summary>
[IgnoreScanning]
public partial class EFCoreRepository<TEntity, TDbContextLocator> : PrivateRepository<TEntity>
    , IRepository<TEntity, TDbContextLocator>
    where TEntity : class, IPrivateEntity, new()
    where TDbContextLocator : class, IDbContextLocator
{
    /// <summary>
    /// <para>zh-cn:构造函数</para>
    /// <para>en-us:Provides the corresponding database repository operation.</para>
    /// </summary>
    /// <param name="serviceProvider"><para>zh-cn:服务提供器</para><para>en-us:The service provider.</para></param>
    public EFCoreRepository(IServiceProvider serviceProvider) : base(typeof(TDbContextLocator), serviceProvider)
    {
    }
}

/// <summary>
/// <para>zh-cn:私有仓储</para>
/// <para>en-us:Provides the corresponding database repository operation.</para>
/// </summary>
/// <typeparam name="TEntity"><para>zh-cn:泛型参数类型</para><para>en-us:The entity type.</para></typeparam>
public partial class PrivateRepository<TEntity> : PrivateRepositoryBase, IPrivateRepository<TEntity>
    where TEntity : class, IPrivateEntity, new()
{
    /// <summary>
    /// <para>zh-cn:非泛型仓储</para>
    /// <para>en-us:Provides the corresponding database repository operation.</para>
    /// </summary>
    private readonly IRepository _repository;

    /// <summary>
    /// <para>zh-cn:数据库上下文池</para>
    /// <para>en-us:Provides the corresponding database repository operation.</para>
    /// </summary>
    private readonly IDbContextPool _dbContextPool;

    /// <summary>
    /// <para>zh-cn:构造函数</para>
    /// <para>en-us:Provides the corresponding database repository operation.</para>
    /// </summary>
    /// <param name="dbContextLocator"><para>zh-cn:参数值。</para><para>en-us:The dbContextLocator value.</para></param>
    /// <param name="serviceProvider"><para>zh-cn:服务提供器</para><para>en-us:The service provider.</para></param>
    public PrivateRepository(Type dbContextLocator, IServiceProvider serviceProvider) : base(dbContextLocator, serviceProvider)
    {
        // 初始化服务提供器
        ServiceProvider = serviceProvider;

        // 设置提供器名称
        ProviderName = Database.ProviderName ?? string.Empty;

        ChangeTracker = Context.ChangeTracker;
        Model = Context.Model;

        //初始化实体
        Entities = Context.Set<TEntity>();
        DetachedEntities = Entities.AsNoTracking();
        EntityType = Entities.EntityType;

        // 初始化数据上下文池
        _dbContextPool = serviceProvider.GetRequiredService<IDbContextPool>();

        // 非泛型仓储
        _repository = serviceProvider.GetRequiredService<IRepository>();
    }

    /// <summary>
    /// <para>zh-cn:实体集合</para>
    /// <para>en-us:Provides the corresponding database repository operation.</para>
    /// </summary>
    public virtual DbSet<TEntity> Entities { get; }

    /// <summary>
    /// <para>zh-cn:不跟踪的（脱轨）实体</para>
    /// <para>en-us:Provides the corresponding database repository operation.</para>
    /// </summary>
    public virtual IQueryable<TEntity> DetachedEntities { get; }

    /// <summary>
    /// <para>zh-cn:查看实体类型</para>
    /// <para>en-us:Provides the corresponding database repository operation.</para>
    /// </summary>
    public virtual IEntityType EntityType { get; }

    /// <summary>
    /// <para>zh-cn:实体追综器</para>
    /// <para>en-us:Provides the corresponding database repository operation.</para>
    /// </summary>
    public virtual ChangeTracker ChangeTracker { get; }

    /// <summary>
    /// <para>zh-cn:实体模型</para>
    /// <para>en-us:Provides the corresponding database repository operation.</para>
    /// </summary>
    public virtual IModel Model { get; }

    /// <summary>
    /// <para>zh-cn:数据库提供器名</para>
    /// <para>en-us:Provides the corresponding database repository operation.</para>
    /// </summary>
    public virtual string ProviderName { get; }

    /// <summary>
    /// <para>zh-cn:服务提供器</para>
    /// <para>en-us:Provides the corresponding database repository operation.</para>
    /// </summary>
    public virtual IServiceProvider ServiceProvider { get; }

    /// <summary>
    /// <para>zh-cn:租户Id</para>
    /// <para>en-us:Provides the corresponding database repository operation.</para>
    /// </summary>
    public virtual Guid? TenantId { get; }

    /// <summary>
    /// <para>zh-cn:判断上下文是否更改</para>
    /// <para>en-us:Provides the corresponding database repository operation.</para>
    /// </summary>
    /// <returns><para>zh-cn:bool</para><para>en-us:The operation result.</para></returns>
    public virtual bool HasChanges()
    {
        return ChangeTracker.HasChanges();
    }

    /// <summary>
    /// <para>zh-cn:将实体加入数据上下文托管</para>
    /// <para>en-us:Provides the corresponding database repository operation.</para>
    /// </summary>
    /// <param name="entity"><para>zh-cn:实体</para><para>en-us:The entity.</para></param>
    /// <returns><para>zh-cn:EntityEntry</para><para>en-us:The operation result.</para></returns>
    public virtual EntityEntry Entry(object entity)
    {
        return Context.Entry(entity);
    }

    /// <summary>
    /// <para>zh-cn:将实体加入数据上下文托管</para>
    /// <para>en-us:Provides the corresponding database repository operation.</para>
    /// </summary>
    /// <param name="entity"><para>zh-cn:实体</para><para>en-us:The entity.</para></param>
    /// <returns><para>zh-cn:操作结果。</para><para>en-us:The operation result.</para></returns>
    public virtual EntityEntry<TEntity> Entry(TEntity entity)
    {
        return Context.Entry(entity);
    }

    /// <summary>
    /// <para>zh-cn:获取实体状态</para>
    /// <para>en-us:Provides the corresponding database repository operation.</para>
    /// </summary>
    /// <param name="entity"><para>zh-cn:实体</para><para>en-us:The entity.</para></param>
    /// <returns><para>zh-cn:操作结果。</para><para>en-us:The operation result.</para></returns>
    public virtual EntityState EntityEntryState(object entity)
    {
        return Entry(entity).State;
    }

    /// <summary>
    /// <para>zh-cn:获取实体状态</para>
    /// <para>en-us:Provides the corresponding database repository operation.</para>
    /// </summary>
    /// <param name="entity"><para>zh-cn:实体</para><para>en-us:The entity.</para></param>
    /// <returns><para>zh-cn:EntityState</para><para>en-us:The operation result.</para></returns>
    public virtual EntityState EntityEntryState(TEntity entity)
    {
        return Entry(entity).State;
    }

    /// <summary>
    /// <para>zh-cn:将实体属性加入托管</para>
    /// <para>en-us:Provides the corresponding database repository operation.</para>
    /// </summary>
    /// <param name="entity"><para>zh-cn:实体</para><para>en-us:The entity.</para></param>
    /// <param name="propertyName"><para>zh-cn:属性名</para><para>en-us:The property name.</para></param>
    /// <returns><para>zh-cn:PropertyEntry</para><para>en-us:The operation result.</para></returns>
    public virtual PropertyEntry EntityPropertyEntry(object entity, string propertyName)
    {
        return Entry(entity).Property(propertyName);
    }

    /// <summary>
    /// <para>zh-cn:将实体属性加入托管</para>
    /// <para>en-us:Provides the corresponding database repository operation.</para>
    /// </summary>
    /// <param name="entity"><para>zh-cn:实体</para><para>en-us:The entity.</para></param>
    /// <param name="propertyName"><para>zh-cn:属性名</para><para>en-us:The property name.</para></param>
    /// <returns><para>zh-cn:PropertyEntry</para><para>en-us:The operation result.</para></returns>
    public virtual PropertyEntry EntityPropertyEntry(TEntity entity, string propertyName)
    {
        return Entry(entity).Property(propertyName);
    }

    /// <summary>
    /// <para>zh-cn:将实体属性加入托管</para>
    /// <para>en-us:Provides the corresponding database repository operation.</para>
    /// </summary>
    /// <typeparam name="TProperty"><para>zh-cn:属性类型</para><para>en-us:The generic parameter type.</para></typeparam>
    /// <param name="entity"><para>zh-cn:实体</para><para>en-us:The entity.</para></param>
    /// <param name="propertyPredicate"><para>zh-cn:属性表达式</para><para>en-us:The property predicate.</para></param>
    /// <returns><para>zh-cn:PropertyEntry</para><para>en-us:The operation result.</para></returns>
    public virtual PropertyEntry<TEntity, TProperty> EntityPropertyEntry<TProperty>(TEntity entity, Expression<Func<TEntity, TProperty>> propertyPredicate)
    {
        return Entry(entity).Property(propertyPredicate);
    }

    /// <summary>
    /// <para>zh-cn:改变实体状态</para>
    /// <para>en-us:Provides the corresponding database repository operation.</para>
    /// </summary>
    /// <param name="entity"><para>zh-cn:实体</para><para>en-us:The entity.</para></param>
    /// <param name="entityState"><para>zh-cn:实体状态</para><para>en-us:The entityState value.</para></param>
    /// <returns><para>zh-cn:EntityEntry</para><para>en-us:The operation result.</para></returns>
    public virtual EntityEntry ChangeEntityState(object entity, EntityState entityState)
    {
        var entityEntry = Entry(entity);
        entityEntry.State = entityState;
        return entityEntry;
    }

    /// <summary>
    /// <para>zh-cn:改变实体状态</para>
    /// <para>en-us:Provides the corresponding database repository operation.</para>
    /// </summary>
    /// <param name="entity"><para>zh-cn:实体</para><para>en-us:The entity.</para></param>
    /// <param name="entityState"><para>zh-cn:实体状态</para><para>en-us:The entityState value.</para></param>
    /// <returns><para>zh-cn:EntityEntry{TEntity}</para><para>en-us:The operation result.</para></returns>
    public virtual EntityEntry<TEntity> ChangeEntityState(TEntity entity, EntityState entityState)
    {
        var entityEntry = Entry(entity);
        entityEntry.State = entityState;
        return entityEntry;
    }

    /// <summary>
    /// <para>zh-cn:改变实体状态</para>
    /// <para>en-us:Provides the corresponding database repository operation.</para>
    /// </summary>
    /// <param name="entityEntry"><para>zh-cn:实体条目</para><para>en-us:The entity entry.</para></param>
    /// <param name="entityState"><para>zh-cn:实体状态</para><para>en-us:The entityState value.</para></param>
    /// <returns><para>zh-cn:EntityEntry</para><para>en-us:The operation result.</para></returns>
    public virtual EntityEntry ChangeEntityState(EntityEntry entityEntry, EntityState entityState)
    {
        entityEntry.State = entityState;
        return entityEntry;
    }

    /// <summary>
    /// <para>zh-cn:改变实体状态</para>
    /// <para>en-us:Provides the corresponding database repository operation.</para>
    /// </summary>
    /// <param name="entityEntry"><para>zh-cn:实体条目</para><para>en-us:The entity entry.</para></param>
    /// <param name="entityState"><para>zh-cn:实体状态</para><para>en-us:The entityState value.</para></param>
    /// <returns><para>zh-cn:EntityEntry{TEntity}</para><para>en-us:The operation result.</para></returns>
    public virtual EntityEntry<TEntity> ChangeEntityState(EntityEntry<TEntity> entityEntry, EntityState entityState)
    {
        entityEntry.State = entityState;
        return entityEntry;
    }

    /// <summary>
    /// <para>zh-cn:检查实体跟踪状态</para>
    /// <para>en-us:Provides the corresponding database repository operation.</para>
    /// </summary>
    /// <param name="id"><para>zh-cn:参数值。</para><para>en-us:The id value.</para></param>
    /// <param name="entityEntry"><para>zh-cn:参数值。</para><para>en-us:The entityEntry value.</para></param>
    /// <param name="keyName"><para>zh-cn:参数值。</para><para>en-us:The keyName value.</para></param>
    /// <returns><para>zh-cn:操作结果。</para><para>en-us:The operation result.</para></returns>
    public virtual bool CheckTrackState(object id, out EntityEntry entityEntry, string? keyName = default)
    {
        return CheckTrackState<TEntity>(id, out entityEntry, keyName);
    }

    /// <summary>
    /// <para>zh-cn:检查实体跟踪状态</para>
    /// <para>en-us:Provides the corresponding database repository operation.</para>
    /// </summary>
    /// <typeparam name="TTrackEntity"><para>zh-cn:泛型参数类型</para><para>en-us:The generic parameter type.</para></typeparam>
    /// <param name="id"><para>zh-cn:参数值。</para><para>en-us:The id value.</para></param>
    /// <param name="entityEntry"><para>zh-cn:参数值。</para><para>en-us:The entityEntry value.</para></param>
    /// <param name="keyName"><para>zh-cn:参数值。</para><para>en-us:The keyName value.</para></param>
    /// <returns><para>zh-cn:操作结果。</para><para>en-us:The operation result.</para></returns>
    public virtual bool CheckTrackState<TTrackEntity>(object id, out EntityEntry entityEntry, string? keyName = default)
        where TTrackEntity : class, IPrivateEntity, new()
    {
        // 获取主键名
        keyName ??= (typeof(TTrackEntity) == typeof(TEntity) ? EntityType : Context.Set<TTrackEntity>().EntityType)
                    .FindPrimaryKey()?.Properties?.AsEnumerable()?.FirstOrDefault()?.PropertyInfo?.Name;
        if (string.IsNullOrWhiteSpace(keyName))
        {
            entityEntry = default!;
            return false;
        }

        // 检查是否已经跟踪
        var trackedEntry = ChangeTracker.Entries().FirstOrDefault(u => u.Entity.GetType() == typeof(TTrackEntity)
                                         && string.Equals(u.CurrentValues[keyName]?.ToString(), id.ToString(), StringComparison.Ordinal));

        if (trackedEntry == null)
        {
            entityEntry = default!;
            return false;
        }

        entityEntry = trackedEntry;
        return true;
    }

    /// <summary>
    /// <para>zh-cn:判断是否被附加</para>
    /// <para>en-us:Provides the corresponding database repository operation.</para>
    /// </summary>
    /// <param name="entity"><para>zh-cn:实体</para><para>en-us:The entity.</para></param>
    /// <returns><para>zh-cn:bool</para><para>en-us:The operation result.</para></returns>
    public virtual bool IsAttached(object entity)
    {
        return EntityEntryState(entity) != EntityState.Detached;
    }

    /// <summary>
    /// <para>zh-cn:判断是否被附加</para>
    /// <para>en-us:Provides the corresponding database repository operation.</para>
    /// </summary>
    /// <param name="entity"><para>zh-cn:实体</para><para>en-us:The entity.</para></param>
    /// <returns><para>zh-cn:bool</para><para>en-us:The operation result.</para></returns>
    public virtual bool IsAttached(TEntity entity)
    {
        return EntityEntryState(entity) != EntityState.Detached;
    }

    /// <summary>
    /// <para>zh-cn:附加实体</para>
    /// <para>en-us:Provides the corresponding database repository operation.</para>
    /// </summary>
    /// <param name="entity"><para>zh-cn:实体</para><para>en-us:The entity.</para></param>
    /// <returns><para>zh-cn:EntityEntry</para><para>en-us:The operation result.</para></returns>
    public virtual EntityEntry Attach(object entity)
    {
        return Context.Attach(entity);
    }

    /// <summary>
    /// <para>zh-cn:附加实体</para>
    /// <para>en-us:Provides the corresponding database repository operation.</para>
    /// </summary>
    /// <param name="entity"><para>zh-cn:实体</para><para>en-us:The entity.</para></param>
    /// <returns><para>zh-cn:EntityEntry</para><para>en-us:The operation result.</para></returns>
    public virtual EntityEntry<TEntity> Attach(TEntity entity)
    {
        return Context.Attach(entity);
    }

    /// <summary>
    /// <para>zh-cn:附加多个实体</para>
    /// <para>en-us:Provides the corresponding database repository operation.</para>
    /// </summary>
    /// <param name="entities"><para>zh-cn:多个实体</para><para>en-us:The entities.</para></param>
    public virtual void AttachRange(params object[] entities)
    {
        Context.AttachRange(entities);
    }

    /// <summary>
    /// <para>zh-cn:附加多个实体</para>
    /// <para>en-us:Provides the corresponding database repository operation.</para>
    /// </summary>
    /// <param name="entities"><para>zh-cn:多个实体</para><para>en-us:The entities.</para></param>
    public virtual void AttachRange(IEnumerable<TEntity> entities)
    {
        Context.AttachRange(entities);
    }

    /// <summary>
    /// <para>zh-cn:取消附加实体</para>
    /// <para>en-us:Provides the corresponding database repository operation.</para>
    /// </summary>
    /// <param name="entity"><para>zh-cn:实体</para><para>en-us:The entity.</para></param>
    public virtual void Detach(object entity)
    {
        ChangeEntityState(entity, EntityState.Detached);
    }

    /// <summary>
    /// <para>zh-cn:取消附加实体</para>
    /// <para>en-us:Provides the corresponding database repository operation.</para>
    /// </summary>
    /// <param name="entity"><para>zh-cn:实体</para><para>en-us:The entity.</para></param>
    public virtual void Detach(TEntity entity)
    {
        ChangeEntityState(entity, EntityState.Detached);
    }

    /// <summary>
    /// <para>zh-cn:取消附加实体</para>
    /// <para>en-us:Provides the corresponding database repository operation.</para>
    /// </summary>
    /// <param name="entityEntry"><para>zh-cn:实体条目</para><para>en-us:The entity entry.</para></param>
    public virtual void Detach(EntityEntry entityEntry)
    {
        ChangeEntityState(entityEntry, EntityState.Detached);
    }

    /// <summary>
    /// <para>zh-cn:取消附加实体</para>
    /// <para>en-us:Provides the corresponding database repository operation.</para>
    /// </summary>
    /// <param name="entityEntry"><para>zh-cn:实体条目</para><para>en-us:The entity entry.</para></param>
    public virtual void Detach(EntityEntry<TEntity> entityEntry)
    {
        ChangeEntityState(entityEntry, EntityState.Detached);
    }

    /// <summary>
    /// <para>zh-cn:获取所有数据库上下文</para>
    /// <para>en-us:Provides the corresponding database repository operation.</para>
    /// </summary>
    /// <returns><para>zh-cn:ConcurrentBag{DbContext}</para><para>en-us:The operation result.</para></returns>
    public virtual ConcurrentDictionary<Guid, DbContext> GetDbContexts()
    {
        return _dbContextPool.GetDbContexts();
    }

    /// <summary>
    /// <para>zh-cn:判断实体是否设置了主键</para>
    /// <para>en-us:Provides the corresponding database repository operation.</para>
    /// </summary>
    /// <param name="entity"><para>zh-cn:实体</para><para>en-us:The entity.</para></param>
    /// <returns><para>zh-cn:bool</para><para>en-us:The operation result.</para></returns>
    public virtual bool IsKeySet(TEntity entity)
    {
        return Entry(entity).IsKeySet;
    }

    /// <summary>
    /// <para>zh-cn:删除数据库</para>
    /// <para>en-us:Provides the corresponding database repository operation.</para>
    /// </summary>
    public virtual void EnsureDeleted()
    {
        Context.Database.EnsureDeleted();
    }

    /// <summary>
    /// <para>zh-cn:删除数据库</para>
    /// <para>en-us:Provides the corresponding database repository operation.</para>
    /// </summary>
    public virtual Task EnsureDeletedAsync(CancellationToken cancellationToken = default)
    {
        return Context.Database.EnsureDeletedAsync(cancellationToken);
    }

    /// <summary>
    /// <para>zh-cn:创建数据库</para>
    /// <para>en-us:Provides the corresponding database repository operation.</para>
    /// </summary>
    public virtual void EnsureCreated()
    {
        Context.Database.EnsureCreated();
    }

    /// <summary>
    /// <para>zh-cn:创建数据库</para>
    /// <para>en-us:Provides the corresponding database repository operation.</para>
    /// </summary>
    public virtual Task EnsureCreatedAsync(CancellationToken cancellationToken = default)
    {
        return Context.Database.EnsureCreatedAsync(cancellationToken);
    }

    /// <summary>
    /// <para>zh-cn:动态改变表名</para>
    /// <para>en-us:Provides the corresponding database repository operation.</para>
    /// </summary>
    /// <param name="tableName"><para>zh-cn:表名</para><para>en-us:The tableName value.</para></param>
    [Obsolete("该方法已过时，请调用 BuildChange<TEntity> 方法代替。")]
    public virtual void ChangeTable(string tableName)
    {
        if (EntityType is IConventionEntityType convention)
        {
            convention.SetTableName(tableName);
        }
    }

    /// <summary>
    /// <para>zh-cn:判断是否是关系型数据库</para>
    /// <para>en-us:Provides the corresponding database repository operation.</para>
    /// </summary>
    /// <returns><para>zh-cn:bool</para><para>en-us:The operation result.</para></returns>
    public virtual bool IsRelational()=> Database.IsRelational();

    /// <summary>
    /// <para>zh-cn:切换仓储</para>
    /// <para>en-us:Provides the corresponding database repository operation.</para>
    /// </summary>
    /// <typeparam name="TChangeEntity"><para>zh-cn:实体类型</para><para>en-us:The generic parameter type.</para></typeparam>
    /// <returns><para>zh-cn:仓储</para><para>en-us:The operation result.</para></returns>
    public virtual new IRepository<TChangeEntity> Change<TChangeEntity>()
        where TChangeEntity : class, IPrivateEntity, new()
    {
        return _repository.Change<TChangeEntity>();
    }

    /// <summary>
    /// <para>zh-cn:切换多数据库上下文仓储</para>
    /// <para>en-us:Provides the corresponding database repository operation.</para>
    /// </summary>
    /// <typeparam name="TChangeEntity"><para>zh-cn:实体类型</para><para>en-us:The generic parameter type.</para></typeparam>
    /// <typeparam name="TChangeDbContextLocator"><para>zh-cn:数据库上下文定位器</para><para>en-us:The generic parameter type.</para></typeparam>
    /// <returns><para>zh-cn:仓储</para><para>en-us:The operation result.</para></returns>
    public virtual IRepository<TChangeEntity, TChangeDbContextLocator> Change<TChangeEntity, TChangeDbContextLocator>()
        where TChangeEntity : class, IPrivateEntity, new()
        where TChangeDbContextLocator : class, IDbContextLocator
    {
        return _repository.Change<TChangeEntity, TChangeDbContextLocator>();
    }

    /// <summary>
    /// <para>zh-cn:重新构建并切换仓储</para>
    /// <para>en-us:Provides the corresponding database repository operation.</para>
    /// </summary>
    /// <remarks>特别注意，Scoped 必须手动释放</remarks>
    /// <typeparam name="TChangeEntity"><para>zh-cn:实体类型</para><para>en-us:The generic parameter type.</para></typeparam>
    /// <returns><para>zh-cn:仓储</para><para>en-us:The operation result.</para></returns>
    public virtual (IRepository<TChangeEntity> Repository, IServiceScope Scoped) BuildChange<TChangeEntity>()
        where TChangeEntity : class, IPrivateEntity, new()
    {
        return _repository.BuildChange<TChangeEntity>();
    }

    /// <summary>
    /// <para>zh-cn:重新构建并切换多数据库上下文仓储</para>
    /// <para>en-us:Provides the corresponding database repository operation.</para>
    /// </summary>
    /// <remarks>特别注意，Scoped 必须手动释放</remarks>
    /// <typeparam name="TChangeEntity"><para>zh-cn:实体类型</para><para>en-us:The generic parameter type.</para></typeparam>
    /// <typeparam name="TChangeDbContextLocator"><para>zh-cn:数据库上下文定位器</para><para>en-us:The generic parameter type.</para></typeparam>
    /// <returns><para>zh-cn:仓储</para><para>en-us:The operation result.</para></returns>
    public virtual (IRepository<TChangeEntity, TChangeDbContextLocator> Repository, IServiceScope Scoped) BuildChange<TChangeEntity, TChangeDbContextLocator>()
        where TChangeEntity : class, IPrivateEntity, new()
        where TChangeDbContextLocator : class, IDbContextLocator
    {
        return _repository.BuildChange<TChangeEntity, TChangeDbContextLocator>();
    }
}
