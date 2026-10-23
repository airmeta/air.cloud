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
using Air.Cloud.EntityFrameWork.Core.Contexts.Attributes;
using Air.Cloud.EntityFrameWork.Core.Extensions.DatabaseProvider;
using Air.Cloud.EntityFrameWork.Core.Internal;
using Air.Cloud.EntityFrameWork.Core.Repositories.Dependencies;

using System.Security.Cryptography;

namespace Air.Cloud.EntityFrameWork.Core.Repositories;

/// <summary>
/// <para>zh-cn:默认主库主从仓储</para>
/// <para>en-us:Provides the corresponding database repository operation.</para>
/// </summary>
[IgnoreScanning]
public partial class MSRepository : MSRepository<MasterDbContextLocator>, IMSRepository
{
    /// <summary>
    /// <para>zh-cn:构造函数</para>
    /// <para>en-us:Provides the corresponding database repository operation.</para>
    /// </summary>
    /// <param name="serviceProvider"><para>zh-cn:服务提供器。</para><para>en-us:The service provider.</para></param>
    /// <param name="repository"><para>zh-cn:非泛型仓储</para><para>en-us:The repository value.</para></param>
    public MSRepository(IServiceProvider serviceProvider
        , IRepository repository) : base(serviceProvider, repository)
    {
    }
}

/// <summary>
/// <para>zh-cn:主从库仓储</para>
/// <para>en-us:Provides the corresponding database repository operation.</para>
/// </summary>
/// <typeparam name="TMasterDbContextLocator"><para>zh-cn:主库</para><para>en-us:The master database context locator type.</para></typeparam>
[IgnoreScanning]
public partial class MSRepository<TMasterDbContextLocator> : IMSRepository<TMasterDbContextLocator>
    where TMasterDbContextLocator : class, IDbContextLocator
{
    /// <summary>
    /// <para>zh-cn:服务提供器</para>
    /// <para>en-us:Provides the corresponding database repository operation.</para>
    /// </summary>
    private readonly IServiceProvider _serviceProvider;

    /// <summary>
    /// <para>zh-cn:非泛型仓储</para>
    /// <para>en-us:Provides the corresponding database repository operation.</para>
    /// </summary>
    private readonly IRepository _repository;

    /// <summary>
    /// <para>zh-cn:构造函数</para>
    /// <para>en-us:Provides the corresponding database repository operation.</para>
    /// </summary>
    /// <param name="serviceProvider"><para>zh-cn:服务提供器。</para><para>en-us:The service provider.</para></param>
    /// <param name="repository"><para>zh-cn:非泛型仓储</para><para>en-us:The repository value.</para></param>
    public MSRepository(IServiceProvider serviceProvider
        , IRepository repository)
    {
        _serviceProvider = serviceProvider;
        _repository = repository;
    }

    /// <summary>
    /// <para>zh-cn:获取主库仓储</para>
    /// <para>en-us:Provides the corresponding database repository operation.</para>
    /// </summary>
    /// <typeparam name="TEntity"><para>zh-cn:实体类型</para><para>en-us:The entity type.</para></typeparam>
    /// <returns><para>zh-cn:操作结果。</para><para>en-us:The operation result.</para></returns>
    public virtual IRepository<TEntity, TMasterDbContextLocator> Master<TEntity>()
        where TEntity : class, IPrivateEntity, new()
    {
        return _repository.Change<TEntity, TMasterDbContextLocator>();
    }

    /// <summary>
    /// <para>zh-cn:动态获取从库（随机）</para>
    /// <para>en-us:Provides the corresponding database repository operation.</para>
    /// </summary>
    /// <typeparam name="TEntity"><para>zh-cn:实体类型</para><para>en-us:The entity type.</para></typeparam>
    /// <returns><para>zh-cn:操作结果。</para><para>en-us:The operation result.</para></returns>
    public virtual IPrivateReadableRepository<TEntity> Slave<TEntity>()
        where TEntity : class, IPrivateEntity, new()
    {
        // 判断数据库主库是否注册
        Penetrates.CheckDbContextLocator(typeof(TMasterDbContextLocator), out var dbContextType);

        // 获取主库贴的特性
        var appDbContextAttribute = DbProvider.GetAppDbContextAttribute(dbContextType)
            ?? throw new InvalidOperationException($"DbContext {dbContextType.FullName} 未配置 {nameof(AppDbContextAttribute)}。");

        // 获取从库列表
        var slaveDbContextLocators = appDbContextAttribute.SlaveDbContextLocators;

        // 如果没有定义从库定位器，则抛出异常
        if (slaveDbContextLocators == null || slaveDbContextLocators.Length == 0) throw new InvalidOperationException("Not found slave locators.");

        // 如果只配置了一个从库，直接返回
        if (slaveDbContextLocators.Length == 1) return Slave<TEntity>(() => slaveDbContextLocators[0]);

        // 获取随机从库索引
        var index = RandomNumberGenerator.GetInt32(0, slaveDbContextLocators.Length);

        // 返回随机从库
        return Slave<TEntity>(() => slaveDbContextLocators[index]);
    }

    /// <summary>
    /// <para>zh-cn:动态获取从库（自定义）</para>
    /// <para>en-us:Provides the corresponding database repository operation.</para>
    /// </summary>
    /// <typeparam name="TEntity"><para>zh-cn:实体类型</para><para>en-us:The entity type.</para></typeparam>
    /// <returns><para>zh-cn:操作结果。</para><para>en-us:The operation result.</para></returns>
    public virtual IPrivateReadableRepository<TEntity> Slave<TEntity>(Func<Type> locatorHandler)
        where TEntity : class, IPrivateEntity, new()
    {
        if (locatorHandler == null) throw new ArgumentNullException(nameof(locatorHandler));

        // 获取定位器类型
        var dbContextLocatorType = locatorHandler();
        if (!typeof(IDbContextLocator).IsAssignableFrom(dbContextLocatorType)) throw new InvalidCastException($"{dbContextLocatorType.Name} is not assignable from {nameof(IDbContextLocator)}.");

        // 判断从库定位器是否绑定
        Penetrates.CheckDbContextLocator(dbContextLocatorType, out _);

        // 解析从库定位器
        var repository = _serviceProvider.GetService(typeof(IRepository<,>).MakeGenericType(typeof(TEntity), dbContextLocatorType)) as IPrivateRepository<TEntity>;
        if (repository == null)
        {
            throw new InvalidOperationException($"Repository for entity {typeof(TEntity).FullName} and locator {dbContextLocatorType.FullName} was not registered.");
        }

        // 返回从库仓储
        return repository.Constraint<IPrivateReadableRepository<TEntity>>();
    }
}

/// <summary>
/// <para>zh-cn:主从库仓储</para>
/// <para>en-us:Provides the corresponding database repository operation.</para>
/// </summary>
/// <typeparam name="TMasterDbContextLocator"><para>zh-cn:主库</para><para>en-us:The master database context locator type.</para></typeparam>
/// <typeparam name="TSlaveDbContextLocator1"><para>zh-cn:从库</para><para>en-us:The first slave database context locator type.</para></typeparam>
[IgnoreScanning]
public partial class MSRepository<TMasterDbContextLocator, TSlaveDbContextLocator1> : IMSRepository<TMasterDbContextLocator, TSlaveDbContextLocator1>
    where TMasterDbContextLocator : class, IDbContextLocator
    where TSlaveDbContextLocator1 : class, IDbContextLocator
{
    /// <summary>
    /// <para>zh-cn:非泛型仓储</para>
    /// <para>en-us:Provides the corresponding database repository operation.</para>
    /// </summary>
    private readonly IRepository _repository;

    /// <summary>
    /// <para>zh-cn:构造函数</para>
    /// <para>en-us:Provides the corresponding database repository operation.</para>
    /// </summary>
    /// <param name="repository"><para>zh-cn:非泛型仓储</para><para>en-us:The repository value.</para></param>
    public MSRepository(IRepository repository)
    {
        _repository = repository;
    }

    /// <summary>
    /// <para>zh-cn:获取主库仓储</para>
    /// <para>en-us:Provides the corresponding database repository operation.</para>
    /// </summary>
    /// <typeparam name="TEntity"><para>zh-cn:实体类型</para><para>en-us:The entity type.</para></typeparam>
    /// <returns><para>zh-cn:操作结果。</para><para>en-us:The operation result.</para></returns>
    public virtual IRepository<TEntity, TMasterDbContextLocator> Master<TEntity>()
        where TEntity : class, IPrivateEntity, new()
    {
        return _repository.Change<TEntity, TMasterDbContextLocator>();
    }

    /// <summary>
    /// <para>zh-cn:获取从库仓储</para>
    /// <para>en-us:Provides the corresponding database repository operation.</para>
    /// </summary>
    /// <typeparam name="TEntity"><para>zh-cn:实体类型</para><para>en-us:The entity type.</para></typeparam>
    /// <returns><para>zh-cn:操作结果。</para><para>en-us:The operation result.</para></returns>
    public virtual IReadableRepository<TEntity, TSlaveDbContextLocator1> Slave1<TEntity>()
        where TEntity : class, IPrivateEntity, new()
    {
        return _repository.Change<TEntity, TSlaveDbContextLocator1>().Constraint<IReadableRepository<TEntity, TSlaveDbContextLocator1>>();
    }
}

/// <summary>
/// <para>zh-cn:主从库仓储</para>
/// <para>en-us:Provides the corresponding database repository operation.</para>
/// </summary>
/// <typeparam name="TMasterDbContextLocator"><para>zh-cn:主库</para><para>en-us:The master database context locator type.</para></typeparam>
/// <typeparam name="TSlaveDbContextLocator1"><para>zh-cn:从库</para><para>en-us:The first slave database context locator type.</para></typeparam>
/// <typeparam name="TSlaveDbContextLocator2"><para>zh-cn:从库</para><para>en-us:The second slave database context locator type.</para></typeparam>
[IgnoreScanning]
public partial class MSRepository<TMasterDbContextLocator, TSlaveDbContextLocator1, TSlaveDbContextLocator2>
    : MSRepository<TMasterDbContextLocator, TSlaveDbContextLocator1>
    , IMSRepository<TMasterDbContextLocator, TSlaveDbContextLocator1, TSlaveDbContextLocator2>
    where TMasterDbContextLocator : class, IDbContextLocator
    where TSlaveDbContextLocator1 : class, IDbContextLocator
    where TSlaveDbContextLocator2 : class, IDbContextLocator
{
    /// <summary>
    /// <para>zh-cn:非泛型仓储</para>
    /// <para>en-us:Provides the corresponding database repository operation.</para>
    /// </summary>
    private readonly IRepository _repository;

    /// <summary>
    /// <para>zh-cn:构造函数</para>
    /// <para>en-us:Provides the corresponding database repository operation.</para>
    /// </summary>
    /// <param name="repository"><para>zh-cn:非泛型仓储</para><para>en-us:The repository value.</para></param>
    public MSRepository(IRepository repository) : base(repository)
    {
        _repository = repository;
    }

    /// <summary>
    /// <para>zh-cn:获取从库仓储2</para>
    /// <para>en-us:Provides the corresponding database repository operation.</para>
    /// </summary>
    /// <typeparam name="TEntity"><para>zh-cn:实体类型</para><para>en-us:The entity type.</para></typeparam>
    /// <returns><para>zh-cn:操作结果。</para><para>en-us:The operation result.</para></returns>
    public virtual IReadableRepository<TEntity, TSlaveDbContextLocator2> Slave2<TEntity>()
        where TEntity : class, IPrivateEntity, new()
    {
        return _repository.Change<TEntity, TSlaveDbContextLocator2>().Constraint<IReadableRepository<TEntity, TSlaveDbContextLocator2>>();
    }
}

/// <summary>
/// <para>zh-cn:主从库仓储</para>
/// <para>en-us:Provides the corresponding database repository operation.</para>
/// </summary>
/// <typeparam name="TMasterDbContextLocator"><para>zh-cn:主库</para><para>en-us:The master database context locator type.</para></typeparam>
/// <typeparam name="TSlaveDbContextLocator1"><para>zh-cn:从库</para><para>en-us:The first slave database context locator type.</para></typeparam>
/// <typeparam name="TSlaveDbContextLocator2"><para>zh-cn:从库</para><para>en-us:The second slave database context locator type.</para></typeparam>
/// <typeparam name="TSlaveDbContextLocator3"><para>zh-cn:从库</para><para>en-us:The third slave database context locator type.</para></typeparam>
[IgnoreScanning]
public partial class MSRepository<TMasterDbContextLocator, TSlaveDbContextLocator1, TSlaveDbContextLocator2, TSlaveDbContextLocator3>
    : MSRepository<TMasterDbContextLocator, TSlaveDbContextLocator1, TSlaveDbContextLocator2>
    , IMSRepository<TMasterDbContextLocator, TSlaveDbContextLocator1, TSlaveDbContextLocator2, TSlaveDbContextLocator3>
    where TMasterDbContextLocator : class, IDbContextLocator
    where TSlaveDbContextLocator1 : class, IDbContextLocator
    where TSlaveDbContextLocator2 : class, IDbContextLocator
    where TSlaveDbContextLocator3 : class, IDbContextLocator
{
    /// <summary>
    /// <para>zh-cn:非泛型仓储</para>
    /// <para>en-us:Provides the corresponding database repository operation.</para>
    /// </summary>
    private readonly IRepository _repository;

    /// <summary>
    /// <para>zh-cn:构造函数</para>
    /// <para>en-us:Provides the corresponding database repository operation.</para>
    /// </summary>
    /// <param name="repository"><para>zh-cn:非泛型仓储</para><para>en-us:The repository value.</para></param>
    public MSRepository(IRepository repository) : base(repository)
    {
        _repository = repository;
    }

    /// <summary>
    /// <para>zh-cn:获取从库仓储3</para>
    /// <para>en-us:Provides the corresponding database repository operation.</para>
    /// </summary>
    /// <typeparam name="TEntity"><para>zh-cn:实体类型</para><para>en-us:The entity type.</para></typeparam>
    /// <returns><para>zh-cn:操作结果。</para><para>en-us:The operation result.</para></returns>
    public virtual IReadableRepository<TEntity, TSlaveDbContextLocator3> Slave3<TEntity>()
        where TEntity : class, IPrivateEntity, new()
    {
        return _repository.Change<TEntity, TSlaveDbContextLocator3>().Constraint<IReadableRepository<TEntity, TSlaveDbContextLocator3>>();
    }
}

/// <summary>
/// <para>zh-cn:主从库仓储</para>
/// <para>en-us:Provides the corresponding database repository operation.</para>
/// </summary>
/// <typeparam name="TMasterDbContextLocator"><para>zh-cn:主库</para><para>en-us:The master database context locator type.</para></typeparam>
/// <typeparam name="TSlaveDbContextLocator1"><para>zh-cn:从库</para><para>en-us:The first slave database context locator type.</para></typeparam>
/// <typeparam name="TSlaveDbContextLocator2"><para>zh-cn:从库</para><para>en-us:The second slave database context locator type.</para></typeparam>
/// <typeparam name="TSlaveDbContextLocator3"><para>zh-cn:从库</para><para>en-us:The third slave database context locator type.</para></typeparam>
/// <typeparam name="TSlaveDbContextLocator4"><para>zh-cn:从库</para><para>en-us:The fourth slave database context locator type.</para></typeparam>
[IgnoreScanning]
public partial class MSRepository<TMasterDbContextLocator, TSlaveDbContextLocator1, TSlaveDbContextLocator2, TSlaveDbContextLocator3, TSlaveDbContextLocator4>
    : MSRepository<TMasterDbContextLocator, TSlaveDbContextLocator1, TSlaveDbContextLocator2, TSlaveDbContextLocator3>
    , IMSRepository<TMasterDbContextLocator, TSlaveDbContextLocator1, TSlaveDbContextLocator2, TSlaveDbContextLocator3, TSlaveDbContextLocator4>
    where TMasterDbContextLocator : class, IDbContextLocator
    where TSlaveDbContextLocator1 : class, IDbContextLocator
    where TSlaveDbContextLocator2 : class, IDbContextLocator
    where TSlaveDbContextLocator3 : class, IDbContextLocator
    where TSlaveDbContextLocator4 : class, IDbContextLocator
{
    /// <summary>
    /// <para>zh-cn:非泛型仓储</para>
    /// <para>en-us:Provides the corresponding database repository operation.</para>
    /// </summary>
    private readonly IRepository _repository;

    /// <summary>
    /// <para>zh-cn:构造函数</para>
    /// <para>en-us:Provides the corresponding database repository operation.</para>
    /// </summary>
    /// <param name="repository"><para>zh-cn:非泛型仓储</para><para>en-us:The repository value.</para></param>
    public MSRepository(IRepository repository) : base(repository)
    {
        _repository = repository;
    }

    /// <summary>
    /// <para>zh-cn:获取从库仓储4</para>
    /// <para>en-us:Provides the corresponding database repository operation.</para>
    /// </summary>
    /// <typeparam name="TEntity"><para>zh-cn:实体类型</para><para>en-us:The entity type.</para></typeparam>
    /// <returns><para>zh-cn:操作结果。</para><para>en-us:The operation result.</para></returns>
    public virtual IReadableRepository<TEntity, TSlaveDbContextLocator4> Slave4<TEntity>()
        where TEntity : class, IPrivateEntity, new()
    {
        return _repository.Change<TEntity, TSlaveDbContextLocator4>().Constraint<IReadableRepository<TEntity, TSlaveDbContextLocator4>>();
    }
}

/// <summary>
/// <para>zh-cn:主从库仓储</para>
/// <para>en-us:Provides the corresponding database repository operation.</para>
/// </summary>
/// <typeparam name="TMasterDbContextLocator"><para>zh-cn:主库</para><para>en-us:The master database context locator type.</para></typeparam>
/// <typeparam name="TSlaveDbContextLocator1"><para>zh-cn:从库</para><para>en-us:The first slave database context locator type.</para></typeparam>
/// <typeparam name="TSlaveDbContextLocator2"><para>zh-cn:从库</para><para>en-us:The second slave database context locator type.</para></typeparam>
/// <typeparam name="TSlaveDbContextLocator3"><para>zh-cn:从库</para><para>en-us:The third slave database context locator type.</para></typeparam>
/// <typeparam name="TSlaveDbContextLocator4"><para>zh-cn:从库</para><para>en-us:The fourth slave database context locator type.</para></typeparam>
/// <typeparam name="TSlaveDbContextLocator5"><para>zh-cn:从库</para><para>en-us:The fifth slave database context locator type.</para></typeparam>
[IgnoreScanning]
public partial class MSRepository<TMasterDbContextLocator, TSlaveDbContextLocator1, TSlaveDbContextLocator2, TSlaveDbContextLocator3, TSlaveDbContextLocator4, TSlaveDbContextLocator5>
    : MSRepository<TMasterDbContextLocator, TSlaveDbContextLocator1, TSlaveDbContextLocator2, TSlaveDbContextLocator3, TSlaveDbContextLocator4>
    , IMSRepository<TMasterDbContextLocator, TSlaveDbContextLocator1, TSlaveDbContextLocator2, TSlaveDbContextLocator3, TSlaveDbContextLocator4, TSlaveDbContextLocator5>
    where TMasterDbContextLocator : class, IDbContextLocator
    where TSlaveDbContextLocator1 : class, IDbContextLocator
    where TSlaveDbContextLocator2 : class, IDbContextLocator
    where TSlaveDbContextLocator3 : class, IDbContextLocator
    where TSlaveDbContextLocator4 : class, IDbContextLocator
    where TSlaveDbContextLocator5 : class, IDbContextLocator
{
    /// <summary>
    /// <para>zh-cn:非泛型仓储</para>
    /// <para>en-us:Provides the corresponding database repository operation.</para>
    /// </summary>
    private readonly IRepository _repository;

    /// <summary>
    /// <para>zh-cn:构造函数</para>
    /// <para>en-us:Provides the corresponding database repository operation.</para>
    /// </summary>
    /// <param name="repository"><para>zh-cn:非泛型仓储</para><para>en-us:The repository value.</para></param>
    public MSRepository(IRepository repository) : base(repository)
    {
        _repository = repository;
    }

    /// <summary>
    /// <para>zh-cn:获取从库仓储5</para>
    /// <para>en-us:Provides the corresponding database repository operation.</para>
    /// </summary>
    /// <typeparam name="TEntity"><para>zh-cn:实体类型</para><para>en-us:The entity type.</para></typeparam>
    /// <returns><para>zh-cn:操作结果。</para><para>en-us:The operation result.</para></returns>
    public virtual IReadableRepository<TEntity, TSlaveDbContextLocator5> Slave5<TEntity>()
        where TEntity : class, IPrivateEntity, new()
    {
        return _repository.Change<TEntity, TSlaveDbContextLocator5>().Constraint<IReadableRepository<TEntity, TSlaveDbContextLocator5>>();
    }
}

/// <summary>
/// <para>zh-cn:主从库仓储</para>
/// <para>en-us:Provides the corresponding database repository operation.</para>
/// </summary>
/// <typeparam name="TMasterDbContextLocator"><para>zh-cn:主库</para><para>en-us:The master database context locator type.</para></typeparam>
/// <typeparam name="TSlaveDbContextLocator1"><para>zh-cn:从库</para><para>en-us:The first slave database context locator type.</para></typeparam>
/// <typeparam name="TSlaveDbContextLocator2"><para>zh-cn:从库</para><para>en-us:The second slave database context locator type.</para></typeparam>
/// <typeparam name="TSlaveDbContextLocator3"><para>zh-cn:从库</para><para>en-us:The third slave database context locator type.</para></typeparam>
/// <typeparam name="TSlaveDbContextLocator4"><para>zh-cn:从库</para><para>en-us:The fourth slave database context locator type.</para></typeparam>
/// <typeparam name="TSlaveDbContextLocator5"><para>zh-cn:从库</para><para>en-us:The fifth slave database context locator type.</para></typeparam>
/// <typeparam name="TSlaveDbContextLocator6"><para>zh-cn:从库</para><para>en-us:The sixth slave database context locator type.</para></typeparam>
[IgnoreScanning]
public partial class MSRepository<TMasterDbContextLocator, TSlaveDbContextLocator1, TSlaveDbContextLocator2, TSlaveDbContextLocator3, TSlaveDbContextLocator4, TSlaveDbContextLocator5, TSlaveDbContextLocator6>
    : MSRepository<TMasterDbContextLocator, TSlaveDbContextLocator1, TSlaveDbContextLocator2, TSlaveDbContextLocator3, TSlaveDbContextLocator4, TSlaveDbContextLocator5>
    , IMSRepository<TMasterDbContextLocator, TSlaveDbContextLocator1, TSlaveDbContextLocator2, TSlaveDbContextLocator3, TSlaveDbContextLocator4, TSlaveDbContextLocator5, TSlaveDbContextLocator6>
    where TMasterDbContextLocator : class, IDbContextLocator
    where TSlaveDbContextLocator1 : class, IDbContextLocator
    where TSlaveDbContextLocator2 : class, IDbContextLocator
    where TSlaveDbContextLocator3 : class, IDbContextLocator
    where TSlaveDbContextLocator4 : class, IDbContextLocator
    where TSlaveDbContextLocator5 : class, IDbContextLocator
    where TSlaveDbContextLocator6 : class, IDbContextLocator
{
    /// <summary>
    /// <para>zh-cn:非泛型仓储</para>
    /// <para>en-us:Provides the corresponding database repository operation.</para>
    /// </summary>
    private readonly IRepository _repository;

    /// <summary>
    /// <para>zh-cn:构造函数</para>
    /// <para>en-us:Provides the corresponding database repository operation.</para>
    /// </summary>
    /// <param name="repository"><para>zh-cn:非泛型仓储</para><para>en-us:The repository value.</para></param>
    public MSRepository(IRepository repository) : base(repository)
    {
        _repository = repository;
    }

    /// <summary>
    /// <para>zh-cn:获取从库仓储6</para>
    /// <para>en-us:Provides the corresponding database repository operation.</para>
    /// </summary>
    /// <typeparam name="TEntity"><para>zh-cn:实体类型</para><para>en-us:The entity type.</para></typeparam>
    /// <returns><para>zh-cn:操作结果。</para><para>en-us:The operation result.</para></returns>
    public virtual IReadableRepository<TEntity, TSlaveDbContextLocator6> Slave6<TEntity>()
        where TEntity : class, IPrivateEntity, new()
    {
        return _repository.Change<TEntity, TSlaveDbContextLocator6>().Constraint<IReadableRepository<TEntity, TSlaveDbContextLocator6>>();
    }
}

/// <summary>
/// <para>zh-cn:主从库仓储</para>
/// <para>en-us:Provides the corresponding database repository operation.</para>
/// </summary>
/// <typeparam name="TMasterDbContextLocator"><para>zh-cn:主库</para><para>en-us:The master database context locator type.</para></typeparam>
/// <typeparam name="TSlaveDbContextLocator1"><para>zh-cn:从库</para><para>en-us:The first slave database context locator type.</para></typeparam>
/// <typeparam name="TSlaveDbContextLocator2"><para>zh-cn:从库</para><para>en-us:The second slave database context locator type.</para></typeparam>
/// <typeparam name="TSlaveDbContextLocator3"><para>zh-cn:从库</para><para>en-us:The third slave database context locator type.</para></typeparam>
/// <typeparam name="TSlaveDbContextLocator4"><para>zh-cn:从库</para><para>en-us:The fourth slave database context locator type.</para></typeparam>
/// <typeparam name="TSlaveDbContextLocator5"><para>zh-cn:从库</para><para>en-us:The fifth slave database context locator type.</para></typeparam>
/// <typeparam name="TSlaveDbContextLocator6"><para>zh-cn:从库</para><para>en-us:The sixth slave database context locator type.</para></typeparam>
/// <typeparam name="TSlaveDbContextLocator7"><para>zh-cn:从库</para><para>en-us:The seventh slave database context locator type.</para></typeparam>
[IgnoreScanning]
public partial class MSRepository<TMasterDbContextLocator, TSlaveDbContextLocator1, TSlaveDbContextLocator2, TSlaveDbContextLocator3, TSlaveDbContextLocator4, TSlaveDbContextLocator5, TSlaveDbContextLocator6, TSlaveDbContextLocator7>
    : MSRepository<TMasterDbContextLocator, TSlaveDbContextLocator1, TSlaveDbContextLocator2, TSlaveDbContextLocator3, TSlaveDbContextLocator4, TSlaveDbContextLocator5, TSlaveDbContextLocator6>
    , IMSRepository<TMasterDbContextLocator, TSlaveDbContextLocator1, TSlaveDbContextLocator2, TSlaveDbContextLocator3, TSlaveDbContextLocator4, TSlaveDbContextLocator5, TSlaveDbContextLocator6, TSlaveDbContextLocator7>
    where TMasterDbContextLocator : class, IDbContextLocator
    where TSlaveDbContextLocator1 : class, IDbContextLocator
    where TSlaveDbContextLocator2 : class, IDbContextLocator
    where TSlaveDbContextLocator3 : class, IDbContextLocator
    where TSlaveDbContextLocator4 : class, IDbContextLocator
    where TSlaveDbContextLocator5 : class, IDbContextLocator
    where TSlaveDbContextLocator6 : class, IDbContextLocator
    where TSlaveDbContextLocator7 : class, IDbContextLocator
{
    /// <summary>
    /// <para>zh-cn:非泛型仓储</para>
    /// <para>en-us:Provides the corresponding database repository operation.</para>
    /// </summary>
    private readonly IRepository _repository;

    /// <summary>
    /// <para>zh-cn:构造函数</para>
    /// <para>en-us:Provides the corresponding database repository operation.</para>
    /// </summary>
    /// <param name="repository"><para>zh-cn:非泛型仓储</para><para>en-us:The repository value.</para></param>
    public MSRepository(IRepository repository) : base(repository)
    {
        _repository = repository;
    }

    /// <summary>
    /// <para>zh-cn:获取从库仓储7</para>
    /// <para>en-us:Provides the corresponding database repository operation.</para>
    /// </summary>
    /// <typeparam name="TEntity"><para>zh-cn:实体类型</para><para>en-us:The entity type.</para></typeparam>
    /// <returns><para>zh-cn:操作结果。</para><para>en-us:The operation result.</para></returns>
    public virtual IReadableRepository<TEntity, TSlaveDbContextLocator7> Slave7<TEntity>()
        where TEntity : class, IPrivateEntity, new()
    {
        return _repository.Change<TEntity, TSlaveDbContextLocator7>().Constraint<IReadableRepository<TEntity, TSlaveDbContextLocator7>>();
    }
}
