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

using Air.Cloud.EntityFrameWork.Core.Contexts.Enums;

namespace Air.Cloud.EntityFrameWork.Core.Contexts.Attributes;

/// <summary>
/// 数据库上下文配置特性
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public class AppDbContextAttribute : Attribute
{
    /// <summary>
    /// 默认构造函数
    /// </summary>
    /// <param name="slaveDbContextLocators"></param>
    public AppDbContextAttribute(params Type[] slaveDbContextLocators)
    {
        SlaveDbContextLocators = slaveDbContextLocators;
    }

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="connectionMetadata">支持数据库连接字符串，配置文件的 ConnectionStrings 中的Key或配置文件的完整的配置路径，如果是内存数据库，则为数据库名称</param>
    /// <param name="slaveDbContextLocators"></param>
    public AppDbContextAttribute(string connectionMetadata, params Type[] slaveDbContextLocators)
    {
        ConnectionMetadata = connectionMetadata;
        SlaveDbContextLocators = slaveDbContextLocators;
    }

    /// <summary>
    /// 数据库连接元数据
    /// </summary>
    /// <remarks>支持数据库连接字符串，配置文件的 ConnectionStrings 中的Key或配置文件的完整的配置路径，如果是内存数据库，则为数据库名称</remarks>
    public string ConnectionMetadata { get; set; }

    /// <summary>
    /// 数据库上下文模式
    /// </summary>
    public DbContextMode Mode { get; set; } = DbContextMode.Cached;

    /// <summary>
    /// 表统一前缀
    /// </summary>
    /// <remarks>前缀不能包含 . 和特殊符号，可使用下划线或短杆线</remarks>
    public string TablePrefix { get; set; }

    /// <summary>
    /// 表统一后缀
    /// </summary>
    /// <remarks>后缀不能包含 . 和特殊符号，可使用下划线或短杆线</remarks>
    public string TableSuffix { get; set; }

    /// <summary>
    /// 指定从库定位器
    /// </summary>
    public Type[] SlaveDbContextLocators { get; set; }
}