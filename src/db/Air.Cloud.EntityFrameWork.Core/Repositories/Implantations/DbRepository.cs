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

using Microsoft.Extensions.DependencyInjection;

namespace Air.Cloud.EntityFrameWork.Core.Repositories.Implantations;

/// <summary>
/// 多数据库仓储
/// </summary>
/// <typeparam name="TDbContextLocator"></typeparam>
public partial class DbRepository<TDbContextLocator> : IDbRepository<TDbContextLocator>
    where TDbContextLocator : class, IDbContextLocator
{
    /// <summary>
    /// 服务提供器
    /// </summary>
    private readonly IServiceProvider _serviceProvider;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="serviceProvider">服务提供器</param>
    public DbRepository(
        IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    /// <summary>
    /// 切换实体
    /// </summary>
    /// <typeparam name="TEntity"></typeparam>
    /// <returns></returns>
    public virtual IRepository<TEntity, TDbContextLocator> Change<TEntity>()
         where TEntity : class, IPrivateEntity, new()
    {
        return _serviceProvider.GetService<IRepository<TEntity, TDbContextLocator>>();
    }

    /// <summary>
    /// 获取 Sql 操作仓储
    /// </summary>
    /// <returns></returns>
    public virtual ISqlRepository<TDbContextLocator> Sql()
    {
        return _serviceProvider.GetService<ISqlRepository<TDbContextLocator>>();
    }

    /// <summary>
    /// 解析服务
    /// </summary>
    /// <typeparam name="TService"></typeparam>
    /// <returns></returns>
    public virtual TService GetService<TService>()
    {
        return _serviceProvider.GetService<TService>();
    }

    /// <summary>
    /// 解析服务
    /// </summary>
    /// <typeparam name="TService"></typeparam>
    /// <returns></returns>
    public virtual TService GetRequiredService<TService>()
    {
        return _serviceProvider.GetRequiredService<TService>();
    }
}