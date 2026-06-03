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

namespace Air.Cloud.EntityFrameWork.Core.Internal;

/// <summary>
/// 实体执行部件
/// </summary>
/// <typeparam name="TEntity"></typeparam>
[IgnoreScanning]
public sealed partial class EntityExecutePart<TEntity>
    where TEntity : class, IPrivateEntity, new()
{
    /// <summary>
    /// 静态缺省 Entity 部件
    /// </summary>
    public static EntityExecutePart<TEntity> Default => new();

    /// <summary>
    /// 实体
    /// </summary>
    public TEntity Entity { get; private set; } = new();

    /// <summary>
    /// 数据库上下文定位器
    /// </summary>
    public Type DbContextLocator { get; private set; } = typeof(MasterDbContextLocator);

    /// <summary>
    /// 数据库上下文执行作用域
    /// </summary>
    public IServiceProvider? ContextScoped { get; private set; } = AppCore.HttpContext?.RequestServices;
}
