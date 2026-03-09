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

namespace Air.Cloud.EntityFrameWork.Core.Repositories.Implantations;

/// <summary>
/// Sql 操作仓储实现
/// </summary>
[IgnoreScanning]
public partial class SqlRepository : SqlRepository<MasterDbContextLocator>, ISqlRepository
{
    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="serviceProvider">服务提供器</param>
    public SqlRepository(IServiceProvider serviceProvider) : base(serviceProvider)
    {
    }
}

/// <summary>
/// Sql 操作仓储实现
/// </summary>
[IgnoreScanning]
public partial class SqlRepository<TDbContextLocator> : PrivateSqlRepository, ISqlRepository<TDbContextLocator>
    where TDbContextLocator : class, IDbContextLocator
{
    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="serviceProvider">服务提供器</param>
    public SqlRepository(IServiceProvider serviceProvider) : base(typeof(TDbContextLocator), serviceProvider)
    {
    }
}
