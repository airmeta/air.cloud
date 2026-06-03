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

namespace Air.Cloud.EntityFrameWork.Core.Internal;

/// <summary>
/// 构建 Sql 字符串执行部件
/// </summary>
[IgnoreScanning]
public sealed partial class SqlExecutePart
{
    /// <summary>
    /// 静态缺省 Sql 部件
    /// </summary>
    public static SqlExecutePart Default => new();

    /// <summary>
    /// Sql 字符串
    /// </summary>
    public string SqlString { get; private set; } = string.Empty;

    /// <summary>
    /// 设置超时时间
    /// </summary>
    public int Timeout { get; private set; }

    /// <summary>
    /// 数据库上下文定位器
    /// </summary>
    public Type DbContextLocator { get; private set; } = typeof(MasterDbContextLocator);

    /// <summary>
    /// 设置服务提供器
    /// </summary>
    public IServiceProvider? ContextScoped { get; private set; } = AppCore.HttpContext?.RequestServices;
}
